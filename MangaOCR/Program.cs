/*
 * MangaOCR - 漫畫文字識別工具
 *
 * 這是一個使用範例。所有功能測試都已移至 MangaOCR.Tests 專案。
 * 請執行以下命令來測試功能：
 *
 *   cd MangaOCR.Tests
 *   dotnet test
 *
 * 用法範例：
 *
 * using MangaOCR.Factories;
 * using MangaOCR.Models;
 * using MangaOCR.Services;
 * using Microsoft.Extensions.Configuration;
 *
 * // 1. 載入配置
 * var configuration = new ConfigurationBuilder()
 *     .AddJsonFile("appsettings.json")
 *     .Build();
 * var settings = configuration.GetSection("OcrSettings").Get<OcrSettings>();
 *
 * // 2. 創建 OCR 服務
 * var factory = new OcrServiceFactory();
 * using var ocrService = factory.CreateOcrService(settings);
 *
 * // 3. 執行 OCR 識別
 * var result = ocrService.RecognizeText("path/to/image.png");
 *
 * // 4. 後處理結果
 * var processor = new ResultProcessor();
 * var processedResult = processor.Process(result, minConfidence: 0.5f);
 *
 * // 5. 分析閱讀順序（日文漫畫：從右到左、從上到下）
 * var orderAnalyzer = new TextOrderAnalyzer();
 * var orderedRegions = orderAnalyzer.AssignReadingOrder(
 *     processedResult.TextRegions,
 *     TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);
 *
 * // 6. 生成視覺化標註圖片
 * var annotator = new ImageAnnotator();
 * annotator.AnnotateReadingOrder("input.png", orderedRegions, "output_order.png");
 * annotator.AnnotateConfidence("input.png", result.TextRegions, "output_confidence.png");
 *
 * 參考文件：
 * - 完整測試範例：MangaOCR.Tests/OcrIntegrationTests.cs
 * - 配置說明：appsettings.json
 * - API 文件：查看各服務類別的 XML 註解
 */

Console.WriteLine("MangaOCR 核心功能已移至 MangaOCR.Core 類別庫。");
Console.WriteLine("請參考上方註解中的用法範例。");
Console.WriteLine();
Console.WriteLine("執行單元測試：");
Console.WriteLine("  cd MangaOCR.Tests");
Console.WriteLine("  dotnet test");
