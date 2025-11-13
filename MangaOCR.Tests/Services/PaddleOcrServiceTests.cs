using MangaOCR.Services;
using Xunit;

namespace MangaOCR.Tests.Services;

/// <summary>
/// PaddleOcrService 單元測試
/// </summary>
public class PaddleOcrServiceTests : IDisposable
{
    private readonly PaddleOcrService _ocrService;
    private readonly string _testImagePath;

    public PaddleOcrServiceTests()
    {
        // 初始化OCR服務（不使用預處理以獲得更好效果）
        _ocrService = new PaddleOcrService(usePreprocessing: false);

        // 設定測試圖片路徑
        _testImagePath = FindTestImage();
    }

    private string FindTestImage()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestData", "4.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "TestData", "4.png"),
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
    public void RecognizeText_WithValidImage_ShouldReturnSuccess()
    {
        // Act
        var result = _ocrService.RecognizeText(_testImagePath);

        // Assert
        Assert.True(result.Success, "OCR識別應該成功");
        Assert.NotNull(result.TextRegions);
        Assert.NotEmpty(result.TextRegions);
        Assert.True(result.ElapsedMilliseconds > 0, "處理時間應該大於0");
    }

    [Fact]
    public void RecognizeText_WithValidImage_ShouldDetectMultipleRegions()
    {
        // Act
        var result = _ocrService.RecognizeText(_testImagePath);

        // Assert
        Assert.True(result.TextRegions.Count > 0, "應該偵測到至少一個文字區域");
        Assert.True(result.TextRegions.Count < 100, "文字區域數量應該在合理範圍內");
    }

    [Fact]
    public void RecognizeText_WithValidImage_ShouldHaveReasonableConfidence()
    {
        // Act
        var result = _ocrService.RecognizeText(_testImagePath);

        // Assert
        var validRegions = result.TextRegions.Where(r => !float.IsNaN(r.Confidence)).ToList();
        Assert.NotEmpty(validRegions);

        var avgConfidence = validRegions.Average(r => r.Confidence);
        Assert.True(avgConfidence > 0.5f, $"平均信心度應該 > 50%，實際為 {avgConfidence:P1}");

        var highConfidenceCount = validRegions.Count(r => r.Confidence >= 0.7f);
        Assert.True(highConfidenceCount > 0, "應該至少有一個高信心度區域");
    }

    [Fact]
    public void RecognizeText_WithValidImage_ShouldHaveValidBoundingBoxes()
    {
        // Act
        var result = _ocrService.RecognizeText(_testImagePath);

        // Assert
        foreach (var region in result.TextRegions)
        {
            Assert.NotNull(region.BoundingBox);
            Assert.True(region.BoundingBox.Width > 0, "寬度應該大於0");
            Assert.True(region.BoundingBox.Height > 0, "高度應該大於0");
            Assert.NotEmpty(region.BoundingBox.Points);
        }
    }

    [Fact]
    public void RecognizeText_WithValidImage_ShouldHaveMostlyNonEmptyText()
    {
        // Act
        var result = _ocrService.RecognizeText(_testImagePath);

        // Assert
        // 至少80%的區域應該有文字（允許部分區域偵測到但無法識別）
        var nonEmptyCount = result.TextRegions.Count(r => !string.IsNullOrWhiteSpace(r.Text));
        var emptyCount = result.TextRegions.Count - nonEmptyCount;
        var nonEmptyPercentage = (double)nonEmptyCount / result.TextRegions.Count;

        Assert.True(nonEmptyPercentage >= 0.8,
            $"至少80%的區域應該有文字，實際: {nonEmptyPercentage:P1} ({nonEmptyCount}/{result.TextRegions.Count}), 空白區域: {emptyCount}");

        Assert.False(string.IsNullOrWhiteSpace(result.FullText), "完整文字不應為空白");
    }

    [Fact]
    public void RecognizeText_WithNullPath_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ocrService.RecognizeText(null!));
    }

    [Fact]
    public void RecognizeText_WithEmptyPath_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ocrService.RecognizeText(""));
    }

    [Fact]
    public void RecognizeText_WithNonExistentFile_ShouldThrowException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_image.png");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _ocrService.RecognizeText(nonExistentPath));
    }

    [Fact]
    public async Task RecognizeTextAsync_WithValidImage_ShouldReturnSuccess()
    {
        // Act
        var result = await _ocrService.RecognizeTextAsync(_testImagePath);

        // Assert
        Assert.True(result.Success, "非同步OCR識別應該成功");
        Assert.NotNull(result.TextRegions);
        Assert.NotEmpty(result.TextRegions);
    }

    [Fact]
    public async Task RecognizeTextAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _ocrService.RecognizeTextAsync(_testImagePath, cts.Token);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void RecognizeText_WithDisposedService_ShouldThrowException()
    {
        // Arrange
        var disposableService = new PaddleOcrService();
        disposableService.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => disposableService.RecognizeText(_testImagePath));
    }

    [Fact]
    public void Constructor_WithPreprocessingEnabled_ShouldInitialize()
    {
        // Act & Assert
        using var service = new PaddleOcrService(usePreprocessing: true);
        var result = service.RecognizeText(_testImagePath);
        Assert.True(result.Success);
    }

    [Fact]
    public void Constructor_WithPreprocessingDisabled_ShouldInitialize()
    {
        // Act & Assert
        using var service = new PaddleOcrService(usePreprocessing: false);
        var result = service.RecognizeText(_testImagePath);
        Assert.True(result.Success);
    }

    public void Dispose()
    {
        _ocrService?.Dispose();
    }
}
