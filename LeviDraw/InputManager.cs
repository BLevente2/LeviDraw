namespace LeviDraw;

internal class InputManager
{

    #region Proterties&Constructors

    internal bool IsDragging { get; private set; }
    internal System.Drawing.Point LastMousePosition { get; set; }
    internal int PressedKeysCount => pressedKeys.Count;

    private HashSet<Keys> pressedKeys;

    internal InputManager()
    {
        pressedKeys = new HashSet<Keys>();
        IsDragging = false;
        LastMousePosition = System.Drawing.Point.Empty;
    }

    #endregion

    #region Methods

    internal void HandleKeyDown(KeyEventArgs e)
    {
        pressedKeys.Add(e.KeyCode);
    }

    internal bool IsKeyPressed(Keys key)
    {
        return pressedKeys.Contains(key);
    }

    internal void HandleKeyUp(KeyEventArgs e)
    {
        pressedKeys.Remove(e.KeyCode);
    }

    internal bool HasActiveInput()
    {
        return pressedKeys.Count > 0;
    }

    internal void HandleMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            IsDragging = true;
            LastMousePosition = e.Location;
        }
    }
    internal void HandleMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            IsDragging = false;
    }

    #endregion

}