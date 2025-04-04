using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace LeviDraw;

public class Axes : IDisposable
{
    public bool ShowAxes { get; set; }
    public bool ShowAxisLabels { get; set; }
    public SKColor AxesColor { get; set; }
    public float AxesThickness { get; set; }
    public SKColor AxesLabelsColor { get; set; }
    public float AxesLabelFontSize { get; set; }
    public string AxesLabelFontFamily { get; set; }
    private SKPaint _axesPaint;
    private SKPaint _textPaint;
    private SKTypeface _labelTypeface;
    private SKFont _labelFont;
    private float _baselineOffset;

    public Axes()
    {
        ShowAxes = true;
        ShowAxisLabels = true;
        AxesColor = SystemColors.WindowText.ToSKColor();
        AxesThickness = 2f;
        AxesLabelsColor = SystemColors.WindowText.ToSKColor();
        AxesLabelFontSize = 12f;
        AxesLabelFontFamily = "Arial";
        _axesPaint = new SKPaint { Color = AxesColor, StrokeWidth = AxesThickness, IsAntialias = true };
        _textPaint = new SKPaint { Color = AxesLabelsColor, IsAntialias = true };
        _labelTypeface = SKTypeface.FromFamilyName(AxesLabelFontFamily);
        _labelFont = new SKFont(_labelTypeface, AxesLabelFontSize);
        _baselineOffset = -(_labelFont.Metrics.Ascent + _labelFont.Metrics.Descent) / 2;
    }

    public void Draw(SKCanvas canvas, SKImageInfo info, float offsetX, float offsetY, float effectiveSpacing, float squareValue)
    {
        canvas.DrawLine(offsetX, 0, offsetX, info.Height, _axesPaint);
        canvas.DrawLine(0, offsetY, info.Width, offsetY, _axesPaint);
        if (ShowAxisLabels)
        {
            DrawAxisLabels(canvas, info, offsetX, offsetY, effectiveSpacing, squareValue);
        }
    }

    private void DrawAxisLabels(SKCanvas canvas, SKImageInfo info, float offsetX, float offsetY, float squareSize, float squareValue)
    {
        float worldLeftSquares = (0 - offsetX) / squareSize;
        float worldRightSquares = (info.Width - offsetX) / squareSize;
        float worldTopSquares = (0 - offsetY) / squareSize;
        float worldBottomSquares = (info.Height - offsetY) / squareSize;
        float labelStep = 5f;
        for (float xSquare = (float)Math.Floor(worldLeftSquares / labelStep) * labelStep; xSquare <= worldRightSquares; xSquare += labelStep)
        {
            float screenX = xSquare * squareSize + offsetX;
            float labelValue = xSquare * squareValue;
            string label = labelValue.ToString("0.####");
            canvas.DrawText(label, screenX, offsetY + AxesLabelFontSize + 2, SKTextAlign.Center, _labelFont, _textPaint);
        }
        for (float ySquare = (float)Math.Floor(worldTopSquares / labelStep) * labelStep; ySquare <= worldBottomSquares; ySquare += labelStep)
        {
            float screenY = ySquare * squareSize + offsetY;
            float labelValue = ySquare * squareValue;
            string label = labelValue.ToString("0.####");
            canvas.DrawText(label, offsetX + 2, screenY + _baselineOffset, SKTextAlign.Left, _labelFont, _textPaint);
        }
    }

    public void Dispose()
    {
        _axesPaint?.Dispose();
        _textPaint?.Dispose();
        _labelFont?.Dispose();
        _labelTypeface?.Dispose();
    }
}