using SkiaSharp;
using MathNet.Symbolics;
using System.Text.RegularExpressions;

namespace LeviDraw;

public class Function : System.IDisposable
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
    private LruCache<double, (double, double)> _cache;
    private int _maxCacheSize;
    private SKRect _cachedVisibleRect;
    private SKMatrix _cachedTransformMatrix;
    private List<Curve> _cachedCurves;
    private bool _hasCachedCurves;

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
        HardToEvaluate = DetermineHardness(_expression.ToString());
        _maxCacheSize = HardToEvaluate ? 10000 : 5000;
        _cache = new LruCache<double, (double, double)>(_maxCacheSize);
        _cachedCurves = new List<Curve>(); // <== Inicializálva, így nem lesz null.
        _hasCachedCurves = false;
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
            return res.Item1;
        double y;
        try { y = _compiledFunction(x); } catch { y = double.NaN; }
        double dy;
        try { dy = _compiledDerivative(x); } catch { dy = double.NaN; }
        _cache.Add(key, (y, dy));
        return y;
    }

    public double EvaluateDerivative(double x)
    {
        double key = Quantize(x);
        if (_cache.TryGetValue(key, out var res))
            return res.Item2;
        double y;
        try { y = _compiledFunction(x); } catch { y = double.NaN; }
        double dy;
        try { dy = _compiledDerivative(x); } catch { dy = double.NaN; }
        _cache.Add(key, (y, dy));
        return dy;
    }

    internal List<Curve> ComputeCurves(SKRect visibleRect, TransformManager transform)
    {
        SKMatrix currentMatrix = transform.Matrix;
        if (_hasCachedCurves && visibleRect.Equals(_cachedVisibleRect) && currentMatrix.Equals(_cachedTransformMatrix))
            return _cachedCurves;
        List<Curve> curves = new List<Curve>();
        List<SKPoint> currentPoints = new List<SKPoint>();
        float left = visibleRect.Left;
        float right = visibleRect.Right;
        float screenX = left;
        double prevDerivative = double.NaN;
        while (screenX <= right)
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
                else if (Math.Abs(deriv) > 500 && Math.Abs(screenPoint.Y - currentPoints[currentPoints.Count - 1].Y) > visibleRect.Height / 4.0f)
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
            float step = GetAdaptiveStepSize(worldX, deriv);
            screenX += step;
        }
        if (currentPoints.Count > 0)
            curves.Add(new Curve(new List<SKPoint>(currentPoints), Color, StrokeWidth));
        _cachedVisibleRect = visibleRect;
        _cachedTransformMatrix = currentMatrix;
        _cachedCurves = curves;
        _hasCachedCurves = true;
        return curves;
    }

    private float GetAdaptiveStepSize(double worldX, double derivative)
    {
        double h = 1e-3;
        double derivativePlus = EvaluateDerivative(worldX + h);
        double derivativeMinus = EvaluateDerivative(worldX - h);
        double curvature = (derivativePlus - derivativeMinus) / (2 * h);
        float maxStep = HardToEvaluate ? 5f : 10f;
        float minStep = HardToEvaluate ? 0.5f : 2f;
        double factor = 1.0 / (1.0 + Math.Abs(derivative) + 0.01 * Math.Abs(curvature));
        double adaptiveStep = maxStep * factor;
        return (float)Math.Clamp(adaptiveStep, minStep, maxStep);
    }

    public void Dispose()
    {
        _cache.Clear();
    }

    #endregion

}