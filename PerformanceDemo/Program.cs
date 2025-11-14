using System.Diagnostics;
using MangaOCR.Models;
using MangaOCR.Services;

Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║       MangaOCR.Core 效能優化成效展示                          ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// 尋找測試圖片
var testImagePath = FindTestImage("4.png");
Console.WriteLine($"✓ 找到測試圖片: {Path.GetFileName(testImagePath)}");
Console.WriteLine($"  檔案大小: {new FileInfo(testImagePath).Length / 1024}KB");
Console.WriteLine();

// 準備測試數據（模擬 20 張圖片）
var imagePaths = Enumerable.Repeat(testImagePath, 20).ToList();
Console.WriteLine($"準備測試: {imagePaths.Count} 張圖片");
Console.WriteLine($"CPU 核心數: {Environment.ProcessorCount}");
Console.WriteLine($"預設最大線程數: {Environment.ProcessorCount / 2}");
Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();

using var ocr = MangaOcrService.CreateDefault();

// 測試 1: 循序批次處理（舊方法）
Console.WriteLine("【測試 1】循序批次處理 (RecognizeTextBatch)");
Console.WriteLine("─────────────────────────────────────────────────────────────");
var sw = Stopwatch.StartNew();
var sequentialResults = ocr.RecognizeTextBatch(imagePaths);
sw.Stop();
var sequentialTime = sw.ElapsedMilliseconds;

Console.WriteLine($"✓ 完成時間: {sequentialTime}ms");
Console.WriteLine($"  成功: {sequentialResults.Count(r => r.Success)}/{sequentialResults.Count}");
Console.WriteLine($"  平均每張: {sequentialTime / imagePaths.Count}ms");
Console.WriteLine();

// 測試 2: 平行批次處理（新方法，預設設定）
Console.WriteLine("【測試 2】平行批次處理 - 預設設定");
Console.WriteLine("─────────────────────────────────────────────────────────────");

var logMessages = new List<string>();
var progressUpdates = new List<string>();

ocr.LogMessage += (sender, e) =>
{
    if (e.Level >= OcrLogLevel.Information)
    {
        var color = e.Level switch
        {
            OcrLogLevel.Error => ConsoleColor.Red,
            OcrLogLevel.Warning => ConsoleColor.Yellow,
            OcrLogLevel.Information => ConsoleColor.Green,
            _ => ConsoleColor.Gray
        };
        Console.ForegroundColor = color;
        Console.WriteLine($"  [{e.Level}] {e.Message}");
        Console.ResetColor();
        logMessages.Add(e.Message);
    }
};

var progressCount = 0;
ocr.ProgressChanged += (sender, e) =>
{
    progressUpdates.Add($"{e.Percentage:P0}");
    if (progressCount++ % 5 == 0)  // 每 5 次顯示一次
    {
        Console.WriteLine($"  進度: {e.Current}/{e.Total} ({e.Percentage:P0})");
    }
};

sw.Restart();
var parallelResults = ocr.RecognizeTextBatchParallel(imagePaths);
sw.Stop();
var parallelTime = sw.ElapsedMilliseconds;

Console.WriteLine($"✓ 完成時間: {parallelTime}ms");
Console.WriteLine($"  成功: {parallelResults.Count(r => r.Success)}/{parallelResults.Count}");
Console.WriteLine($"  平均每張: {parallelTime / imagePaths.Count}ms");
Console.WriteLine($"  收到日誌: {logMessages.Count} 條");
Console.WriteLine($"  進度更新: {progressUpdates.Count} 次");
Console.WriteLine();

// 測試 3: 平行批次處理（自訂線程數）
Console.WriteLine("【測試 3】平行批次處理 - 自訂 4 線程 + 智能排程");
Console.WriteLine("─────────────────────────────────────────────────────────────");

var options = new BatchProcessingOptions
{
    MaxDegreeOfParallelism = 4,
    EnableSmartScheduling = true,
    LargeFileSizeThreshold = 500_000  // 500KB
};

sw.Restart();
var customResults = ocr.RecognizeTextBatchParallel(imagePaths, options);
sw.Stop();
var customTime = sw.ElapsedMilliseconds;

Console.WriteLine($"✓ 完成時間: {customTime}ms");
Console.WriteLine($"  成功: {customResults.Count(r => r.Success)}/{customResults.Count}");
Console.WriteLine($"  平均每張: {customTime / imagePaths.Count}ms");
Console.WriteLine();

// 效能總結
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("【效能總結】");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();

var speedup1 = (double)sequentialTime / parallelTime;
var speedup2 = (double)sequentialTime / customTime;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"循序處理:           {sequentialTime,6}ms  (基準)");
Console.WriteLine($"平行處理 (預設):    {parallelTime,6}ms  (快 {speedup1:F2}x) ⚡");
Console.WriteLine($"平行處理 (4線程):   {customTime,6}ms  (快 {speedup2:F2}x) ⚡⚡");
Console.ResetColor();
Console.WriteLine();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"✓ 效能提升: {(sequentialTime - parallelTime) / (double)sequentialTime:P1}");
Console.ResetColor();
Console.WriteLine();

// 功能展示
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("【新功能展示】");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();

Console.WriteLine("1. ✓ 平行批次處理");
Console.WriteLine($"   - 預設最大線程數: CPU 核心數 / 2 = {Environment.ProcessorCount / 2}");
Console.WriteLine("   - 可自訂線程數");
Console.WriteLine();

Console.WriteLine("2. ✓ 智能排程");
Console.WriteLine("   - 大檔案優先處理");
Console.WriteLine("   - 避免最後等待大檔案");
Console.WriteLine();

Console.WriteLine("3. ✓ 事件驅動");
Console.WriteLine($"   - LogMessage 事件: 收到 {logMessages.Count} 條日誌");
Console.WriteLine($"   - ProgressChanged 事件: {progressUpdates.Count} 次進度更新");
Console.WriteLine("   - 使用者可自行決定如何收集資料");
Console.WriteLine();

Console.WriteLine("4. ✓ 取消支援");
Console.WriteLine("   - 支援 CancellationToken");
Console.WriteLine("   - 可隨時中斷處理");
Console.WriteLine();

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();

// Helper function
static string FindTestImage(string fileName)
{
    var possiblePaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), "..", "TestData", fileName),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "TestData", fileName),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestData", fileName),
    };

    foreach (var path in possiblePaths)
    {
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }
    }

    throw new FileNotFoundException($"找不到測試圖片: {fileName}");
}
