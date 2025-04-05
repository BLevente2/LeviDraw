using SkiaSharp;

namespace LeviDraw;

internal class Curve
{

    #region Properties&Constructors

    internal List<SKPoint> Points { get; private set; }
    internal SKColor Color { get; set; }
    internal float StrokeWidth { get; set; }

    private SKPath _cachedPath;

    internal Curve(List<SKPoint> points, SKColor color, float strokeWidth)
    {
        Points = points;
        Color = color;
        StrokeWidth = strokeWidth;
        _cachedPath = new SKPath();
        if (Points.Count > 0)
        {
            _cachedPath.MoveTo(Points[0]);
            for (int i = 1; i < Points.Count; i++)
                _cachedPath.LineTo(Points[i]);
        }
    }

    #endregion

    #region Methods

    internal SKPath GetPath()
    {
        return _cachedPath;
    }
    internal void Draw(SKCanvas canvas)
    {
        using (var paint = new SKPaint { Color = Color, StrokeWidth = StrokeWidth, IsAntialias = true, Style = SKPaintStyle.Stroke })
            canvas.DrawPath(GetPath(), paint);
    }
    internal void InvalidateCache()
    {
        _cachedPath.Dispose();
        _cachedPath = new SKPath();
        if (Points.Count > 0)
        {
            _cachedPath.MoveTo(Points[0]);
            for (int i = 1; i < Points.Count; i++)
                _cachedPath.LineTo(Points[i]);
        }
    }

    #endregion

}