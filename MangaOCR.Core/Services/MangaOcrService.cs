using MangaOCR.Factories;
using MangaOCR.Interfaces;
using MangaOCR.Models;

namespace MangaOCR.Services;

/// <summary>
/// MangaOCR 主服務類（推薦使用）
/// 提供便捷的工廠方法來創建 OCR 服務
/// </summary>
public class MangaOcrService : IDisposable
{
    private readonly OcrMode _mode;
    private readonly OcrSettings _settings;
    private IOcrService? _standardService;
    private AdaptiveOcrService? _adaptiveService;

    private MangaOcrService(OcrMode mode, OcrSettings? settings = null)
    {
        _mode = mode;
        _settings = settings ?? new OcrSettings();
    }

    /// <summary>
    /// 創建 OCR 服務（預設使用自適應模式）
    /// </summary>
    /// <param name="settings">OCR 設定（可選）</param>
    /// <returns>MangaOCR 服務實例</returns>
    public static MangaOcrService Create(OcrSettings? settings = null)
    {
        return CreateAdaptive(settings);
    }

    /// <summary>
    /// 創建自適應 OCR 服務（推薦）
    /// 自動分析圖像質量並選擇最佳參數
    /// </summary>
    /// <param name="settings">基礎 OCR 設定（可選）</param>
    /// <returns>MangaOCR 服務實例</returns>
    public static MangaOcrService CreateAdaptive(OcrSettings? settings = null)
    {
        return new MangaOcrService(OcrMode.Adaptive, settings);
    }

    /// <summary>
    /// 創建標準 OCR 服務
    /// 使用固定參數
    /// </summary>
    /// <param name="settings">OCR 設定（可選）</param>
    /// <returns>MangaOCR 服務實例</returns>
    public static MangaOcrService CreateStandard(OcrSettings? settings = null)
    {
        return new MangaOcrService(OcrMode.Standard, settings);
    }

    /// <summary>
    /// 使用預設設定創建服務（自適應模式 + 日文 + 標準參數）
    /// </summary>
    /// <returns>MangaOCR 服務實例</returns>
    public static MangaOcrService CreateDefault()
    {
        var settings = new OcrSettings
        {
            Provider = OcrProvider.PaddleOCR,
            Language = "Japanese",
            MinConfidence = 0.5f,
            UsePreprocessing = false,
            AllowRotateDetection = true,
            Enable180Classification = true,
            UnclipRatio = 1.5f,
            MaxSize = 1024,
            BoxScoreThreshold = 0.6f,
            Threshold = 0.3f
        };

        return CreateAdaptive(settings);
    }

    /// <summary>
    /// 識別圖片中的文字
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <param name="verbose">是否顯示詳細診斷資訊（僅自適應模式有效）</param>
    /// <returns>OCR 識別結果</returns>
    public OcrResult RecognizeText(string imagePath, bool verbose = false)
    {
        return _mode switch
        {
            OcrMode.Adaptive => RecognizeWithAdaptiveMode(imagePath, verbose),
            OcrMode.Standard => RecognizeWithStandardMode(imagePath),
            _ => throw new NotSupportedException($"不支援的 OCR 模式: {_mode}")
        };
    }

    /// <summary>
    /// 識別圖片中的文字（非同步）
    /// </summary>
    public async Task<OcrResult> RecognizeTextAsync(
        string imagePath,
        bool verbose = false,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => RecognizeText(imagePath, verbose), cancellationToken);
    }

    /// <summary>
    /// 獲取當前使用的 OCR 模式
    /// </summary>
    public OcrMode Mode => _mode;

    /// <summary>
    /// 獲取推薦參數說明（僅自適應模式有效）
    /// </summary>
    public string GetRecommendationExplanation(string imagePath)
    {
        if (_mode != OcrMode.Adaptive)
        {
            return "此方法僅在自適應模式下可用";
        }

        _adaptiveService ??= new AdaptiveOcrService(_settings);
        return _adaptiveService.GetRecommendationExplanation(imagePath);
    }

    private OcrResult RecognizeWithAdaptiveMode(string imagePath, bool verbose)
    {
        _adaptiveService ??= new AdaptiveOcrService(_settings);
        return _adaptiveService.RecognizeTextWithAutoParams(imagePath, verbose);
    }

    private OcrResult RecognizeWithStandardMode(string imagePath)
    {
        if (_standardService == null)
        {
            var factory = new OcrServiceFactory();
            _standardService = factory.CreateOcrService(_settings);
        }

        return _standardService.RecognizeText(imagePath);
    }

    public void Dispose()
    {
        _standardService?.Dispose();
        _adaptiveService?.Dispose();
    }
}
