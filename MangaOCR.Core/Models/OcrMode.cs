namespace MangaOCR.Models;

/// <summary>
/// OCR 執行模式
/// </summary>
public enum OcrMode
{
    /// <summary>
    /// 自適應模式（預設）：自動分析圖像質量並推薦最佳參數
    /// 優點：智能、自動優化、速度快
    /// </summary>
    Adaptive,

    /// <summary>
    /// 標準模式：使用固定參數
    /// 優點：可預測、可控制
    /// </summary>
    Standard,

    /// <summary>
    /// 手動模式：完全自訂參數
    /// 優點：完全控制
    /// </summary>
    Manual
}
