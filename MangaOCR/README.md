# MangaOCR - 漫畫文字識別工具

使用.NET 9開發的漫畫OCR工具，可以將漫畫圖片中的文字提取出來。

## 專案結構

```
MangaOCR/
├── Models/         # 資料模型
├── Services/       # 核心服務（OCR、影像處理等）
├── Utils/          # 工具類別
├── Interfaces/     # 介面定義
└── Program.cs      # 主程式進入點
```

## 技術堆疊

- **.NET 9.0**
- 目標平台：Windows/Linux/macOS

### 核心套件

- **Sdcb.PaddleOCR** (3.0.1) - PaddleOCR引擎，專為亞洲語言（中文/日文）優化
- **Sdcb.PaddleOCR.Models.LocalV3** (2.7.0.1) - 預訓練模型
- **SixLabors.ImageSharp** (3.1.12) - 現代化的跨平台影像處理庫
- **OpenCvSharp4** (4.11.0) - OpenCV的.NET包裝（PaddleOCR依賴）

## 開發進度

- [x] 建立專案架構
- [x] 建立單元測試專案
- [x] 選擇並整合OCR引擎（PaddleOCR）
- [x] 安裝必要的NuGet套件
- [ ] 實作影像處理功能
- [ ] 實作文字識別功能
- [ ] 多語言支援

## 計劃支援的功能

- 支援常見漫畫圖片格式（JPG, PNG, WEBP等）
- 影像預處理（二值化、去噪、對比度調整）
- 文字區域自動檢測
- 多語言識別（繁中、簡中、日文、英文）
- 多種輸出格式（TXT, JSON, XML）
