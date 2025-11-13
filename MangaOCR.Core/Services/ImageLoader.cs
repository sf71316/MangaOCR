using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using MangaOCR.Interfaces;

namespace MangaOCR.Services;

/// <summary>
/// 影像載入器實作
/// 支援常見漫畫圖片格式：JPG, PNG, BMP, GIF, WEBP等
/// </summary>
public class ImageLoader : IImageLoader
{
    private static readonly string[] SupportedExtensions =
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff", ".tif"
    };

    /// <summary>
    /// 從檔案路徑載入影像
    /// </summary>
    /// <param name="filePath">影像檔案路徑</param>
    /// <returns>載入的影像</returns>
    /// <exception cref="ArgumentNullException">檔案路徑為null</exception>
    /// <exception cref="FileNotFoundException">檔案不存在</exception>
    /// <exception cref="NotSupportedException">不支援的檔案格式</exception>
    public Image<Rgba32> LoadImage(string filePath)
    {
        ValidateFilePath(filePath);

        try
        {
            return Image.Load<Rgba32>(filePath);
        }
        catch (UnknownImageFormatException ex)
        {
            throw new NotSupportedException(
                $"不支援的影像格式: {Path.GetExtension(filePath)}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"載入影像時發生錯誤: {filePath}", ex);
        }
    }

    /// <summary>
    /// 從檔案路徑載入影像（非同步）
    /// </summary>
    /// <param name="filePath">影像檔案路徑</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>載入的影像</returns>
    public async Task<Image<Rgba32>> LoadImageAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ValidateFilePath(filePath);

        try
        {
            return await Image.LoadAsync<Rgba32>(filePath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 取消操作直接拋出，不包裝
            throw;
        }
        catch (UnknownImageFormatException ex)
        {
            throw new NotSupportedException(
                $"不支援的影像格式: {Path.GetExtension(filePath)}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"載入影像時發生錯誤: {filePath}", ex);
        }
    }

    /// <summary>
    /// 檢查檔案格式是否支援
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <returns>是否支援</returns>
    public bool IsSupportedFormat(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    /// <summary>
    /// 取得支援的檔案格式列表
    /// </summary>
    /// <returns>支援的副檔名陣列</returns>
    public string[] GetSupportedFormats()
    {
        return SupportedExtensions.ToArray();
    }

    /// <summary>
    /// 驗證檔案路徑
    /// </summary>
    private void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath), "檔案路徑不可為空");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"找不到檔案: {filePath}", filePath);
        }

        if (!IsSupportedFormat(filePath))
        {
            throw new NotSupportedException(
                $"不支援的檔案格式: {Path.GetExtension(filePath)}。" +
                $"支援的格式: {string.Join(", ", SupportedExtensions)}");
        }
    }
}
