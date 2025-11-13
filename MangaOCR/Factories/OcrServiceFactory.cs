using MangaOCR.Interfaces;
using MangaOCR.Models;
using MangaOCR.Services;

namespace MangaOCR.Factories;

/// <summary>
/// OCR服務工廠實作
/// </summary>
public class OcrServiceFactory : IOcrServiceFactory
{
    /// <summary>
    /// 根據配置創建對應的OCR服務
    /// </summary>
    public IOcrService CreateOcrService(OcrSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.Provider switch
        {
            OcrProvider.PaddleOCR => new PaddleOcrService(settings),

            OcrProvider.Tesseract => throw new NotImplementedException(
                "Tesseract OCR尚未實作，請使用PaddleOCR"),

            OcrProvider.Azure => throw new NotImplementedException(
                "Azure Computer Vision尚未實作，請使用PaddleOCR"),

            OcrProvider.WindowsOCR => throw new NotImplementedException(
                "Windows OCR尚未實作，請使用PaddleOCR"),

            _ => throw new NotSupportedException(
                $"不支援的OCR引擎: {settings.Provider}")
        };
    }
}
