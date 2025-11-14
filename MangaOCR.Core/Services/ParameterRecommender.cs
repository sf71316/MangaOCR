using MangaOCR.Models;

namespace MangaOCR.Services;

/// <summary>
/// OCR 參數推薦器，根據圖像質量自動推薦最佳參數
/// （內部實現）
/// </summary>
internal class ParameterRecommender
{
    private readonly ImageQualityAnalyzer _qualityAnalyzer;

    public ParameterRecommender()
    {
        _qualityAnalyzer = new ImageQualityAnalyzer();
    }

    /// <summary>
    /// 根據圖像路徑推薦 OCR 參數
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <param name="baseSettings">基礎設定（可選，用於繼承其他參數如語言等）</param>
    /// <returns>推薦的 OCR 設定</returns>
    public (OcrSettings RecommendedSettings, ImageQualityMetrics QualityMetrics) RecommendParameters(
        string imagePath,
        OcrSettings? baseSettings = null)
    {
        // 分析圖像質量
        var metrics = _qualityAnalyzer.AnalyzeImage(imagePath);

        // 根據質量推薦參數
        var recommendedSettings = CreateRecommendedSettings(metrics, baseSettings);

        return (recommendedSettings, metrics);
    }

    /// <summary>
    /// 根據圖像質量指標創建推薦設定
    /// </summary>
    private OcrSettings CreateRecommendedSettings(ImageQualityMetrics metrics, OcrSettings? baseSettings)
    {
        // 從基礎設定複製，或創建新設定
        var settings = baseSettings != null
            ? CloneSettings(baseSettings)
            : new OcrSettings();

        // 根據質量等級調整參數
        switch (metrics.QualityLevel)
        {
            case ImageQualityLevel.High:
                ApplyHighQualityParameters(settings, metrics);
                break;

            case ImageQualityLevel.Medium:
                ApplyMediumQualityParameters(settings, metrics);
                break;

            case ImageQualityLevel.Low:
                ApplyLowQualityParameters(settings, metrics);
                break;
        }

        return settings;
    }

    /// <summary>
    /// 高品質圖片參數（清晰、高對比度）
    /// 策略：使用標準參數，追求速度
    /// </summary>
    private void ApplyHighQualityParameters(OcrSettings settings, ImageQualityMetrics metrics)
    {
        // 高品質圖片使用標準參數即可
        settings.MaxSize = 1024;
        settings.UnclipRatio = 1.5f;
        settings.BoxScoreThreshold = 0.6f;
        settings.Threshold = 0.3f;

        // 如果解析度非常高，適度提升 MaxSize
        if (metrics.MaxDimension >= 2000)
        {
            settings.MaxSize = 1280;
        }
    }

    /// <summary>
    /// 中等品質圖片參數（稍微模糊或對比度中等）
    /// 策略：提升檢測能力
    /// </summary>
    private void ApplyMediumQualityParameters(OcrSettings settings, ImageQualityMetrics metrics)
    {
        // 提高解析度以補償模糊
        settings.MaxSize = 1280;

        // 提高擴展比例
        settings.UnclipRatio = 1.8f;

        // 降低閾值以檢測更多區域
        settings.BoxScoreThreshold = 0.55f;
        settings.Threshold = 0.3f;

        // 如果模糊度很低，進一步調整
        if (metrics.BlurScore < 150)
        {
            settings.UnclipRatio = 2.0f;
            settings.BoxScoreThreshold = 0.5f;
        }
    }

    /// <summary>
    /// 低品質圖片參數（模糊、低對比度）
    /// 策略：最激進的參數，盡可能提取文字
    /// </summary>
    private void ApplyLowQualityParameters(OcrSettings settings, ImageQualityMetrics metrics)
    {
        // 最高解析度
        settings.MaxSize = 1920;

        // 最大擴展比例
        settings.UnclipRatio = 2.0f;

        // 最低閾值
        settings.BoxScoreThreshold = 0.5f;
        settings.Threshold = 0.25f;

        // 如果對比度也很低，考慮啟用預處理
        if (metrics.Contrast < 30)
        {
            settings.UsePreprocessing = true;
        }
    }

    /// <summary>
    /// 複製 OcrSettings
    /// </summary>
    private OcrSettings CloneSettings(OcrSettings source)
    {
        return new OcrSettings
        {
            Provider = source.Provider,
            Language = source.Language,
            ModelPath = source.ModelPath,
            MinConfidence = source.MinConfidence,
            UsePreprocessing = source.UsePreprocessing,
            AllowRotateDetection = source.AllowRotateDetection,
            Enable180Classification = source.Enable180Classification,
            UnclipRatio = source.UnclipRatio,
            MaxSize = source.MaxSize,
            BoxScoreThreshold = source.BoxScoreThreshold,
            Threshold = source.Threshold
        };
    }

    /// <summary>
    /// 獲取參數推薦說明
    /// </summary>
    public string GetRecommendationExplanation(ImageQualityMetrics metrics, OcrSettings settings)
    {
        var explanation = $@"【圖像質量分析】
解析度: {metrics.Width}x{metrics.Height} ({metrics.MaxDimension}px)
模糊度分數: {metrics.BlurScore:F1} {GetBlurDescription(metrics.BlurScore)}
對比度: {metrics.Contrast:F1} {GetContrastDescription(metrics.Contrast)}
亮度: {metrics.Brightness:F1}
質量等級: {metrics.QualityLevel}

【推薦參數】
MaxSize: {settings.MaxSize}
UnclipRatio: {settings.UnclipRatio}
BoxScoreThreshold: {settings.BoxScoreThreshold}
Threshold: {settings.Threshold}
UsePreprocessing: {settings.UsePreprocessing}

【推薦理由】
{GetReasoningExplanation(metrics)}";

        return explanation;
    }

    private string GetBlurDescription(double blurScore)
    {
        if (blurScore >= 300) return "(清晰)";
        if (blurScore >= 100) return "(中等)";
        return "(模糊)";
    }

    private string GetContrastDescription(double contrast)
    {
        if (contrast >= 50) return "(高對比度)";
        if (contrast >= 30) return "(中等對比度)";
        return "(低對比度)";
    }

    private string GetReasoningExplanation(ImageQualityMetrics metrics)
    {
        return metrics.QualityLevel switch
        {
            ImageQualityLevel.High =>
                "圖片質量良好，使用標準參數即可達到最佳效果，同時保持處理速度。",

            ImageQualityLevel.Medium =>
                "圖片質量中等，提高 MaxSize 和 UnclipRatio 以補償模糊或對比度不足。",

            ImageQualityLevel.Low =>
                "圖片質量較低，使用最激進參數以盡可能提取文字。建議考慮對原圖進行預處理。",

            _ => "未知質量等級"
        };
    }
}
