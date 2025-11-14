using MangaOCR.Models;

namespace MangaOCR.Interfaces;

/// <summary>
/// OCR服務介面
/// </summary>
public interface IOcrService : IDisposable
{
    /// <summary>
    /// 日誌事件（讓使用者自行決定如何收集和處理日誌）
    /// </summary>
    event EventHandler<OcrLogEventArgs>? LogMessage;

    /// <summary>
    /// 進度事件（批次處理時回報進度）
    /// </summary>
    event EventHandler<OcrProgressEventArgs>? ProgressChanged;
    /// <summary>
    /// 從檔案路徑進行完整 OCR 識別（檢測 + 識別）
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <returns>OCR結果</returns>
    OcrResult RecognizeText(string imagePath);

    /// <summary>
    /// 從檔案路徑進行完整 OCR 識別（非同步）
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>OCR結果</returns>
    Task<OcrResult> RecognizeTextAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 只檢測文字區域座標，不識別文字內容（快速模式）
    /// 適用場景：用戶互動選取、預處理工作流
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <returns>檢測到的文字區域列表（只包含座標，Text 為空）</returns>
    List<TextRegion> DetectTextRegions(string imagePath);

    /// <summary>
    /// 只檢測文字區域座標（非同步）
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>檢測到的文字區域列表</returns>
    Task<List<TextRegion>> DetectTextRegionsAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 只識別單一文字區域（跳過檢測階段，極速模式）
    /// 適用場景：用戶已截取好的文字圖片、點選特定文字框
    /// 假設整張圖片就是一個文字區域
    /// </summary>
    /// <param name="imagePath">已截取的文字圖片路徑</param>
    /// <returns>識別結果（只包含一個文字區域）</returns>
    OcrResult RecognizeTextOnly(string imagePath);

    /// <summary>
    /// 只識別單一文字區域（非同步）
    /// </summary>
    /// <param name="imagePath">已截取的文字圖片路徑</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>識別結果</returns>
    Task<OcrResult> RecognizeTextOnlyAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批次識別多個已截取的文字圖片（適合大量處理）
    /// 每個圖片都跳過檢測階段，直接識別
    /// </summary>
    /// <param name="imagePaths">已截取的文字圖片路徑列表</param>
    /// <returns>識別結果列表</returns>
    List<OcrResult> RecognizeTextBatch(List<string> imagePaths);

    /// <summary>
    /// 批次識別多個已截取的文字圖片（非同步）
    /// </summary>
    /// <param name="imagePaths">已截取的文字圖片路徑列表</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>識別結果列表</returns>
    Task<List<OcrResult>> RecognizeTextBatchAsync(List<string> imagePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批次識別多個已截取的文字圖片（進階版，支援平行處理和智能排程）
    /// </summary>
    /// <param name="imagePaths">已截取的文字圖片路徑列表</param>
    /// <param name="options">批次處理選項（可指定最大線程數、智能排程等）</param>
    /// <returns>識別結果列表</returns>
    List<OcrResult> RecognizeTextBatchParallel(List<string> imagePaths, BatchProcessingOptions? options = null);

    /// <summary>
    /// 批次識別多個已截取的文字圖片（進階版，非同步）
    /// </summary>
    /// <param name="imagePaths">已截取的文字圖片路徑列表</param>
    /// <param name="options">批次處理選項</param>
    /// <returns>識別結果列表</returns>
    Task<List<OcrResult>> RecognizeTextBatchParallelAsync(List<string> imagePaths, BatchProcessingOptions? options = null);
}
