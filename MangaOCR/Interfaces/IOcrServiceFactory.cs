using MangaOCR.Models;

namespace MangaOCR.Interfaces;

/// <summary>
/// OCR服務工廠介面
/// </summary>
public interface IOcrServiceFactory
{
    /// <summary>
    /// 根據配置創建OCR服務
    /// </summary>
    /// <param name="settings">OCR配置</param>
    /// <returns>OCR服務實例</returns>
    IOcrService CreateOcrService(OcrSettings settings);
}
