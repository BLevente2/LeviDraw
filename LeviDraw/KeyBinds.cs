namespace LeviDraw;

    public class KeyBinds
    {
        public readonly Movement Movement;
        public readonly Zoom Zoom;
        public readonly SquareValue SquareValue;

        public KeyBinds()
        {
            Movement = new Movement();
            Zoom = new Zoom();
            SquareValue = new SquareValue();
        }
    }

    public class Movement
    {
        public Keys Up { get; set; }
        public Keys Down { get; set; }
        public Keys Left { get; set; }
        public Keys Right { get; set; }

        public Movement()
        {
            Up = Keys.W;
            Down = Keys.S;
            Left = Keys.A;
            Right = Keys.D;
        }
    }

    public class Zoom
    {
        public Keys In { get; set; }
        public Keys Out { get; set; }

        public Zoom()
        {
            In = Keys.E;
            Out = Keys.Q;
        }
    }

public class SquareValue
{
    public Keys Increase { get; set; }
    public Keys Decrease { get; set; }

    public SquareValue()
    {
        Increase = Keys.Add;
        Decrease = Keys.Subtract;
    }
}