using MangaOCR.Models;

namespace MangaOCR.Services;

/// <summary>
/// 文字順序分析器 - 用於檢測和排序OCR結果的閱讀順序
/// </summary>
public class TextOrderAnalyzer
{
    /// <summary>
    /// 閱讀方向
    /// </summary>
    public enum ReadingDirection
    {
        /// <summary>從左到右，從上到下（英文、簡體中文）</summary>
        LeftToRightTopToBottom,

        /// <summary>從右到左，從上到下（日文漫畫、繁體中文）</summary>
        RightToLeftTopToBottom,

        /// <summary>從上到下，從右到左（傳統直排）</summary>
        TopToBottomRightToLeft,

        /// <summary>自動檢測</summary>
        Auto
    }

    /// <summary>
    /// 根據指定的閱讀方向排序文字區域
    /// </summary>
    public List<TextRegion> SortByReadingOrder(List<TextRegion> regions, ReadingDirection direction = ReadingDirection.Auto)
    {
        if (regions == null || regions.Count == 0)
            return regions;

        // 如果是自動模式，嘗試檢測閱讀方向
        if (direction == ReadingDirection.Auto)
        {
            direction = DetectReadingDirection(regions);
        }

        return direction switch
        {
            ReadingDirection.LeftToRightTopToBottom => SortLeftToRightTopToBottom(regions),
            ReadingDirection.RightToLeftTopToBottom => SortRightToLeftTopToBottom(regions),
            ReadingDirection.TopToBottomRightToLeft => SortTopToBottomRightToLeft(regions),
            _ => regions
        };
    }

    /// <summary>
    /// 分析OCR結果返回的原始順序
    /// </summary>
    public string AnalyzeOriginalOrder(List<TextRegion> regions)
    {
        if (regions == null || regions.Count < 2)
            return "區域數量不足，無法分析順序";

        var analysis = new System.Text.StringBuilder();
        analysis.AppendLine("原始OCR返回順序分析：");
        analysis.AppendLine();

        for (int i = 0; i < Math.Min(10, regions.Count); i++)
        {
            var region = regions[i];
            analysis.AppendLine($"[{i + 1}] {region.Text}");
            analysis.AppendLine($"    位置: X={region.BoundingBox.X}, Y={region.BoundingBox.Y}");

            if (i > 0)
            {
                var prev = regions[i - 1];
                var deltaX = region.BoundingBox.X - prev.BoundingBox.X;
                var deltaY = region.BoundingBox.Y - prev.BoundingBox.Y;
                analysis.AppendLine($"    與前一個的位移: ΔX={deltaX:+#;-#;0}, ΔY={deltaY:+#;-#;0}");
            }
            analysis.AppendLine();
        }

        return analysis.ToString();
    }

    /// <summary>
    /// 診斷跨頁檢測結果
    /// </summary>
    public string DiagnosePageDetection(List<TextRegion> regions)
    {
        var pageInfo = DetectPages(regions);
        var result = new System.Text.StringBuilder();

        result.AppendLine("=== 跨頁檢測診斷 ===");
        result.AppendLine($"檢測結果: {(pageInfo.IsTwoPage ? "雙頁並排" : "單頁")}");

        if (pageInfo.IsTwoPage)
        {
            var rightCount = regions.Count(r => r.BoundingBox.X + r.BoundingBox.Width / 2 >= pageInfo.MiddleX);
            var leftCount = regions.Count(r => r.BoundingBox.X + r.BoundingBox.Width / 2 < pageInfo.MiddleX);

            result.AppendLine($"分隔線位置: X = {pageInfo.MiddleX}");
            result.AppendLine($"右頁文字區域: {rightCount} 個");
            result.AppendLine($"左頁文字區域: {leftCount} 個");
            result.AppendLine($"閱讀順序: 先讀完右頁 → 再讀左頁");
            result.AppendLine();
            result.AppendLine("X座標分布:");

            var leftRegions = regions.Where(r => r.BoundingBox.X + r.BoundingBox.Width / 2 < pageInfo.MiddleX)
                .OrderBy(r => r.BoundingBox.X).Take(5);
            var rightRegions = regions.Where(r => r.BoundingBox.X + r.BoundingBox.Width / 2 >= pageInfo.MiddleX)
                .OrderBy(r => r.BoundingBox.X).Take(5);

            result.AppendLine("左頁前5個: " + string.Join(", ", leftRegions.Select(r => $"X={r.BoundingBox.X}")));
            result.AppendLine("右頁前5個: " + string.Join(", ", rightRegions.Select(r => $"X={r.BoundingBox.X}")));
        }

        return result.ToString();
    }

    /// <summary>
    /// 檢測閱讀方向
    /// </summary>
    private ReadingDirection DetectReadingDirection(List<TextRegion> regions)
    {
        if (regions.Count < 3)
            return ReadingDirection.RightToLeftTopToBottom; // 預設日文漫畫模式

        // 計算文字區域的平均寬高比
        var avgAspectRatio = regions.Average(r => (double)r.BoundingBox.Width / r.BoundingBox.Height);

        // 如果寬度遠大於高度，可能是橫排文字
        if (avgAspectRatio > 2.0)
        {
            // 檢查是左到右還是右到左
            var leftToRightCount = 0;
            var rightToLeftCount = 0;

            for (int i = 1; i < Math.Min(10, regions.Count); i++)
            {
                var deltaX = regions[i].BoundingBox.X - regions[i - 1].BoundingBox.X;
                if (Math.Abs(deltaX) > 20) // 有明顯的水平移動
                {
                    if (deltaX > 0) leftToRightCount++;
                    else rightToLeftCount++;
                }
            }

            return rightToLeftCount > leftToRightCount
                ? ReadingDirection.RightToLeftTopToBottom
                : ReadingDirection.LeftToRightTopToBottom;
        }
        else
        {
            // 直排文字
            return ReadingDirection.TopToBottomRightToLeft;
        }
    }

    /// <summary>
    /// 從左到右，從上到下排序（英文模式）
    /// </summary>
    private List<TextRegion> SortLeftToRightTopToBottom(List<TextRegion> regions)
    {
        return regions
            .OrderBy(r => r.BoundingBox.Y / 50) // 先按行分組（允許50像素的誤差）
            .ThenBy(r => r.BoundingBox.X)       // 同一行內從左到右
            .ToList();
    }

    /// <summary>
    /// 從右到左，從上到下排序（日文漫畫模式）
    /// 支援跨頁檢測：如果圖片包含兩頁，會先讀右頁再讀左頁
    /// </summary>
    private List<TextRegion> SortRightToLeftTopToBottom(List<TextRegion> regions)
    {
        if (regions.Count == 0)
            return regions;

        // 檢測是否為跨頁（兩頁並排）
        var pageInfo = DetectPages(regions);

        if (pageInfo.IsTwoPage)
        {
            // 在掃描圖中：右側=實體書右頁（先讀），左側=實體書左頁（後讀）
            var physicalRightPage = regions.Where(r => r.BoundingBox.X >= pageInfo.MiddleX).ToList();
            var physicalLeftPage = regions.Where(r => r.BoundingBox.X < pageInfo.MiddleX).ToList();

            // 分別排序
            var rightSorted = physicalRightPage
                .OrderBy(r => r.BoundingBox.Y / 50)
                .ThenByDescending(r => r.BoundingBox.X)
                .ToList();

            var leftSorted = physicalLeftPage
                .OrderBy(r => r.BoundingBox.Y / 50)
                .ThenByDescending(r => r.BoundingBox.X)
                .ToList();

            // 先讀實體書的右頁（圖片右側），再讀實體書的左頁（圖片左側）
            var result = new List<TextRegion>();
            result.AddRange(rightSorted);
            result.AddRange(leftSorted);
            return result;
        }
        else
        {
            // 單頁，直接排序
            return regions
                .OrderBy(r => r.BoundingBox.Y / 50)
                .ThenByDescending(r => r.BoundingBox.X)
                .ToList();
        }
    }

    /// <summary>
    /// 檢測圖片是否包含兩頁並排
    /// </summary>
    private (bool IsTwoPage, int MiddleX) DetectPages(List<TextRegion> regions)
    {
        if (regions.Count < 4)
            return (false, 0);

        // 收集所有區域的 X 座標
        var xCoords = regions.Select(r => r.BoundingBox.X + r.BoundingBox.Width / 2).OrderBy(x => x).ToList();

        // 計算圖片寬度
        var minX = regions.Min(r => r.BoundingBox.X);
        var maxX = regions.Max(r => r.BoundingBox.X + r.BoundingBox.Width);
        var imageWidth = maxX - minX;

        // 檢查是否有明顯的左右分組
        // 策略：看文字區域的 X 座標分布是否有明顯的間隙
        var middleX = minX + imageWidth / 2;

        // 計算左半部和右半部的文字密度
        var leftCount = regions.Count(r => r.BoundingBox.X + r.BoundingBox.Width / 2 < middleX);
        var rightCount = regions.Count(r => r.BoundingBox.X + r.BoundingBox.Width / 2 >= middleX);

        // 如果兩邊都有文字，且數量相對均衡（不是90:10這種極端情況）
        var minRatio = Math.Min(leftCount, rightCount) / (double)Math.Max(leftCount, rightCount);
        var isTwoPage = leftCount >= 3 && rightCount >= 3 && minRatio >= 0.4;

        // 如果是兩頁，尋找更精確的分隔線
        if (isTwoPage)
        {
            // 尋找 X 座標分布的最大間隙
            var maxGap = 0;
            var gapPosition = middleX;

            for (int i = 1; i < xCoords.Count; i++)
            {
                var gap = xCoords[i] - xCoords[i - 1];
                if (gap > maxGap && xCoords[i] > minX + imageWidth * 0.3 && xCoords[i] < minX + imageWidth * 0.7)
                {
                    maxGap = gap;
                    gapPosition = (xCoords[i] + xCoords[i - 1]) / 2;
                }
            }

                // 如果找到明顯的間隙（大於圖片寬度的5%），檢查分隔後是否平衡
            if (maxGap > imageWidth * 0.05)
            {
                var testLeftCount = regions.Count(r => r.BoundingBox.X + r.BoundingBox.Width / 2 < gapPosition);
                var testRightCount = regions.Count(r => r.BoundingBox.X + r.BoundingBox.Width / 2 >= gapPosition);
                var testRatio = Math.Min(testLeftCount, testRightCount) / (double)Math.Max(testLeftCount, testRightCount);

                // 只有在分隔後仍然相對平衡時，才使用間隙位置
                if (testRatio >= 0.3)
                {
                    middleX = gapPosition;
                }
                // 否則保持使用圖片中點
            }
        }

        return (isTwoPage, middleX);
    }

    /// <summary>
    /// 從上到下，從右到左排序（直排模式）
    /// </summary>
    private List<TextRegion> SortTopToBottomRightToLeft(List<TextRegion> regions)
    {
        return regions
            .OrderByDescending(r => r.BoundingBox.X / 50) // 先按列分組（從右到左）
            .ThenBy(r => r.BoundingBox.Y)                  // 同一列內從上到下
            .ToList();
    }

    /// <summary>
    /// 為文字區域分配閱讀順序編號
    /// </summary>
    public List<(TextRegion Region, int ReadingOrder)> AssignReadingOrder(
        List<TextRegion> regions,
        ReadingDirection direction = ReadingDirection.Auto)
    {
        var sorted = SortByReadingOrder(regions, direction);
        return sorted.Select((region, index) => (region, index + 1)).ToList();
    }
}
