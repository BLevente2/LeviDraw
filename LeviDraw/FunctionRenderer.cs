using SkiaSharp;

namespace LeviDraw;

internal static class FunctionRenderer
{
    internal static void RenderFunctions(SKCanvas canvas, List<Function> functions, SKRect visibleRect, TransformManager transform)
    {
        var tasks = functions.Select(f => Task.Run(() => f.ComputeCurves(visibleRect, transform))).ToArray();
        Task.WaitAll(tasks);
        foreach (var task in tasks)
            foreach (var curve in task.Result)
                curve.Draw(canvas);
    }
}