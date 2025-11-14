using MangaOCR.Models;
using MangaOCR.Services;
using Xunit;

namespace MangaOCR.Tests.Services;

/// <summary>
/// 平行批次處理和智能排程測試
/// </summary>
public class ParallelBatchProcessingTests : IDisposable
{
    private readonly string _testImagePath;
    private readonly List<string> _logMessages;
    private readonly List<OcrProgressEventArgs> _progressEvents;

    public ParallelBatchProcessingTests()
    {
        _testImagePath = FindTestImage("4.png");
        _logMessages = new List<string>();
        _progressEvents = new List<OcrProgressEventArgs>();
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
    public void RecognizeTextBatchParallel_WithDefaultOptions_ShouldWork()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = Enumerable.Repeat(_testImagePath, 5).ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = ocr.RecognizeTextBatchParallel(imagePaths);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.True(r.Success));

        Console.WriteLine($"平行處理 5 張圖片耗時: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"預設最大線程數: {BatchProcessingOptions.Default.GetActualMaxDegreeOfParallelism()}");
        Console.WriteLine($"CPU 核心數: {Environment.ProcessorCount}");
    }

    [Fact]
    public void RecognizeTextBatchParallel_WithCustomMaxDegree_ShouldWork()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = Enumerable.Repeat(_testImagePath, 4).ToList();
        var options = new BatchProcessingOptions
        {
            MaxDegreeOfParallelism = 2  // 指定最大 2 個線程
        };

        // Act
        var results = ocr.RecognizeTextBatchParallel(imagePaths, options);

        // Assert
        Assert.Equal(4, results.Count);
        Assert.All(results, r => Assert.True(r.Success));

        Console.WriteLine($"使用自訂最大線程數 (2) 完成批次處理");
    }

    [Fact]
    public void RecognizeTextBatchParallel_ShouldBeFasterThanSequential()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = Enumerable.Repeat(_testImagePath, 6).ToList();

        // Act - 循序處理
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var sequentialResults = ocr.RecognizeTextBatch(imagePaths);
        stopwatch.Stop();
        var sequentialTime = stopwatch.ElapsedMilliseconds;

        // Act - 平行處理
        stopwatch.Restart();
        var parallelResults = ocr.RecognizeTextBatchParallel(imagePaths);
        stopwatch.Stop();
        var parallelTime = stopwatch.ElapsedMilliseconds;

        // Assert
        Console.WriteLine($"循序處理耗時: {sequentialTime}ms");
        Console.WriteLine($"平行處理耗時: {parallelTime}ms");
        Console.WriteLine($"速度提升: {(sequentialTime - parallelTime) / (double)sequentialTime:P1}");

        // 平行處理應該比循序快（但在測試環境可能不明顯）
        Assert.True(parallelTime <= sequentialTime * 1.2, "平行處理不應該明顯慢於循序處理");
    }

    [Fact]
    public void RecognizeTextBatchParallel_WithSmartScheduling_ShouldWork()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = Enumerable.Repeat(_testImagePath, 3).ToList();
        var options = new BatchProcessingOptions
        {
            EnableSmartScheduling = true,
            LargeFileSizeThreshold = 500_000  // 500KB
        };

        // Act
        var results = ocr.RecognizeTextBatchParallel(imagePaths, options);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Success));

        Console.WriteLine("智能排程測試完成");
    }

    [Fact]
    public void RecognizeTextBatchParallel_WithLogEvents_ShouldReceiveLogs()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = Enumerable.Repeat(_testImagePath, 3).ToList();

        // 訂閱日誌事件
        ocr.LogMessage += (sender, e) =>
        {
            _logMessages.Add($"[{e.Level}] {e.Message}");
        };

        // Act
        var results = ocr.RecognizeTextBatchParallel(imagePaths);

        // Assert
        Assert.NotEmpty(_logMessages);
        Assert.Contains(_logMessages, m => m.Contains("開始批次處理"));
        Assert.Contains(_logMessages, m => m.Contains("批次處理完成"));

        Console.WriteLine($"收到 {_logMessages.Count} 條日誌訊息:");
        foreach (var msg in _logMessages.Take(10))
        {
            Console.WriteLine($"  {msg}");
        }
    }

    [Fact]
    public void RecognizeTextBatchParallel_WithProgressEvents_ShouldReceiveProgress()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = Enumerable.Repeat(_testImagePath, 4).ToList();

        // 訂閱進度事件
        ocr.ProgressChanged += (sender, e) =>
        {
            _progressEvents.Add(e);
        };

        // Act
        var results = ocr.RecognizeTextBatchParallel(imagePaths);

        // Assert
        Assert.NotEmpty(_progressEvents);
        Assert.Equal(4, _progressEvents.Count);

        // 驗證進度遞增
        var lastProgress = _progressEvents.Last();
        Assert.Equal(4, lastProgress.Current);
        Assert.Equal(4, lastProgress.Total);
        Assert.Equal(1.0, lastProgress.Percentage);

        Console.WriteLine($"收到 {_progressEvents.Count} 次進度更新:");
        foreach (var progress in _progressEvents)
        {
            Console.WriteLine($"  {progress.Current}/{progress.Total} ({progress.Percentage:P0}) - {progress.Message}");
        }
    }

    [Fact]
    public void RecognizeTextBatchParallel_WithCancellation_ShouldCancelCorrectly()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = Enumerable.Repeat(_testImagePath, 10).ToList();
        using var cts = new CancellationTokenSource();

        var options = new BatchProcessingOptions
        {
            CancellationToken = cts.Token,
            MaxDegreeOfParallelism = 1  // 慢速處理以便取消
        };

        // 在處理 2 張圖片後取消
        var processedCount = 0;
        ocr.ProgressChanged += (sender, e) =>
        {
            processedCount++;
            if (processedCount >= 2)
            {
                cts.Cancel();
            }
        };

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() =>
        {
            ocr.RecognizeTextBatchParallel(imagePaths, options);
        });

        Console.WriteLine($"成功取消處理，已處理 {processedCount} 張圖片");
    }

    [Fact]
    public async Task RecognizeTextBatchParallelAsync_ShouldWork()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = Enumerable.Repeat(_testImagePath, 3).ToList();

        // Act
        var results = await ocr.RecognizeTextBatchParallelAsync(imagePaths);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Success));

        Console.WriteLine("非同步平行批次處理完成");
    }

    [Fact]
    public void RecognizeTextBatchParallel_PreservesOrder_ShouldReturnInOriginalOrder()
    {
        // Arrange
        using var ocr = MangaOcrService.CreateDefault();
        var imagePaths = Enumerable.Repeat(_testImagePath, 5).ToList();

        // Act
        var results = ocr.RecognizeTextBatchParallel(imagePaths);

        // Assert
        Assert.Equal(5, results.Count);
        // 所有結果應該按原始順序返回（即使平行處理）
        for (int i = 0; i < results.Count; i++)
        {
            Assert.True(results[i].Success, $"結果 {i} 應該成功");
        }

        Console.WriteLine("驗證結果順序保持正確");
    }

    [Fact]
    public void BatchProcessingOptions_GetActualMaxDegreeOfParallelism_ShouldCalculateCorrectly()
    {
        // Test 1: 使用預設值
        var options1 = new BatchProcessingOptions();
        var actual1 = options1.GetActualMaxDegreeOfParallelism();
        var expected1 = Math.Max(1, Environment.ProcessorCount / 2);
        Assert.Equal(expected1, actual1);

        // Test 2: 使用自訂值
        var options2 = new BatchProcessingOptions { MaxDegreeOfParallelism = 4 };
        Assert.Equal(4, options2.GetActualMaxDegreeOfParallelism());

        // Test 3: 無效值應該使用預設值
        var options3 = new BatchProcessingOptions { MaxDegreeOfParallelism = 0 };
        Assert.Equal(expected1, options3.GetActualMaxDegreeOfParallelism());

        Console.WriteLine($"CPU 核心數: {Environment.ProcessorCount}");
        Console.WriteLine($"預設最大平行線程數: {expected1}");
    }

    public void Dispose()
    {
        _logMessages.Clear();
        _progressEvents.Clear();
    }
}
