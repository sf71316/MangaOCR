using MangaOCR.Factories;
using MangaOCR.Models;
using MangaOCR.Services;
using Xunit;

namespace MangaOCR.Tests;

/// <summary>
/// 自適應 OCR 與標準參數對比測試
/// </summary>
public class AdaptiveOcrComparisonTests : IDisposable
{
    private readonly string _testImagePath4;
    private readonly string _testImagePath5;

    public AdaptiveOcrComparisonTests()
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
    public void CompareAdaptiveVsStandard_Image4()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              4.png 辨識率對比測試                              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // 標準參數
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

        Console.WriteLine("【方法 1：標準參數】");
        Console.WriteLine($"  MaxSize: {standardSettings.MaxSize}");
        Console.WriteLine($"  UnclipRatio: {standardSettings.UnclipRatio}");
        Console.WriteLine($"  BoxScoreThreshold: {standardSettings.BoxScoreThreshold}");
        Console.WriteLine();

        var factory = new OcrServiceFactory();
        using var standardService = factory.CreateOcrService(standardSettings);
        var processor = new ResultProcessor();

        var standardResult = standardService.RecognizeText(_testImagePath4);
        var standardProcessed = processor.Process(standardResult, minConfidence: standardSettings.MinConfidence);

        var standardStats = CalculateStats(standardResult, standardProcessed);
        DisplayStats("標準參數", standardStats);

        Console.WriteLine();
        Console.WriteLine("【方法 2：自適應參數】");

        // 自適應參數
        using var adaptiveService = MangaOcrService.CreateAdaptive(standardSettings);
        var adaptiveResult = adaptiveService.RecognizeText(_testImagePath4, verbose: true);
        var adaptiveProcessed = processor.Process(adaptiveResult, minConfidence: standardSettings.MinConfidence);

        var adaptiveStats = CalculateStats(adaptiveResult, adaptiveProcessed);
        DisplayStats("自適應參數", adaptiveStats);

        Console.WriteLine();
        Console.WriteLine("【對比結果】");
        CompareStats(standardStats, adaptiveStats);

        // Assert - 確保自適應參數不會比標準參數差太多
        Assert.True(adaptiveStats.RawAvgConfidence >= standardStats.RawAvgConfidence * 0.95,
            "自適應參數的平均信心度不應低於標準參數的 95%");
        Assert.True(adaptiveStats.ProcessedRegionCount >= standardStats.ProcessedRegionCount * 0.9,
            "自適應參數的處理後區域數不應低於標準參數的 90%");
    }

    [Fact]
    public void CompareAdaptiveVsStandard_Image5()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              5.jpg 辨識率對比測試                              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // 標準參數
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

        Console.WriteLine("【方法 1：標準參數】");
        Console.WriteLine($"  MaxSize: {standardSettings.MaxSize}");
        Console.WriteLine($"  UnclipRatio: {standardSettings.UnclipRatio}");
        Console.WriteLine($"  BoxScoreThreshold: {standardSettings.BoxScoreThreshold}");
        Console.WriteLine();

        var factory = new OcrServiceFactory();
        using var standardService = factory.CreateOcrService(standardSettings);
        var processor = new ResultProcessor();

        var standardResult = standardService.RecognizeText(_testImagePath5);
        var standardProcessed = processor.Process(standardResult, minConfidence: standardSettings.MinConfidence);

        var standardStats = CalculateStats(standardResult, standardProcessed);
        DisplayStats("標準參數", standardStats);

        Console.WriteLine();
        Console.WriteLine("【方法 2：自適應參數】");

        // 自適應參數
        using var adaptiveService = MangaOcrService.CreateAdaptive(standardSettings);
        var adaptiveResult = adaptiveService.RecognizeText(_testImagePath5, verbose: true);
        var adaptiveProcessed = processor.Process(adaptiveResult, minConfidence: standardSettings.MinConfidence);

        var adaptiveStats = CalculateStats(adaptiveResult, adaptiveProcessed);
        DisplayStats("自適應參數", adaptiveStats);

        Console.WriteLine();
        Console.WriteLine("【對比結果】");
        CompareStats(standardStats, adaptiveStats);

        // Assert - 確保自適應參數不會比標準參數差太多
        Assert.True(adaptiveStats.RawAvgConfidence >= standardStats.RawAvgConfidence * 0.95,
            "自適應參數的平均信心度不應低於標準參數的 95%");
        Assert.True(adaptiveStats.ProcessedRegionCount >= standardStats.ProcessedRegionCount * 0.9,
            "自適應參數的處理後區域數不應低於標準參數的 90%");
    }

    [Fact]
    public void AdaptiveOcr_BothImages_ShouldMeetAccuracyTargets()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         自適應 OCR 綜合測試（4.png + 5.jpg）                   ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var baseSettings = new OcrSettings
        {
            Language = "Japanese",
            MinConfidence = 0.5f
        };

        using var adaptiveService = MangaOcrService.CreateAdaptive(baseSettings);
        var processor = new ResultProcessor();

        // 測試 4.png
        Console.WriteLine("【測試圖片 1: 4.png】");
        var result4 = adaptiveService.RecognizeText(_testImagePath4, verbose: true);
        var processed4 = processor.Process(result4, minConfidence: baseSettings.MinConfidence);

        var stats4 = CalculateStats(result4, processed4);
        Console.WriteLine();
        Console.WriteLine("結果統計：");
        Console.WriteLine($"  原始區域數: {stats4.RawRegionCount}");
        Console.WriteLine($"  平均信心度: {stats4.RawAvgConfidence:P1}");
        Console.WriteLine($"  高信心度區域: {stats4.HighConfidenceCount}");
        Console.WriteLine($"  處理後區域數: {stats4.ProcessedRegionCount}");
        Console.WriteLine($"  處理後平均信心度: {stats4.ProcessedAvgConfidence:P1}");

        // 驗證 4.png 達標
        Assert.True(stats4.ProcessedAvgConfidence >= 0.85,
            $"4.png 處理後平均信心度應 >= 85%，實際: {stats4.ProcessedAvgConfidence:P1}");

        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine();

        // 測試 5.jpg
        Console.WriteLine("【測試圖片 2: 5.jpg】");
        var result5 = adaptiveService.RecognizeText(_testImagePath5, verbose: true);
        var processed5 = processor.Process(result5, minConfidence: baseSettings.MinConfidence);

        var stats5 = CalculateStats(result5, processed5);
        Console.WriteLine();
        Console.WriteLine("結果統計：");
        Console.WriteLine($"  原始區域數: {stats5.RawRegionCount}");
        Console.WriteLine($"  平均信心度: {stats5.RawAvgConfidence:P1}");
        Console.WriteLine($"  高信心度區域: {stats5.HighConfidenceCount}");
        Console.WriteLine($"  處理後區域數: {stats5.ProcessedRegionCount}");
        Console.WriteLine($"  處理後平均信心度: {stats5.ProcessedAvgConfidence:P1}");

        // 驗證 5.jpg 達標（標準可以稍微寬鬆）
        Assert.True(stats5.ProcessedAvgConfidence >= 0.80,
            $"5.jpg 處理後平均信心度應 >= 80%，實際: {stats5.ProcessedAvgConfidence:P1}");

        Console.WriteLine();
        Console.WriteLine("═════════════════════════════════════════════════════════════════");
        Console.WriteLine("✓ 兩張圖片都達到辨識率標準！");
    }

    private class OcrStats
    {
        public int RawRegionCount { get; set; }
        public double RawAvgConfidence { get; set; }
        public int HighConfidenceCount { get; set; }
        public int ProcessedRegionCount { get; set; }
        public double ProcessedAvgConfidence { get; set; }
        public long ElapsedMs { get; set; }
    }

    private OcrStats CalculateStats(OcrResult rawResult, OcrResult processedResult)
    {
        var validRegions = rawResult.TextRegions.Where(r => !float.IsNaN(r.Confidence)).ToList();
        var processedValidRegions = processedResult.TextRegions.Where(r => !float.IsNaN(r.Confidence)).ToList();

        return new OcrStats
        {
            RawRegionCount = rawResult.TextRegions.Count,
            RawAvgConfidence = validRegions.Any() ? validRegions.Average(r => r.Confidence) : 0,
            HighConfidenceCount = validRegions.Count(r => r.Confidence >= 0.7f),
            ProcessedRegionCount = processedResult.TextRegions.Count,
            ProcessedAvgConfidence = processedValidRegions.Any() ? processedValidRegions.Average(r => r.Confidence) : 0,
            ElapsedMs = rawResult.ElapsedMilliseconds
        };
    }

    private void DisplayStats(string method, OcrStats stats)
    {
        Console.WriteLine($"【{method} 結果】");
        Console.WriteLine($"  原始區域數: {stats.RawRegionCount}");
        Console.WriteLine($"  原始平均信心度: {stats.RawAvgConfidence:P1}");
        Console.WriteLine($"  高信心度區域(>=70%): {stats.HighConfidenceCount}");
        Console.WriteLine($"  處理後區域數: {stats.ProcessedRegionCount}");
        Console.WriteLine($"  處理後平均信心度: {stats.ProcessedAvgConfidence:P1}");
        Console.WriteLine($"  耗時: {stats.ElapsedMs}ms");
    }

    private void CompareStats(OcrStats standard, OcrStats adaptive)
    {
        var regionDiff = adaptive.RawRegionCount - standard.RawRegionCount;
        var confidenceDiff = (adaptive.RawAvgConfidence - standard.RawAvgConfidence) * 100;
        var highConfDiff = adaptive.HighConfidenceCount - standard.HighConfidenceCount;
        var processedDiff = adaptive.ProcessedRegionCount - standard.ProcessedRegionCount;
        var processedConfDiff = (adaptive.ProcessedAvgConfidence - standard.ProcessedAvgConfidence) * 100;
        var timeDiff = adaptive.ElapsedMs - standard.ElapsedMs;

        Console.WriteLine($"  原始區域數差異: {FormatDiff(regionDiff)} 個");
        Console.WriteLine($"  原始平均信心度差異: {FormatDiff(confidenceDiff, isPercentage: true)}");
        Console.WriteLine($"  高信心度區域差異: {FormatDiff(highConfDiff)} 個");
        Console.WriteLine($"  處理後區域數差異: {FormatDiff(processedDiff)} 個");
        Console.WriteLine($"  處理後平均信心度差異: {FormatDiff(processedConfDiff, isPercentage: true)}");
        Console.WriteLine($"  耗時差異: {FormatDiff(timeDiff)} ms");

        Console.WriteLine();
        Console.WriteLine("【結論】");
        if (Math.Abs(confidenceDiff) < 1 && Math.Abs(processedDiff) <= 2)
        {
            Console.WriteLine("  ✓ 自適應參數與標準參數效果相當");
        }
        else if (confidenceDiff > 0 || processedDiff > 0)
        {
            Console.WriteLine("  ✓ 自適應參數表現優於標準參數");
        }
        else
        {
            Console.WriteLine("  ⚠ 自適應參數表現略低於標準參數（可能需要調整閾值）");
        }
    }

    private string FormatDiff(double diff, bool isPercentage = false)
    {
        var sign = diff >= 0 ? "+" : "";
        if (isPercentage)
        {
            return $"{sign}{diff:F2}%";
        }
        return $"{sign}{diff:F0}";
    }

    private string FormatDiff(long diff)
    {
        var sign = diff >= 0 ? "+" : "";
        return $"{sign}{diff}";
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
