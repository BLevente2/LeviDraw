using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Diagnostics;

namespace LeviDraw;

public class CoordinateSystem : SKGLControl, System.IDisposable
{
    #region Properties&Constructors

    public Grid Grid { get; set; }
    public Axes Axes { get; set; }
    public KeyBinds KeyBinds { get; set; }
    public List<Function> Functions { get; set; }
    public List<CoordinatePoint> Points { get; set; }

    private float offsetX;
    private float offsetY;
    private float scale;
    private float squareValue;
    private List<LineSegment> segments;
    private LineSegment? currentSegment;
    private InputManager inputManager;
    private TransformManager transformManager;
    private System.Windows.Forms.Timer updateTimer;
    private Stopwatch stopwatch;
    private float moveSpeedMultiplier;

    private const float MinScale = 0.5f;
    private const float MaxScale = 5f;
    private const float MinSquareValue = 0.1f;
    private const float MaxSquareValue = 10f;
    private const float MouseMoveThreshold = 2f;

    public CoordinateSystem() : base()
    {
        BackColor = System.Drawing.SystemColors.Control;
        TabStop = true;

        offsetX = 0f;
        offsetY = 0f;
        scale = 1f;
        squareValue = 1f;
        moveSpeedMultiplier = 375.0f;
        Grid = new Grid();
        Axes = new Axes();
        KeyBinds = new KeyBinds();
        Functions = new List<Function>();
        Points = new List<CoordinatePoint>();
        inputManager = new InputManager();
        transformManager = new TransformManager();
        PaintSurface += OnPaintSurface;
        updateTimer = new System.Windows.Forms.Timer();
        updateTimer.Interval = 16;
        updateTimer.Tick += UpdateTimer_Tick;
        stopwatch = Stopwatch.StartNew();
        segments = new List<LineSegment>();
    }

    #endregion

    #region Methods

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        inputManager.HandleKeyDown(e);
        if (inputManager.HasActiveInput() && !updateTimer.Enabled)
        {
            stopwatch.Restart();
            updateTimer.Start();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        inputManager.HandleKeyUp(e);
        if (!inputManager.HasActiveInput() && !inputManager.IsDragging && updateTimer.Enabled)
        {
            updateTimer.Stop();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        inputManager.HandleMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            if (!updateTimer.Enabled)
            {
                stopwatch.Restart();
                updateTimer.Start();
            }
        }
        if (e.Button == MouseButtons.Right)
        {
            currentSegment = new LineSegment(transformManager.ScreenToWorld(e.Location), transformManager.ScreenToWorld(e.Location));
            MarkDirty();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (inputManager.IsDragging)
        {
            float dx = e.X - inputManager.LastMousePosition.X;
            float dy = e.Y - inputManager.LastMousePosition.Y;
            if (Math.Abs(dx) >= MouseMoveThreshold || Math.Abs(dy) >= MouseMoveThreshold)
            {
                offsetX += dx;
                offsetY += dy;
                inputManager.LastMousePosition = e.Location;
                MarkDirty();
            }
        }
        if (currentSegment != null)
        {
            currentSegment.EndPoint = transformManager.ScreenToWorld(e.Location);
            MarkDirty();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        inputManager.HandleMouseUp(e);
        if (e.Button == MouseButtons.Left)
        {
            if (inputManager.PressedKeysCount == 0 && updateTimer.Enabled)
            {
                updateTimer.Stop();
            }
        }
        if (e.Button == MouseButtons.Right && currentSegment != null)
        {
            currentSegment.EndPoint = transformManager.ScreenToWorld(e.Location);
            segments.Add(currentSegment);
            currentSegment = null;
            MarkDirty();
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Delta > 0)
        {
            squareValue = Math.Max(squareValue - 0.1f, MinSquareValue);
        }
        else
        {
            squareValue = Math.Min(squareValue + 0.1f, MaxSquareValue);
        }
        MarkDirty();
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button == MouseButtons.Left)
        {
            SKPoint worldPos = transformManager.ScreenToWorld(e.Location);
            CoordinatePoint newPoint = new CoordinatePoint(worldPos, 5f);
            Points.Add(newPoint);
            MarkDirty();
        }
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        float dt = (float)stopwatch.Elapsed.TotalSeconds;
        stopwatch.Restart();
        bool updated = false;
        if (inputManager.IsKeyPressed(KeyBinds.Movement.Left))
        {
            offsetX -= moveSpeedMultiplier * dt;
            updated = true;
        }
        if (inputManager.IsKeyPressed(KeyBinds.Movement.Right))
        {
            offsetX += moveSpeedMultiplier * dt;
            updated = true;
        }
        if (inputManager.IsKeyPressed(KeyBinds.Movement.Up))
        {
            offsetY -= moveSpeedMultiplier * dt;
            updated = true;
        }
        if (inputManager.IsKeyPressed(KeyBinds.Movement.Down))
        {
            offsetY += moveSpeedMultiplier * dt;
            updated = true;
        }
        double factor = Math.Pow(1.01, dt / 0.016);
        if (inputManager.IsKeyPressed(KeyBinds.Zoom.Out) && scale >= MinScale)
        {
            scale = (float)Math.Max(MinScale, scale / factor);
            updated = true;
        }
        if (inputManager.IsKeyPressed(KeyBinds.Zoom.In) && scale <= MaxScale)
        {
            scale = (float)Math.Min(MaxScale, scale * factor);
            updated = true;
        }
        float squareStep = 0.1f * (dt / 0.016f);
        if (inputManager.IsKeyPressed(KeyBinds.SquareValue.Decrease))
        {
            squareValue = Math.Min(squareValue + squareStep, MaxSquareValue);
            updated = true;
        }
        if (inputManager.IsKeyPressed(KeyBinds.SquareValue.Increase))
        {
            squareValue = Math.Max(squareValue - squareStep, MinSquareValue);
            updated = true;
        }
        if (updated)
        {
            MarkDirty();
        }
    }

    private void MarkDirty()
    {
        Invalidate();
    }

    private async void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear(new SKColor(BackColor.R, BackColor.G, BackColor.B, BackColor.A));
        SKImageInfo info = new SKImageInfo(Width, Height);
        float effectiveGridSpacing = Grid.DefaultGridSpacing * scale;
        int spacing = Math.Max(1, (int)Math.Round(effectiveGridSpacing));
        if (Grid.ShowGrid)
            Grid.Draw(canvas, info, offsetX, offsetY, spacing);
        if (Axes.ShowAxes)
            Axes.Draw(canvas, info, offsetX, offsetY, effectiveGridSpacing, squareValue);
        transformManager.Update(offsetX, offsetY, scale, squareValue);
        SKRect visibleRect = new SKRect(0, 0, info.Width, info.Height);
        if (Functions.Count > 0)
            await FunctionRenderer.RenderFunctions(canvas, Functions, visibleRect, transformManager);
        foreach (var point in Points)
        {
            SKPoint screenPos = transformManager.WorldToScreen(point.Position);
            float screenRadius = point.Size;
            point.Draw(canvas, screenPos, screenRadius);
        }
        Func<SKPoint, SKPoint> convert = pt => transformManager.WorldToScreen(pt);
        foreach (var seg in segments)
        {
            SKPoint startScreen = convert(seg.StartPoint);
            SKPoint endScreen = convert(seg.EndPoint);
            float minX = Math.Min(startScreen.X, endScreen.X);
            float maxX = Math.Max(startScreen.X, endScreen.X);
            float minY = Math.Min(startScreen.Y, endScreen.Y);
            float maxY = Math.Max(startScreen.Y, endScreen.Y);
            if (maxX < 0 || minX > info.Width || maxY < 0 || minY > info.Height)
                continue;
            seg.Draw(canvas, convert);
        }
        if (currentSegment != null)
            currentSegment.Draw(canvas, convert);
    }

    public void MoveCoordinateSystem(float dx, float dy)
    {
        offsetX += dx;
        offsetY += dy;
        MarkDirty();
    }

    public void SetSquareValue(float newValue)
    {
        squareValue = Math.Max(MinSquareValue, Math.Min(MaxSquareValue, newValue));
        MarkDirty();
    }

    public void SetSquareScale(float newValue)
    {
        scale = Math.Max(MinScale, Math.Min(MaxScale, newValue));
        MarkDirty();
    }

    public float SquareScale
    {
        get => scale;
        set
        {
            scale = Math.Max(MinScale, Math.Min(MaxScale, value));
            MarkDirty();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            updateTimer?.Dispose();
            Axes?.Dispose();
            Grid?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion

}