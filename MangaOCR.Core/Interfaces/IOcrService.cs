using MangaOCR.Models;

namespace MangaOCR.Interfaces;

/// <summary>
/// OCR服務介面
/// </summary>
public interface IOcrService : IDisposable
{
    /// <summary>
    /// 從檔案路徑進行OCR識別
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <returns>OCR結果</returns>
    OcrResult RecognizeText(string imagePath);

    /// <summary>
    /// 從檔案路徑進行OCR識別（非同步）
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>OCR結果</returns>
    Task<OcrResult> RecognizeTextAsync(string imagePath, CancellationToken cancellationToken = default);
}
