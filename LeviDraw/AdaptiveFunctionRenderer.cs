using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeviDraw
{
    internal class AdaptiveFunctionRenderer : IFunctionRenderer
    {
        public List<Curve> ComputeCurves(Function function, SKRect visibleRect, TransformManager transform)
        {
            System.Drawing.Point leftPoint = new System.Drawing.Point((int)visibleRect.Left, 0);
            System.Drawing.Point rightPoint = new System.Drawing.Point((int)visibleRect.Right, 0);
            double leftWorldX = transform.ScreenToWorld(leftPoint).X;
            double rightWorldX = transform.ScreenToWorld(rightPoint).X;
            double baseStep = (rightWorldX - leftWorldX) / visibleRect.Width;
            double steepThreshold = 10.0;
            var asymptotes = function.ComputeVerticalAsymptotePositions(leftWorldX, rightWorldX);
            double tol = baseStep * 0.5;
            List<CurveSegment> segments = new List<CurveSegment>();
            CurveSegment currentSegment = null;
            bool newSegmentFromAsymptote = false;
            double x = leftWorldX;
            while (x <= rightWorldX)
            {
                var value = function.EvaluateBoth(x);
                double y = value.y;
                if (double.IsNaN(y) || double.IsInfinity(y))
                {
                    if (currentSegment != null && currentSegment.Points.Count > 0)
                    {
                        currentSegment.OpenEnd = true;
                        segments.Add(currentSegment);
                        currentSegment = null;
                    }
                    x += baseStep;
                    continue;
                }
                if (asymptotes.Any(a => Math.Abs(x - a) < tol))
                {
                    if (currentSegment != null && currentSegment.Points.Count > 0)
                    {
                        currentSegment.OpenEnd = true;
                        segments.Add(currentSegment);
                        currentSegment = null;
                    }
                    newSegmentFromAsymptote = true;
                    x += tol;
                    continue;
                }
                if (currentSegment == null)
                {
                    currentSegment = new CurveSegment();
                    if (newSegmentFromAsymptote)
                    {
                        currentSegment.OpenStart = true;
                        newSegmentFromAsymptote = false;
                    }
                }
                SKPoint screenPoint = transform.WorldToScreen(new SKPoint((float)x, (float)y));
                currentSegment.Points.Add(screenPoint);
                double derivative = value.dy;
                double dx;
                if (double.IsNaN(derivative) || Math.Abs(derivative) < 1e-6)
                    dx = baseStep;
                else if (Math.Abs(derivative) <= steepThreshold)
                    dx = baseStep;
                else
                {
                    double yStep = baseStep;
                    double targetY = y + Math.Sign(derivative) * yStep;
                    double newX = x;
                    bool converged = false;
                    for (int i = 0; i < 10; i++)
                    {
                        var res = function.EvaluateBoth(newX);
                        double fVal = res.y - targetY;
                        double fDeriv = res.dy;
                        if (Math.Abs(fDeriv) < 1e-6)
                            break;
                        double nextX = newX - fVal / fDeriv;
                        if (Math.Abs(nextX - newX) < 1e-6)
                        {
                            newX = nextX;
                            converged = true;
                            break;
                        }
                        newX = nextX;
                    }
                    if (converged && !double.IsNaN(newX) && !double.IsInfinity(newX))
                    {
                        dx = newX - x;
                        if (dx <= 0)
                            dx = baseStep;
                    }
                    else
                        dx = baseStep;
                }
                if (dx < baseStep * 0.1)
                    dx = baseStep * 0.1;
                x += dx;
            }
            if (currentSegment != null && currentSegment.Points.Count > 0)
                segments.Add(currentSegment);
            List<Curve> curves = new List<Curve>();
            foreach (var seg in segments)
            {
                if (seg.Points.Count > 1)
                {
                    var curve = new Curve(new List<SKPoint>(seg.Points), function.Color, function.StrokeWidth);
                    curve.OpenStart = seg.OpenStart;
                    curve.OpenEnd = seg.OpenEnd;
                    curves.Add(curve);
                }
            }
            return curves;
        }
    }
}
