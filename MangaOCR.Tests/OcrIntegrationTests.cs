using MangaOCR.Factories;
using MangaOCR.Models;
using MangaOCR.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MangaOCR.Tests;

/// <summary>
/// OCR整合測試，涵蓋完整的工作流程
/// </summary>
public class OcrIntegrationTests : IDisposable
{
    private readonly string _testImagePath;
    private readonly OcrSettings _ocrSettings;

    public OcrIntegrationTests()
    {
        // 載入配置（模擬 Program.cs 的配置載入）
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "MangaOCR"))
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        _ocrSettings = configuration.GetSection("OcrSettings").Get<OcrSettings>() ?? new OcrSettings();

        // 尋找測試圖片
        _testImagePath = FindTestImage();
    }

    private string FindTestImage()
    {
        var possiblePaths = new[]
        {
            // 從測試專案目錄
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestData", "4.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "TestData", "4.png"),
            // 從專案根目錄
            Path.Combine(Directory.GetCurrentDirectory(), "TestData", "4.png"),
        };

        foreach (var path in possiblePaths)
        {
            var normalizedPath = Path.GetFullPath(path);
            if (File.Exists(normalizedPath))
            {
                return normalizedPath;
            }
        }

        throw new FileNotFoundException("找不到測試圖片 4.png");
    }

    [Fact]
    public void FullOcrWorkflow_WithConfiguration_ShouldSucceed()
    {
        // Arrange
        var factory = new OcrServiceFactory();

        // Act
        using var ocrService = factory.CreateOcrService(_ocrSettings);
        var result = ocrService.RecognizeText(_testImagePath);

        // Assert
        Assert.True(result.Success, "OCR識別應該成功");
        Assert.NotEmpty(result.TextRegions);
        Assert.True(result.ElapsedMilliseconds > 0);
    }

    [Fact]
    public void FullOcrWorkflow_WithPostProcessing_ShouldImproveQuality()
    {
        // Arrange
        var factory = new OcrServiceFactory();
        using var ocrService = factory.CreateOcrService(_ocrSettings);
        var processor = new ResultProcessor();

        // Act
        var rawResult = ocrService.RecognizeText(_testImagePath);
        var processedResult = processor.Process(rawResult, minConfidence: _ocrSettings.MinConfidence);

        // Assert
        Assert.True(rawResult.Success);
        Assert.True(processedResult.Success);

        // 後處理應該過濾掉一些低品質區域
        Assert.True(processedResult.TextRegions.Count <= rawResult.TextRegions.Count,
            "後處理後的區域數應該 <= 原始區域數");

        // 後處理後的平均信心度應該更高
        var rawAvgConfidence = rawResult.TextRegions
            .Where(r => !float.IsNaN(r.Confidence))
            .Average(r => r.Confidence);

        var processedAvgConfidence = processedResult.TextRegions
            .Where(r => !float.IsNaN(r.Confidence))
            .Average(r => r.Confidence);

        Assert.True(processedAvgConfidence >= rawAvgConfidence,
            $"後處理後的平均信心度({processedAvgConfidence:P1})應該 >= 原始平均信心度({rawAvgConfidence:P1})");
    }

    [Fact]
    public void FullOcrWorkflow_ShouldMeetAccuracyTargets()
    {
        // Arrange
        var factory = new OcrServiceFactory();
        using var ocrService = factory.CreateOcrService(_ocrSettings);
        var processor = new ResultProcessor();

        // Act
        var rawResult = ocrService.RecognizeText(_testImagePath);
        var processedResult = processor.Process(rawResult, minConfidence: _ocrSettings.MinConfidence);

        // Assert - 檢查是否達到優化後的準確度目標
        var validRegions = processedResult.TextRegions
            .Where(r => !float.IsNaN(r.Confidence))
            .ToList();

        Assert.NotEmpty(validRegions);

        // 平均信心度應該 >= 85%（優化後的目標）
        var avgConfidence = validRegions.Average(r => r.Confidence);
        Assert.True(avgConfidence >= 0.85f,
            $"平均信心度應該 >= 85%，實際為 {avgConfidence:P1}");

        // 高信心度區域(>=70%)應該至少有 20 個
        var highConfidenceCount = validRegions.Count(r => r.Confidence >= 0.7f);
        Assert.True(highConfidenceCount >= 20,
            $"高信心度區域應該 >= 20，實際為 {highConfidenceCount}");

        // 非空白區域比例應該 >= 80%
        var nonEmptyCount = processedResult.TextRegions.Count(r => !string.IsNullOrWhiteSpace(r.Text));
        var nonEmptyPercentage = (double)nonEmptyCount / processedResult.TextRegions.Count;
        Assert.True(nonEmptyPercentage >= 0.8,
            $"非空白區域比例應該 >= 80%，實際為 {nonEmptyPercentage:P1}");
    }

    [Fact]
    public void ReadingOrderAnalysis_RightToLeftTopToBottom_ShouldProduceValidOrder()
    {
        // Arrange
        var factory = new OcrServiceFactory();
        using var ocrService = factory.CreateOcrService(_ocrSettings);
        var processor = new ResultProcessor();
        var orderAnalyzer = new TextOrderAnalyzer();

        // Act
        var rawResult = ocrService.RecognizeText(_testImagePath);
        var processedResult = processor.Process(rawResult, minConfidence: _ocrSettings.MinConfidence);
        var orderedRegions = orderAnalyzer.AssignReadingOrder(
            processedResult.TextRegions,
            TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);

        // Assert
        Assert.NotEmpty(orderedRegions);

        // 檢查順序編號的連續性
        var orders = orderedRegions.Select(x => x.ReadingOrder).ToList();
        Assert.Equal(1, orders.Min()); // 應該從 1 開始
        Assert.Equal(orderedRegions.Count, orders.Max()); // 應該連續到最後
        Assert.Equal(orderedRegions.Count, orders.Distinct().Count()); // 沒有重複的順序編號
    }

    [Fact]
    public void ReadingOrderAnalysis_AutoDetect_ShouldProduceValidOrder()
    {
        // Arrange
        var factory = new OcrServiceFactory();
        using var ocrService = factory.CreateOcrService(_ocrSettings);
        var processor = new ResultProcessor();
        var orderAnalyzer = new TextOrderAnalyzer();

        // Act
        var rawResult = ocrService.RecognizeText(_testImagePath);
        var processedResult = processor.Process(rawResult, minConfidence: _ocrSettings.MinConfidence);
        var orderedRegions = orderAnalyzer.SortByReadingOrder(
            processedResult.TextRegions,
            TextOrderAnalyzer.ReadingDirection.Auto);

        // Assert
        Assert.NotEmpty(orderedRegions);
        Assert.Equal(processedResult.TextRegions.Count, orderedRegions.Count);
    }

    [Fact]
    public void ImageAnnotation_ReadingOrder_ShouldGenerateFile()
    {
        // Arrange
        var factory = new OcrServiceFactory();
        using var ocrService = factory.CreateOcrService(_ocrSettings);
        var processor = new ResultProcessor();
        var orderAnalyzer = new TextOrderAnalyzer();
        var annotator = new ImageAnnotator();

        var outputDir = Path.GetDirectoryName(_testImagePath)!;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputPath = Path.Combine(outputDir, $"test_reading_order_{timestamp}.png");

        try
        {
            // Act
            var rawResult = ocrService.RecognizeText(_testImagePath);
            var processedResult = processor.Process(rawResult, minConfidence: _ocrSettings.MinConfidence);
            var orderedRegions = orderAnalyzer.AssignReadingOrder(
                processedResult.TextRegions,
                TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);

            annotator.AnnotateReadingOrder(_testImagePath, orderedRegions, outputPath);

            // Assert
            Assert.True(File.Exists(outputPath), $"應該生成標註圖片: {outputPath}");

            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0, "生成的圖片檔案大小應該 > 0");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void ImageAnnotation_Confidence_ShouldGenerateFile()
    {
        // Arrange
        var factory = new OcrServiceFactory();
        using var ocrService = factory.CreateOcrService(_ocrSettings);
        var annotator = new ImageAnnotator();

        var outputDir = Path.GetDirectoryName(_testImagePath)!;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputPath = Path.Combine(outputDir, $"test_confidence_{timestamp}.png");

        try
        {
            // Act
            var rawResult = ocrService.RecognizeText(_testImagePath);
            annotator.AnnotateConfidence(_testImagePath, rawResult.TextRegions, outputPath);

            // Assert
            Assert.True(File.Exists(outputPath), $"應該生成標註圖片: {outputPath}");

            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0, "生成的圖片檔案大小應該 > 0");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void OcrSettings_FromConfiguration_ShouldHaveOptimizedValues()
    {
        // Assert - 驗證配置值是否為優化後的設定
        Assert.Equal(OcrProvider.PaddleOCR, _ocrSettings.Provider);
        Assert.Equal("Japanese", _ocrSettings.Language);
        Assert.Equal(1.5f, _ocrSettings.UnclipRatio);
        Assert.Equal(1024, _ocrSettings.MaxSize);
        Assert.Equal(0.6f, _ocrSettings.BoxScoreThreshold);
        Assert.Equal(0.3f, _ocrSettings.Threshold);
        Assert.True(_ocrSettings.AllowRotateDetection);
        Assert.True(_ocrSettings.Enable180Classification);
        Assert.False(_ocrSettings.UsePreprocessing);
    }

    /// <summary>
    /// 生成 Debug 標註圖片（閱讀順序 + 信心度）
    /// 注意：此測試會保留生成的圖片，不會自動刪除
    /// </summary>
    [Fact]
    public void GenerateDebugAnnotationImages()
    {
        // Arrange
        var factory = new OcrServiceFactory();
        using var ocrService = factory.CreateOcrService(_ocrSettings);
        var processor = new ResultProcessor();
        var orderAnalyzer = new TextOrderAnalyzer();
        var annotator = new ImageAnnotator();

        var outputDir = Path.GetDirectoryName(_testImagePath)!;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var readingOrderPath = Path.Combine(outputDir, $"debug_reading_order_{timestamp}.png");
        var confidencePath = Path.Combine(outputDir, $"debug_confidence_{timestamp}.png");

        // Act
        var rawResult = ocrService.RecognizeText(_testImagePath);
        var processedResult = processor.Process(rawResult, minConfidence: _ocrSettings.MinConfidence);

        // 1. 生成閱讀順序標註圖片
        var orderedRegions = orderAnalyzer.AssignReadingOrder(
            processedResult.TextRegions,
            TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);
        annotator.AnnotateReadingOrder(_testImagePath, orderedRegions, readingOrderPath);

        // 2. 生成信心度標註圖片
        annotator.AnnotateConfidence(_testImagePath, rawResult.TextRegions, confidencePath);

        // Assert
        Assert.True(File.Exists(readingOrderPath), $"應該生成閱讀順序標註圖片: {readingOrderPath}");
        Assert.True(File.Exists(confidencePath), $"應該生成信心度標註圖片: {confidencePath}");

        var readingOrderInfo = new FileInfo(readingOrderPath);
        var confidenceInfo = new FileInfo(confidencePath);
        Assert.True(readingOrderInfo.Length > 0, "閱讀順序圖片檔案大小應該 > 0");
        Assert.True(confidenceInfo.Length > 0, "信心度圖片檔案大小應該 > 0");

        // 輸出圖片路徑供檢視
        Console.WriteLine($"✓ 已生成閱讀順序標註圖片: {Path.GetFileName(readingOrderPath)}");
        Console.WriteLine($"✓ 已生成信心度標註圖片: {Path.GetFileName(confidencePath)}");
        Console.WriteLine($"  位置: {outputDir}");

        // 注意：不刪除圖片，保留供檢視
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
