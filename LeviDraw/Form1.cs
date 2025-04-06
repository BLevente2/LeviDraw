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

            Function linear = new Function("hx", "x", SKColors.LightSkyBlue, 2f);
            Function cubic = new Function("hx", "x^3", SKColors.Yellow, 2f);
            Function tan = new Function("hx", "tan(x)", SKColors.Aqua, 2f);
            Function hiperbolic = new Function("hx", "1/x", SKColors.Azure, 2f);
            //_coordinateSystem.Functions.Add(linear);
            //_coordinateSystem.Functions.Add(cubic);
            //_coordinateSystem.Functions.Add(tan);
            _coordinateSystem.Functions.Add(hiperbolic);

            _coordinateSystem.Grid.GridThickness = 0.5f;

            CoordinatePoint pont1 = new CoordinatePoint(new SKPoint(2, 2), 10f, SKColors.SkyBlue);
            //_coordinateSystem.Points.Add(pont1);

            _coordinateSystem.Axes.ShowAxes = false;
            _coordinateSystem.Grid.ShowGrid = false;

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
