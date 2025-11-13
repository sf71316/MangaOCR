namespace MangaOCR.Tests;

using Sdcb.PaddleOCR;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// 驗證已安裝的NuGet套件是否正常運作
/// </summary>
public class PackageVerificationTests
{
    [Fact]
    public void PaddleOCR_Package_ShouldBeInstalled()
    {
        // Arrange & Act
        var ocrType = typeof(PaddleOcrAll);

        // Assert
        Assert.NotNull(ocrType);
    }

    [Fact]
    public void ImageSharp_Package_ShouldBeInstalled()
    {
        // Arrange & Act
        var imageType = typeof(Image);

        // Assert
        Assert.NotNull(imageType);
    }

    [Fact]
    public void ImageSharp_CanCreateImage()
    {
        // Arrange
        const int width = 100;
        const int height = 100;

        // Act
        using var image = new Image<Rgba32>(width, height);

        // Assert
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
    }

    [Fact]
    public void OpenCvSharp_Package_ShouldBeAvailable()
    {
        // Arrange & Act - OpenCvSharp是PaddleOCR的依賴
        var matType = typeof(OpenCvSharp.Mat);

        // Assert
        Assert.NotNull(matType);
    }
}
