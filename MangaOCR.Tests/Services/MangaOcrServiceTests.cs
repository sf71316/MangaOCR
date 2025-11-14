using MangaOCR.Models;
using MangaOCR.Services;
using Xunit;

namespace MangaOCR.Tests.Services;

/// <summary>
/// MangaOcrService 工廠模式測試
/// </summary>
public class MangaOcrServiceTests : IDisposable
{
    private readonly string _testImagePath;

    public MangaOcrServiceTests()
    {
        _testImagePath = FindTestImage("4.png");
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
    public void CreateDefault_ShouldReturnAdaptiveService()
    {
        // Act
        using var service = MangaOcrService.CreateDefault();

        // Assert
        Assert.NotNull(service);
        Assert.Equal(OcrMode.Adaptive, service.Mode);

        // Verify it can recognize text
        var result = service.RecognizeText(_testImagePath);
        Assert.True(result.Success);
        Assert.NotEmpty(result.TextRegions);
    }

    [Fact]
    public void Create_ShouldDefaultToAdaptiveMode()
    {
        // Act
        using var service = MangaOcrService.Create();

        // Assert
        Assert.NotNull(service);
        Assert.Equal(OcrMode.Adaptive, service.Mode);

        // Verify it can recognize text
        var result = service.RecognizeText(_testImagePath);
        Assert.True(result.Success);
        Assert.NotEmpty(result.TextRegions);
    }

    [Fact]
    public void CreateAdaptive_WithCustomSettings_ShouldUseAdaptiveMode()
    {
        // Arrange
        var settings = new OcrSettings
        {
            Language = "Japanese",
            MinConfidence = 0.5f,
            UsePreprocessing = false
        };

        // Act
        using var service = MangaOcrService.CreateAdaptive(settings);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(OcrMode.Adaptive, service.Mode);

        // Verify it can recognize text with verbose output
        var result = service.RecognizeText(_testImagePath, verbose: false);
        Assert.True(result.Success);
        Assert.NotEmpty(result.TextRegions);
    }

    [Fact]
    public void CreateStandard_WithFixedParams_ShouldUseStandardMode()
    {
        // Arrange
        var settings = new OcrSettings
        {
            Language = "Japanese",
            MinConfidence = 0.5f,
            MaxSize = 1024,
            UnclipRatio = 1.5f,
            BoxScoreThreshold = 0.6f
        };

        // Act
        using var service = MangaOcrService.CreateStandard(settings);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(OcrMode.Standard, service.Mode);

        // Verify it can recognize text
        var result = service.RecognizeText(_testImagePath);
        Assert.True(result.Success);
        Assert.NotEmpty(result.TextRegions);
    }

    [Fact]
    public void GetRecommendationExplanation_InAdaptiveMode_ShouldReturnExplanation()
    {
        // Arrange
        using var service = MangaOcrService.CreateAdaptive();

        // Act
        var explanation = service.GetRecommendationExplanation(_testImagePath);

        // Assert
        Assert.NotNull(explanation);
        Assert.NotEmpty(explanation);
        Assert.Contains("圖像質量分析", explanation);
    }

    [Fact]
    public void GetRecommendationExplanation_InStandardMode_ShouldReturnMessage()
    {
        // Arrange
        using var service = MangaOcrService.CreateStandard();

        // Act
        var explanation = service.GetRecommendationExplanation(_testImagePath);

        // Assert
        Assert.NotNull(explanation);
        Assert.Contains("僅在自適應模式下可用", explanation);
    }

    [Fact]
    public async Task RecognizeTextAsync_ShouldWorkCorrectly()
    {
        // Arrange
        using var service = MangaOcrService.CreateDefault();

        // Act
        var result = await service.RecognizeTextAsync(_testImagePath);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.TextRegions);
    }

    [Fact]
    public void CompareAdaptiveVsStandard_ShouldProduceSimilarResults()
    {
        // Arrange
        var standardSettings = new OcrSettings
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

        using var adaptiveService = MangaOcrService.CreateAdaptive(standardSettings);
        using var standardService = MangaOcrService.CreateStandard(standardSettings);

        // Act
        var adaptiveResult = adaptiveService.RecognizeText(_testImagePath);
        var standardResult = standardService.RecognizeText(_testImagePath);

        // Assert - Both should succeed
        Assert.True(adaptiveResult.Success);
        Assert.True(standardResult.Success);

        // Both should detect regions
        Assert.NotEmpty(adaptiveResult.TextRegions);
        Assert.NotEmpty(standardResult.TextRegions);

        // Calculate average confidence
        var adaptiveAvgConf = adaptiveResult.TextRegions
            .Where(r => !float.IsNaN(r.Confidence))
            .Average(r => r.Confidence);
        var standardAvgConf = standardResult.TextRegions
            .Where(r => !float.IsNaN(r.Confidence))
            .Average(r => r.Confidence);

        // Adaptive should be within 95% of standard
        Assert.True(adaptiveAvgConf >= standardAvgConf * 0.95,
            $"Adaptive confidence ({adaptiveAvgConf:P1}) should be >= 95% of standard ({standardAvgConf:P1})");

        Console.WriteLine($"Adaptive: {adaptiveResult.TextRegions.Count} regions, {adaptiveAvgConf:P1} confidence, {adaptiveResult.ElapsedMilliseconds}ms");
        Console.WriteLine($"Standard: {standardResult.TextRegions.Count} regions, {standardAvgConf:P1} confidence, {standardResult.ElapsedMilliseconds}ms");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
