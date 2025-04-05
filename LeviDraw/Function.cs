using SkiaSharp;
using MathNet.Symbolics;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LeviDraw;

public class Function : IDisposable
{

    #region Properties&Constructors

    public string Name { get; private set; }
    public SKColor Color { get; set; }
    public float StrokeWidth { get; set; }
    public bool HardToEvaluate { get; private set; }
    public string ExpressionString { get; private set; }

    private SymbolicExpression _expression;
    private SymbolicExpression _derivativeExpression;
    private Func<double, double> _compiledFunction;
    private Func<double, double> _compiledDerivative;
    private ConcurrentDictionary<double, (double y, double dy)> _cache;
    private int _maxCacheSize;

    public Function(string name, string expression, SKColor color, float strokeWidth)
    {
        Name = name;
        ExpressionString = expression;
        Color = color;
        StrokeWidth = strokeWidth;
        _expression = SymbolicExpression.Parse(expression);
        _derivativeExpression = _expression.Differentiate("x");
        _compiledFunction = _expression.Compile("x");
        _compiledDerivative = _derivativeExpression.Compile("x");
        _cache = new ConcurrentDictionary<double, (double, double)>();
        HardToEvaluate = DetermineHardness(_expression.ToString());
        _maxCacheSize = HardToEvaluate ? 10000 : 5000;
    }

    #endregion

    #region Methods

    private bool DetermineHardness(string expr)
    {
        string s = expr.ToLowerInvariant();
        bool containsTrig = s.Contains("sin(") || s.Contains("cos(") || s.Contains("tan(") || s.Contains("cot(") || s.Contains("sinh(") || s.Contains("tanh(") || s.Contains("coth(");
        bool hasHyperbola = s.Contains("/x") || s.Contains("1/x");
        bool highDegree = false;
        foreach (Match m in Regex.Matches(s, @"x\^(\d+(\.\d+)?)"))
        {
            if (double.TryParse(m.Groups[1].Value, out double degree) && degree >= 4)
            {
                highDegree = true;
                break;
            }
        }
        return containsTrig || hasHyperbola || highDegree;
    }

    private double Quantize(double x)
    {
        return Math.Round(x, 6);
    }

    public double Evaluate(double x)
    {
        double key = Quantize(x);
        if (_cache.TryGetValue(key, out var res))
            return res.y;
        double y;
        try { y = _compiledFunction(x); } catch { y = double.NaN; }
        double dy;
        try { dy = _compiledDerivative(x); } catch { dy = double.NaN; }
        if (_cache.Count >= _maxCacheSize)
        {
            var firstKey = _cache.Keys.FirstOrDefault();
            _cache.TryRemove(firstKey, out _);
        }
        _cache[key] = (y, dy);
        return y;
    }

    public double EvaluateDerivative(double x)
    {
        double key = Quantize(x);
        if (_cache.TryGetValue(key, out var res))
            return res.dy;
        double y;
        try { y = _compiledFunction(x); } catch { y = double.NaN; }
        double dy;
        try { dy = _compiledDerivative(x); } catch { dy = double.NaN; }
        if (_cache.Count >= _maxCacheSize)
        {
            var firstKey = _cache.Keys.FirstOrDefault();
            _cache.TryRemove(firstKey, out _);
        }
        _cache[key] = (y, dy);
        return dy;
    }

    internal List<Curve> ComputeCurves(SKRect visibleRect, TransformManager transform)
    {
        List<Curve> curves = new List<Curve>();
        List<SKPoint> currentPoints = new List<SKPoint>();
        float step = GetAdaptiveStepSize(0, 0);
        double prevDerivative = double.NaN;
        float left = visibleRect.Left;
        float right = visibleRect.Right;
        for (float screenX = left; screenX <= right; screenX += step)
        {
            double worldX = transform.ScreenToWorld(new System.Drawing.Point((int)screenX, 0)).X;
            double yVal = Evaluate(worldX);
            double deriv = EvaluateDerivative(worldX);
            SKPoint screenPoint = transform.WorldToScreen(new SKPoint((float)worldX, (float)yVal));
            bool invalid = double.IsNaN(yVal) || double.IsInfinity(yVal) || screenPoint.Y < -10000 || screenPoint.Y > visibleRect.Height + 10000;
            bool breakSegment = false;
            if (currentPoints.Count > 0)
            {
                if (invalid)
                    breakSegment = true;
                else if (!double.IsNaN(prevDerivative) && Math.Abs(deriv - prevDerivative) > 1200)
                    breakSegment = true;
                else if (Math.Abs(deriv) > 500 && Math.Abs(screenPoint.Y - currentPoints.Last().Y) > visibleRect.Height / 4.0f)
                    breakSegment = true;
            }
            if (breakSegment)
            {
                if (currentPoints.Count > 0)
                {
                    curves.Add(new Curve(new List<SKPoint>(currentPoints), Color, StrokeWidth));
                    currentPoints.Clear();
                }
                if (!invalid)
                {
                    currentPoints.Add(screenPoint);
                    prevDerivative = deriv;
                }
            }
            else
            {
                if (!invalid)
                {
                    currentPoints.Add(screenPoint);
                    prevDerivative = deriv;
                }
                else if (currentPoints.Count > 0)
                {
                    curves.Add(new Curve(new List<SKPoint>(currentPoints), Color, StrokeWidth));
                    currentPoints.Clear();
                }
            }
            step = GetAdaptiveStepSize(deriv, prevDerivative);
        }
        if (currentPoints.Count > 0)
            curves.Add(new Curve(new List<SKPoint>(currentPoints), Color, StrokeWidth));
        return curves;
    }

    private float GetAdaptiveStepSize(double derivative, double prevDerivative)
    {
        double absDeriv = Math.Abs(derivative);
        double derivDiff = !double.IsNaN(prevDerivative) ? Math.Abs(derivative - prevDerivative) : 0;
        float maxStep = (HardToEvaluate ? 5f : 10f);
        float minStep = (HardToEvaluate ? 0.5f : 2f);
        if (absDeriv >= 500)
            return minStep;
        double factor = 1.0 / (1.0 + absDeriv + derivDiff / 100.0);
        double adaptiveStep = maxStep * factor;
        return (float)Math.Clamp(adaptiveStep, minStep, maxStep);
    }

    public void Dispose()
    {
        _cache.Clear();
    }

    #endregion

}