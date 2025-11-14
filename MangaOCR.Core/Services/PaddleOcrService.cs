using System.Diagnostics;
using MangaOCR.Interfaces;
using MangaOCR.Models;
using OpenCvSharp;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;

namespace MangaOCR.Services;

/// <summary>
/// PaddleOCR服務實作
/// </summary>
public class PaddleOcrService : IOcrService
{
    private readonly PaddleOcrAll _ocr;
    private readonly IImageProcessor _imageProcessor;
    private bool _disposed;
    private readonly bool _usePreprocessing;

    /// <summary>
    /// 建立PaddleOCR服務（使用預設配置：日文模型）
    /// </summary>
    public PaddleOcrService() : this(new OcrSettings())
    {
    }

    /// <summary>
    /// 建立PaddleOCR服務（使用日文+中文模型，舊版建構函數，保留向後兼容）
    /// </summary>
    /// <param name="usePreprocessing">是否啟用影像預處理（預設false，因為PaddleOCR對原圖效果更好）</param>
    [Obsolete("請使用接受 OcrSettings 參數的建構函數")]
    public PaddleOcrService(bool usePreprocessing) : this(new OcrSettings { UsePreprocessing = usePreprocessing })
    {
    }

    /// <summary>
    /// 建立PaddleOCR服務（使用自訂配置）
    /// </summary>
    /// <param name="settings">OCR配置</param>
    public PaddleOcrService(OcrSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _usePreprocessing = settings.UsePreprocessing;
        _imageProcessor = new ImageProcessor();

        // 根據語言選擇對應的線上模型
        var model = settings.Language.ToLowerInvariant() switch
        {
            "japanese" or "ja" or "日文" => OnlineFullModels.JapanV4.DownloadAsync().GetAwaiter().GetResult(),
            "chinese" or "zh" or "中文" => OnlineFullModels.ChineseV4.DownloadAsync().GetAwaiter().GetResult(),
            "chinesetraditional" or "zh-tw" or "繁體中文" => OnlineFullModels.ChineseV4.DownloadAsync().GetAwaiter().GetResult(),
            "chinesesimplified" or "zh-cn" or "簡體中文" => OnlineFullModels.ChineseV4.DownloadAsync().GetAwaiter().GetResult(),
            "english" or "en" or "英文" => OnlineFullModels.EnglishV4.DownloadAsync().GetAwaiter().GetResult(),
            "korean" or "ko" or "韓文" => OnlineFullModels.KoreanV4.DownloadAsync().GetAwaiter().GetResult(),
            _ => OnlineFullModels.JapanV4.DownloadAsync().GetAwaiter().GetResult() // 預設日文
        };

        _ocr = new PaddleOcrAll(model)
        {
            AllowRotateDetection = settings.AllowRotateDetection,
            Enable180Classification = settings.Enable180Classification,
        };

        // 應用偵測參數以提升準確度
        _ocr.Detector.UnclipRatio = settings.UnclipRatio;
        _ocr.Detector.MaxSize = settings.MaxSize;
    }

    /// <summary>
    /// 從檔案路徑進行OCR識別
    /// </summary>
    public OcrResult RecognizeText(string imagePath)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PaddleOcrService));
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentNullException(nameof(imagePath));
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"找不到圖片檔案: {imagePath}");
        }

        var stopwatch = Stopwatch.StartNew();
        var result = new OcrResult { Success = true };

        try
        {
            // 使用OpenCV載入圖片
            using var src = Cv2.ImRead(imagePath);

            if (src.Empty())
            {
                throw new InvalidOperationException($"無法載入圖片: {imagePath}");
            }

            // 影像預處理（如果啟用）
            Mat processedImage = src;
            if (_usePreprocessing)
            {
                processedImage = _imageProcessor.PreprocessForOcr(src);
            }

            try
            {
                // 執行OCR
                var ocrResult = _ocr.Run(processedImage);

                // 轉換結果
                foreach (var region in ocrResult.Regions)
                {
                    var textRegion = new TextRegion
                    {
                        Text = region.Text,
                        Confidence = region.Score,
                        BoundingBox = new BoundingBox()
                    };

                    // 從RotatedRect獲取四個角點
                    var points = region.Rect.Points();
                    foreach (var p in points)
                    {
                        textRegion.BoundingBox.Points.Add(new Models.Point((int)p.X, (int)p.Y));
                    }

                    // 計算邊界框
                    if (textRegion.BoundingBox.Points.Count > 0)
                    {
                        var minX = textRegion.BoundingBox.Points.Min(p => p.X);
                        var minY = textRegion.BoundingBox.Points.Min(p => p.Y);
                        var maxX = textRegion.BoundingBox.Points.Max(p => p.X);
                        var maxY = textRegion.BoundingBox.Points.Max(p => p.Y);

                        textRegion.BoundingBox.X = minX;
                        textRegion.BoundingBox.Y = minY;
                        textRegion.BoundingBox.Width = maxX - minX;
                        textRegion.BoundingBox.Height = maxY - minY;
                    }

                    result.TextRegions.Add(textRegion);
                }

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
            finally
            {
                // 釋放預處理的影像
                if (_usePreprocessing && processedImage != src)
                {
                    processedImage?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 從檔案路徑進行OCR識別（非同步）
    /// </summary>
    public Task<OcrResult> RecognizeTextAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        // PaddleOCR目前不支援真正的非同步操作，所以用Task.Run包裝
        return Task.Run(() => RecognizeText(imagePath), cancellationToken);
    }

    /// <summary>
    /// 只檢測文字區域座標，不識別文字內容（快速模式）
    /// </summary>
    public List<TextRegion> DetectTextRegions(string imagePath)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PaddleOcrService));
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentNullException(nameof(imagePath));
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"找不到圖片檔案: {imagePath}");
        }

        var regions = new List<TextRegion>();

        try
        {
            using var src = Cv2.ImRead(imagePath);

            if (src.Empty())
            {
                throw new InvalidOperationException($"無法載入圖片: {imagePath}");
            }

            // 影像預處理（如果啟用）
            Mat processedImage = src;
            if (_usePreprocessing)
            {
                processedImage = _imageProcessor.PreprocessForOcr(src);
            }

            try
            {
                // 只執行檢測階段
                var detectedRects = _ocr.Detector.Run(processedImage);

                // 轉換為 TextRegion 格式（只包含座標，不包含文字）
                foreach (var rect in detectedRects)
                {
                    var textRegion = new TextRegion
                    {
                        Text = string.Empty,  // 不識別文字內容
                        Confidence = float.NaN,  // 檢測階段沒有信心度
                        BoundingBox = new BoundingBox()
                    };

                    // 從 RotatedRect 獲取四個角點
                    var points = rect.Points();
                    foreach (var p in points)
                    {
                        textRegion.BoundingBox.Points.Add(new Models.Point((int)p.X, (int)p.Y));
                    }

                    // 計算邊界框
                    if (textRegion.BoundingBox.Points.Count > 0)
                    {
                        var minX = textRegion.BoundingBox.Points.Min(p => p.X);
                        var minY = textRegion.BoundingBox.Points.Min(p => p.Y);
                        var maxX = textRegion.BoundingBox.Points.Max(p => p.X);
                        var maxY = textRegion.BoundingBox.Points.Max(p => p.Y);

                        textRegion.BoundingBox.X = minX;
                        textRegion.BoundingBox.Y = minY;
                        textRegion.BoundingBox.Width = maxX - minX;
                        textRegion.BoundingBox.Height = maxY - minY;
                    }

                    regions.Add(textRegion);
                }
            }
            finally
            {
                if (_usePreprocessing && processedImage != src)
                {
                    processedImage?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"檢測文字區域失敗: {ex.Message}", ex);
        }

        return regions;
    }

    /// <summary>
    /// 只檢測文字區域座標（非同步）
    /// </summary>
    public Task<List<TextRegion>> DetectTextRegionsAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => DetectTextRegions(imagePath), cancellationToken);
    }

    /// <summary>
    /// 只識別單一文字區域（跳過檢測階段，極速模式）
    /// </summary>
    public OcrResult RecognizeTextOnly(string imagePath)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PaddleOcrService));
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentNullException(nameof(imagePath));
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"找不到圖片檔案: {imagePath}");
        }

        var stopwatch = Stopwatch.StartNew();
        var result = new OcrResult { Success = true };

        try
        {
            using var src = Cv2.ImRead(imagePath);

            if (src.Empty())
            {
                throw new InvalidOperationException($"無法載入圖片: {imagePath}");
            }

            // 影像預處理（如果啟用）
            Mat processedImage = src;
            if (_usePreprocessing)
            {
                processedImage = _imageProcessor.PreprocessForOcr(src);
            }

            try
            {
                // 只執行識別階段（假設整張圖片就是一個文字區域）
                var recognizerResult = _ocr.Recognizer.Run(processedImage);

                // 由於沒有檢測階段，無法獲得精確的信心度，使用整張圖片作為邊界框
                var textRegion = new TextRegion
                {
                    Text = recognizerResult.Text,
                    Confidence = recognizerResult.Score,
                    BoundingBox = new BoundingBox
                    {
                        X = 0,
                        Y = 0,
                        Width = processedImage.Width,
                        Height = processedImage.Height
                    }
                };

                // 添加四個角點
                textRegion.BoundingBox.Points.Add(new Models.Point(0, 0));
                textRegion.BoundingBox.Points.Add(new Models.Point(processedImage.Width, 0));
                textRegion.BoundingBox.Points.Add(new Models.Point(processedImage.Width, processedImage.Height));
                textRegion.BoundingBox.Points.Add(new Models.Point(0, processedImage.Height));

                result.TextRegions.Add(textRegion);

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
            finally
            {
                if (_usePreprocessing && processedImage != src)
                {
                    processedImage?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 只識別單一文字區域（非同步）
    /// </summary>
    public Task<OcrResult> RecognizeTextOnlyAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => RecognizeTextOnly(imagePath), cancellationToken);
    }

    /// <summary>
    /// 批次識別多個已截取的文字圖片
    /// </summary>
    public List<OcrResult> RecognizeTextBatch(List<string> imagePaths)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PaddleOcrService));
        }

        if (imagePaths == null || imagePaths.Count == 0)
        {
            throw new ArgumentException("圖片路徑列表不能為空", nameof(imagePaths));
        }

        var results = new List<OcrResult>();

        foreach (var imagePath in imagePaths)
        {
            try
            {
                var result = RecognizeTextOnly(imagePath);
                results.Add(result);
            }
            catch (Exception ex)
            {
                // 即使某個圖片失敗，也繼續處理其他圖片
                results.Add(new OcrResult
                {
                    Success = false,
                    ErrorMessage = $"處理 {imagePath} 失敗: {ex.Message}"
                });
            }
        }

        return results;
    }

    /// <summary>
    /// 批次識別多個已截取的文字圖片（非同步）
    /// </summary>
    public Task<List<OcrResult>> RecognizeTextBatchAsync(List<string> imagePaths, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => RecognizeTextBatch(imagePaths), cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _ocr?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
