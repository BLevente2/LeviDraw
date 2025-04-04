using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Timers;

namespace LeviDraw;

public class CoordinateSystem : SKControl, IDisposable
{
    public Grid Grid { get; set; }
    public Axes Axes { get; set; }
    public KeyBinds KeyBinds { get; set; }
    private float offsetX;
    private float offsetY;
    private float scale;
    private System.Timers.Timer updateTimer;
    private HashSet<Keys> pressedKeys;
    private float moveSpeedMultiplier;
    private bool isDragging;
    private Point lastMousePosition;
    private volatile bool _dirty;
    private const float MinScale = 0.5f;
    private const float MaxScale = 5f;
    private float squareValue;
    private const float MinSquareValue = 0.1f;
    private const float MaxSquareValue = 10f;
    private const float MouseMoveThreshold = 2f;

    public CoordinateSystem() : base()
    {
        BackColor = SystemColors.Control;
        TabStop = true;
        offsetX = 0f;
        offsetY = 0f;
        scale = 1f;
        squareValue = 1f;
        moveSpeedMultiplier = 5.0f;
        Grid = new Grid(scale);
        Axes = new Axes();
        KeyBinds = new KeyBinds();
        PaintSurface += OnPaintSurface;
        updateTimer = new System.Timers.Timer(16);
        updateTimer.AutoReset = true;
        updateTimer.Elapsed += UpdateTimer_Elapsed;
        pressedKeys = new HashSet<Keys>();
        isDragging = false;
        _dirty = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!pressedKeys.Contains(e.KeyCode))
        {
            pressedKeys.Add(e.KeyCode);
        }
        if (pressedKeys.Count > 0 && !updateTimer.Enabled)
        {
            updateTimer.Start();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (pressedKeys.Contains(e.KeyCode))
        {
            pressedKeys.Remove(e.KeyCode);
        }
        if (pressedKeys.Count == 0 && !isDragging && updateTimer.Enabled)
        {
            updateTimer.Stop();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button == MouseButtons.Left)
        {
            isDragging = true;
            lastMousePosition = e.Location;
            if (!updateTimer.Enabled)
            {
                updateTimer.Start();
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (isDragging)
        {
            float dx = e.X - lastMousePosition.X;
            float dy = e.Y - lastMousePosition.Y;
            if (Math.Abs(dx) >= MouseMoveThreshold || Math.Abs(dy) >= MouseMoveThreshold)
            {
                offsetX += dx;
                offsetY += dy;
                lastMousePosition = e.Location;
                MarkDirty();
            }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
        {
            isDragging = false;
            if (pressedKeys.Count == 0 && updateTimer.Enabled)
            {
                updateTimer.Stop();
            }
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Delta > 0)
        {
            squareValue = Math.Min(squareValue + 0.1f, MaxSquareValue);
        }
        else
        {
            squareValue = Math.Max(squareValue - 0.1f, MinSquareValue);
        }
        MarkDirty();
    }

    private void UpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        bool updated = false;
        if (pressedKeys.Contains(KeyBinds.Movement.Left))
        {
            offsetX -= moveSpeedMultiplier;
            updated = true;
        }
        if (pressedKeys.Contains(KeyBinds.Movement.Right))
        {
            offsetX += moveSpeedMultiplier;
            updated = true;
        }
        if (pressedKeys.Contains(KeyBinds.Movement.Up))
        {
            offsetY -= moveSpeedMultiplier;
            updated = true;
        }
        if (pressedKeys.Contains(KeyBinds.Movement.Down))
        {
            offsetY += moveSpeedMultiplier;
            updated = true;
        }
        if (pressedKeys.Contains(KeyBinds.Zoom.Out) && scale >= MinScale)
        {
            scale *= 0.99f;
            updated = true;
        }
        if (pressedKeys.Contains(KeyBinds.Zoom.In) && scale <= MaxScale)
        {
            scale *= 1.01f;
            updated = true;
        }
        if (pressedKeys.Contains(KeyBinds.SquareValue.Increase))
        {
            squareValue = Math.Min(squareValue + 0.1f, MaxSquareValue);
            updated = true;
        }
        if (pressedKeys.Contains(KeyBinds.SquareValue.Decrease))
        {
            squareValue = Math.Max(squareValue - 0.1f, MinSquareValue);
            updated = true;
        }
        if (updated)
        {
            _dirty = true;
        }
        if (_dirty)
        {
            Invoke((Action)(() => { Invalidate(); _dirty = false; }));
        }
    }

    private void MarkDirty()
    {
        _dirty = true;
        if (!updateTimer.Enabled)
        {
            Invalidate();
            _dirty = false;
        }
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear(new SKColor(BackColor.R, BackColor.G, BackColor.B, BackColor.A));
        SKImageInfo info = new SKImageInfo(Width, Height);
        float effectiveGridSpacing = Grid.DefaultGridSpacing * scale;
        int spacing = Math.Max(1, (int)Math.Round(effectiveGridSpacing));
        if (Grid.ShowGrid)
        {
            Grid.Draw(canvas, info, offsetX, offsetY, spacing);
        }
        if (Axes.ShowAxes)
        {
            Axes.Draw(canvas, info, offsetX, offsetY, effectiveGridSpacing, squareValue);
        }
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
}