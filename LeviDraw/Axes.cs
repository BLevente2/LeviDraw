using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace LeviDraw;

public class Axes : IDisposable
{

    #region PropertiesAndConstructors
    public bool ShowAxes { get; set; }
    public bool ShowAxisLabels { get; set; }
    
    public SKColor AxesColor
    {
        get => _axesColor;
        set
        {
            if (_axesColor != value)
            {
                _axesColor = value;
                _axesPaint.Dispose();
                _axesPaint = new SKPaint { Color = _axesColor, StrokeWidth = _axesThickness, IsAntialias = true };
                InvalidateLabelsCache();
            }
        }
    }
    
    public float AxesThickness
    {
        get => _axesThickness;
        set
        {
            if (_axesThickness != value)
            {
                _axesThickness = value;
                _axesPaint.Dispose();
                _axesPaint = new SKPaint { Color = _axesColor, StrokeWidth = _axesThickness, IsAntialias = true };
                InvalidateLabelsCache();
            }
        }
    }
    
    public SKColor AxesLabelsColor
    {
        get => _axesLabelsColor;
        set
        {
            if (_axesLabelsColor != value)
            {
                _axesLabelsColor = value;
                _textPaint.Dispose();
                _textPaint = new SKPaint { Color = _axesLabelsColor, IsAntialias = true };
                InvalidateLabelsCache();
            }
        }
    }
    
    public float AxesLabelFontSize
    {
        get => _axesLabelFontSize;
        set
        {
            if (_axesLabelFontSize != value)
            {
                _axesLabelFontSize = value;
                _labelFont.Dispose();
                _labelFont = new SKFont(_labelTypeface, _axesLabelFontSize);
                _baselineOffset = -(_labelFont.Metrics.Ascent + _labelFont.Metrics.Descent) / 2;
                InvalidateLabelsCache();
            }
        }
    }
    
    public string AxesLabelFontFamily
    {
        get => _axesLabelFontFamily;
        set
        {
            if (_axesLabelFontFamily != value)
            {
                _axesLabelFontFamily = value;
                _labelTypeface.Dispose();
                _labelTypeface = SKTypeface.FromFamilyName(_axesLabelFontFamily);
                _labelFont.Dispose();
                _labelFont = new SKFont(_labelTypeface, _axesLabelFontSize);
                _baselineOffset = -(_labelFont.Metrics.Ascent + _labelFont.Metrics.Descent) / 2;
                InvalidateLabelsCache();
            }
        }
    }
    
    private SKPaint _axesPaint;
    private SKPaint _textPaint;
    private SKTypeface _labelTypeface;
    private SKFont _labelFont;
    private float _baselineOffset;
    private SKColor _axesColor;
    private float _axesThickness;
    private SKColor _axesLabelsColor;
    private float _axesLabelFontSize;
    private string _axesLabelFontFamily;
    private SKImage? _cachedLabelsImage;
    private float _cachedOffsetX, _cachedOffsetY, _cachedEffectiveSpacing, _cachedSquareValue;
    private int _cachedWidth, _cachedHeight;

    public Axes()
    {
        ShowAxes = true;
        ShowAxisLabels = true;
        _axesColor = System.Drawing.SystemColors.WindowText.ToSKColor();
        _axesThickness = 2f;
        _axesLabelsColor = System.Drawing.SystemColors.WindowText.ToSKColor();
        _axesLabelFontSize = 12f;
        _axesLabelFontFamily = "Arial";
        _axesPaint = new SKPaint { Color = _axesColor, StrokeWidth = _axesThickness, IsAntialias = true };
        _textPaint = new SKPaint { Color = _axesLabelsColor, IsAntialias = true };
        _labelTypeface = SKTypeface.FromFamilyName(_axesLabelFontFamily);
        _labelFont = new SKFont(_labelTypeface, _axesLabelFontSize);
        _baselineOffset = -(_labelFont.Metrics.Ascent + _labelFont.Metrics.Descent) / 2;
    }

    #endregion

    #region Methods

    public void Draw(SKCanvas canvas, SKImageInfo info, float offsetX, float offsetY, float effectiveSpacing, float squareValue)
    {
        canvas.DrawLine(offsetX, 0, offsetX, info.Height, _axesPaint);
        canvas.DrawLine(0, offsetY, info.Width, offsetY, _axesPaint);
        if (ShowAxisLabels) DrawAxisLabels(canvas, info, offsetX, offsetY, effectiveSpacing, squareValue);
    }

    private void DrawAxisLabels(SKCanvas canvas, SKImageInfo info, float offsetX, float offsetY, float squareSize, float squareValue)
    {
        if (_cachedLabelsImage != null && _cachedWidth == info.Width && _cachedHeight == info.Height &&
            _cachedOffsetX == offsetX && _cachedOffsetY == offsetY &&
            _cachedEffectiveSpacing == squareSize && _cachedSquareValue == squareValue)
        {
            canvas.DrawImage(_cachedLabelsImage, 0, 0);
            return;
        }
        using (var surface = SKSurface.Create(new SKImageInfo(info.Width, info.Height)))
        {
            var tempCanvas = surface.Canvas;
            float worldLeftSquares = (0 - offsetX) / squareSize;
            float worldRightSquares = (info.Width - offsetX) / squareSize;
            float worldTopSquares = (0 - offsetY) / squareSize;
            float worldBottomSquares = (info.Height - offsetY) / squareSize;
            float labelStep = 5f;
            for (float xSquare = (float)System.Math.Floor(worldLeftSquares / labelStep) * labelStep; xSquare <= worldRightSquares; xSquare += labelStep)
            {
                float screenX = xSquare * squareSize + offsetX;
                float labelValue = xSquare * squareValue;
                string label = labelValue.ToString("0.####");
                tempCanvas.DrawText(label, screenX, offsetY + _axesLabelFontSize + 2, SKTextAlign.Center, _labelFont, _textPaint);
            }
            for (float ySquare = (float)System.Math.Floor(worldTopSquares / labelStep) * labelStep; ySquare <= worldBottomSquares; ySquare += labelStep)
            {
                float screenY = ySquare * squareSize + offsetY;
                float labelValue = ySquare * squareValue;
                string label = labelValue.ToString("0.####");
                tempCanvas.DrawText(label, offsetX + 2, screenY + _baselineOffset, SKTextAlign.Left, _labelFont, _textPaint);
            }
            _cachedLabelsImage?.Dispose();
            _cachedLabelsImage = surface.Snapshot();
            _cachedWidth = info.Width;
            _cachedHeight = info.Height;
            _cachedOffsetX = offsetX;
            _cachedOffsetY = offsetY;
            _cachedEffectiveSpacing = squareSize;
            _cachedSquareValue = squareValue;
            canvas.DrawImage(_cachedLabelsImage, 0, 0);
        }
    }

    private void InvalidateLabelsCache()
    {
        _cachedLabelsImage?.Dispose();
        _cachedLabelsImage = null;
    }

    public void Dispose()
    {
        _axesPaint?.Dispose();
        _textPaint?.Dispose();
        _labelFont?.Dispose();
        _labelTypeface?.Dispose();
        _cachedLabelsImage?.Dispose();
    }

    #endregion

}