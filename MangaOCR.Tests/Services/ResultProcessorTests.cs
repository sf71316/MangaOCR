using MangaOCR.Models;
using MangaOCR.Services;
using Xunit;

namespace MangaOCR.Tests.Services;

/// <summary>
/// ResultProcessor 單元測試
/// </summary>
public class ResultProcessorTests
{
    private readonly ResultProcessor _processor;

    public ResultProcessorTests()
    {
        _processor = new ResultProcessor();
    }

    [Fact]
    public void FilterByConfidence_WithHighThreshold_ShouldRemoveLowConfidenceRegions()
    {
        // Arrange
        var result = CreateTestResult(new[]
        {
            ("高信心度文字", 0.9f),
            ("中信心度文字", 0.6f),
            ("低信心度文字", 0.3f)
        });

        // Act
        var filtered = _processor.FilterByConfidence(result, 0.5f);

        // Assert
        Assert.Equal(2, filtered.TextRegions.Count);
        Assert.Contains(filtered.TextRegions, r => r.Text == "高信心度文字");
        Assert.Contains(filtered.TextRegions, r => r.Text == "中信心度文字");
        Assert.DoesNotContain(filtered.TextRegions, r => r.Text == "低信心度文字");
    }

    [Fact]
    public void RemoveEmptyRegions_ShouldRemoveEmptyAndWhitespaceText()
    {
        // Arrange
        var result = new OcrResult
        {
            Success = true,
            TextRegions = new List<TextRegion>
            {
                CreateTextRegion("正常文字", 0.8f, 0, 0),
                CreateTextRegion("", 0.5f, 10, 10),
                CreateTextRegion("  ", 0.6f, 20, 20),
                CreateTextRegion("另一段文字", 0.9f, 30, 30)
            }
        };

        // Act
        var cleaned = _processor.RemoveEmptyRegions(result);

        // Assert
        Assert.Equal(2, cleaned.TextRegions.Count);
        Assert.All(cleaned.TextRegions, r => Assert.False(string.IsNullOrWhiteSpace(r.Text)));
    }

    [Fact]
    public void CleanText_ShouldRemoveControlCharactersAndNormalizeWhitespace()
    {
        // Arrange
        var result = new OcrResult
        {
            Success = true,
            TextRegions = new List<TextRegion>
            {
                CreateTextRegion("  前後有空白  ", 0.8f, 0, 0),
                CreateTextRegion("多個  空白  字符", 0.8f, 10, 10)
            }
        };

        // Act
        var cleaned = _processor.CleanText(result);

        // Assert
        Assert.Equal("前後有空白", cleaned.TextRegions[0].Text);
        Assert.Equal("多個 空白 字符", cleaned.TextRegions[1].Text);
    }

    [Fact]
    public void Process_WithMinConfidence_ShouldApplyAllFilters()
    {
        // Arrange
        var result = new OcrResult
        {
            Success = true,
            TextRegions = new List<TextRegion>
            {
                CreateTextRegion("高信心度", 0.9f, 0, 0),
                CreateTextRegion("", 0.8f, 10, 10),
                CreateTextRegion("低信心度", 0.3f, 20, 20),
                CreateTextRegion("  空白  ", 0.7f, 30, 30)
            }
        };

        // Act
        var processed = _processor.Process(result, 0.6f);

        // Assert
        Assert.Equal(2, processed.TextRegions.Count);
        Assert.Contains(processed.TextRegions, r => r.Text == "高信心度");
        Assert.Contains(processed.TextRegions, r => r.Text == "空白");
    }

    [Fact]
    public void Process_WithNullResult_ShouldReturnFailedResult()
    {
        // Act
        var processed = _processor.Process(null!);

        // Assert
        Assert.NotNull(processed);
        Assert.False(processed.Success);
    }

    [Fact]
    public void FilterByConfidence_WithNaNConfidence_ShouldFilterOut()
    {
        // Arrange
        var result = new OcrResult
        {
            Success = true,
            TextRegions = new List<TextRegion>
            {
                CreateTextRegion("正常文字", 0.8f, 0, 0),
                CreateTextRegion("NaN信心度", float.NaN, 10, 10)
            }
        };

        // Act
        var filtered = _processor.FilterByConfidence(result, 0.5f);

        // Assert
        Assert.Single(filtered.TextRegions);
        Assert.Equal("正常文字", filtered.TextRegions[0].Text);
    }

    [Fact]
    public void RemoveDuplicates_WithOverlappingRegions_ShouldKeepHigherConfidence()
    {
        // Arrange - 創建兩個重疊的區域
        var result = new OcrResult
        {
            Success = true,
            TextRegions = new List<TextRegion>
            {
                CreateTextRegionWithBox("高信心度", 0.9f, 0, 0, 100, 50),
                CreateTextRegionWithBox("低信心度", 0.6f, 10, 5, 90, 45), // 大部分重疊
                CreateTextRegionWithBox("不重疊", 0.8f, 200, 0, 100, 50)
            }
        };

        // Act
        var deduplicated = _processor.RemoveDuplicates(result, 0.5f);

        // Assert
        Assert.Equal(2, deduplicated.TextRegions.Count);
        Assert.Contains(deduplicated.TextRegions, r => r.Text == "高信心度");
        Assert.Contains(deduplicated.TextRegions, r => r.Text == "不重疊");
        Assert.DoesNotContain(deduplicated.TextRegions, r => r.Text == "低信心度");
    }

    [Fact]
    public void RemoveDuplicates_WithNonOverlappingRegions_ShouldKeepAll()
    {
        // Arrange
        var result = new OcrResult
        {
            Success = true,
            TextRegions = new List<TextRegion>
            {
                CreateTextRegionWithBox("文字1", 0.9f, 0, 0, 50, 50),
                CreateTextRegionWithBox("文字2", 0.8f, 100, 0, 50, 50),
                CreateTextRegionWithBox("文字3", 0.7f, 200, 0, 50, 50)
            }
        };

        // Act
        var deduplicated = _processor.RemoveDuplicates(result);

        // Assert
        Assert.Equal(3, deduplicated.TextRegions.Count);
    }

    [Fact]
    public void Process_WithEmptyResult_ShouldReturnEmptyProcessedResult()
    {
        // Arrange
        var result = new OcrResult
        {
            Success = true,
            TextRegions = new List<TextRegion>()
        };

        // Act
        var processed = _processor.Process(result);

        // Assert
        Assert.True(processed.Success);
        Assert.Empty(processed.TextRegions);
    }

    [Fact]
    public void Process_WithFailedResult_ShouldReturnOriginalResult()
    {
        // Arrange
        var result = new OcrResult
        {
            Success = false,
            ErrorMessage = "OCR失敗"
        };

        // Act
        var processed = _processor.Process(result);

        // Assert
        Assert.False(processed.Success);
        Assert.Equal("OCR失敗", processed.ErrorMessage);
    }

    // Helper methods
    private OcrResult CreateTestResult((string text, float confidence)[] regions)
    {
        return new OcrResult
        {
            Success = true,
            TextRegions = regions.Select((r, i) =>
                CreateTextRegion(r.text, r.confidence, i * 10, i * 10)).ToList()
        };
    }

    private TextRegion CreateTextRegion(string text, float confidence, int x, int y)
    {
        return new TextRegion
        {
            Text = text,
            Confidence = confidence,
            BoundingBox = new BoundingBox
            {
                X = x,
                Y = y,
                Width = 50,
                Height = 20,
                Points = new List<MangaOCR.Models.Point>
                {
                    new(x, y),
                    new(x + 50, y),
                    new(x + 50, y + 20),
                    new(x, y + 20)
                }
            }
        };
    }

    private TextRegion CreateTextRegionWithBox(string text, float confidence, int x, int y, int width, int height)
    {
        return new TextRegion
        {
            Text = text,
            Confidence = confidence,
            BoundingBox = new BoundingBox
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Points = new List<MangaOCR.Models.Point>
                {
                    new(x, y),
                    new(x + width, y),
                    new(x + width, y + height),
                    new(x, y + height)
                }
            }
        };
    }
}
