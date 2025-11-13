namespace MangaOCR.Models;

/// <summary>
/// OCR服務配置
/// </summary>
public class OcrSettings
{
    /// <summary>
    /// OCR引擎提供者
    /// </summary>
    public OcrProvider Provider { get; set; } = OcrProvider.PaddleOCR;

    /// <summary>
    /// 識別語言
    /// </summary>
    public string Language { get; set; } = "Japanese";

    /// <summary>
    /// 自訂模型路徑（可選，null則使用線上模型）
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// 最低信心度閾值
    /// </summary>
    public float MinConfidence { get; set; } = 0.5f;

    /// <summary>
    /// 是否啟用影像預處理
    /// </summary>
    public bool UsePreprocessing { get; set; } = false;

    /// <summary>
    /// 是否允許旋轉偵測
    /// </summary>
    public bool AllowRotateDetection { get; set; } = true;

    /// <summary>
    /// 是否啟用180度分類
    /// </summary>
    public bool Enable180Classification { get; set; } = true;
}

/// <summary>
/// OCR引擎提供者枚舉
/// </summary>
public enum OcrProvider
{
    /// <summary>PaddleOCR（預設）</summary>
    PaddleOCR,

    /// <summary>Tesseract OCR</summary>
    Tesseract,

    /// <summary>Azure Computer Vision</summary>
    Azure,

    /// <summary>Windows OCR</summary>
    WindowsOCR
}
