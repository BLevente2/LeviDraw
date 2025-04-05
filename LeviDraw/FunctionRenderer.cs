using SkiaSharp;

namespace LeviDraw;

internal static class FunctionRenderer
{

    internal static Task RenderFunctions(SKCanvas canvas, List<Function> functions, SKRect visibleRect, TransformManager transform)
    {
        foreach (var function in functions)
        {
            var curves = function.ComputeCurves(visibleRect, transform);
            foreach (var curve in curves)
                curve.Draw(canvas);
        }
        return Task.CompletedTask;
    }

}