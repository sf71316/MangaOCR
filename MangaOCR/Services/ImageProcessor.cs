using MangaOCR.Interfaces;
using OpenCvSharp;

namespace MangaOCR.Services;

/// <summary>
/// 影像處理器實作
/// 提供各種影像預處理功能以提升OCR準確度
/// </summary>
public class ImageProcessor : IImageProcessor
{
    /// <summary>
    /// 預處理影像以提升OCR準確度
    /// 組合多種處理技術，針對漫畫圖片優化
    /// </summary>
    public Mat PreprocessForOcr(Mat source)
    {
        if (source == null || source.Empty())
        {
            throw new ArgumentException("輸入影像不可為空", nameof(source));
        }

        Mat processed = source.Clone();

        try
        {
            // 1. 轉換為灰階（如果不是灰階）
            if (processed.Channels() > 1)
            {
                Cv2.CvtColor(processed, processed, ColorConversionCodes.BGR2GRAY);
            }

            // 2. 降噪處理（保留文字細節）
            processed = Denoise(processed);

            // 3. 對比度增強（CLAHE - 限制對比度自適應直方圖均衡化）
            processed = EnhanceContrast(processed);

            // 4. 銳化處理（增強文字邊緣）
            processed = Sharpen(processed);

            // 5. 自適應二值化（處理光照不均的情況）
            processed = Binarize(processed);

            return processed;
        }
        catch
        {
            processed?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 二值化處理 - 使用自適應閾值
    /// 對於光照不均勻的漫畫圖片效果更好
    /// </summary>
    public Mat Binarize(Mat source)
    {
        if (source == null || source.Empty())
        {
            throw new ArgumentException("輸入影像不可為空", nameof(source));
        }

        var binary = new Mat();

        // 確保是灰階圖
        var gray = source.Channels() == 1 ? source : new Mat();
        if (source.Channels() > 1)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
        }

        // 使用自適應閾值 - 高斯加權
        // blockSize: 鄰域大小（必須是奇數）
        // C: 從平均值中減去的常數
        Cv2.AdaptiveThreshold(
            gray,
            binary,
            255,
            AdaptiveThresholdTypes.GaussianC,
            ThresholdTypes.Binary,
            blockSize: 15,  // 較大的區塊以處理大字
            c: 10           // 調整敏感度
        );

        if (source.Channels() > 1)
        {
            gray.Dispose();
        }

        return binary;
    }

    /// <summary>
    /// 去噪處理 - 使用形態學操作和雙邊濾波
    /// 保留邊緣細節同時減少噪點
    /// </summary>
    public Mat Denoise(Mat source)
    {
        if (source == null || source.Empty())
        {
            throw new ArgumentException("輸入影像不可為空", nameof(source));
        }

        var denoised = new Mat();

        // 使用雙邊濾波 - 能保持邊緣清晰同時平滑噪點
        // d: 濾波器直徑
        // sigmaColor: 顏色空間標準差
        // sigmaSpace: 座標空間標準差
        Cv2.BilateralFilter(
            source,
            denoised,
            d: 5,
            sigmaColor: 75,
            sigmaSpace: 75
        );

        return denoised;
    }

    /// <summary>
    /// 增強對比度 - 使用CLAHE
    /// 限制對比度自適應直方圖均衡化，避免過度增強
    /// </summary>
    public Mat EnhanceContrast(Mat source)
    {
        if (source == null || source.Empty())
        {
            throw new ArgumentException("輸入影像不可為空", nameof(source));
        }

        var enhanced = new Mat();

        // 確保是灰階圖
        var gray = source.Channels() == 1 ? source : new Mat();
        if (source.Channels() > 1)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
        }

        // 創建CLAHE對象
        using var clahe = Cv2.CreateCLAHE(
            clipLimit: 2.0,      // 限制對比度放大的上限
            tileGridSize: new Size(8, 8)  // 劃分網格大小
        );

        clahe.Apply(gray, enhanced);

        if (source.Channels() > 1)
        {
            gray.Dispose();
        }

        return enhanced;
    }

    /// <summary>
    /// 銳化處理 - 增強文字邊緣
    /// 使用Unsharp Masking技術
    /// </summary>
    public Mat Sharpen(Mat source)
    {
        if (source == null || source.Empty())
        {
            throw new ArgumentException("輸入影像不可為空", nameof(source));
        }

        var blurred = new Mat();
        var sharpened = new Mat();

        try
        {
            // 高斯模糊
            Cv2.GaussianBlur(source, blurred, new Size(0, 0), sigmaX: 3);

            // Unsharp mask: sharpened = original + amount * (original - blurred)
            Cv2.AddWeighted(
                source, 1.5,      // 原圖權重
                blurred, -0.5,    // 模糊圖負權重
                0,                // 偏移量
                sharpened
            );

            return sharpened;
        }
        finally
        {
            blurred?.Dispose();
        }
    }
}
