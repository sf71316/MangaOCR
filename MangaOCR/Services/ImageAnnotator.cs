using MangaOCR.Models;
using OpenCvSharp;

namespace MangaOCR.Services;

/// <summary>
/// 圖片標註器 - 在圖片上標註OCR識別結果
/// </summary>
public class ImageAnnotator
{
    /// <summary>
    /// 在圖片上標註閱讀順序編號
    /// </summary>
    /// <param name="imagePath">原始圖片路徑</param>
    /// <param name="orderedRegions">已排序的文字區域（含閱讀順序）</param>
    /// <param name="outputPath">輸出圖片路徑</param>
    public void AnnotateReadingOrder(
        string imagePath,
        List<(TextRegion Region, int Order)> orderedRegions,
        string outputPath)
    {
        // 載入圖片
        using var image = Cv2.ImRead(imagePath);
        if (image.Empty())
        {
            throw new InvalidOperationException($"無法載入圖片: {imagePath}");
        }

        // 為每個文字區域標註編號
        foreach (var (region, order) in orderedRegions)
        {
            var box = region.BoundingBox;

            // 繪製邊界框（綠色）
            var rect = new Rect(box.X, box.Y, box.Width, box.Height);
            Cv2.Rectangle(image, rect, new Scalar(0, 255, 0), 2);

            // 計算編號顯示位置（左上角）
            var textPos = new OpenCvSharp.Point(box.X, box.Y - 5);
            if (textPos.Y < 20)
                textPos.Y = box.Y + 20; // 如果太靠近頂部，往下移

            // 繪製編號背景（白色矩形）
            var text = order.ToString();
            var textSize = Cv2.GetTextSize(text, HersheyFonts.HersheySimplex, 0.8, 2, out var baseline);
            var bgRect = new Rect(
                textPos.X - 2,
                textPos.Y - textSize.Height - 2,
                textSize.Width + 4,
                textSize.Height + baseline + 4
            );
            Cv2.Rectangle(image, bgRect, new Scalar(255, 255, 255), -1);

            // 繪製編號文字（紅色，粗體）
            Cv2.PutText(
                image,
                text,
                textPos,
                HersheyFonts.HersheySimplex,
                0.8,
                new Scalar(0, 0, 255),
                2
            );

            // 在區域中心繪製小圓點
            var centerX = box.X + box.Width / 2;
            var centerY = box.Y + box.Height / 2;
            Cv2.Circle(image, new OpenCvSharp.Point(centerX, centerY), 3, new Scalar(255, 0, 0), -1);
        }

        // 在圖片上添加說明文字
        var title = "Reading Order (Right to Left, Top to Bottom)";
        Cv2.PutText(
            image,
            title,
            new OpenCvSharp.Point(10, 30),
            HersheyFonts.HersheySimplex,
            0.7,
            new Scalar(0, 0, 0),
            2
        );

        // 儲存標註後的圖片
        Cv2.ImWrite(outputPath, image);
        Console.WriteLine($"✓ 標註完成，已儲存到: {outputPath}");
    }

    /// <summary>
    /// 在圖片上標註信心度資訊
    /// </summary>
    public void AnnotateConfidence(
        string imagePath,
        List<TextRegion> regions,
        string outputPath)
    {
        using var image = Cv2.ImRead(imagePath);
        if (image.Empty())
        {
            throw new InvalidOperationException($"無法載入圖片: {imagePath}");
        }

        foreach (var region in regions)
        {
            var box = region.BoundingBox;
            Scalar color;

            // 根據信心度選擇顏色
            if (float.IsNaN(region.Confidence) || string.IsNullOrWhiteSpace(region.Text))
            {
                color = new Scalar(128, 128, 128); // 灰色：失敗
            }
            else if (region.Confidence >= 0.7f)
            {
                color = new Scalar(0, 255, 0); // 綠色：高信心度
            }
            else if (region.Confidence >= 0.5f)
            {
                color = new Scalar(0, 255, 255); // 黃色：中信心度
            }
            else
            {
                color = new Scalar(0, 0, 255); // 紅色：低信心度
            }

            // 繪製邊界框
            var rect = new Rect(box.X, box.Y, box.Width, box.Height);
            Cv2.Rectangle(image, rect, color, 2);
        }

        // 添加圖例
        var legendY = 60;
        DrawLegend(image, 10, legendY, "High (>=70%)", new Scalar(0, 255, 0));
        DrawLegend(image, 10, legendY + 30, "Medium (50-70%)", new Scalar(0, 255, 255));
        DrawLegend(image, 10, legendY + 60, "Low (<50%)", new Scalar(0, 0, 255));
        DrawLegend(image, 10, legendY + 90, "Failed", new Scalar(128, 128, 128));

        Cv2.ImWrite(outputPath, image);
        Console.WriteLine($"✓ 信心度標註完成，已儲存到: {outputPath}");
    }

    private void DrawLegend(Mat image, int x, int y, string text, Scalar color)
    {
        // 繪製色塊
        Cv2.Rectangle(image, new Rect(x, y, 20, 20), color, -1);
        Cv2.Rectangle(image, new Rect(x, y, 20, 20), new Scalar(0, 0, 0), 1);

        // 繪製文字
        Cv2.PutText(
            image,
            text,
            new OpenCvSharp.Point(x + 25, y + 15),
            HersheyFonts.HersheySimplex,
            0.5,
            new Scalar(0, 0, 0),
            1
        );
    }
}
