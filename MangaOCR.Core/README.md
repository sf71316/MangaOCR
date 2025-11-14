# MangaOCR.Core

專為漫畫文字識別優化的 .NET OCR 類別庫，基於 PaddleOCR v3.0.1 實現。

## 特色功能

- ✨ **自適應 OCR**：自動分析圖像質量並選擇最佳參數（預設模式）
- 🚀 **高性能**：自適應模式平均快 38%，辨識率維持相同水準
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
// 同步識別
public OcrResult RecognizeText(string imagePath, bool verbose = false)

// 非同步識別
public async Task<OcrResult> RecognizeTextAsync(
    string imagePath,
    bool verbose = false,
    CancellationToken cancellationToken = default)
```

#### 輔助方法

```csharp
// 獲取當前模式
public OcrMode Mode { get; }

// 獲取推薦參數說明（僅自適應模式）
public string GetRecommendationExplanation(string imagePath)
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

### Q4: 如何處理傾斜或旋轉的文字？

**答**：
1. 確保 `AllowRotateDetection = true`
2. 確保 `Enable180Classification = true`
3. 如果文字嚴重傾斜，考慮預先校正圖像

### Q5: 自適應模式和標準模式有什麼區別？

**答**：

| 特性 | 自適應模式 | 標準模式 |
|------|-----------|----------|
| 參數調整 | 自動根據圖像質量 | 固定參數 |
| 速度 | 平均快 38% | 標準速度 |
| 準確率 | 相同 | 相同 |
| 適用場景 | 圖像質量不一 | 圖像質量穩定 |
| 可預測性 | 參數會變化 | 參數固定 |

---

## 更新記錄

> ⚠️ **重要**：當 `MangaOcrService`、`OcrSettings` 或任何公開 API 有變更時，請同步更新此文檔！

### 更新檢查清單

- [ ] 工廠方法是否有新增或修改？
- [ ] OcrSettings 是否有新增或刪除參數？
- [ ] 參數的預設值是否有變更？
- [ ] 自適應模式的推薦邏輯是否有調整？
- [ ] 效能測試結果是否有更新？
- [ ] 使用範例是否需要更新？

---

## 授權

此專案使用 PaddleOCR v3.0.1，請遵守其授權條款。

---

## 參考資源

- [PaddleOCR 官方文檔](https://github.com/PaddlePaddle/PaddleOCR)
- [Sdcb.PaddleOCR NuGet](https://www.nuget.org/packages/Sdcb.PaddleOCR)
- 測試範例：`MangaOCR.Tests/`
- 使用範例：`MangaOCR/Program.cs`
