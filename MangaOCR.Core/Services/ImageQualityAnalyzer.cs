using MangaOCR.Models;
using OpenCvSharp;

namespace MangaOCR.Services;

/// <summary>
/// 圖像質量分析器，用於評估圖片的模糊度、對比度等指標
/// （內部實現）
/// </summary>
internal class ImageQualityAnalyzer
{
    /// <summary>
    /// 分析圖片質量
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <returns>圖像質量指標</returns>
    public ImageQualityMetrics AnalyzeImage(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentNullException(nameof(imagePath));

        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"圖片不存在: {imagePath}");

        using var image = Cv2.ImRead(imagePath, ImreadModes.Color);

        if (image.Empty())
            throw new InvalidOperationException($"無法載入圖片: {imagePath}");

        var metrics = new ImageQualityMetrics
        {
            Width = image.Width,
            Height = image.Height,
            BlurScore = CalculateBlurScore(image),
            Contrast = CalculateContrast(image),
            Brightness = CalculateBrightness(image)
        };

        // 根據指標判定質量等級
        metrics.QualityLevel = DetermineQualityLevel(metrics);

        return metrics;
    }

    /// <summary>
    /// 計算模糊度分數（Laplacian variance）
    /// 原理：Laplacian 算子對邊緣敏感，清晰圖片邊緣多，模糊圖片邊緣少
    /// </summary>
    private double CalculateBlurScore(Mat image)
    {
        // 轉換為灰度圖
        using var gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

        // 使用 Laplacian 算子
        using var laplacian = new Mat();
        Cv2.Laplacian(gray, laplacian, MatType.CV_64F);

        // 計算 Laplacian 的變異數（variance）
        Cv2.MeanStdDev(laplacian, out var mean, out var stddev);

        // variance = stddev^2
        double variance = stddev.Val0 * stddev.Val0;

        return variance;
    }

    /// <summary>
    /// 計算對比度（標準差）
    /// 原理：標準差大 = 像素值分布廣 = 對比度高
    /// </summary>
    private double CalculateContrast(Mat image)
    {
        // 轉換為灰度圖
        using var gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

        // 計算標準差
        Cv2.MeanStdDev(gray, out var mean, out var stddev);

        return stddev.Val0;
    }

    /// <summary>
    /// 計算平均亮度
    /// </summary>
    private double CalculateBrightness(Mat image)
    {
        // 轉換為灰度圖
        using var gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

        // 計算平均值
        var mean = Cv2.Mean(gray);

        return mean.Val0;
    }

    /// <summary>
    /// 根據指標判定質量等級
    /// </summary>
    private ImageQualityLevel DetermineQualityLevel(ImageQualityMetrics metrics)
    {
        // 質量判定規則（根據實際測試調整）

        // 1. 模糊度判定
        bool isSharp = metrics.BlurScore >= 300;  // 清晰
        bool isModerate = metrics.BlurScore >= 100; // 中等

        // 2. 對比度判定
        bool isHighContrast = metrics.Contrast >= 50;
        bool isMediumContrast = metrics.Contrast >= 30;

        // 3. 解析度判定
        bool isHighResolution = metrics.MaxDimension >= 1500;
        bool isMediumResolution = metrics.MaxDimension >= 800;

        // 綜合判定
        if (isSharp && isHighContrast)
        {
            return ImageQualityLevel.High;
        }
        else if (isModerate && isMediumContrast && isMediumResolution)
        {
            return ImageQualityLevel.Medium;
        }
        else
        {
            return ImageQualityLevel.Low;
        }
    }

    /// <summary>
    /// 分析圖片質量（非同步版本）
    /// </summary>
    public Task<ImageQualityMetrics> AnalyzeImageAsync(string imagePath)
    {
        return Task.Run(() => AnalyzeImage(imagePath));
    }
}
