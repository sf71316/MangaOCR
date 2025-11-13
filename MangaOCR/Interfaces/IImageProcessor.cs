using OpenCvSharp;

namespace MangaOCR.Interfaces;

/// <summary>
/// 影像處理器介面
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// 預處理影像以提升OCR準確度
    /// </summary>
    /// <param name="source">原始影像</param>
    /// <returns>處理後的影像</returns>
    Mat PreprocessForOcr(Mat source);

    /// <summary>
    /// 二值化處理
    /// </summary>
    Mat Binarize(Mat source);

    /// <summary>
    /// 去噪處理
    /// </summary>
    Mat Denoise(Mat source);

    /// <summary>
    /// 增強對比度
    /// </summary>
    Mat EnhanceContrast(Mat source);

    /// <summary>
    /// 銳化處理
    /// </summary>
    Mat Sharpen(Mat source);
}
