using SkiaSharp;
using MathNet.Symbolics;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace LeviDraw;

public class Function : System.IDisposable
{
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
    public bool HasCachedCurves { get; private set; }
    private SKRect _cachedVisibleRect;
    private SKMatrix _cachedTransformMatrix;
    private List<Curve> _cachedCurves;
    private IFunctionRenderer _renderer;

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
        _cache = new LruCache<double, (double, double)>(HardToEvaluate ? 10000 : 5000);
        _cachedCurves = new List<Curve>();
        HasCachedCurves = false;
        if (IsLinear())
            _renderer = new LinearFunctionRenderer();
        else if (ExpressionString.ToLowerInvariant().Contains("/x") || ExpressionString.ToLowerInvariant().Contains("1/x") || ExpressionString.ToLowerInvariant().Contains("tan(") || ExpressionString.ToLowerInvariant().Contains("cot("))
            _renderer = new AdaptiveFunctionRenderer();
        else
            _renderer = new DefaultFunctionRenderer();
    }

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
    public bool IsLinear()
    {
        double d0 = EvaluateDerivative(0);
        double d1 = EvaluateDerivative(1);
        if (double.IsNaN(d0) || double.IsNaN(d1))
            return false;
        return Math.Abs(d0 - d1) < 1e-6;
    }
    public SymbolicExpression GetVerticalAsymptoteExpression()
    {
        var lowerExpr = ExpressionString.ToLowerInvariant();
        if (lowerExpr.Contains("tan("))
            return SymbolicExpression.Parse("(pi/2) + pi*k");
        if (lowerExpr.Contains("cot("))
            return SymbolicExpression.Parse("pi*k");
        if (ExpressionString.Contains("/"))
        {
            var parts = ExpressionString.Split('/');
            if (parts.Length == 2)
            {
                string denominatorStr = parts[1];
                return SymbolicExpression.Parse(denominatorStr);
            }
            return SymbolicExpression.Parse("0");
        }
        return SymbolicExpression.Parse("null");
    }
    public List<double> ComputeVerticalAsymptotePositions(double xMin, double xMax)
    {
        var asymptotes = new List<double>();
        if (ExpressionString.ToLowerInvariant().Contains("tan("))
        {
            double period = Math.PI;
            double baseAsymptote = Math.PI / 2;
            int kMin = (int)Math.Ceiling((xMin - baseAsymptote) / period);
            int kMax = (int)Math.Floor((xMax - baseAsymptote) / period);
            for (int k = kMin; k <= kMax; k++)
                asymptotes.Add(baseAsymptote + period * k);
        }
        else if (ExpressionString.ToLowerInvariant().Contains("cot("))
        {
            double period = Math.PI;
            int kMin = (int)Math.Ceiling(xMin / period);
            int kMax = (int)Math.Floor(xMax / period);
            for (int k = kMin; k <= kMax; k++)
                asymptotes.Add(period * k);
        }
        else if (ExpressionString.Contains("/"))
        {
            try
            {
                var parts = ExpressionString.Split('/');
                if (parts.Length == 2)
                {
                    string denominatorStr = parts[1];
                    var denominatorExpr = SymbolicExpression.Parse(denominatorStr);
                    var denominatorFunc = denominatorExpr.Compile("x");
                    int samples = 1000;
                    double step = (xMax - xMin) / samples;
                    double prevValue = denominatorFunc(xMin);
                    double currentX = xMin;
                    for (int i = 1; i <= samples; i++)
                    {
                        currentX = xMin + i * step;
                        double currentValue = denominatorFunc(currentX);
                        if (Math.Abs(currentValue) < 1e-6)
                            asymptotes.Add(currentX);
                        else if (prevValue * currentValue < 0)
                        {
                            double a = xMin + (i - 1) * step;
                            double b = currentX;
                            double mid = (a + b) / 2;
                            for (int j = 0; j < 20; j++)
                            {
                                double fmid = denominatorFunc(mid);
                                if (Math.Abs(fmid) < 1e-9)
                                    break;
                                if (denominatorFunc(a) * fmid < 0)
                                    b = mid;
                                else
                                    a = mid;
                                mid = (a + b) / 2;
                            }
                            asymptotes.Add(mid);
                        }
                        prevValue = currentValue;
                    }
                }
            }
            catch { }
        }
        asymptotes.Sort();
        return asymptotes;
    }
    internal List<Curve> GetCurves(SKRect visibleRect, TransformManager transform)
    {
        SKMatrix currentMatrix = transform.Matrix;
        if (HasCachedCurves && visibleRect.Equals(_cachedVisibleRect) && currentMatrix.Equals(_cachedTransformMatrix))
            return _cachedCurves;
        List<Curve> curves = _renderer.ComputeCurves(this, visibleRect, transform);
        _cachedVisibleRect = visibleRect;
        _cachedTransformMatrix = currentMatrix;
        _cachedCurves = curves;
        HasCachedCurves = true;
        return curves;
    }
    public void Dispose()
    {
        _cache.Clear();
    }
}
