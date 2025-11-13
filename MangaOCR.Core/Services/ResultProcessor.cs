using MangaOCR.Interfaces;
using MangaOCR.Models;
using System.Text.RegularExpressions;

namespace MangaOCR.Services;

/// <summary>
/// OCR結果後處理服務
/// </summary>
public class ResultProcessor : IResultProcessor
{
    /// <summary>
    /// 處理OCR結果，應用所有優化策略
    /// </summary>
    public OcrResult Process(OcrResult result, float minConfidence = 0.5f)
    {
        if (result == null || !result.Success)
            return result ?? new OcrResult { Success = false, ErrorMessage = "輸入結果為null" };

        // 1. 移除空白區域
        result = RemoveEmptyRegions(result);

        // 2. 過濾低信心度
        result = FilterByConfidence(result, minConfidence);

        // 3. 移除重複區域
        result = RemoveDuplicates(result);

        // 4. 清理文字內容
        result = CleanText(result);

        return result;
    }

    /// <summary>
    /// 過濾低信心度的文字區域
    /// </summary>
    public OcrResult FilterByConfidence(OcrResult result, float minConfidence)
    {
        if (result == null || !result.Success)
            return result ?? new OcrResult { Success = false, ErrorMessage = "輸入結果為null" };

        var filteredRegions = result.TextRegions
            .Where(r => !float.IsNaN(r.Confidence) && r.Confidence >= minConfidence)
            .ToList();

        return new OcrResult
        {
            Success = true,
            TextRegions = filteredRegions,
            ElapsedMilliseconds = result.ElapsedMilliseconds,
            ErrorMessage = result.ErrorMessage
        };
    }

    /// <summary>
    /// 移除空白或無效的文字區域
    /// </summary>
    public OcrResult RemoveEmptyRegions(OcrResult result)
    {
        if (result == null || !result.Success)
            return result ?? new OcrResult { Success = false, ErrorMessage = "輸入結果為null" };

        var nonEmptyRegions = result.TextRegions
            .Where(r => !string.IsNullOrWhiteSpace(r.Text))
            .ToList();

        return new OcrResult
        {
            Success = true,
            TextRegions = nonEmptyRegions,
            ElapsedMilliseconds = result.ElapsedMilliseconds,
            ErrorMessage = result.ErrorMessage
        };
    }

    /// <summary>
    /// 移除重疊或重複的文字區域
    /// </summary>
    public OcrResult RemoveDuplicates(OcrResult result, float overlapThreshold = 0.95f)
    {
        if (result == null || !result.Success || result.TextRegions.Count == 0)
            return result ?? new OcrResult { Success = false, ErrorMessage = "輸入結果為null" };

        var uniqueRegions = new List<TextRegion>();
        var processedRegions = new HashSet<int>();

        // 按信心度排序（保留信心度高的）
        var sortedRegions = result.TextRegions
            .Select((region, index) => new { Region = region, Index = index })
            .OrderByDescending(x => x.Region.Confidence)
            .ToList();

        foreach (var item in sortedRegions)
        {
            if (processedRegions.Contains(item.Index))
                continue;

            var currentRegion = item.Region;
            uniqueRegions.Add(currentRegion);
            processedRegions.Add(item.Index);

            // 檢查其他區域是否與當前區域重疊
            for (int i = 0; i < result.TextRegions.Count; i++)
            {
                if (processedRegions.Contains(i))
                    continue;

                var otherRegion = result.TextRegions[i];
                if (IsOverlapping(currentRegion.BoundingBox, otherRegion.BoundingBox, overlapThreshold))
                {
                    processedRegions.Add(i);
                }
            }
        }

        return new OcrResult
        {
            Success = true,
            TextRegions = uniqueRegions.OrderBy(r => r.BoundingBox.Y).ThenBy(r => r.BoundingBox.X).ToList(),
            ElapsedMilliseconds = result.ElapsedMilliseconds,
            ErrorMessage = result.ErrorMessage
        };
    }

    /// <summary>
    /// 清理文字內容
    /// </summary>
    public OcrResult CleanText(OcrResult result)
    {
        if (result == null || !result.Success)
            return result ?? new OcrResult { Success = false, ErrorMessage = "輸入結果為null" };

        var cleanedRegions = result.TextRegions.Select(region =>
        {
            var cleanedText = CleanTextContent(region.Text);
            return new TextRegion
            {
                Text = cleanedText,
                Confidence = region.Confidence,
                BoundingBox = region.BoundingBox
            };
        }).ToList();

        return new OcrResult
        {
            Success = true,
            TextRegions = cleanedRegions,
            ElapsedMilliseconds = result.ElapsedMilliseconds,
            ErrorMessage = result.ErrorMessage
        };
    }

    /// <summary>
    /// 清理文字內容（移除無效字符、正規化空白）
    /// </summary>
    private string CleanTextContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // 移除控制字符（保留常見的CJK字符和假名）
        text = Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

        // 正規化空白字符
        text = Regex.Replace(text, @"\s+", " ");

        // 移除前後空白
        text = text.Trim();

        return text;
    }

    /// <summary>
    /// 檢查兩個邊界框是否重疊
    /// </summary>
    private bool IsOverlapping(BoundingBox box1, BoundingBox box2, float threshold)
    {
        // 計算交集面積
        int x1 = Math.Max(box1.X, box2.X);
        int y1 = Math.Max(box1.Y, box2.Y);
        int x2 = Math.Min(box1.X + box1.Width, box2.X + box2.Width);
        int y2 = Math.Min(box1.Y + box1.Height, box2.Y + box2.Height);

        if (x2 <= x1 || y2 <= y1)
            return false; // 沒有交集

        int intersectionArea = (x2 - x1) * (y2 - y1);
        int box1Area = box1.Width * box1.Height;
        int box2Area = box2.Width * box2.Height;
        int minArea = Math.Min(box1Area, box2Area);

        // 計算交集佔較小框的比例
        float overlapRatio = (float)intersectionArea / minArea;

        return overlapRatio >= threshold;
    }
}
