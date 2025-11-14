namespace MangaOCR.Models;

/// <summary>
/// OCR 日誌等級
/// </summary>
public enum OcrLogLevel
{
    /// <summary>追蹤（最詳細）</summary>
    Trace,
    /// <summary>除錯</summary>
    Debug,
    /// <summary>資訊</summary>
    Information,
    /// <summary>警告</summary>
    Warning,
    /// <summary>錯誤</summary>
    Error
}

/// <summary>
/// OCR 日誌事件參數
/// </summary>
public class OcrLogEventArgs : EventArgs
{
    /// <summary>
    /// 日誌等級
    /// </summary>
    public OcrLogLevel Level { get; set; }

    /// <summary>
    /// 日誌訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 時間戳記
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 額外資料（可選）
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// OCR 進度事件參數
/// </summary>
public class OcrProgressEventArgs : EventArgs
{
    /// <summary>
    /// 當前進度（已處理項目數）
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    /// 總項目數
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 當前處理的圖片路徑
    /// </summary>
    public string? CurrentImagePath { get; set; }

    /// <summary>
    /// 進度百分比 (0.0 - 1.0)
    /// </summary>
    public double Percentage => Total > 0 ? (double)Current / Total : 0;

    /// <summary>
    /// 訊息
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// 批次處理選項
/// </summary>
public class BatchProcessingOptions
{
    /// <summary>
    /// 最大平行處理線程數（預設為 CPU 核心數 / 2）
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// 是否啟用智能排程（根據檔案大小優化處理順序）
    /// </summary>
    public bool EnableSmartScheduling { get; set; } = true;

    /// <summary>
    /// 大檔案閾值（位元組），超過此大小視為大檔案（預設 1MB）
    /// </summary>
    public long LargeFileSizeThreshold { get; set; } = 1_000_000;

    /// <summary>
    /// 取消權杖
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = default;

    /// <summary>
    /// 取得預設選項
    /// </summary>
    public static BatchProcessingOptions Default => new();

    /// <summary>
    /// 取得實際使用的平行處理線程數
    /// </summary>
    public int GetActualMaxDegreeOfParallelism()
    {
        if (MaxDegreeOfParallelism.HasValue && MaxDegreeOfParallelism.Value > 0)
        {
            return MaxDegreeOfParallelism.Value;
        }

        // 預設為 CPU 核心數 / 2，最少 1
        return Math.Max(1, Environment.ProcessorCount / 2);
    }
}
