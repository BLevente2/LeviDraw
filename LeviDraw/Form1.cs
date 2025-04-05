using SkiaSharp;

namespace LeviDraw
{
    public partial class Form1 : Form
    {
        CoordinateSystem _coordinateSystem;


        public Form1()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;

            _coordinateSystem = new CoordinateSystem();
            _coordinateSystem.Location = new Point(0, 0);
            _coordinateSystem.Size = new Size(this.Width, this.Height);

            _coordinateSystem.Dock = DockStyle.Fill;

            this.Controls.Add(_coordinateSystem);

            Function sin = new Function("fx", "sin(x)", SKColors.Yellow, 1f);
            Function quadratic = new Function("gx", "x^2", SKColors.Red, 1f);
            Function cubic = new Function("hx", "x^3", SKColors.Yellow, 1f);
            //_coordinateSystem.Functions.Add(sin);
            //_coordinateSystem.Functions.Add(quadratic);
            _coordinateSystem.Functions.Add(cubic);

            _coordinateSystem.Grid.ShowGrid = false;
            _coordinateSystem.Axes.ShowAxes = false;

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
