using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace LeviDraw;

public class Grid : System.IDisposable
{

    #region PropertiesAndConstructors

    public const float DefaultGridSpacing = 30f;

    public bool ShowGrid { get; set; }
    
    public SKColor GridColor
    {
        get => _gridColor;
        set
        {
            if (_gridColor != value)
            {
                _gridColor = value;
                InvalidateCache();
            }
        }
    }
    
    public float GridThickness
    {
        get => _gridThickness;
        set
        {
            if (_gridThickness != value)
            {
                _gridThickness = value;
                InvalidateCache();
            }
        }
    }

    private SKImage? gridPatternImage;
    private int _lastSpacing;
    private SKColor _gridColor;
    private float _gridThickness;

    public Grid()
    {
        ShowGrid = true;
        _gridColor = System.Drawing.SystemColors.ControlText.ToSKColor();
        _gridThickness = 1f;
        _lastSpacing = 0;
    }

    #endregion

    #region Methods

    public void Draw(SKCanvas canvas, SKImageInfo info, float offsetX, float offsetY, int spacing)
    {
        if (gridPatternImage == null || spacing != _lastSpacing)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(spacing, spacing)))
            {
                var patternCanvas = surface.Canvas;
                patternCanvas.Clear(SKColors.Transparent);
                using (var paint = new SKPaint { Color = GridColor, StrokeWidth = GridThickness, IsAntialias = true })
                {
                    patternCanvas.DrawLine(0, 0, 0, spacing, paint);
                    patternCanvas.DrawLine(0, 0, spacing, 0, paint);
                }
                gridPatternImage?.Dispose();
                gridPatternImage = surface.Snapshot();
            }
            _lastSpacing = spacing;
        }
        float rX = ((offsetX % spacing) + spacing) % spacing;
        float rY = ((offsetY % spacing) + spacing) % spacing;
        SKMatrix translationMatrix = SKMatrix.CreateTranslation(rX, rY);
        using (var shader = SKShader.CreateImage(gridPatternImage, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, translationMatrix))
        {
            using (var paint = new SKPaint { Shader = shader, IsAntialias = true })
            {
                canvas.DrawRect(new SKRect(0, 0, info.Width, info.Height), paint);
            }
        }
    }

    private void InvalidateCache()
    {
        gridPatternImage?.Dispose();
        gridPatternImage = null;
    }

    public void Dispose()
    {
        gridPatternImage?.Dispose();
        gridPatternImage = null;
    }

    #endregion

}