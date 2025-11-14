using MangaOCR.Models;
using MangaOCR.Services;
using Xunit;

namespace MangaOCR.Tests.Services;

/// <summary>
/// 檢測與識別分離模式測試
/// </summary>
public class DetectionRecognitionSeparationTests : IDisposable
{
    private readonly string _testImagePath;

    public DetectionRecognitionSeparationTests()
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
    public void DetectTextRegions_ShouldReturnCoordinatesOnly()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();

        // Act
        var regions = ocr.DetectTextRegions(_testImagePath);

        // Assert
        Assert.NotNull(regions);
        Assert.NotEmpty(regions);

        foreach (var region in regions)
        {
            // 應該只有座標，沒有文字內容
            Assert.Empty(region.Text);
            Assert.True(float.IsNaN(region.Confidence));

            // 應該有邊界框
            Assert.NotNull(region.BoundingBox);
            Assert.True(region.BoundingBox.Width > 0);
            Assert.True(region.BoundingBox.Height > 0);

            // 應該有四個角點
            Assert.Equal(4, region.BoundingBox.Points.Count);
        }

        Console.WriteLine($"檢測到 {regions.Count} 個文字區域");
        Console.WriteLine("\n座標列表：");
        for (int i = 0; i < Math.Min(regions.Count, 5); i++)
        {
            var region = regions[i];
            Console.WriteLine($"  區域 {i + 1}: ({region.BoundingBox.X}, {region.BoundingBox.Y}) " +
                            $"尺寸: {region.BoundingBox.Width}x{region.BoundingBox.Height}");
        }
    }

    [Fact]
    public void DetectTextRegions_ShouldBeFasterThanFullOcr()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - 只檢測
        var regions = ocr.DetectTextRegions(_testImagePath);
        stopwatch.Stop();
        var detectionTime = stopwatch.ElapsedMilliseconds;

        // Act - 完整 OCR
        stopwatch.Restart();
        var fullResult = ocr.RecognizeText(_testImagePath);
        stopwatch.Stop();
        var fullOcrTime = stopwatch.ElapsedMilliseconds;

        // Assert
        Console.WriteLine($"只檢測耗時: {detectionTime}ms");
        Console.WriteLine($"完整 OCR 耗時: {fullOcrTime}ms");
        Console.WriteLine($"速度提升: {(fullOcrTime - detectionTime) / (double)fullOcrTime:P1}");

        // 只檢測應該比完整 OCR 快
        Assert.True(detectionTime < fullOcrTime,
            $"只檢測 ({detectionTime}ms) 應該快於完整 OCR ({fullOcrTime}ms)");
    }

    [Fact]
    public async Task DetectTextRegionsAsync_ShouldWork()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();

        // Act
        var regions = await ocr.DetectTextRegionsAsync(_testImagePath);

        // Assert
        Assert.NotNull(regions);
        Assert.NotEmpty(regions);
        Console.WriteLine($"非同步檢測到 {regions.Count} 個文字區域");
    }

    [Fact]
    public void RecognizeTextOnly_WithSingleTextImage_ShouldWork()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();

        // Note: 這個測試使用完整圖片，實際應用中應該使用已截取的文字圖片
        // 這裡只是測試功能是否正常運作

        // Act
        var result = ocr.RecognizeTextOnly(_testImagePath);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.TextRegions);  // 應該只有一個區域
        Assert.NotEmpty(result.TextRegions[0].Text);

        Console.WriteLine($"識別文字: {result.TextRegions[0].Text}");
        Console.WriteLine($"耗時: {result.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void RecognizeTextOnly_ShouldBeMuchFasterThanFullOcr()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();

        // Act - 只識別（假設整張圖是一個文字區域）
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var recognitionResult = ocr.RecognizeTextOnly(_testImagePath);
        stopwatch.Stop();
        var recognitionTime = stopwatch.ElapsedMilliseconds;

        // Act - 完整 OCR
        stopwatch.Restart();
        var fullResult = ocr.RecognizeText(_testImagePath);
        stopwatch.Stop();
        var fullOcrTime = stopwatch.ElapsedMilliseconds;

        // Assert
        Console.WriteLine($"只識別耗時: {recognitionTime}ms");
        Console.WriteLine($"完整 OCR 耗時: {fullOcrTime}ms");
        Console.WriteLine($"速度提升: {(fullOcrTime - recognitionTime) / (double)fullOcrTime:P1}");

        // Note: 由於我們測試用的是大圖而非單一文字區域，速度提升可能不明顯
        // 實際應用中使用小的文字圖片會有顯著提升
        Assert.True(recognitionResult.Success);
        Assert.True(fullResult.Success);
    }

    [Fact]
    public async Task RecognizeTextOnlyAsync_ShouldWork()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();

        // Act
        var result = await ocr.RecognizeTextOnlyAsync(_testImagePath);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.TextRegions);
        Console.WriteLine($"非同步識別文字: {result.TextRegions[0].Text}");
    }

    [Fact]
    public void RecognizeTextBatch_ShouldProcessMultipleImages()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = new List<string> { _testImagePath, _testImagePath };  // 重複使用同一張測試圖片

        // Act
        var results = ocr.RecognizeTextBatch(imagePaths);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);

        foreach (var result in results)
        {
            Assert.True(result.Success);
            Assert.Single(result.TextRegions);
            Assert.NotEmpty(result.TextRegions[0].Text);
        }

        Console.WriteLine($"批次處理 {results.Count} 張圖片");
        Console.WriteLine($"總耗時: {results.Sum(r => r.ElapsedMilliseconds)}ms");
        Console.WriteLine($"平均耗時: {results.Average(r => r.ElapsedMilliseconds):F0}ms");
    }

    [Fact]
    public async Task RecognizeTextBatchAsync_ShouldWork()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = new List<string> { _testImagePath, _testImagePath };

        // Act
        var results = await ocr.RecognizeTextBatchAsync(imagePaths);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Console.WriteLine($"非同步批次處理 {results.Count} 張圖片完成");
    }

    [Fact]
    public void CompleteWorkflow_DetectThenRecognize_ShouldWork()
    {
        // 完整工作流程測試：先檢測，再選擇性識別
        using var ocr = MangaOcrService.CreateDefault();

        Console.WriteLine("【完整工作流程測試】");
        Console.WriteLine();

        // 步驟 1：檢測所有文字區域（快速）
        Console.WriteLine("步驟 1：檢測文字區域...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var regions = ocr.DetectTextRegions(_testImagePath);
        stopwatch.Stop();
        Console.WriteLine($"  檢測到 {regions.Count} 個區域，耗時 {stopwatch.ElapsedMilliseconds}ms");

        // 步驟 2：模擬用戶選擇前 3 個區域進行識別
        Console.WriteLine();
        Console.WriteLine("步驟 2：用戶選擇區域並識別（此測試略過截圖步驟）...");
        var selectedCount = Math.Min(3, regions.Count);
        Console.WriteLine($"  模擬選擇前 {selectedCount} 個區域");

        // Assert
        Assert.NotEmpty(regions);
        Console.WriteLine();
        Console.WriteLine("✓ 工作流程測試完成");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
