/*
 * MangaOCR - 漫畫文字識別工具
 *
 * 這是一個使用範例。所有功能測試都已移至 MangaOCR.Tests 專案。
 * 請執行以下命令來測試功能：
 *
 *   cd MangaOCR.Tests
 *   dotnet test
 *
 * ═══════════════════════════════════════════════════════════════════
 * 【推薦】快速開始 - 使用自適應 OCR（預設）
 * ═══════════════════════════════════════════════════════════════════
 *
 * using MangaOCR.Services;
 *
 * // 最簡單的方式：使用預設配置（日文漫畫 + 自適應參數）
 * using var ocr = MangaOcrService.CreateDefault();
 * var result = ocr.RecognizeText("image.png");
 *
 * // 顯示識別結果
 * foreach (var region in result.TextRegions)
 * {
 *     Console.WriteLine($"{region.Text} (信心度: {region.Confidence:P1})");
 * }
 *
 * ═══════════════════════════════════════════════════════════════════
 * 【進階】自適應模式 - 自動分析圖像質量並推薦最佳參數
 * ═══════════════════════════════════════════════════════════════════
 *
 * using MangaOCR.Models;
 * using MangaOCR.Services;
 *
 * // 方法 1：使用預設自適應模式
 * using var adaptiveOcr = MangaOcrService.Create();
 * var result1 = adaptiveOcr.RecognizeText("image.png");
 *
 * // 方法 2：使用自訂設定的自適應模式
 * var settings = new OcrSettings
 * {
 *     Language = "Japanese",
 *     MinConfidence = 0.5f,
 *     UsePreprocessing = false
 * };
 * using var customAdaptiveOcr = MangaOcrService.CreateAdaptive(settings);
 * var result2 = customAdaptiveOcr.RecognizeText("image.png", verbose: true);
 *
 * // 方法 3：查看推薦參數說明
 * var explanation = customAdaptiveOcr.GetRecommendationExplanation("image.png");
 * Console.WriteLine(explanation);
 *
 * ═══════════════════════════════════════════════════════════════════
 * 【標準】固定參數模式 - 使用預設標準參數
 * ═══════════════════════════════════════════════════════════════════
 *
 * using MangaOCR.Models;
 * using MangaOCR.Services;
 *
 * var settings = new OcrSettings
 * {
 *     Language = "Japanese",
 *     MinConfidence = 0.5f,
 *     MaxSize = 1024,
 *     UnclipRatio = 1.5f,
 *     BoxScoreThreshold = 0.6f
 * };
 *
 * using var standardOcr = MangaOcrService.CreateStandard(settings);
 * var result = standardOcr.RecognizeText("image.png");
 *
 * ═══════════════════════════════════════════════════════════════════
 * 【完整】完整工作流程範例
 * ═══════════════════════════════════════════════════════════════════
 *
 * using MangaOCR.Services;
 *
 * // 1. 創建自適應 OCR 服務（推薦）
 * using var ocr = MangaOcrService.CreateDefault();
 *
 * // 2. 執行 OCR 識別（verbose=true 顯示診斷資訊）
 * var result = ocr.RecognizeText("manga_page.png", verbose: true);
 *
 * // 3. 後處理結果（過濾低信心度區域）
 * var processor = new ResultProcessor();
 * var processedResult = processor.Process(result, minConfidence: 0.5f);
 *
 * // 4. 分析閱讀順序（日文漫畫：從右到左、從上到下）
 * var orderAnalyzer = new TextOrderAnalyzer();
 * var orderedRegions = orderAnalyzer.AssignReadingOrder(
 *     processedResult.TextRegions,
 *     TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);
 *
 * // 5. 生成視覺化標註圖片
 * var annotator = new ImageAnnotator();
 * annotator.AnnotateReadingOrder("manga_page.png", orderedRegions, "output_order.png");
 * annotator.AnnotateConfidence("manga_page.png", result.TextRegions, "output_confidence.png");
 *
 * // 6. 輸出識別文字（按閱讀順序）
 * foreach (var region in orderedRegions.OrderBy(r => r.ReadingOrder))
 * {
 *     Console.WriteLine($"[{region.ReadingOrder}] {region.Text} ({region.Confidence:P1})");
 * }
 *
 * ═══════════════════════════════════════════════════════════════════
 * 模式選擇建議
 * ═══════════════════════════════════════════════════════════════════
 *
 * 🎯 自適應模式（Adaptive）- 預設推薦
 *    - 優點：自動分析圖像質量並選擇最佳參數
 *    - 適用：大多數使用場景，特別是圖像質量不一致時
 *    - 速度：平均快 38%（相同辨識率）
 *    - 使用：MangaOcrService.Create() 或 CreateAdaptive()
 *
 * 📐 標準模式（Standard）
 *    - 優點：可預測、參數固定
 *    - 適用：圖像質量穩定、需要參數一致性時
 *    - 使用：MangaOcrService.CreateStandard(settings)
 *
 * 參考文件：
 * - 完整測試範例：MangaOCR.Tests/OcrIntegrationTests.cs
 * - 自適應測試：MangaOCR.Tests/AdaptiveOcrComparisonTests.cs
 * - 配置說明：appsettings.json
 * - API 文件：查看各服務類別的 XML 註解
 */

Console.WriteLine("MangaOCR 核心功能已移至 MangaOCR.Core 類別庫。");
Console.WriteLine("請參考上方註解中的用法範例。");
Console.WriteLine();
Console.WriteLine("執行單元測試：");
Console.WriteLine("  cd MangaOCR.Tests");
Console.WriteLine("  dotnet test");
