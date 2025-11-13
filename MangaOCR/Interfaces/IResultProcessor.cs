using MangaOCR.Models;

namespace MangaOCR.Interfaces;

/// <summary>
/// OCR結果後處理介面
/// </summary>
public interface IResultProcessor
{
    /// <summary>
    /// 處理OCR結果，過濾和優化識別內容
    /// </summary>
    /// <param name="result">原始OCR結果</param>
    /// <param name="minConfidence">最低信心度閾值（0-1之間）</param>
    /// <returns>處理後的OCR結果</returns>
    OcrResult Process(OcrResult result, float minConfidence = 0.5f);

    /// <summary>
    /// 過濾低信心度的文字區域
    /// </summary>
    /// <param name="result">OCR結果</param>
    /// <param name="minConfidence">最低信心度閾值</param>
    /// <returns>過濾後的結果</returns>
    OcrResult FilterByConfidence(OcrResult result, float minConfidence);

    /// <summary>
    /// 移除空白或無效的文字區域
    /// </summary>
    /// <param name="result">OCR結果</param>
    /// <returns>清理後的結果</returns>
    OcrResult RemoveEmptyRegions(OcrResult result);

    /// <summary>
    /// 移除重疊或重複的文字區域
    /// </summary>
    /// <param name="result">OCR結果</param>
    /// <param name="overlapThreshold">重疊閾值（0-1之間）</param>
    /// <returns>去重後的結果</returns>
    OcrResult RemoveDuplicates(OcrResult result, float overlapThreshold = 0.8f);

    /// <summary>
    /// 清理文字內容（移除無效字符、正規化）
    /// </summary>
    /// <param name="result">OCR結果</param>
    /// <returns>清理後的結果</returns>
    OcrResult CleanText(OcrResult result);
}
