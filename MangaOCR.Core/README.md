# MangaOCR.Core

一個專為日文漫畫設計的 OCR（光學字元識別）類別庫，基於 PaddleOCR 引擎，支援高準確度的文字識別、閱讀順序分析和視覺化標註。

## 功能特色

- ✓ **高準確度識別**：平均信心度 88%+，針對日文漫畫優化
- ✓ **智慧後處理**：自動過濾低品質區域、去重、文字清理
- ✓ **閱讀順序分析**：支援日文漫畫（從右到左）、英文（從左到右）等多種閱讀方向
- ✓ **視覺化標註**：生成帶有閱讀順序和信心度標註的 debug 圖片
- ✓ **可配置參數**：支援靈活的 OCR 參數調整以適應不同場景
- ✓ **多語言支援**：日文、中文、英文、韓文等

## 系統需求

- .NET 9.0 或更高版本
- Windows x64（目前使用 PaddleInference runtime for Windows）
- 建議至少 4GB RAM

## 安裝

### 1. 引用專案

在您的專案中加入 MangaOCR.Core 專案引用：

```xml
<ItemGroup>
  <ProjectReference Include="path\to\MangaOCR.Core\MangaOCR.Core.csproj" />
</ItemGroup>
```

### 2. NuGet 套件依賴

MangaOCR.Core 會自動引用以下套件（無需手動安裝）：

```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.0" />
<PackageReference Include="OpenCvSharp4.runtime.win" Version="4.11.0.20250507" />
<PackageReference Include="Sdcb.PaddleInference.runtime.win64.mkl" Version="3.1.0.54" />
<PackageReference Include="Sdcb.PaddleOCR" Version="3.0.1" />
<PackageReference Include="Sdcb.PaddleOCR.Models.LocalV3" Version="2.7.0.1" />
<PackageReference Include="Sdcb.PaddleOCR.Models.Online" Version="3.0.1" />
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.12" />
```

## 快速開始

### 基本用法

```csharp
using MangaOCR.Factories;
using MangaOCR.Models;
using MangaOCR.Services;

// 1. 配置 OCR 設定
var settings = new OcrSettings
{
    Provider = OcrProvider.PaddleOCR,
    Language = "Japanese",
    MinConfidence = 0.5f,
    UsePreprocessing = false,
    AllowRotateDetection = true,
    Enable180Classification = true,
    UnclipRatio = 1.5f,
    MaxSize = 1024,
    BoxScoreThreshold = 0.6f,
    Threshold = 0.3f
};

// 2. 創建 OCR 服務
var factory = new OcrServiceFactory();
using var ocrService = factory.CreateOcrService(settings);

// 3. 執行 OCR 識別
var result = ocrService.RecognizeText("path/to/manga/page.png");

if (result.Success)
{
    Console.WriteLine($"識別完成，耗時：{result.ElapsedMilliseconds}ms");
    Console.WriteLine($"識別區域數：{result.TextRegions.Count}");

    foreach (var region in result.TextRegions)
    {
        Console.WriteLine($"文字：{region.Text}，信心度：{region.Confidence:P1}");
    }
}
```

### 完整工作流程

```csharp
using MangaOCR.Factories;
using MangaOCR.Models;
using MangaOCR.Services;
using Microsoft.Extensions.Configuration;

// 1. 從配置文件載入設定
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var settings = configuration.GetSection("OcrSettings").Get<OcrSettings>();

// 2. 創建 OCR 服務
var factory = new OcrServiceFactory();
using var ocrService = factory.CreateOcrService(settings);

// 3. 執行 OCR 識別
var rawResult = ocrService.RecognizeText("manga_page.png");

// 4. 後處理結果（過濾低品質、去重）
var processor = new ResultProcessor();
var processedResult = processor.Process(rawResult, minConfidence: settings.MinConfidence);

// 5. 分析閱讀順序（日文漫畫：從右到左、從上到下）
var orderAnalyzer = new TextOrderAnalyzer();
var orderedRegions = orderAnalyzer.AssignReadingOrder(
    processedResult.TextRegions,
    TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);

// 6. 按順序輸出文字
foreach (var item in orderedRegions)
{
    Console.WriteLine($"[{item.ReadingOrder:D2}] {item.Region.Text}");
}

// 7. 生成視覺化標註圖片（可選）
var annotator = new ImageAnnotator();
annotator.AnnotateReadingOrder("manga_page.png", orderedRegions, "output_order.png");
annotator.AnnotateConfidence("manga_page.png", rawResult.TextRegions, "output_confidence.png");
```

## 配置說明

### appsettings.json 範例

```json
{
  "OcrSettings": {
    "Provider": "PaddleOCR",
    "Language": "Japanese",
    "ModelPath": null,
    "MinConfidence": 0.5,
    "UsePreprocessing": false,
    "AllowRotateDetection": true,
    "Enable180Classification": true,
    "UnclipRatio": 1.5,
    "MaxSize": 1024,
    "BoxScoreThreshold": 0.6,
    "Threshold": 0.3
  }
}
```

### 配置參數說明

#### 基本參數

| 參數 | 類型 | 預設值 | 說明 |
|------|------|--------|------|
| `Provider` | enum | `PaddleOCR` | OCR 引擎提供者（目前僅支援 PaddleOCR） |
| `Language` | string | `"Japanese"` | 識別語言：`Japanese`, `Chinese`, `English`, `Korean` 等 |
| `ModelPath` | string? | `null` | 自訂模型路徑（null 則使用線上模型） |
| `MinConfidence` | float | `0.5` | 最低信心度閾值（0.0-1.0） |
| `UsePreprocessing` | bool | `false` | 是否啟用影像預處理（**建議關閉**，PaddleOCR 對原圖效果更好） |

#### 旋轉偵測參數

| 參數 | 類型 | 預設值 | 說明 |
|------|------|--------|------|
| `AllowRotateDetection` | bool | `true` | 是否允許旋轉偵測 |
| `Enable180Classification` | bool | `true` | 是否啟用 180 度分類 |

#### 進階偵測參數（影響準確度）

| 參數 | 類型 | 預設值 | 說明 |
|------|------|--------|------|
| `UnclipRatio` | float | `1.5` | 文字區域擴展比例<br>• 較小值 (1.2-1.3)：適合清晰的漫畫字體<br>• 較大值 (1.8-2.0)：適合小而密集的文字 |
| `MaxSize` | int | `1024` | 偵測時的最大圖片尺寸<br>• 提高值 (1280-1920)：提升準確度但降低速度<br>• 降低值 (640-800)：提升速度但降低準確度<br>• **建議：1024** 為日文漫畫最佳值 |
| `BoxScoreThreshold` | float | `0.6` | 邊界框信心度閾值<br>• 提高值 (0.7-0.8)：減少誤判但可能遺漏文字<br>• 降低值 (0.5)：檢測更多文字但可能增加誤判 |
| `Threshold` | float | `0.3` | 二值化閾值<br>• 提高值 (0.4)：減少雜訊<br>• 降低值 (0.2)：檢測更多文字 |

## 核心類別說明

### OcrServiceFactory

**用途**：創建 OCR 服務實例

```csharp
var factory = new OcrServiceFactory();
var ocrService = factory.CreateOcrService(settings);
```

### PaddleOcrService

**用途**：執行 OCR 識別

**主要方法**：
- `OcrResult RecognizeText(string imagePath)` - 同步識別
- `Task<OcrResult> RecognizeTextAsync(string imagePath, CancellationToken cancellationToken)` - 非同步識別

**注意事項**：
- 首次使用會自動下載模型（約 10-30 秒）
- 使用完畢後請呼叫 `Dispose()` 釋放資源
- 建議使用 `using` 語句管理生命週期

### ResultProcessor

**用途**：後處理 OCR 結果

**功能**：
- 過濾空白和低信心度區域
- 移除重複/重疊的文字框
- 清理文字（移除控制字元、標準化空白）

```csharp
var processor = new ResultProcessor();
var processedResult = processor.Process(rawResult, minConfidence: 0.5f);
```

### TextOrderAnalyzer

**用途**：分析和排序文字閱讀順序

**支援的閱讀方向**：
- `RightToLeftTopToBottom` - 日文漫畫（從右到左、從上到下）
- `LeftToRightTopToBottom` - 英文模式（從左到右、從上到下）
- `TopToBottomRightToLeft` - 直排模式（從上到下、從右到左）
- `Auto` - 自動檢測

```csharp
var orderAnalyzer = new TextOrderAnalyzer();

// 方法 1：排序文字區域
var sortedRegions = orderAnalyzer.SortByReadingOrder(
    textRegions,
    TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);

// 方法 2：分配閱讀順序編號
var orderedRegions = orderAnalyzer.AssignReadingOrder(
    textRegions,
    TextOrderAnalyzer.ReadingDirection.RightToLeftTopToBottom);
// 返回：List<(TextRegion Region, int ReadingOrder)>
```

### ImageAnnotator

**用途**：生成視覺化標註圖片

**主要方法**：

```csharp
var annotator = new ImageAnnotator();

// 標註閱讀順序（顯示編號）
annotator.AnnotateReadingOrder(
    inputImagePath,
    orderedRegions,
    outputImagePath);

// 標註信心度（顏色編碼）
// 綠色：高信心度 (>=70%)
// 黃色：中等信心度 (50-70%)
// 紅色：低信心度 (<50%)
annotator.AnnotateConfidence(
    inputImagePath,
    textRegions,
    outputImagePath);
```

## 資料模型

### OcrResult

```csharp
public class OcrResult
{
    public bool Success { get; set; }               // 是否成功
    public string? ErrorMessage { get; set; }       // 錯誤訊息
    public List<TextRegion> TextRegions { get; set; } // 識別的文字區域
    public string FullText { get; set; }            // 完整文字（所有區域組合）
    public long ElapsedMilliseconds { get; set; }   // 處理耗時（毫秒）
}
```

### TextRegion

```csharp
public class TextRegion
{
    public string Text { get; set; }              // 識別的文字
    public float Confidence { get; set; }         // 信心度 (0.0-1.0)
    public BoundingBox BoundingBox { get; set; }  // 邊界框座標
}
```

### BoundingBox

```csharp
public class BoundingBox
{
    public int X { get; set; }                    // 左上角 X 座標
    public int Y { get; set; }                    // 左上角 Y 座標
    public int Width { get; set; }                // 寬度
    public int Height { get; set; }               // 高度
    public List<Point> Points { get; set; }       // 四個角點座標（旋轉矩形）
}
```

## 最佳實踐

### 1. 資源管理

```csharp
// ✓ 正確：使用 using 自動釋放資源
using var ocrService = factory.CreateOcrService(settings);
var result = ocrService.RecognizeText(imagePath);

// ✗ 錯誤：忘記釋放資源
var ocrService = factory.CreateOcrService(settings);
var result = ocrService.RecognizeText(imagePath);
// 記憶體洩漏！
```

### 2. 批次處理

```csharp
// ✓ 正確：重用 OCR 服務實例
using var ocrService = factory.CreateOcrService(settings);
foreach (var imagePath in imageFiles)
{
    var result = ocrService.RecognizeText(imagePath);
    // 處理結果...
}

// ✗ 錯誤：每次都創建新實例（浪費資源）
foreach (var imagePath in imageFiles)
{
    using var ocrService = factory.CreateOcrService(settings);
    var result = ocrService.RecognizeText(imagePath);
}
```

### 3. 非同步處理

```csharp
// 推薦用於 Web 應用或 UI 應用
var result = await ocrService.RecognizeTextAsync(imagePath, cancellationToken);
```

### 4. 錯誤處理

```csharp
var result = ocrService.RecognizeText(imagePath);

if (!result.Success)
{
    Console.WriteLine($"OCR 失敗：{result.ErrorMessage}");
    return;
}

// 檢查是否有識別到文字
if (result.TextRegions.Count == 0)
{
    Console.WriteLine("警告：未識別到任何文字");
    return;
}

// 處理結果...
```

### 5. 參數調整策略

**場景 1：清晰的漫畫掃描圖**
```csharp
var settings = new OcrSettings
{
    Language = "Japanese",
    MaxSize = 1024,           // 標準尺寸
    UnclipRatio = 1.3f,       // 較小擴展
    BoxScoreThreshold = 0.7f, // 較高閾值
    MinConfidence = 0.6f      // 較高過濾
};
```

**場景 2：模糊或低解析度圖片**
```csharp
var settings = new OcrSettings
{
    Language = "Japanese",
    MaxSize = 1280,           // 提高解析度
    UnclipRatio = 1.8f,       // 較大擴展
    BoxScoreThreshold = 0.5f, // 較低閾值
    MinConfidence = 0.4f      // 較低過濾
};
```

**場景 3：小字或密集文字**
```csharp
var settings = new OcrSettings
{
    Language = "Japanese",
    MaxSize = 1920,           // 最大解析度
    UnclipRatio = 2.0f,       // 最大擴展
    BoxScoreThreshold = 0.5f,
    Threshold = 0.2f          // 檢測更多文字
};
```

## 注意事項

### ⚠️ 重要限制

1. **平台限制**
   - 目前僅支援 Windows x64
   - 如需支援 Linux/macOS，需要更換 PaddleInference runtime 套件

2. **模型下載**
   - 首次執行會自動下載模型（約 10-50 MB，視語言而定）
   - 需要網路連線
   - 模型會快取在本機，後續執行無需重新下載

3. **記憶體使用**
   - 每個 OCR 服務實例約佔用 200-500 MB 記憶體
   - 大圖片（>4000x4000）可能需要更多記憶體
   - 建議批次處理時限制並行數量

4. **執行緒安全**
   - `PaddleOcrService` 實例**不是執行緒安全的**
   - 多執行緒環境下，每個執行緒應建立獨立實例
   - 或使用鎖機制保護共用實例

### ⚠️ 效能考量

1. **首次執行較慢**
   - 模型載入需要 1-3 秒
   - 後續調用速度正常（每張圖約 1-3 秒）

2. **圖片大小影響**
   - 建議圖片寬高不超過 2000 像素
   - 過大的圖片會自動縮放（根據 `MaxSize` 設定）

3. **準確度 vs 速度權衡**
   - 提高 `MaxSize` 會提升準確度但降低速度
   - 根據實際需求調整參數

### ⚠️ 常見問題

**Q: 為什麼有些文字沒有被識別？**
- 檢查 `BoxScoreThreshold` 是否過高，嘗試降低至 0.5
- 檢查 `MinConfidence` 過濾閾值
- 嘗試提高 `MaxSize` 以處理小字

**Q: 為什麼有很多誤判的區域？**
- 提高 `BoxScoreThreshold` 至 0.7-0.8
- 提高 `MinConfidence` 過濾閾值
- 使用 `ResultProcessor` 進行後處理

**Q: 處理速度太慢怎麼辦？**
- 降低 `MaxSize` 至 800-960
- 確認圖片解析度不要過高
- 考慮使用非同步處理提升吞吐量

**Q: 如何支援其他語言？**
- 修改 `Language` 設定：`"Chinese"`, `"English"`, `"Korean"` 等
- PaddleOCR 會自動下載對應的語言模型

**Q: 可以離線使用嗎？**
- 首次執行需要網路下載模型
- 模型下載後可離線使用
- 也可以預先下載模型並設定 `ModelPath`

## 測試

完整的單元測試和整合測試請參考 `MangaOCR.Tests` 專案：

```bash
cd MangaOCR.Tests
dotnet test
```

### 生成 Debug 圖片

執行以下測試可生成視覺化標註圖片（保存在 TestData 目錄）：

```bash
dotnet test --filter "FullyQualifiedName~GenerateDebugAnnotationImages"
```

## 授權

本專案使用的主要依賴套件授權：
- PaddleOCR: Apache 2.0
- OpenCvSharp: Apache 2.0
- ImageSharp: Apache 2.0

## 技術支援

如有問題或建議，請參考：
- 測試專案範例：`MangaOCR.Tests/OcrIntegrationTests.cs`
- 示範程式註解：`MangaOCR/Program.cs`

---

**版本**: 1.0.0
**最後更新**: 2025-11-13
**最低 .NET 版本**: 9.0
