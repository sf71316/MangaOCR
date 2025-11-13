using MangaOCR.Services;

Console.WriteLine("=== MangaOCR - 漫畫文字識別工具 ===");
Console.WriteLine();

// 檢查測試圖片
var possiblePaths = new[]
{
    // 從專案根目錄執行時
    Path.Combine(Directory.GetCurrentDirectory(), "TestData", "4.png"),
    Path.Combine(Directory.GetCurrentDirectory(), "TestData", "4.jpg"),
    // 從MangaOCR目錄執行時
    Path.Combine(Directory.GetCurrentDirectory(), "..", "TestData", "4.png"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", "TestData", "4.jpg"),
    // 從bin目錄執行時
    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestData", "4.png"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestData", "4.jpg"),
};

string? testImagePath = null;
foreach (var path in possiblePaths)
{
    var normalizedPath = Path.GetFullPath(path);
    if (File.Exists(normalizedPath))
    {
        testImagePath = normalizedPath;
        break;
    }
}

if (testImagePath == null)
{
    Console.WriteLine("✗ 找不到測試圖片");
    Console.WriteLine("請將測試圖片 (4.jpg 或 4.png) 放到 TestData 目錄下");
    Console.WriteLine($"當前工作目錄: {Directory.GetCurrentDirectory()}");

    // 顯示嘗試的路徑
    Console.WriteLine("\n嘗試過的路徑:");
    foreach (var path in possiblePaths)
    {
        Console.WriteLine($"  - {Path.GetFullPath(path)} (存在: {File.Exists(Path.GetFullPath(path))})");
    }
    return;
}

Console.WriteLine($"✓ 找到測試圖片: {Path.GetFileName(testImagePath)}");
Console.WriteLine();

// 初始化OCR服務（使用原圖，不使用預處理）
Console.WriteLine("正在初始化PaddleOCR服務...");
Console.WriteLine("（首次使用會下載日文模型，請稍候）");
using var ocrService = new PaddleOcrService();
Console.WriteLine("✓ OCR服務初始化完成");
Console.WriteLine();

// 執行OCR
Console.WriteLine("正在識別圖片中的文字...");
var rawResult = ocrService.RecognizeText(testImagePath);

if (rawResult.Success)
{
    Console.WriteLine($"✓ OCR識別完成！耗時: {rawResult.ElapsedMilliseconds}ms");
    Console.WriteLine();

    // 顯示原始結果統計
    Console.WriteLine("【原始識別結果】");
    Console.WriteLine($"  識別區域數: {rawResult.TextRegions.Count}");
    var validRegions = rawResult.TextRegions.Where(r => !float.IsNaN(r.Confidence)).ToList();
    if (validRegions.Any())
    {
        var avgConfidence = validRegions.Average(r => r.Confidence);
        Console.WriteLine($"  平均信心度: {avgConfidence:P1}");
        var highConfidenceCount = validRegions.Count(r => r.Confidence >= 0.7f);
        Console.WriteLine($"  高信心度區域(>=70%): {highConfidenceCount}");
    }
    var nonEmptyCount = rawResult.TextRegions.Count(r => !string.IsNullOrWhiteSpace(r.Text));
    Console.WriteLine($"  非空白區域: {nonEmptyCount}/{rawResult.TextRegions.Count}");

    // 應用結果後處理
    Console.WriteLine();
    Console.WriteLine("正在應用結果後處理...");
    var processor = new ResultProcessor();
    var processedResult = processor.Process(rawResult, minConfidence: 0.6f);

    Console.WriteLine();
    Console.WriteLine("【後處理結果】");
    Console.WriteLine($"  識別區域數: {processedResult.TextRegions.Count}");
    if (processedResult.TextRegions.Any())
    {
        var avgConfidenceProcessed = processedResult.TextRegions.Where(r => !float.IsNaN(r.Confidence)).Average(r => r.Confidence);
        Console.WriteLine($"  平均信心度: {avgConfidenceProcessed:P1}");
        var highConfidenceCountProcessed = processedResult.TextRegions.Count(r => r.Confidence >= 0.7f);
        Console.WriteLine($"  高信心度區域(>=70%): {highConfidenceCountProcessed}");
    }

    // 顯示改善效果
    var removed = rawResult.TextRegions.Count - processedResult.TextRegions.Count;
    Console.WriteLine($"  已過濾: {removed} 個低品質區域");

    Console.WriteLine();
    Console.WriteLine("=== 所有識別結果（按圖片位置從上到下排列）===");
    Console.WriteLine("圖例：✓高信心度(>=70%) ◆中信心度(50-70%) ✗低信心度(<50%) ⚠失敗/空白");
    Console.WriteLine();

    // 按Y座標排序（從上到下），然後按X座標（從左到右）
    var sortedRegions = rawResult.TextRegions
        .OrderBy(r => r.BoundingBox.Y)
        .ThenBy(r => r.BoundingBox.X)
        .ToList();

    int index = 1;
    foreach (var region in sortedRegions)
    {
        string status;
        string statusIcon;

        if (string.IsNullOrWhiteSpace(region.Text))
        {
            status = "失敗/空白";
            statusIcon = "⚠";
        }
        else if (float.IsNaN(region.Confidence))
        {
            status = "無信心度";
            statusIcon = "⚠";
        }
        else if (region.Confidence >= 0.7f)
        {
            status = "高信心度";
            statusIcon = "✓";
        }
        else if (region.Confidence >= 0.5f)
        {
            status = "中信心度";
            statusIcon = "◆";
        }
        else
        {
            status = "低信心度";
            statusIcon = "✗";
        }

        var confidenceStr = float.IsNaN(region.Confidence) ? "N/A" : $"{region.Confidence:P1}";
        var textDisplay = string.IsNullOrWhiteSpace(region.Text) ? "[空白]" : region.Text;

        Console.WriteLine($"[{index:D2}] {statusIcon} {textDisplay}");
        Console.WriteLine($"      信心度: {confidenceStr} ({status})");
        Console.WriteLine($"      位置: X={region.BoundingBox.X}, Y={region.BoundingBox.Y}, W={region.BoundingBox.Width}, H={region.BoundingBox.Height}");
        Console.WriteLine();
        index++;
    }

    Console.WriteLine("=== 統計摘要 ===");
    var highCount = sortedRegions.Count(r => !float.IsNaN(r.Confidence) && r.Confidence >= 0.7f);
    var mediumCount = sortedRegions.Count(r => !float.IsNaN(r.Confidence) && r.Confidence >= 0.5f && r.Confidence < 0.7f);
    var lowCount = sortedRegions.Count(r => !float.IsNaN(r.Confidence) && r.Confidence < 0.5f);
    var failedCount = sortedRegions.Count(r => string.IsNullOrWhiteSpace(r.Text) || float.IsNaN(r.Confidence));

    Console.WriteLine($"✓ 高信心度(>=70%): {highCount} 個");
    Console.WriteLine($"◆ 中信心度(50-70%): {mediumCount} 個");
    Console.WriteLine($"✗ 低信心度(<50%): {lowCount} 個");
    Console.WriteLine($"⚠ 失敗/空白: {failedCount} 個");
    Console.WriteLine($"總計: {sortedRegions.Count} 個區域");

    Console.WriteLine();
    Console.WriteLine("=== 後處理後的完整文字 ===");
    Console.WriteLine(processedResult.FullText);
}
else
{
    Console.WriteLine($"✗ OCR識別失敗: {rawResult.ErrorMessage}");
}

Console.WriteLine();
Console.WriteLine("=== 程式執行完成 ===");
