using MangaOCR.Services;

namespace MangaOCR.Tests;

/// <summary>
/// 測試從TestData目錄載入實際的測試圖片
/// </summary>
public class TestImageLoadFromTestData
{
    [Fact]
    public void LoadImage_FromTestDataDirectory_ShouldSucceed()
    {
        // Arrange
        var imageLoader = new ImageLoader();

        // 尋找TestData目錄中的測試圖片
        var projectRoot = FindProjectRoot();
        var testImagePath = Path.Combine(projectRoot, "TestData", "4.png");

        // 如果4.png不存在，嘗試4.jpg
        if (!File.Exists(testImagePath))
        {
            testImagePath = Path.Combine(projectRoot, "TestData", "4.jpg");
        }

        // 如果測試圖片不存在，跳過此測試
        if (!File.Exists(testImagePath))
        {
            // 使用Skip annotation會更好，但這裡我們用Assert.True並記錄訊息
            Assert.True(File.Exists(testImagePath),
                $"測試圖片不存在: {testImagePath}。請將測試圖片放到 TestData 目錄下。");
            return;
        }

        // Act
        using var image = imageLoader.LoadImage(testImagePath);

        // Assert
        Assert.NotNull(image);
        Assert.True(image.Width > 0, "圖片寬度應大於0");
        Assert.True(image.Height > 0, "圖片高度應大於0");

        // 輸出圖片資訊（在測試輸出中可見）
        Console.WriteLine($"✓ 成功載入測試圖片: {testImagePath}");
        Console.WriteLine($"  尺寸: {image.Width} x {image.Height}");
    }

    /// <summary>
    /// 尋找專案根目錄（包含TestData的目錄）
    /// </summary>
    private string FindProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // 從當前目錄往上找，直到找到包含TestData的目錄
        while (currentDir != null)
        {
            var testDataPath = Path.Combine(currentDir, "TestData");
            if (Directory.Exists(testDataPath))
            {
                return currentDir;
            }

            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        // 如果找不到，返回當前目錄的上四層（通常是從 bin/Debug/net9.0 到專案根目錄）
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
    }
}
