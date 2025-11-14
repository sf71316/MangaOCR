using MangaOCR.Models;
using MangaOCR.Services;
using Xunit;

namespace MangaOCR.Tests.Services;

/// <summary>
/// 自適應 OCR 服務測試
/// </summary>
public class AdaptiveOcrServiceTests : IDisposable
{
    private readonly string _testImagePath4;
    private readonly string _testImagePath5;

    public AdaptiveOcrServiceTests()
    {
        _testImagePath4 = FindTestImage("4.png");
        _testImagePath5 = FindTestImage("5.jpg");
    }

    private string FindTestImage(string fileName)
    {
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestData", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "TestData", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "TestData", fileName),
        };

        foreach (var path in possiblePaths)
        {
            var normalizedPath = Path.GetFullPath(path);
            if (File.Exists(normalizedPath))
            {
                return normalizedPath;
            }
        }

        throw new FileNotFoundException($"找不到測試圖片: {fileName}");
    }

    [Fact]
    public void ImageQualityAnalyzer_WithValidImage_ShouldAnalyzeQuality()
    {
        // Arrange
        var analyzer = new ImageQualityAnalyzer();

        // Act
        var metrics = analyzer.AnalyzeImage(_testImagePath4);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.Width > 0);
        Assert.True(metrics.Height > 0);
        Assert.True(metrics.BlurScore >= 0);
        Assert.True(metrics.Contrast >= 0);
        Assert.True(metrics.Brightness >= 0);

        // 輸出診斷資訊
        Console.WriteLine($"圖片: 4.png");
        Console.WriteLine(metrics.Summary);
    }

    [Fact]
    public void ParameterRecommender_WithHighQualityImage_ShouldRecommendStandardParams()
    {
        // Arrange
        var recommender = new ParameterRecommender();

        // Act
        var (settings, metrics) = recommender.RecommendParameters(_testImagePath4);

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(metrics);

        // 輸出診斷資訊
        Console.WriteLine($"=== 圖片: 4.png ===");
        Console.WriteLine(recommender.GetRecommendationExplanation(metrics, settings));
    }

    [Fact]
    public void ParameterRecommender_WithLowerQualityImage_ShouldRecommendOptimizedParams()
    {
        // Arrange
        var recommender = new ParameterRecommender();

        // Act
        var (settings, metrics) = recommender.RecommendParameters(_testImagePath5);

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(metrics);

        // 輸出診斷資訊
        Console.WriteLine($"=== 圖片: 5.jpg ===");
        Console.WriteLine(recommender.GetRecommendationExplanation(metrics, settings));
    }

    [Fact]
    public void AdaptiveOcrService_WithAutoParams_ShouldRecognizeText()
    {
        // Arrange
        using var adaptiveService = MangaOcrService.CreateAdaptive();

        // Act
        var result = adaptiveService.RecognizeText(_testImagePath4, verbose: true);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.TextRegions);
        Assert.True(result.ElapsedMilliseconds > 0);
    }

    [Fact]
    public void AdaptiveOcrService_CompareWithManualParams_ForImage4()
    {
        // Arrange
        var baseSettings = new OcrSettings
        {
            Language = "Japanese",
            MinConfidence = 0.5f
        };

        using var adaptiveService = MangaOcrService.CreateAdaptive(baseSettings);

        Console.WriteLine("=== 測試圖片: 4.png（自適應參數）===");
        Console.WriteLine();

        // Act - 自適應參數
        var result = adaptiveService.RecognizeText(_testImagePath4, verbose: true);

        // Assert
        Assert.True(result.Success);

        var validRegions = result.TextRegions.Where(r => !float.IsNaN(r.Confidence)).ToList();
        if (validRegions.Any())
        {
            var avgConfidence = validRegions.Average(r => r.Confidence);
            var highConfidenceCount = validRegions.Count(r => r.Confidence >= 0.7f);

            Console.WriteLine();
            Console.WriteLine("【識別結果】");
            Console.WriteLine($"  識別區域數: {result.TextRegions.Count}");
            Console.WriteLine($"  平均信心度: {avgConfidence:P1}");
            Console.WriteLine($"  高信心度區域: {highConfidenceCount}");
        }
    }

    [Fact]
    public void AdaptiveOcrService_CompareWithManualParams_ForImage5()
    {
        // Arrange
        var baseSettings = new OcrSettings
        {
            Language = "Japanese",
            MinConfidence = 0.5f
        };

        using var adaptiveService = MangaOcrService.CreateAdaptive(baseSettings);

        Console.WriteLine("=== 測試圖片: 5.jpg（自適應參數）===");
        Console.WriteLine();

        // Act - 自適應參數
        var result = adaptiveService.RecognizeText(_testImagePath5, verbose: true);

        // Assert
        Assert.True(result.Success);

        var validRegions = result.TextRegions.Where(r => !float.IsNaN(r.Confidence)).ToList();
        if (validRegions.Any())
        {
            var avgConfidence = validRegions.Average(r => r.Confidence);
            var highConfidenceCount = validRegions.Count(r => r.Confidence >= 0.7f);

            Console.WriteLine();
            Console.WriteLine("【識別結果】");
            Console.WriteLine($"  識別區域數: {result.TextRegions.Count}");
            Console.WriteLine($"  平均信心度: {avgConfidence:P1}");
            Console.WriteLine($"  高信心度區域: {highConfidenceCount}");
        }
    }

    [Fact]
    public void AdaptiveOcrService_AnalyzeOnly_ShouldReturnRecommendation()
    {
        // Arrange
        using var adaptiveService = MangaOcrService.CreateAdaptive();

        // Act
        var explanation = adaptiveService.GetRecommendationExplanation(_testImagePath4);

        // Assert
        Assert.NotEmpty(explanation);
        Assert.Contains("圖像質量分析", explanation);

        Console.WriteLine(explanation);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
