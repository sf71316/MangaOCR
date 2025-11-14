namespace MangaOCR.Models;

/// <summary>
/// 圖像質量評估指標
/// </summary>
public class ImageQualityMetrics
{
    /// <summary>
    /// 模糊度分數（Laplacian variance）
    /// 值越高 = 越清晰，值越低 = 越模糊
    /// 典型範圍：清晰圖片 > 500，模糊圖片 < 100
    /// </summary>
    public double BlurScore { get; set; }

    /// <summary>
    /// 對比度（標準差）
    /// 值越高 = 對比度越高
    /// 典型範圍：50-100
    /// </summary>
    public double Contrast { get; set; }

    /// <summary>
    /// 平均亮度
    /// 範圍：0-255
    /// </summary>
    public double Brightness { get; set; }

    /// <summary>
    /// 圖片解析度（寬 x 高）
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 圖片解析度（寬 x 高）
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 最大邊長
    /// </summary>
    public int MaxDimension => Math.Max(Width, Height);

    /// <summary>
    /// 總像素數
    /// </summary>
    public long TotalPixels => (long)Width * Height;

    /// <summary>
    /// 圖片質量等級
    /// </summary>
    public ImageQualityLevel QualityLevel { get; set; }

    /// <summary>
    /// 質量評估摘要
    /// </summary>
    public string Summary =>
        $"解析度: {Width}x{Height}, 模糊度: {BlurScore:F1}, 對比度: {Contrast:F1}, 質量: {QualityLevel}";
}

/// <summary>
/// 圖片質量等級
/// </summary>
public enum ImageQualityLevel
{
    /// <summary>
    /// 高品質：清晰、高對比度
    /// </summary>
    High,

    /// <summary>
    /// 中等品質：稍微模糊或對比度中等
    /// </summary>
    Medium,

    /// <summary>
    /// 低品質：模糊、低對比度或解析度不足
    /// </summary>
    Low
}
