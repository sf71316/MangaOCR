using MangaOCR.Factories;
using MangaOCR.Interfaces;
using MangaOCR.Models;

namespace MangaOCR.Services;

/// <summary>
/// 自適應 OCR 服務，自動根據圖像質量調整參數
/// （內部實現，請使用 MangaOcrService 工廠方法）
/// </summary>
internal class AdaptiveOcrService : IDisposable
{
    private readonly ParameterRecommender _recommender;
    private readonly OcrServiceFactory _factory;
    private readonly OcrSettings _baseSettings;
    private IOcrService? _currentOcrService;

    public AdaptiveOcrService(OcrSettings? baseSettings = null)
    {
        _recommender = new ParameterRecommender();
        _factory = new OcrServiceFactory();
        _baseSettings = baseSettings ?? new OcrSettings();
    }

    /// <summary>
    /// 自動識別文字（自動分析圖像質量並推薦參數）
    /// </summary>
    /// <param name="imagePath">圖片路徑</param>
    /// <param name="verbose">是否輸出詳細診斷資訊</param>
    /// <returns>OCR 識別結果</returns>
    public OcrResult RecognizeTextWithAutoParams(string imagePath, bool verbose = false)
    {
        // 1. 分析圖像質量並推薦參數
        var (recommendedSettings, qualityMetrics) = _recommender.RecommendParameters(imagePath, _baseSettings);

        // 2. 輸出診斷資訊（如果需要）
        if (verbose)
        {
            var explanation = _recommender.GetRecommendationExplanation(qualityMetrics, recommendedSettings);
            Console.WriteLine(explanation);
            Console.WriteLine();
        }

        // 3. 使用推薦參數創建 OCR 服務
        _currentOcrService?.Dispose();
        _currentOcrService = _factory.CreateOcrService(recommendedSettings);

        // 4. 執行 OCR
        var result = _currentOcrService.RecognizeText(imagePath);

        // 5. 在結果中附加質量資訊（可選）
        if (verbose && result.Success)
        {
            Console.WriteLine($"✓ OCR 完成：耗時 {result.ElapsedMilliseconds}ms，識別 {result.TextRegions.Count} 個區域");
        }

        return result;
    }

    /// <summary>
    /// 自動識別文字（非同步版本）
    /// </summary>
    public async Task<OcrResult> RecognizeTextWithAutoParamsAsync(
        string imagePath,
        bool verbose = false,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => RecognizeTextWithAutoParams(imagePath, verbose), cancellationToken);
    }

    /// <summary>
    /// 僅分析圖像質量並推薦參數（不執行 OCR）
    /// </summary>
    public (OcrSettings RecommendedSettings, ImageQualityMetrics QualityMetrics) AnalyzeAndRecommend(
        string imagePath)
    {
        return _recommender.RecommendParameters(imagePath, _baseSettings);
    }

    /// <summary>
    /// 獲取推薦參數的詳細說明
    /// </summary>
    public string GetRecommendationExplanation(string imagePath)
    {
        var (settings, metrics) = _recommender.RecommendParameters(imagePath, _baseSettings);
        return _recommender.GetRecommendationExplanation(metrics, settings);
    }

    public void Dispose()
    {
        _currentOcrService?.Dispose();
    }
}
