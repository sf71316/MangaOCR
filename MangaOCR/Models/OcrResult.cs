namespace MangaOCR.Models;

/// <summary>
/// OCR識別結果
/// </summary>
public class OcrResult
{
    /// <summary>
    /// 識別的文字區域集合
    /// </summary>
    public List<TextRegion> TextRegions { get; set; } = new();

    /// <summary>
    /// 完整的識別文字（所有區域合併）
    /// </summary>
    public string FullText => string.Join(Environment.NewLine, TextRegions.Select(r => r.Text));

    /// <summary>
    /// 識別耗時（毫秒）
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 錯誤訊息（如果失敗）
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 文字區域
/// </summary>
public class TextRegion
{
    /// <summary>
    /// 識別的文字內容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 信心度（0.0 - 1.0）
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// 文字區域的邊界框
    /// </summary>
    public BoundingBox BoundingBox { get; set; } = new();
}

/// <summary>
/// 邊界框
/// </summary>
public class BoundingBox
{
    /// <summary>
    /// X座標
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y座標
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// 寬度
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 高度
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 四個角點座標
    /// </summary>
    public List<Point> Points { get; set; } = new();
}

/// <summary>
/// 座標點
/// </summary>
public class Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}
