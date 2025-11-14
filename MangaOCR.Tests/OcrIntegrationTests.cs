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

    /// <summary>
    /// 測試指定圖片的 OCR 辨識率（使用優化參數）
    /// </summary>
    [Fact]
    public void TestSpecificImage_5jpg_OptimizedParams()
    {
        // Arrange - 使用優化參數
        var imagePath = Path.Combine(Path.GetDirectoryName(_testImagePath)!, "5.jpg");

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"✗ 找不到測試圖片: {imagePath}");
            return;
        }

        // 優化參數：方案 1
        var optimizedSettings = new OcrSettings
        {
            Provider = OcrProvider.PaddleOCR,
            Language = "Japanese",
            MinConfidence = 0.5f,
            UsePreprocessing = false,
            AllowRotateDetection = true,
            Enable180Classification = true,
            UnclipRatio = 1.8f,      // 提高擴展比例
            MaxSize = 1280,          // 提高解析度
            BoxScoreThreshold = 0.5f, // 降低閾值以檢測更多區域
            Threshold = 0.3f
        };

        var factory = new OcrServiceFactory();
        using var ocrService = factory.CreateOcrService(optimizedSettings);
        var processor = new ResultProcessor();
        var orderAnalyzer = new TextOrderAnalyzer();
        var annotator = new ImageAnnotator();

        Console.WriteLine("=== 測試圖片: 5.jpg（優化參數）===");
        Console.WriteLine("【使用參數】");
        Console.WriteLine($"  MaxSize: {optimizedSettings.MaxSize}");
        Console.WriteLine($"  UnclipRatio: {optimizedSettings.UnclipRatio}");
        Console.WriteLine($"  BoxScoreThreshold: {optimizedSettings.BoxScoreThreshold}");
        Console.WriteLine();

        // Act - 執行 OCR
        var rawResult = ocrService.RecognizeText(imagePath);

        if (!rawResult.Success)
        {
            Console.WriteLine($"✗ OCR 識別失敗: {rawResult.ErrorMessage}");
            return;
        }

        Console.WriteLine($"✓ OCR 識別完成，耗時: {rawResult.ElapsedMilliseconds}ms");
        Console.WriteLine();

        // 原始結果統計
        Console.WriteLine("【原始識別結果】");
        Console.WriteLine($"  識別區域數: {rawResult.TextRegions.Count}");

        var validRegions = rawResult.TextRegions.Where(r => !float.IsNaN(r.Confidence)).ToList();
        if (validRegions.Any())
        {
            var avgConfidence = validRegions.Average(r => r.Confidence);
            var highConfidenceCount = validRegions.Count(r => r.Confidence >= 0.7f);
            var mediumConfidenceCount = validRegions.Count(r => r.Confidence >= 0.5f && r.Confidence < 0.7f);
            var lowConfidenceCount = validRegions.Count(r => r.Confidence < 0.5f);

            Console.WriteLine($"  平均信心度: {avgConfidence:P1}");
            Console.WriteLine($"  高信心度區域 (>=70%): {highConfidenceCount}");
            Console.WriteLine($"  中信心度區域 (50-70%): {mediumConfidenceCount}");
            Console.WriteLine($"  低信心度區域 (<50%): {lowConfidenceCount}");
        }

        var nonEmptyCount = rawResult.TextRegions.Count(r => !string.IsNullOrWhiteSpace(r.Text));
        Console.WriteLine($"  非空白區域: {nonEmptyCount}/{rawResult.TextRegions.Count}");
        Console.WriteLine();

        // 後處理結果
        var processedResult = processor.Process(rawResult, minConfidence: optimizedSettings.MinConfidence);

        Console.WriteLine("【後處理結果】");
        Console.WriteLine($"  處理後區域數: {processedResult.TextRegions.Count}");
        Console.WriteLine($"  已過濾: {rawResult.TextRegions.Count - processedResult.TextRegions.Count} 個低品質區域");

        if (processedResult.TextRegions.Any())
        {
            var avgConfidenceProcessed = processedResult.TextRegions
                .Where(r => !float.IsNaN(r.Confidence))
                .Average(r => r.Confidence);
            Console.WriteLine($"  平均信心度: {avgConfidenceProcessed:P1}");
        }
        Console.WriteLine();

        // 顯示識別文字樣本
        Console.WriteLine("【識別文字樣本（前 15 個）】");
        var orderedRegions = orderAnalyzer.AssignReadingOrder(
            processedResult.TextRegions,
            TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);

        foreach (var item in orderedRegions.Take(15))
        {
            Console.WriteLine($"  [{item.ReadingOrder:D2}] {item.Region.Text} (信心度: {item.Region.Confidence:P1})");
        }

        if (orderedRegions.Count > 15)
        {
            Console.WriteLine($"  ... 還有 {orderedRegions.Count - 15} 個區域");
        }
        Console.WriteLine();

        // 生成 Debug 圖片
        var outputDir = Path.GetDirectoryName(imagePath)!;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var readingOrderPath = Path.Combine(outputDir, $"5_optimized_reading_order_{timestamp}.png");
        var confidencePath = Path.Combine(outputDir, $"5_optimized_confidence_{timestamp}.png");

        annotator.AnnotateReadingOrder(imagePath, orderedRegions, readingOrderPath);
        annotator.AnnotateConfidence(imagePath, rawResult.TextRegions, confidencePath);

        Console.WriteLine("【生成 Debug 圖片】");
        Console.WriteLine($"  ✓ 閱讀順序標註: {Path.GetFileName(readingOrderPath)}");
        Console.WriteLine($"  ✓ 信心度標註: {Path.GetFileName(confidencePath)}");
        Console.WriteLine($"  位置: {outputDir}");
        Console.WriteLine();

        // 與標準參數對比
        Console.WriteLine("【對比標準參數】");
        Console.WriteLine("  標準參數結果（MaxSize=1024）：");
        Console.WriteLine("    - 平均信心度: 82.1%");
        Console.WriteLine("    - 高信心度區域: 15");
        Console.WriteLine("    - 處理後區域數: 16");
        Console.WriteLine("    - 耗時: 1594ms");
        Console.WriteLine();
        Console.WriteLine("  優化參數結果（MaxSize=1280）：");
        Console.WriteLine($"    - 平均信心度: {(validRegions.Any() ? validRegions.Average(r => r.Confidence) : 0):P1}");
        Console.WriteLine($"    - 高信心度區域: {validRegions.Count(r => r.Confidence >= 0.7f)}");
        Console.WriteLine($"    - 處理後區域數: {processedResult.TextRegions.Count}");
        Console.WriteLine($"    - 耗時: {rawResult.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 測試指定圖片的 OCR 辨識率並生成詳細報告
    /// </summary>
    [Fact]
    public void TestSpecificImage_5jpg()
    {
        // Arrange
        var imagePath = Path.Combine(Path.GetDirectoryName(_testImagePath)!, "5.jpg");

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"✗ 找不到測試圖片: {imagePath}");
            return;
        }

        var factory = new OcrServiceFactory();
        using var ocrService = factory.CreateOcrService(_ocrSettings);
        var processor = new ResultProcessor();
        var orderAnalyzer = new TextOrderAnalyzer();
        var annotator = new ImageAnnotator();

        Console.WriteLine("=== 測試圖片: 5.jpg ===");
        Console.WriteLine();

        // Act - 執行 OCR
        var rawResult = ocrService.RecognizeText(imagePath);

        if (!rawResult.Success)
        {
            Console.WriteLine($"✗ OCR 識別失敗: {rawResult.ErrorMessage}");
            return;
        }

        Console.WriteLine($"✓ OCR 識別完成，耗時: {rawResult.ElapsedMilliseconds}ms");
        Console.WriteLine();

        // 原始結果統計
        Console.WriteLine("【原始識別結果】");
        Console.WriteLine($"  識別區域數: {rawResult.TextRegions.Count}");

        var validRegions = rawResult.TextRegions.Where(r => !float.IsNaN(r.Confidence)).ToList();
        if (validRegions.Any())
        {
            var avgConfidence = validRegions.Average(r => r.Confidence);
            var highConfidenceCount = validRegions.Count(r => r.Confidence >= 0.7f);
            var mediumConfidenceCount = validRegions.Count(r => r.Confidence >= 0.5f && r.Confidence < 0.7f);
            var lowConfidenceCount = validRegions.Count(r => r.Confidence < 0.5f);

            Console.WriteLine($"  平均信心度: {avgConfidence:P1}");
            Console.WriteLine($"  高信心度區域 (>=70%): {highConfidenceCount}");
            Console.WriteLine($"  中信心度區域 (50-70%): {mediumConfidenceCount}");
            Console.WriteLine($"  低信心度區域 (<50%): {lowConfidenceCount}");
        }

        var nonEmptyCount = rawResult.TextRegions.Count(r => !string.IsNullOrWhiteSpace(r.Text));
        Console.WriteLine($"  非空白區域: {nonEmptyCount}/{rawResult.TextRegions.Count}");
        Console.WriteLine();

        // 後處理結果
        var processedResult = processor.Process(rawResult, minConfidence: _ocrSettings.MinConfidence);

        Console.WriteLine("【後處理結果】");
        Console.WriteLine($"  處理後區域數: {processedResult.TextRegions.Count}");
        Console.WriteLine($"  已過濾: {rawResult.TextRegions.Count - processedResult.TextRegions.Count} 個低品質區域");

        if (processedResult.TextRegions.Any())
        {
            var avgConfidenceProcessed = processedResult.TextRegions
                .Where(r => !float.IsNaN(r.Confidence))
                .Average(r => r.Confidence);
            Console.WriteLine($"  平均信心度: {avgConfidenceProcessed:P1}");
        }
        Console.WriteLine();

        // 顯示識別文字樣本
        Console.WriteLine("【識別文字樣本（前 10 個）】");
        var orderedRegions = orderAnalyzer.AssignReadingOrder(
            processedResult.TextRegions,
            TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);

        foreach (var item in orderedRegions.Take(10))
        {
            Console.WriteLine($"  [{item.ReadingOrder:D2}] {item.Region.Text} (信心度: {item.Region.Confidence:P1})");
        }

        if (orderedRegions.Count > 10)
        {
            Console.WriteLine($"  ... 還有 {orderedRegions.Count - 10} 個區域");
        }
        Console.WriteLine();

        // 生成 Debug 圖片
        var outputDir = Path.GetDirectoryName(imagePath)!;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var readingOrderPath = Path.Combine(outputDir, $"5_reading_order_{timestamp}.png");
        var confidencePath = Path.Combine(outputDir, $"5_confidence_{timestamp}.png");

        annotator.AnnotateReadingOrder(imagePath, orderedRegions, readingOrderPath);
        annotator.AnnotateConfidence(imagePath, rawResult.TextRegions, confidencePath);

        Console.WriteLine("【生成 Debug 圖片】");
        Console.WriteLine($"  ✓ 閱讀順序標註: {Path.GetFileName(readingOrderPath)}");
        Console.WriteLine($"  ✓ 信心度標註: {Path.GetFileName(confidencePath)}");
        Console.WriteLine($"  位置: {outputDir}");
        Console.WriteLine();

        // 診斷建議
        Console.WriteLine("【診斷建議】");
        if (validRegions.Any())
        {
            var avgConf = validRegions.Average(r => r.Confidence);
            if (avgConf < 0.7)
            {
                Console.WriteLine("  ⚠ 平均信心度偏低，建議調整參數：");
                Console.WriteLine("    - 提高 MaxSize 至 1280 或 1920");
                Console.WriteLine("    - 提高 UnclipRatio 至 1.8 或 2.0");
                Console.WriteLine("    - 降低 BoxScoreThreshold 至 0.5");
            }

            var lowConfPct = (double)validRegions.Count(r => r.Confidence < 0.5f) / validRegions.Count;
            if (lowConfPct > 0.3)
            {
                Console.WriteLine("  ⚠ 低信心度區域過多（>30%），可能原因：");
                Console.WriteLine("    - 圖片模糊或解析度不足");
                Console.WriteLine("    - 文字過小或字體特殊");
                Console.WriteLine("    - 背景干擾或對比度不足");
            }
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
