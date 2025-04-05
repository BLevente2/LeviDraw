using SkiaSharp;

namespace LeviDraw;

public class LineSegment
{
    #region PropertiesAndConstructors

    public SKPoint StartPoint { get; set; }
    public SKPoint EndPoint { get; set; }
    public SKColor Color { get; set; }
    public float Thickness { get; set; }

    private static readonly Dictionary<(SKColor, float), SKPaint> paintCache = new Dictionary<(SKColor, float), SKPaint>();

    public LineSegment(SKPoint startPoint, SKPoint endPoint, SKColor? color = null, float thickness = 2f)
    {
        StartPoint = startPoint;
        EndPoint = endPoint;
        Color = color ?? SKColors.Yellow;
        Thickness = thickness;
    }

    #endregion

    #region Methods

    public void Draw(SKCanvas canvas, Func<SKPoint, SKPoint> worldToScreen)
    {
        SKPoint startScreen = worldToScreen(StartPoint);
        SKPoint endScreen = worldToScreen(EndPoint);
        var key = (Color, Thickness);
        if (!paintCache.TryGetValue(key, out var paint))
        {
            paint = new SKPaint { Color = Color, StrokeWidth = Thickness, IsAntialias = true };
            paintCache[key] = paint;
        }
        canvas.DrawLine(startScreen.X, startScreen.Y, endScreen.X, endScreen.Y, paint);
    }

    #endregion

}