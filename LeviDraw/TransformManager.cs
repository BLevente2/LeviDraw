using SkiaSharp;

namespace LeviDraw;

internal class TransformManager
{

    #region Properties&Constructors

    private float currentOffsetX;
    private float currentOffsetY;
    private float currentScale;
    private float currentSquareValue;
    private SKMatrix _matrix;
    private SKMatrix _inverseMatrix;

    internal SKMatrix Matrix => _matrix;

    internal TransformManager()
    {
        currentOffsetX = 0;
        currentOffsetY = 0;
        currentScale = 1;
        currentSquareValue = 1;
        _matrix = SKMatrix.CreateIdentity();
        _inverseMatrix = SKMatrix.CreateIdentity();
    }

    #endregion

    #region Methods

    internal void Update(float offsetX, float offsetY, float scale, float squareValue)
    {
        currentOffsetX = offsetX;
        currentOffsetY = offsetY;
        currentScale = scale;
        currentSquareValue = squareValue;
        float factor = (Grid.DefaultGridSpacing * currentScale) / currentSquareValue;
        _matrix = SKMatrix.CreateScaleTranslation(factor, -factor, offsetX, offsetY);
        if (!_matrix.TryInvert(out _inverseMatrix))
        {
            _inverseMatrix = SKMatrix.CreateIdentity();
        }
    }

    internal SKPoint WorldToScreen(SKPoint point)
    {
        return _matrix.MapPoint(point);
    }

    internal SKPoint ScreenToWorld(System.Drawing.Point point)
    {
        return _inverseMatrix.MapPoint(new SKPoint(point.X, point.Y));
    }

    #endregion

}