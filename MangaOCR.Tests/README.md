# MangaOCR.Tests - 測試專案

## 測試框架

- **xUnit** - 主要測試框架
- **Microsoft.NET.Test.Sdk** - .NET測試SDK
- **Coverlet** - 程式碼覆蓋率工具

## 執行測試

```bash
# 執行所有測試
dotnet test

# 執行測試並顯示詳細輸出
dotnet test --verbosity detailed

# 執行測試並產生覆蓋率報告
dotnet test /p:CollectCoverage=true
```

## 測試結構

```
MangaOCR.Tests/
├── Services/       # 服務類別測試
├── Utils/          # 工具類別測試
├── Models/         # 模型測試
└── SetupTests.cs   # 環境設置測試
```

## 測試命名規範

測試方法命名遵循以下格式：
```
[被測試方法]_[測試情境]_[期望結果]
```

例如：
- `LoadImage_WithValidPath_ShouldReturnImage()`
- `ProcessImage_WithNullInput_ShouldThrowException()`

## 測試涵蓋範圍

每個功能模組都需要包含：
- 正常情況測試
- 邊界條件測試
- 異常處理測試
- 效能測試（如需要）
