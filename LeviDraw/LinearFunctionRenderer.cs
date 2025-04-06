using SkiaSharp;

namespace LeviDraw;

internal class LinearFunctionRenderer : IFunctionRenderer
{
    public List<Curve> ComputeCurves(Function function, SKRect visibleRect, TransformManager transform)
    {
        System.Drawing.Point leftPoint = new System.Drawing.Point((int)visibleRect.Left, 0);
        System.Drawing.Point rightPoint = new System.Drawing.Point((int)visibleRect.Right, 0);
        double leftWorldX = transform.ScreenToWorld(leftPoint).X;
        double rightWorldX = transform.ScreenToWorld(rightPoint).X;
        double leftY = function.Evaluate(leftWorldX);
        double rightY = function.Evaluate(rightWorldX);
        SKPoint p1 = transform.WorldToScreen(new SKPoint((float)leftWorldX, (float)leftY));
        SKPoint p2 = transform.WorldToScreen(new SKPoint((float)rightWorldX, (float)rightY));
        List<SKPoint> points = new List<SKPoint> { p1, p2 };
        return new List<Curve> { new Curve(points, function.Color, function.StrokeWidth) };
    }
}