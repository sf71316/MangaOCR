using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MangaOCR.Interfaces;

/// <summary>
/// 影像載入器介面
/// </summary>
public interface IImageLoader
{
    /// <summary>
    /// 從檔案路徑載入影像
    /// </summary>
    /// <param name="filePath">影像檔案路徑</param>
    /// <returns>載入的影像</returns>
    Image<Rgba32> LoadImage(string filePath);

    /// <summary>
    /// 從檔案路徑載入影像（非同步）
    /// </summary>
    /// <param name="filePath">影像檔案路徑</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>載入的影像</returns>
    Task<Image<Rgba32>> LoadImageAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查檔案格式是否支援
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <returns>是否支援</returns>
    bool IsSupportedFormat(string filePath);

    /// <summary>
    /// 取得支援的檔案格式列表
    /// </summary>
    /// <returns>支援的副檔名陣列</returns>
    string[] GetSupportedFormats();
}
