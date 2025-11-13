using MangaOCR.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MangaOCR.Tests.Services;

/// <summary>
/// ImageLoader 單元測試
/// </summary>
public class ImageLoaderTests : IDisposable
{
    private readonly ImageLoader _imageLoader;
    private readonly string _testImagePath;
    private readonly List<string> _tempFiles;

    public ImageLoaderTests()
    {
        _imageLoader = new ImageLoader();
        _tempFiles = new List<string>();

        // 建立測試用的臨時圖片
        _testImagePath = CreateTestImage(100, 100);
    }

    [Fact]
    public void LoadImage_WithValidJpgFile_ShouldSucceed()
    {
        // Arrange
        var jpgPath = CreateTestImage(50, 50, "test.jpg");

        // Act
        using var image = _imageLoader.LoadImage(jpgPath);

        // Assert
        Assert.NotNull(image);
        Assert.Equal(50, image.Width);
        Assert.Equal(50, image.Height);
    }

    [Fact]
    public void LoadImage_WithValidPngFile_ShouldSucceed()
    {
        // Arrange
        var pngPath = CreateTestImage(75, 75, "test.png");

        // Act
        using var image = _imageLoader.LoadImage(pngPath);

        // Assert
        Assert.NotNull(image);
        Assert.Equal(75, image.Width);
        Assert.Equal(75, image.Height);
    }

    [Fact]
    public void LoadImage_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => _imageLoader.LoadImage(null!));

        Assert.Contains("檔案路徑不可為空", exception.Message);
    }

    [Fact]
    public void LoadImage_WithEmptyPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => _imageLoader.LoadImage(string.Empty));

        Assert.Contains("檔案路徑不可為空", exception.Message);
    }

    [Fact]
    public void LoadImage_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.jpg");

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(
            () => _imageLoader.LoadImage(nonExistentPath));

        Assert.Contains("找不到檔案", exception.Message);
    }

    [Fact]
    public void LoadImage_WithUnsupportedFormat_ShouldThrowNotSupportedException()
    {
        // Arrange
        var txtPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
        File.WriteAllText(txtPath, "test");
        _tempFiles.Add(txtPath);

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(
            () => _imageLoader.LoadImage(txtPath));

        Assert.Contains("不支援的檔案格式", exception.Message);
    }

    [Fact]
    public async Task LoadImageAsync_WithValidFile_ShouldSucceed()
    {
        // Act
        using var image = await _imageLoader.LoadImageAsync(_testImagePath);

        // Assert
        Assert.NotNull(image);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
    }

    [Fact]
    public async Task LoadImageAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _imageLoader.LoadImageAsync(_testImagePath, cts.Token));
    }

    [Theory]
    [InlineData("test.jpg", true)]
    [InlineData("test.jpeg", true)]
    [InlineData("test.png", true)]
    [InlineData("test.bmp", true)]
    [InlineData("test.gif", true)]
    [InlineData("test.webp", true)]
    [InlineData("test.tiff", true)]
    [InlineData("test.tif", true)]
    [InlineData("test.txt", false)]
    [InlineData("test.pdf", false)]
    [InlineData("test.doc", false)]
    [InlineData("TEST.JPG", true)] // 測試大小寫不敏感
    public void IsSupportedFormat_WithVariousExtensions_ShouldReturnExpected(
        string fileName, bool expected)
    {
        // Act
        var result = _imageLoader.IsSupportedFormat(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSupportedFormat_WithNullPath_ShouldReturnFalse()
    {
        // Act
        var result = _imageLoader.IsSupportedFormat(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSupportedFormat_WithEmptyPath_ShouldReturnFalse()
    {
        // Act
        var result = _imageLoader.IsSupportedFormat(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetSupportedFormats_ShouldReturnExpectedFormats()
    {
        // Act
        var formats = _imageLoader.GetSupportedFormats();

        // Assert
        Assert.NotNull(formats);
        Assert.NotEmpty(formats);
        Assert.Contains(".jpg", formats);
        Assert.Contains(".png", formats);
        Assert.Contains(".webp", formats);
    }

    /// <summary>
    /// 建立測試用的圖片檔案
    /// </summary>
    private string CreateTestImage(int width, int height, string? fileName = null)
    {
        fileName ??= $"test_{Guid.NewGuid()}.jpg";
        var path = Path.Combine(Path.GetTempPath(), fileName);

        using (var image = new Image<Rgba32>(width, height))
        {
            // 填充顏色讓圖片有內容
            image.Mutate(ctx => ctx.BackgroundColor(Color.Blue));
            image.SaveAsJpeg(path);
        }

        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        // 清理測試產生的臨時檔案
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // 忽略清理錯誤
            }
        }
    }
}
