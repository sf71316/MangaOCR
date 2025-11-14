# MangaOCR.Core

專為漫畫文字識別優化的 .NET OCR 類別庫，基於 PaddleOCR v3.0.1 實現。

## 特色功能

- ✨ **自適應 OCR**：自動分析圖像質量並選擇最佳參數（預設模式）
- 🚀 **高性能**：自適應模式平均快 38%，辨識率維持相同水準
- ⚡ **檢測識別分離**：支援只檢測座標、只識別文字、批次處理（速度提升 60+ 倍）
- 🔥 **平行批次處理**：智能多線程處理，可自訂線程數（預設 CPU 核心數/2）
- 📈 **智能排程**：大檔案優先處理，優化整體處理時間
- 📡 **事件驅動**：即時日誌和進度回報，使用者自行決定如何收集資料
- 🎯 **專為漫畫優化**：預設配置針對日文漫畫場景調整
- 📊 **結果後處理**：自動過濾低信心度區域、分析閱讀順序
- 🔍 **視覺化除錯**：生成標註圖片（信心度熱圖、閱讀順序）

---

## 快速開始

### 安裝

```bash
dotnet add reference MangaOCR.Core
```

### 最簡單的使用方式

```csharp
using MangaOCR.Services;

// 一行程式碼搞定（日文漫畫 + 自適應參數）
using var ocr = MangaOcrService.CreateDefault();
var result = ocr.RecognizeText("manga_page.png");

// 顯示識別結果
foreach (var region in result.TextRegions)
{
    Console.WriteLine($"{region.Text} (信心度: {region.Confidence:P1})");
}
```

---

## 工廠方法

`MangaOcrService` 提供多種工廠方法來創建 OCR 服務：

### 1. CreateDefault() - 預設配置（推薦）

```csharp
using var ocr = MangaOcrService.CreateDefault();
```

**配置內容**：
- 模式：自適應（Adaptive）
- 語言：Japanese
- MinConfidence: 0.5
- MaxSize: 1024
- UnclipRatio: 1.5
- BoxScoreThreshold: 0.6
- Threshold: 0.3

**適用場景**：日文漫畫識別，圖像質量不一致

---

### 2. Create() - 自適應模式（預設）

```csharp
using var ocr = MangaOcrService.Create();
```

等同於 `CreateAdaptive()`，使用自適應模式。

---

### 3. CreateAdaptive(settings) - 自訂自適應模式

```csharp
var settings = new OcrSettings
{
    Language = "Japanese",
    MinConfidence = 0.5f
    // 其他參數會自動根據圖像質量調整
};
using var ocr = MangaOcrService.CreateAdaptive(settings);
var result = ocr.RecognizeText("image.png", verbose: true); // verbose=true 顯示診斷資訊
```

**工作原理**：
1. 分析圖像質量（模糊度、對比度、亮度）
2. 根據質量等級自動推薦最佳參數
3. 執行 OCR 識別

**優點**：
- 無需手動調參
- 速度快（平均快 38%）
- 辨識率穩定

---

### 4. CreateStandard(settings) - 標準模式

```csharp
var settings = new OcrSettings
{
    Language = "Japanese",
    MinConfidence = 0.5f,
    MaxSize = 1024,
    UnclipRatio = 1.5f,
    BoxScoreThreshold = 0.6f,
    Threshold = 0.3f
};
using var ocr = MangaOcrService.CreateStandard(settings);
var result = ocr.RecognizeText("image.png");
```

**適用場景**：
- 圖像質量穩定
- 需要參數一致性
- 手動微調參數

---

## OCR 參數詳解

> ⚠️ **重要提醒**：此章節需與程式碼同步更新！
> 當 `OcrSettings` 有任何變更時，必須同步更新此文檔。

### 核心參數

| 參數 | 類型 | 預設值 | 影響 | 調整建議 |
|------|------|--------|------|----------|
| **Provider** | `OcrProvider` | `PaddleOCR` | OCR 引擎選擇 | 目前僅支援 PaddleOCR |
| **Language** | `string` | `"Japanese"` | 識別語言模型 | `"Japanese"`, `"Chinese"`, `"English"` |
| **MinConfidence** | `float` | `0.5f` | 後處理過濾閾值 | 提高可減少誤判，降低可增加召回率 |

---

### 檢測參數（Detection）

這些參數影響**文字區域檢測**的準確性：

#### MaxSize
- **類型**：`int`
- **預設值**：`1024`
- **影響**：圖像縮放的最大邊長
- **效果**：
  - ⬆️ **提高**：處理更多細節，但速度變慢
  - ⬇️ **降低**：速度更快，但可能漏檢小字
- **建議範圍**：`960` ~ `1920`
- **自適應模式調整**：
  - 高質量圖像：`1024`
  - 中質量圖像：`1280`
  - 低質量圖像：`1920`

#### UnclipRatio
- **類型**：`float`
- **預設值**：`1.5f`
- **影響**：文字區域邊界擴展比例
- **效果**：
  - ⬆️ **提高**：文字框更大，包含更多邊緣字符，但可能合併相鄰區域
  - ⬇️ **降低**：文字框更緊密，但可能截斷字符
- **建議範圍**：`1.2` ~ `2.0`
- **自適應模式調整**：
  - 高質量圖像：`1.5`
  - 中質量圖像：`1.8`
  - 低質量圖像：`2.0`

#### BoxScoreThreshold
- **類型**：`float`
- **預設值**：`0.6f`
- **影響**：文字區域檢測的信心度閾值
- **效果**：
  - ⬆️ **提高**：只檢測高信心度區域，減少誤判
  - ⬇️ **降低**：檢測更多可能的文字區域，增加召回率
- **建議範圍**：`0.3` ~ `0.8`
- **自適應模式調整**：
  - 高質量圖像：`0.6`
  - 中質量圖像：`0.55`
  - 低質量圖像：`0.5`

#### Threshold
- **類型**：`float`
- **預設值**：`0.3f`
- **影響**：二值化閾值（文字與背景分離）
- **效果**：
  - ⬆️ **提高**：更嚴格的文字/背景分離
  - ⬇️ **降低**：更寬鬆的分離，適合對比度低的圖像
- **建議範圍**：`0.2` ~ `0.5`
- **自適應模式調整**：
  - 高質量圖像：`0.3`
  - 中質量圖像：`0.3`
  - 低質量圖像：`0.25`

---

### 預處理與旋轉參數

#### UsePreprocessing
- **類型**：`bool`
- **預設值**：`false`
- **影響**：是否進行圖像預處理（去噪、增強對比度）
- **效果**：
  - `true`：改善低質量圖像，但增加處理時間
  - `false`：保持原圖，速度更快
- **建議**：漫畫圖像通常質量較好，建議關閉

#### AllowRotateDetection
- **類型**：`bool`
- **預設值**：`true`
- **影響**：是否檢測文字旋轉角度
- **效果**：
  - `true`：可識別旋轉的文字
  - `false`：只識別水平文字，速度稍快
- **建議**：漫畫可能有傾斜文字，建議開啟

#### Enable180Classification
- **類型**：`bool`
- **預設值**：`true`
- **影響**：是否檢測 180° 倒置的文字
- **效果**：
  - `true`：可識別上下顛倒的文字
  - `false`：速度稍快
- **建議**：漫畫較少出現倒置文字，可根據需求調整

---

## 使用範例

### 範例 1：基本識別

```csharp
using MangaOCR.Services;

using var ocr = MangaOcrService.CreateDefault();
var result = ocr.RecognizeText("manga.png");

if (result.Success)
{
    Console.WriteLine($"識別成功！找到 {result.TextRegions.Count} 個文字區域");
    Console.WriteLine($"耗時：{result.ElapsedMilliseconds}ms");

    foreach (var region in result.TextRegions)
    {
        Console.WriteLine($"文字：{region.Text}");
        Console.WriteLine($"信心度：{region.Confidence:P1}");
        Console.WriteLine($"位置：({region.BoundingBox.X}, {region.BoundingBox.Y})");
        Console.WriteLine();
    }
}
```

---

### 範例 2：自訂參數（標準模式）

```csharp
using MangaOCR.Models;
using MangaOCR.Services;

var settings = new OcrSettings
{
    Language = "Japanese",
    MinConfidence = 0.6f,      // 提高信心度閾值，減少誤判
    MaxSize = 1280,            // 提高解析度，處理更多細節
    UnclipRatio = 1.8f,        // 擴大文字框，避免截斷
    BoxScoreThreshold = 0.5f,  // 降低閾值，增加召回率
    UsePreprocessing = true    // 開啟預處理，改善低質量圖像
};

using var ocr = MangaOcrService.CreateStandard(settings);
var result = ocr.RecognizeText("low_quality_manga.png");
```

---

### 範例 3：自適應模式 + 詳細診斷

```csharp
using MangaOCR.Services;

using var ocr = MangaOcrService.CreateAdaptive();

// verbose=true 顯示圖像質量分析和參數推薦
var result = ocr.RecognizeText("manga.png", verbose: true);

// 輸出範例：
// 【圖像質量分析】
// 尺寸：1228x883 (1228px)
// 模糊度分數: 6401.8 (清晰)
// 對比度: 86.2 (高對比)
// 亮度: 203.1
// 質量等級: High
//
// 【推薦參數】
// MaxSize: 1024
// UnclipRatio: 1.5
// BoxScoreThreshold: 0.6
// ...

// 查看推薦說明
var explanation = ocr.GetRecommendationExplanation("manga.png");
Console.WriteLine(explanation);
```

---

### 範例 4：結果後處理

```csharp
using MangaOCR.Services;

using var ocr = MangaOcrService.CreateDefault();
var rawResult = ocr.RecognizeText("manga.png");

// 1. 過濾低信心度區域
var processor = new ResultProcessor();
var filteredResult = processor.Process(rawResult, minConfidence: 0.6f);

Console.WriteLine($"原始區域數：{rawResult.TextRegions.Count}");
Console.WriteLine($"過濾後區域數：{filteredResult.TextRegions.Count}");

// 2. 分析閱讀順序（日文漫畫：從右到左、從上到下）
var orderAnalyzer = new TextOrderAnalyzer();
var orderedRegions = orderAnalyzer.AssignReadingOrder(
    filteredResult.TextRegions,
    TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom
);

// 3. 按閱讀順序輸出
foreach (var region in orderedRegions.OrderBy(r => r.ReadingOrder))
{
    Console.WriteLine($"[{region.ReadingOrder}] {region.Text}");
}
```

---

### 範例 5：視覺化標註

```csharp
using MangaOCR.Services;

using var ocr = MangaOcrService.CreateDefault();
var result = ocr.RecognizeText("manga.png");

var annotator = new ImageAnnotator();

// 生成信心度熱圖（綠色=高信心、黃色=中信心、紅色=低信心）
annotator.AnnotateConfidence(
    "manga.png",
    result.TextRegions,
    "manga_confidence.png"
);

// 生成閱讀順序標註
var processor = new ResultProcessor();
var filtered = processor.Process(result, minConfidence: 0.5f);

var orderAnalyzer = new TextOrderAnalyzer();
var ordered = orderAnalyzer.AssignReadingOrder(
    filtered.TextRegions,
    TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom
);

annotator.AnnotateReadingOrder(
    "manga.png",
    ordered,
    "manga_reading_order.png"
);

Console.WriteLine("已生成標註圖片：");
Console.WriteLine("  - manga_confidence.png (信心度熱圖)");
Console.WriteLine("  - manga_reading_order.png (閱讀順序)");
```

---

### 範例 6：非同步處理

```csharp
using MangaOCR.Services;

using var ocr = MangaOcrService.CreateDefault();

// 非同步識別
var result = await ocr.RecognizeTextAsync("manga.png", verbose: false);

Console.WriteLine($"識別完成：{result.TextRegions.Count} 個區域");
```

---

### 範例 7：批次處理多張圖片

```csharp
using MangaOCR.Services;

var imageFiles = Directory.GetFiles("manga_pages", "*.png");
using var ocr = MangaOcrService.CreateDefault();

var results = new List<(string FileName, OcrResult Result)>();

foreach (var imagePath in imageFiles)
{
    var result = ocr.RecognizeText(imagePath);
    results.Add((Path.GetFileName(imagePath), result));
    Console.WriteLine($"已處理：{Path.GetFileName(imagePath)} - {result.TextRegions.Count} 個區域");
}

// 統計資訊
var totalRegions = results.Sum(r => r.Result.TextRegions.Count);
var avgConfidence = results
    .SelectMany(r => r.Result.TextRegions)
    .Where(region => !float.IsNaN(region.Confidence))
    .Average(region => region.Confidence);

Console.WriteLine($"\n批次處理完成：");
Console.WriteLine($"  總圖片數：{results.Count}");
Console.WriteLine($"  總區域數：{totalRegions}");
Console.WriteLine($"  平均信心度：{avgConfidence:P1}");
```

---

### 範例 8：只檢測文字座標（快速模式）

```csharp
using MangaOCR.Services;

using var ocr = MangaOcrService.CreateDefault();

// 只檢測文字區域座標，不識別文字內容
// 適用場景：用戶互動選取、預處理工作流
var regions = ocr.DetectTextRegions("manga.png");

Console.WriteLine($"檢測到 {regions.Count} 個文字區域");
foreach (var region in regions)
{
    Console.WriteLine($"座標：({region.BoundingBox.X}, {region.BoundingBox.Y})");
    Console.WriteLine($"尺寸：{region.BoundingBox.Width}x{region.BoundingBox.Height}");
}

// 速度對比：只檢測比完整 OCR 快約 40%
```

**使用場景**：
- 用戶需要先看到所有文字位置，再選擇要識別的區域
- 批次預處理：先檢測所有頁面的文字位置，再批次識別
- 互動式 OCR：讓用戶點選感興趣的文字框

---

### 範例 9：只識別單一文字區域（極速模式）

```csharp
using MangaOCR.Services;

using var ocr = MangaOcrService.CreateDefault();

// 假設整張圖片就是一個已截取的文字區域
// 跳過檢測階段，直接識別文字內容
var result = ocr.RecognizeTextOnly("cropped_text.png");

if (result.Success && result.TextRegions.Count > 0)
{
    Console.WriteLine($"識別文字：{result.TextRegions[0].Text}");
    Console.WriteLine($"信心度：{result.TextRegions[0].Confidence:P1}");
    Console.WriteLine($"耗時：{result.ElapsedMilliseconds}ms");
}

// 速度對比：處理小圖片時比完整 OCR 快 60+ 倍
// 完整 OCR (4056x2908)：~1300ms
// 只識別 (100x50)：~20ms
```

**使用場景**：
- 用戶已手動截取好文字圖片
- 點選特定文字框進行即時翻譯
- 處理已知只包含一個文字區域的小圖片

---

### 範例 10：批次識別多個已截取的文字圖片

```csharp
using MangaOCR.Services;

// 假設用戶已經截取了多個文字圖片
var croppedImages = new List<string>
{
    "text_region_1.png",
    "text_region_2.png",
    "text_region_3.png",
    // ... 更多圖片
};

using var ocr = MangaOcrService.CreateDefault();

// 批次識別（每個圖片都跳過檢測階段）
var results = ocr.RecognizeTextBatch(croppedImages);

Console.WriteLine($"批次識別完成：");
foreach (var (result, index) in results.Select((r, i) => (r, i)))
{
    if (result.Success && result.TextRegions.Count > 0)
    {
        var text = result.TextRegions[0].Text;
        var confidence = result.TextRegions[0].Confidence;
        Console.WriteLine($"  [{index + 1}] {text} ({confidence:P1})");
    }
}

// 統計資訊
var successCount = results.Count(r => r.Success);
var avgTime = results.Where(r => r.Success).Average(r => r.ElapsedMilliseconds);
Console.WriteLine($"\n成功識別：{successCount}/{results.Count}");
Console.WriteLine($"平均耗時：{avgTime:F0}ms");
```

**使用場景**：
- 批次處理大量已截取的文字圖片
- 分階段處理：先檢測所有頁面，再批次識別選定區域
- 高性能場景：1000 個小圖片只需 ~20 秒（vs 完整 OCR 的 ~1300 秒）

---

### 範例 11：完整工作流程（檢測 + 選擇性識別）

```csharp
using MangaOCR.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

using var ocr = MangaOcrService.CreateDefault();

// 步驟 1：快速檢測所有文字區域
Console.WriteLine("步驟 1：檢測文字區域...");
var regions = ocr.DetectTextRegions("manga_page.png");
Console.WriteLine($"  檢測到 {regions.Count} 個區域");

// 步驟 2：用戶選擇感興趣的區域（模擬）
// 實際應用中可以透過 UI 讓用戶點選
var selectedRegions = regions.Take(3).ToList();
Console.WriteLine($"\n步驟 2：用戶選擇了 {selectedRegions.Count} 個區域");

// 步驟 3：截取選定區域的圖片
var croppedImages = new List<string>();
using var originalImage = Image.Load("manga_page.png");

foreach (var (region, index) in selectedRegions.Select((r, i) => (r, i)))
{
    var bbox = region.BoundingBox;
    var cropped = originalImage.Clone(img =>
        img.Crop(new Rectangle(bbox.X, bbox.Y, bbox.Width, bbox.Height))
    );

    var outputPath = $"temp_region_{index}.png";
    cropped.Save(outputPath);
    croppedImages.Add(outputPath);
}

// 步驟 4：批次識別選定區域（極速模式）
Console.WriteLine("\n步驟 3：識別選定區域...");
var recognitionResults = ocr.RecognizeTextBatch(croppedImages);

foreach (var (result, index) in recognitionResults.Select((r, i) => (r, i)))
{
    if (result.Success && result.TextRegions.Count > 0)
    {
        var text = result.TextRegions[0].Text;
        var confidence = result.TextRegions[0].Confidence;
        Console.WriteLine($"  區域 {index + 1}: {text} ({confidence:P1})");
    }
}

// 清理臨時文件
foreach (var file in croppedImages)
{
    File.Delete(file);
}

// 效能優勢：
// - 完整 OCR：~1300ms
// - 檢測 + 批次識別 3 個區域：~800ms + 3×20ms = ~860ms
// - 對於大圖片選擇性識別，速度提升明顯！
```

---

### 範例 12：平行批次處理（高性能）

```csharp
using MangaOCR.Models;
using MangaOCR.Services;

// 假設有大量已截取的文字圖片
var croppedImages = Directory.GetFiles("cropped_texts", "*.png").ToList();
Console.WriteLine($"準備處理 {croppedImages.Count} 張圖片");

using var ocr = MangaOcrService.CreateDefault();

// 方法 1：使用預設設定（CPU 核心數 / 2）
var results1 = ocr.RecognizeTextBatchParallel(croppedImages);

// 方法 2：自訂線程數和選項
var options = new BatchProcessingOptions
{
    MaxDegreeOfParallelism = 4,        // 最多 4 個線程同時處理
    EnableSmartScheduling = true,       // 啟用智能排程
    LargeFileSizeThreshold = 500_000   // 500KB 以上視為大檔案
};

var results2 = ocr.RecognizeTextBatchParallel(croppedImages, options);

Console.WriteLine($"處理完成：{results2.Count} 張圖片");

// 效能對比：
// 循序處理 100 張：~2000ms
// 平行處理 100 張（4 線程）：~600ms （提升 3.3 倍）
```

**優勢**：
- 自動利用多核 CPU
- 智能排程避免大檔案阻塞
- 可自訂線程數控制資源使用

---

### 範例 13：事件監聽（日誌和進度）

```csharp
using MangaOCR.Models;
using MangaOCR.Services;

using var ocr = MangaOCR.CreateDefault();

// 訂閱日誌事件（使用者自行決定如何處理）
ocr.LogMessage += (sender, e) =>
{
    var color = e.Level switch
    {
        OcrLogLevel.Error => ConsoleColor.Red,
        OcrLogLevel.Warning => ConsoleColor.Yellow,
        OcrLogLevel.Information => ConsoleColor.Green,
        _ => ConsoleColor.Gray
    };

    Console.ForegroundColor = color;
    Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] [{e.Level}] {e.Message}");
    Console.ResetColor();

    // 也可以寫入日誌文件或發送到監控系統
    // _logger.Log(e.Level, e.Message);
};

// 訂閱進度事件（即時追蹤處理進度）
ocr.ProgressChanged += (sender, e) =>
{
    Console.WriteLine($"進度: {e.Percentage:P0} ({e.Current}/{e.Total})");
    Console.WriteLine($"  當前處理: {Path.GetFileName(e.CurrentImagePath)}");

    // 也可以更新 UI 進度條
    // progressBar.Value = e.Percentage * 100;
};

// 執行批次處理（會觸發事件）
var imagePaths = Directory.GetFiles("images", "*.png").ToList();
var results = ocr.RecognizeTextBatchParallel(imagePaths);

Console.WriteLine($"\n批次處理完成！成功: {results.Count(r => r.Success)}/{results.Count}");
```

**使用場景**：
- 桌面應用程式：更新 UI 進度條
- 後端服務：記錄日誌到文件或監控系統
- 除錯：追蹤處理過程和錯誤

---

### 範例 14：取消處理（可中斷的批次任務）

```csharp
using MangaOCR.Models;
using MangaOCR.Services;

using var ocr = MangaOcrService.CreateDefault();
using var cts = new CancellationTokenSource();

var imagePaths = Directory.GetFiles("images", "*.png").ToList();
var options = new BatchProcessingOptions
{
    CancellationToken = cts.Token,
    MaxDegreeOfParallelism = 4
};

// 在另一個線程中設定 5 秒後自動取消
_ = Task.Run(async () =>
{
    await Task.Delay(5000);
    cts.Cancel();
    Console.WriteLine("已發送取消請求...");
});

try
{
    var results = ocr.RecognizeTextBatchParallel(imagePaths, options);
    Console.WriteLine($"全部完成！處理了 {results.Count} 張圖片");
}
catch (OperationCanceledException)
{
    Console.WriteLine("處理已取消");
}

// 使用場景：
// - 使用者點擊「取消」按鈕
// - 超時保護
// - 應用程式關閉時優雅終止
```

---

## 完整工作流程

```csharp
using MangaOCR.Models;
using MangaOCR.Services;

// 1. 創建 OCR 服務（自適應模式）
using var ocr = MangaOcrService.CreateDefault();

// 2. 執行 OCR 識別
var result = ocr.RecognizeText("manga_page.png", verbose: true);

// 3. 檢查結果
if (!result.Success)
{
    Console.WriteLine($"識別失敗：{result.ErrorMessage}");
    return;
}

// 4. 後處理：過濾低信心度區域
var processor = new ResultProcessor();
var filtered = processor.Process(result, minConfidence: 0.5f);

// 5. 分析閱讀順序
var orderAnalyzer = new TextOrderAnalyzer();
var ordered = orderAnalyzer.AssignReadingOrder(
    filtered.TextRegions,
    TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom
);

// 6. 生成視覺化標註
var annotator = new ImageAnnotator();
annotator.AnnotateReadingOrder("manga_page.png", ordered, "output_order.png");
annotator.AnnotateConfidence("manga_page.png", result.TextRegions, "output_confidence.png");

// 7. 輸出結果
Console.WriteLine($"\n識別結果（按閱讀順序）：");
foreach (var region in ordered.OrderBy(r => r.ReadingOrder))
{
    Console.WriteLine($"[{region.ReadingOrder}] {region.Text} ({region.Confidence:P1})");
}

// 8. 統計資訊
var stats = new
{
    TotalRegions = result.TextRegions.Count,
    FilteredRegions = filtered.TextRegions.Count,
    AvgConfidence = filtered.TextRegions.Average(r => r.Confidence),
    HighConfidenceCount = filtered.TextRegions.Count(r => r.Confidence >= 0.7f),
    ElapsedMs = result.ElapsedMilliseconds
};

Console.WriteLine($"\n統計資訊：");
Console.WriteLine($"  原始區域數：{stats.TotalRegions}");
Console.WriteLine($"  過濾後區域數：{stats.FilteredRegions}");
Console.WriteLine($"  平均信心度：{stats.AvgConfidence:P1}");
Console.WriteLine($"  高信心度區域：{stats.HighConfidenceCount}");
Console.WriteLine($"  處理耗時：{stats.ElapsedMs}ms");
```

---

## 性能優化建議

### 1. 選擇合適的模式

| 場景 | 推薦模式 | 原因 |
|------|----------|------|
| 圖像質量不一致 | 自適應模式 | 自動調參，平均快 38% |
| 圖像質量穩定 | 標準模式 | 參數一致，可預測 |
| 需要極致性能 | 標準模式 + 手動調優 | 針對特定場景微調 |

---

### 2. 參數調優方向

**提升速度**：
```csharp
var settings = new OcrSettings
{
    MaxSize = 960,              // 降低解析度
    UsePreprocessing = false,   // 關閉預處理
    AllowRotateDetection = false // 關閉旋轉檢測（如果確定無旋轉文字）
};
```

**提升準確率**：
```csharp
var settings = new OcrSettings
{
    MaxSize = 1280,             // 提高解析度
    UnclipRatio = 1.8f,         // 擴大文字框
    BoxScoreThreshold = 0.5f,   // 降低檢測閾值
    UsePreprocessing = true     // 開啟預處理（低質量圖像）
};
```

**平衡模式**（推薦）：
```csharp
var settings = new OcrSettings
{
    MaxSize = 1024,
    UnclipRatio = 1.5f,
    BoxScoreThreshold = 0.6f,
    UsePreprocessing = false
};
```

---

### 3. 批次處理優化

```csharp
// ✓ 好的做法：重用同一個 OCR 實例
using var ocr = MangaOcrService.CreateDefault();
foreach (var image in images)
{
    var result = ocr.RecognizeText(image);
    // 處理結果...
}

// ✗ 不好的做法：每次都創建新實例
foreach (var image in images)
{
    using var ocr = MangaOcrService.CreateDefault(); // 重複初始化，浪費資源
    var result = ocr.RecognizeText(image);
}
```

---

## 圖像質量分析（自適應模式）

自適應模式會自動分析以下指標：

### 1. 模糊度分數（Blur Score）
- **計算方式**：Laplacian variance
- **閾值**：
  - `>= 300`：清晰
  - `>= 100`：中等
  - `< 100`：模糊

### 2. 對比度（Contrast）
- **計算方式**：灰度標準差
- **閾值**：
  - `>= 50`：高對比
  - `>= 30`：中對比
  - `< 30`：低對比

### 3. 質量等級（Quality Level）

| 等級 | 條件 | MaxSize | UnclipRatio | BoxScoreThreshold |
|------|------|---------|-------------|-------------------|
| **High** | 清晰 + 高對比 | 1024 | 1.5 | 0.6 |
| **Medium** | 中等清晰或中對比 | 1280 | 1.8 | 0.55 |
| **Low** | 模糊或低對比 | 1920 | 2.0 | 0.5 |

---

## 模式選擇建議

### 自適應模式（Adaptive）- 預設推薦

**優點**：
- ✓ 自動分析圖像質量並選擇最佳參數
- ✓ 速度快（平均快 38%，相同辨識率）
- ✓ 無需手動調參
- ✓ 適應不同質量的圖像

**適用場景**：
- 圖像質量不一致
- 不同來源的漫畫圖片
- 快速原型開發
- 大多數使用場景

**使用方式**：
```csharp
using var ocr = MangaOcrService.CreateDefault(); // 或 CreateAdaptive()
var result = ocr.RecognizeText("image.png", verbose: true);
```

---

### 標準模式（Standard）

**優點**：
- ✓ 參數固定，行為可預測
- ✓ 完全控制所有參數
- ✓ 適合微調優化

**適用場景**：
- 圖像質量穩定
- 需要參數一致性
- 手動微調特定場景

**使用方式**：
```csharp
var settings = new OcrSettings
{
    Language = "Japanese",
    MaxSize = 1024,
    UnclipRatio = 1.5f,
    // ... 其他參數
};
using var ocr = MangaOcrService.CreateStandard(settings);
var result = ocr.RecognizeText("image.png");
```

---

## 測試結果參考

基於測試圖片 `4.png` 和 `5.jpg` 的對比：

| 圖片 | 模式 | 區域數 | 平均信心度 | 耗時 |
|------|------|--------|-----------|------|
| 4.png | 標準 | 29 | 88.1% | 2304ms |
| 4.png | **自適應** | 29 | **88.1%** | **1341ms** ⚡ |
| 5.jpg | 標準 | 16 | 90.0% | 1573ms |
| 5.jpg | **自適應** | 16 | **90.0%** | **1018ms** ⚡ |

**結論**：自適應模式在相同辨識率下，速度提升 **38%**。

---

## API 參考

### MangaOcrService

#### 工廠方法

```csharp
// 預設配置（日文漫畫 + 自適應）
public static MangaOcrService CreateDefault()

// 自適應模式（預設）
public static MangaOcrService Create(OcrSettings? settings = null)

// 自適應模式（明確指定）
public static MangaOcrService CreateAdaptive(OcrSettings? settings = null)

// 標準模式（固定參數）
public static MangaOcrService CreateStandard(OcrSettings? settings = null)
```

#### 識別方法

```csharp
// 完整 OCR（檢測 + 識別）
public OcrResult RecognizeText(string imagePath, bool verbose = false)
public async Task<OcrResult> RecognizeTextAsync(
    string imagePath,
    bool verbose = false,
    CancellationToken cancellationToken = default)

// 只檢測文字區域座標（快速模式）
public List<TextRegion> DetectTextRegions(string imagePath)
public async Task<List<TextRegion>> DetectTextRegionsAsync(
    string imagePath,
    CancellationToken cancellationToken = default)

// 只識別單一文字區域（極速模式，跳過檢測）
public OcrResult RecognizeTextOnly(string imagePath)
public async Task<OcrResult> RecognizeTextOnlyAsync(
    string imagePath,
    CancellationToken cancellationToken = default)

// 批次識別多個已截取的文字圖片（循序處理）
public List<OcrResult> RecognizeTextBatch(List<string> imagePaths)
public async Task<List<OcrResult>> RecognizeTextBatchAsync(
    List<string> imagePaths,
    CancellationToken cancellationToken = default)

// 批次識別（平行處理，高性能）⭐ 推薦
public List<OcrResult> RecognizeTextBatchParallel(
    List<string> imagePaths,
    BatchProcessingOptions? options = null)
public async Task<List<OcrResult>> RecognizeTextBatchParallelAsync(
    List<string> imagePaths,
    BatchProcessingOptions? options = null)
```

#### 事件

```csharp
// 日誌事件（使用者自行決定如何收集和處理）
public event EventHandler<OcrLogEventArgs>? LogMessage;

// 進度事件（批次處理時回報進度）
public event EventHandler<OcrProgressEventArgs>? ProgressChanged;
```

#### 輔助方法

```csharp
// 獲取當前模式
public OcrMode Mode { get; }

// 獲取推薦參數說明（僅自適應模式）
public string GetRecommendationExplanation(string imagePath)
```

---

### BatchProcessingOptions

```csharp
public class BatchProcessingOptions
{
    // 最大平行線程數（null 則使用預設值：CPU 核心數 / 2）
    public int? MaxDegreeOfParallelism { get; set; }

    // 是否啟用智能排程（預設 true）
    public bool EnableSmartScheduling { get; set; } = true;

    // 大檔案閾值（預設 1MB）
    public long LargeFileSizeThreshold { get; set; } = 1_000_000;

    // 取消權杖
    public CancellationToken CancellationToken { get; set; }

    // 取得實際使用的平行線程數
    public int GetActualMaxDegreeOfParallelism()
}
```

---

### OcrLogEventArgs

```csharp
public class OcrLogEventArgs : EventArgs
{
    public OcrLogLevel Level { get; set; }        // 日誌等級
    public string Message { get; set; }            // 日誌訊息
    public DateTime Timestamp { get; set; }        // 時間戳記
    public Dictionary<string, object>? Data { get; set; }  // 額外資料
}

public enum OcrLogLevel
{
    Trace,        // 追蹤（最詳細）
    Debug,        // 除錯
    Information,  // 資訊
    Warning,      // 警告
    Error         // 錯誤
}
```

---

### OcrProgressEventArgs

```csharp
public class OcrProgressEventArgs : EventArgs
{
    public int Current { get; set; }               // 當前進度
    public int Total { get; set; }                 // 總數
    public string? CurrentImagePath { get; set; }  // 當前處理的圖片
    public double Percentage { get; }              // 進度百分比 (0.0-1.0)
    public string? Message { get; set; }           // 訊息
}
```

---

### OcrResult

```csharp
public class OcrResult
{
    public bool Success { get; set; }                // 是否成功
    public string ErrorMessage { get; set; }          // 錯誤訊息
    public List<TextRegion> TextRegions { get; set; } // 文字區域列表
    public long ElapsedMilliseconds { get; set; }     // 處理耗時
}
```

---

### TextRegion

```csharp
public class TextRegion
{
    public string Text { get; set; }            // 識別的文字
    public float Confidence { get; set; }       // 信心度 (0.0 ~ 1.0)
    public Rectangle BoundingBox { get; set; }  // 邊界框
    public int ReadingOrder { get; set; }       // 閱讀順序（需經 TextOrderAnalyzer 分析）
}
```

---

## 常見問題

### Q1: 如何提高識別準確率？

**答**：
1. 使用自適應模式（預設）
2. 提高 `MaxSize` 到 1280 或 1920
3. 調整 `UnclipRatio` 到 1.8 ~ 2.0
4. 降低 `BoxScoreThreshold` 到 0.5
5. 如果圖像質量差，開啟 `UsePreprocessing`

### Q2: 識別速度太慢怎麼辦？

**答**：
1. 降低 `MaxSize` 到 960
2. 關閉 `UsePreprocessing`
3. 關閉 `AllowRotateDetection`（如果確定無旋轉文字）
4. 使用自適應模式（平均快 38%）

### Q3: 為什麼有些文字沒有被識別？

**答**：
1. 檢查 `BoxScoreThreshold` 是否太高，嘗試降低到 0.5
2. 提高 `UnclipRatio` 到 1.8 或 2.0
3. 提高 `MaxSize` 以處理更多細節
4. 檢查 `MinConfidence` 是否過濾掉了低信心度區域
5. 使用 `verbose: true` 查看診斷資訊

### Q4: 何時應該使用檢測/識別分離模式？

**答**：

**使用 `DetectTextRegions()` 的時機**：
- 需要讓用戶先看到文字位置，再選擇要識別的區域
- 批次預處理：先檢測所有頁面，再批次識別
- 互動式應用：讓用戶點選感興趣的文字框
- 速度提升：比完整 OCR 快約 40%

**使用 `RecognizeTextOnly()` 的時機**：
- 用戶已手動截取好文字圖片
- 點選特定文字框進行即時翻譯
- 處理已知只包含一個文字區域的小圖片
- 速度提升：處理小圖片比完整 OCR 快 60+ 倍

**使用 `RecognizeTextBatch()` 的時機**：
- 批次處理大量已截取的文字圖片
- 高性能場景：1000 個小圖片只需 ~20 秒（vs 完整 OCR 的 ~1300 秒）
- 分階段處理：先檢測所有頁面，再批次識別選定區域

**性能對比**：
```
完整 OCR (4056x2908)：        ~1300ms
只檢測：                      ~800ms   (快 38%)
只識別 (100x50 小圖)：        ~20ms    (快 65 倍)
批次識別 1000 個小圖：        ~20s     (vs ~1300s)
```

### Q5: 如何處理傾斜或旋轉的文字？

**答**：
1. 確保 `AllowRotateDetection = true`
2. 確保 `Enable180Classification = true`
3. 如果文字嚴重傾斜，考慮預先校正圖像

### Q6: 自適應模式和標準模式有什麼區別？

**答**：

| 特性 | 自適應模式 | 標準模式 |
|------|-----------|----------|
| 參數調整 | 自動根據圖像質量 | 固定參數 |
| 速度 | 平均快 38% | 標準速度 |
| 準確率 | 相同 | 相同 |
| 適用場景 | 圖像質量不一 | 圖像質量穩定 |
| 可預測性 | 參數會變化 | 參數固定 |

### Q7: 什麼時候應該使用平行批次處理？

**答**：

**適用場景**：
- 處理大量圖片（> 10 張）
- 多核 CPU 環境
- 需要最快完成批次任務

**性能對比**：
```
循序處理 100 張：~2000ms
平行處理 100 張（4 線程）：~600ms（提升 3.3 倍）
```

**使用建議**：
```csharp
// 少量圖片（< 10 張）- 使用循序處理
var results = ocr.RecognizeTextBatch(imagePaths);

// 大量圖片（>= 10 張）- 使用平行處理
var options = new BatchProcessingOptions
{
    MaxDegreeOfParallelism = 4,  // 根據 CPU 核心數調整
    EnableSmartScheduling = true
};
var results = ocr.RecognizeTextBatchParallel(imagePaths, options);
```

### Q8: 如何監聽處理進度和日誌？

**答**：

透過事件訂閱機制，使用者可以自行決定如何收集和處理資料：

**監聽日誌**：
```csharp
ocr.LogMessage += (sender, e) =>
{
    // 寫入文件
    File.AppendAllText("ocr.log", $"{e.Timestamp} [{e.Level}] {e.Message}\n");

    // 或發送到監控系統
    _logger.Log(e.Level, e.Message);
};
```

**監聽進度**：
```csharp
ocr.ProgressChanged += (sender, e) =>
{
    // 更新 UI 進度條
    progressBar.Value = (int)(e.Percentage * 100);

    // 或記錄進度
    Console.WriteLine($"{e.Current}/{e.Total} ({e.Percentage:P0})");
};
```

**使用場景**：
- 桌面應用程式：即時更新 UI
- 後端服務：結構化日誌記錄
- 除錯：追蹤處理細節

---

## 授權

此專案使用 PaddleOCR v3.0.1，請遵守其授權條款。

---

## 參考資源

- [PaddleOCR 官方文檔](https://github.com/PaddlePaddle/PaddleOCR)
- [Sdcb.PaddleOCR NuGet](https://www.nuget.org/packages/Sdcb.PaddleOCR)
- 測試範例：`MangaOCR.Tests/`
