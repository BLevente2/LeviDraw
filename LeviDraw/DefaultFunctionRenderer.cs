using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeviDraw
{
    internal class DefaultFunctionRenderer : IFunctionRenderer
    {
        public List<Curve> ComputeCurves(Function function, SKRect visibleRect, TransformManager transform)
        {
            List<CurveSegment> segments = new List<CurveSegment>();
            CurveSegment currentSegment = null;
            float left = visibleRect.Left;
            float right = visibleRect.Right;
            System.Drawing.Point leftPt = new System.Drawing.Point((int)left, 0);
            System.Drawing.Point rightPt = new System.Drawing.Point((int)right, 0);
            double leftWorldX = transform.ScreenToWorld(leftPt).X;
            double rightWorldX = transform.ScreenToWorld(rightPt).X;
            var asymptotePositions = function.ComputeVerticalAsymptotePositions(leftWorldX, rightWorldX);
            double tol = (rightWorldX - leftWorldX) / visibleRect.Width * 0.5;
            float screenX = left;
            double prevDerivative = double.NaN;
            bool newSegmentFromAsymptote = false;
            while (screenX <= right)
            {
                double worldX = transform.ScreenToWorld(new System.Drawing.Point((int)screenX, 0)).X;
                if (asymptotePositions.Any(a => Math.Abs(worldX - a) < tol))
                {
                    if (currentSegment != null && currentSegment.Points.Count > 0)
                    {
                        currentSegment.OpenEnd = true;
                        segments.Add(currentSegment);
                        currentSegment = null;
                    }
                    newSegmentFromAsymptote = true;
                    screenX += (float)tol;
                    continue;
                }
                double yVal = function.Evaluate(worldX);
                double deriv = function.EvaluateDerivative(worldX);
                SKPoint screenPoint = transform.WorldToScreen(new SKPoint((float)worldX, (float)yVal));
                bool invalid = double.IsNaN(yVal) || double.IsInfinity(yVal) || screenPoint.Y < -10000 || screenPoint.Y > visibleRect.Height + 10000;
                bool breakSegment = false;
                if (currentSegment != null && currentSegment.Points.Count > 0)
                {
                    if (invalid)
                        breakSegment = true;
                    else if (!double.IsNaN(prevDerivative) && Math.Abs(deriv - prevDerivative) > 1200)
                        breakSegment = true;
                    else if (Math.Abs(deriv) > 500 && Math.Abs(screenPoint.Y - currentSegment.Points[currentSegment.Points.Count - 1].Y) > visibleRect.Height / 4.0f)
                        breakSegment = true;
                }
                if (breakSegment)
                {
                    if (currentSegment != null && currentSegment.Points.Count > 0)
                    {
                        currentSegment.OpenEnd = true;
                        segments.Add(currentSegment);
                        currentSegment = null;
                    }
                    if (!invalid)
                    {
                        currentSegment = new CurveSegment();
                        if (newSegmentFromAsymptote)
                        {
                            currentSegment.OpenStart = true;
                            newSegmentFromAsymptote = false;
                        }
                        currentSegment.Points.Add(screenPoint);
                        prevDerivative = deriv;
                    }
                }
                else
                {
                    if (!invalid)
                    {
                        if (currentSegment == null)
                        {
                            currentSegment = new CurveSegment();
                            if (newSegmentFromAsymptote)
                            {
                                currentSegment.OpenStart = true;
                                newSegmentFromAsymptote = false;
                            }
                        }
                        currentSegment.Points.Add(screenPoint);
                        prevDerivative = deriv;
                    }
                    else if (currentSegment != null && currentSegment.Points.Count > 0)
                    {
                        currentSegment.OpenEnd = true;
                        segments.Add(currentSegment);
                        currentSegment = null;
                    }
                }
                float step = GetAdaptiveStepSize(function, transform, worldX, deriv, visibleRect);
                screenX += step;
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

        private float GetAdaptiveStepSize(Function function, TransformManager transform, double worldX, double derivative, SKRect visibleRect)
        {
            double h = 1e-3;
            double derivativePlus = function.EvaluateDerivative(worldX + h);
            double derivativeMinus = function.EvaluateDerivative(worldX - h);
            double curvature = (derivativePlus - derivativeMinus) / (2 * h);
            float maxStep = function.HardToEvaluate ? 5f : 10f;
            float minStep = function.HardToEvaluate ? 0.5f : 2f;
            double factor = 1.0 / (1.0 + Math.Abs(derivative) + 0.01 * Math.Abs(curvature));
            double adaptiveStep = maxStep * factor;
            return (float)Math.Clamp(adaptiveStep, minStep, maxStep);
        }
    }
}
