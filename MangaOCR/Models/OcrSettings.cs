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

    /// <summary>
    /// 文字區域擴展比例（預設1.5）
    /// - 較小值(1.2-1.3)：適合清晰的漫畫字體
    /// - 較大值(1.8-2.0)：適合小而密集的文字
    /// </summary>
    public float UnclipRatio { get; set; } = 1.5f;

    /// <summary>
    /// 偵測時的最大圖片尺寸（預設960）
    /// - 提高值(1280-1920)：提升準確度但降低速度
    /// - 降低值(640-800)：提升速度但降低準確度
    /// </summary>
    public int MaxSize { get; set; } = 1024;

    /// <summary>
    /// 邊界框信心度閾值（預設0.6）
    /// - 提高值(0.7-0.8)：減少誤判但可能遺漏文字
    /// - 降低值(0.5)：檢測更多文字但可能增加誤判
    /// </summary>
    public float BoxScoreThreshold { get; set; } = 0.6f;

    /// <summary>
    /// 二值化閾值（預設0.3）
    /// - 提高值(0.4)：減少雜訊
    /// - 降低值(0.2)：檢測更多文字
    /// </summary>
    public float Threshold { get; set; } = 0.3f;
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
