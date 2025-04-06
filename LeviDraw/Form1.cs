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

            Function sin = new Function("fx", "sin(x)", SKColors.Aqua, 55f);
            _coordinateSystem.Functions.Add(sin);

            _coordinateSystem.Grid.GridThickness = 0.5f;


        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
