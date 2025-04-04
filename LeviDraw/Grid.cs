using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace LeviDraw;

public class Grid : IDisposable
{
    public const float DefaultGridSpacing = 30f;
    public bool ShowGrid { get; set; }
    public SKColor GridColor { get; set; }
    public float GridThickness { get; set; }
    private SKImage? gridPatternImage;
    private int _lastSpacing;

    public Grid(float scale)
    {
        ShowGrid = true;
        GridColor = SystemColors.ControlText.ToSKColor();
        GridThickness = 1f;
        _lastSpacing = 0;
    }

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
                if (gridPatternImage != null)
                {
                    gridPatternImage.Dispose();
                }
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

    public void Dispose()
    {
        gridPatternImage?.Dispose();
        gridPatternImage = null;
    }
}