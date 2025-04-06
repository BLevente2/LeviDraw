using SkiaSharp;

namespace LeviDraw;

public class CoordinatePoint
{
    public SKPoint Position { get; set; }
    private SKColor color;
    public SKColor Color
    {
        get => color;
        set => color = value;
    }
    private float size;
    public float Size
    {
        get => size;
        set => size = Math.Max(MinSize, Math.Min(MaxSize, value));
    }
    public float MinSize { get; set; }
    public float MaxSize { get; set; }
    public static readonly SKColor DefaultColor = SKColors.Red;
    private static Dictionary<SKColor, SKPaint> paintCache = new Dictionary<SKColor, SKPaint>();

    public CoordinatePoint(SKPoint position, float size, SKColor? color = null, float minSize = 1f, float maxSize = 50f)
    {
        Position = position;
        MinSize = minSize;
        MaxSize = maxSize;
        Size = size;
        Color = color ?? DefaultColor;
    }

    public void Draw(SKCanvas canvas, SKPoint screenPos, float screenRadius)
    {
        if (!paintCache.TryGetValue(Color, out SKPaint paint))
        {
            paint = new SKPaint { Color = Color, IsAntialias = true, Style = SKPaintStyle.Fill };
            paintCache[Color] = paint;
        }
        canvas.DrawCircle(screenPos.X, screenPos.Y, screenRadius, paint);
    }
}