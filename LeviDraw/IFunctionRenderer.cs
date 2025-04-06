using SkiaSharp;

namespace LeviDraw;

internal interface IFunctionRenderer
{
    public List<Curve> ComputeCurves(Function function, SKRect visibleRect, TransformManager transform);
}