using SkiaSharp;

namespace LeviDraw;

internal class Curve
{
    internal List<SKPoint> Points { get; private set; }
    internal SKColor Color { get; set; }
    internal float StrokeWidth { get; set; }
    internal bool OpenStart { get; set; }
    internal bool OpenEnd { get; set; }
    private SKPath _cachedPath;

    internal Curve(List<SKPoint> points, SKColor color, float strokeWidth)
    {
        Points = points;
        Color = color;
        StrokeWidth = strokeWidth;
        OpenStart = false;
        OpenEnd = false;
        _cachedPath = new SKPath();
        if (Points.Count > 0)
        {
            _cachedPath.MoveTo(Points[0]);
            for (int i = 1; i < Points.Count; i++)
                _cachedPath.LineTo(Points[i]);
        }
    }

    internal SKPath GetPath()
    {
        return _cachedPath;
    }
    internal void Draw(SKCanvas canvas)
    {
        using (var paint = new SKPaint { Color = Color, StrokeWidth = StrokeWidth, IsAntialias = true, Style = SKPaintStyle.Stroke })
            canvas.DrawPath(GetPath(), paint);
        using (var markerPaint = new SKPaint { Color = Color, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true })
        {
            if (OpenStart && Points.Count > 0)
                canvas.DrawCircle(Points[0], 4, markerPaint);
            if (OpenEnd && Points.Count > 0)
                canvas.DrawCircle(Points[Points.Count - 1], 4, markerPaint);
        }
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
}

internal class CurveSegment
{
    internal List<SKPoint> Points { get; set; }
    internal bool OpenStart { get; set; }
    internal bool OpenEnd { get; set; }
    internal CurveSegment()
    {
        Points = new List<SKPoint>();
        OpenStart = false;
        OpenEnd = false;
    }
}
