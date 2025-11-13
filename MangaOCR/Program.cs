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

    // 分析OCR原始順序
    Console.WriteLine();
    Console.WriteLine("=== OCR原始返回順序分析 ===");
    var orderAnalyzer = new TextOrderAnalyzer();
    Console.WriteLine(orderAnalyzer.AnalyzeOriginalOrder(rawResult.TextRegions.Take(10).ToList()));

    // 跨頁檢測診斷
    Console.WriteLine();
    Console.WriteLine(orderAnalyzer.DiagnosePageDetection(processedResult.TextRegions));

    // 顯示所有區域的X座標
    Console.WriteLine();
    Console.WriteLine("=== 所有文字區域X座標分布 ===");
    var sortedByX = processedResult.TextRegions.OrderBy(r => r.BoundingBox.X).ToList();
    foreach (var region in sortedByX)
    {
        Console.WriteLine($"X={region.BoundingBox.X:D4}: {region.Text}");
    }
    Console.WriteLine($"X範圍: {sortedByX.First().BoundingBox.X} ~ {sortedByX.Last().BoundingBox.X + sortedByX.Last().BoundingBox.Width}");

    // 測試不同的閱讀順序
    Console.WriteLine("=== 閱讀順序測試 ===");
    Console.WriteLine();

    Console.WriteLine("【順序1：從左到右、從上到下（英文模式）】");
    var leftToRight = orderAnalyzer.SortByReadingOrder(
        processedResult.TextRegions,
        TextOrderAnalyzer.ReadingDirection.LeftToRightTopToBottom);
    Console.WriteLine(string.Join(" → ", leftToRight.Take(10).Select(r => r.Text)));

    Console.WriteLine();
    Console.WriteLine("【順序2：從右到左、從上到下（日文漫畫模式）】");
    var rightToLeft = orderAnalyzer.SortByReadingOrder(
        processedResult.TextRegions,
        TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);
    Console.WriteLine(string.Join(" → ", rightToLeft.Take(10).Select(r => r.Text)));

    Console.WriteLine();
    Console.WriteLine("【順序3：從上到下、從右到左（直排模式）】");
    var topToBottom = orderAnalyzer.SortByReadingOrder(
        processedResult.TextRegions,
        TextOrderAnalyzer.ReadingDirection.TopToBottomRightToLeft);
    Console.WriteLine(string.Join(" → ", topToBottom.Take(10).Select(r => r.Text)));

    Console.WriteLine();
    Console.WriteLine("【順序4：自動檢測】");
    var autoDetect = orderAnalyzer.SortByReadingOrder(
        processedResult.TextRegions,
        TextOrderAnalyzer.ReadingDirection.Auto);
    Console.WriteLine(string.Join(" → ", autoDetect.Take(10).Select(r => r.Text)));

    Console.WriteLine();
    Console.WriteLine("=== 推薦的閱讀順序（日文漫畫：從右到左、從上到下）===");
    var orderedRegions = orderAnalyzer.AssignReadingOrder(
        processedResult.TextRegions,
        TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);

    foreach (var (region, order) in orderedRegions.Take(15))
    {
        Console.WriteLine($"[{order:D2}] {region.Text} (信心度: {region.Confidence:P1})");
    }

    if (orderedRegions.Count > 15)
    {
        Console.WriteLine($"... 還有 {orderedRegions.Count - 15} 個區域");
    }

    // 生成標註圖片
    Console.WriteLine();
    Console.WriteLine("=== 生成視覺化標註圖片 ===");
    var annotator = new ImageAnnotator();

    var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "TestData");
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var readingOrderOutput = Path.Combine(outputDir, $"4_reading_order_{timestamp}.png");
    var confidenceOutput = Path.Combine(outputDir, $"4_confidence_{timestamp}.png");

    try
    {
        // 標註閱讀順序（使用後處理後的結果）
        annotator.AnnotateReadingOrder(testImagePath, orderedRegions, readingOrderOutput);

        // 標註信心度（使用原始結果，展示所有區域）
        annotator.AnnotateConfidence(testImagePath, rawResult.TextRegions, confidenceOutput);

        Console.WriteLine();
        Console.WriteLine("生成的圖片：");
        Console.WriteLine($"  1. 閱讀順序標註: {Path.GetFileName(readingOrderOutput)}");
        Console.WriteLine($"  2. 信心度標註: {Path.GetFileName(confidenceOutput)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ 生成標註圖片失敗: {ex.Message}");
    }
}
else
{
    Console.WriteLine($"✗ OCR識別失敗: {rawResult.ErrorMessage}");
}

Console.WriteLine();
Console.WriteLine("=== 程式執行完成 ===");
