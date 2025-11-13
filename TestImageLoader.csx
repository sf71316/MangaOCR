#!/usr/bin/env dotnet-script
#r "nuget: SixLabors.ImageSharp, 3.1.12"

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

// 測試圖片路徑
var testImagePath = Path.GetFullPath("TestData/4.png");
Console.WriteLine($"測試圖片路徑: {testImagePath}");
Console.WriteLine($"檔案存在: {File.Exists(testImagePath)}");

if (File.Exists(testImagePath))
{
    try
    {
        using var image = Image.Load<Rgba32>(testImagePath);
        Console.WriteLine($"✓ 成功載入影像");
        Console.WriteLine($"  尺寸: {image.Width} x {image.Height}");
        Console.WriteLine($"  格式: {Path.GetExtension(testImagePath)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ 載入失敗: {ex.Message}");
    }
}
