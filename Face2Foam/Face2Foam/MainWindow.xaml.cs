using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Face2FoamLib;

namespace Face2Foam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        Face2FoamLib.CameraConnector cameraConnector;
        Face2FoamLib.ImageProcessor imageProcessor;
        Face2FoamLib.ImageProcessor.ImageProcessorSettings imageProcessorSettings;

        public ImageProcessorView ImageView { get; protected set; }
        public MainWindow()
        {
            Closed += (o, e) => Dispose();

            cameraConnector = new Face2FoamLib.CameraConnector();
            imageProcessor = new Face2FoamLib.ImageProcessor();
            imageProcessorSettings = new Face2FoamLib.ImageProcessor.ImageProcessorSettings();
#if DEBUG
            imageProcessorSettings.BackgroundFilters.Add(new Face2FoamLib.ImageProcessor.ImageProcessorSettings.ColorFilterSettings() { Color = new AForge.Imaging.RGB(160, 170, 180), Radius = 255 });
#endif
#if !DEBUG
            imageProcessorSettings.BackgroundFilters.Add(new Face2FoamLib.ImageProcessor.ImageProcessorSettings.ColorFilterSettings() { Color = new AForge.Imaging.RGB(200, 200, 200), Radius = 255 });
#endif
            ImageView = new ImageProcessorView(imageProcessor, cameraConnector, imageProcessorSettings);
            DataContext = this;
            InitializeComponent();

#if DEBUG
            ImageView.ImageSourceFolder = @"C:\Users\andre\OneDrive\Documents\Projects\Foam Cutter\Face Profiler\Sample Images 2022-02-06";
            ImageView.GCodePreamble = "G21\r\nG90\r\nG28 X Y\r\nG0 X0 Y0\r\nG1 F600";
#endif
#if !DEBUG
            ImageView.ImageSourceFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            ImageView.GCodeFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            ImageView.GCodePreamble = (new System.Net.WebClient()).DownloadString(@"https://raw.githubusercontent.com/VectorSpaceHQ/foamo/main/Face2Foam/GCode%20Preamble.txt");
#endif

        }

        public void Dispose()
        {
            if (cameraConnector != null) cameraConnector.Dispose();
        }


        private void ForegroundImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Image i = sender as Image;
            System.Windows.Point p = e.GetPosition(i);
            ImageView.AddForegroundFilterFromMouseClick(Convert.ToDouble(p.X)/i.Width, Convert.ToDouble(p.Y) / i.Height);
        }

        private void BackgroundImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Image i = sender as Image;
            System.Windows.Point p = e.GetPosition(i);
            ImageView.AddBackgroundFilterFromMouseClick(Convert.ToDouble(p.X) / i.Width, Convert.ToDouble(p.Y) / i.Height);
        }

        private void Smoothing_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            Image i = sender as Image;
            System.Windows.Point p = e.GetPosition(i);
            ImageView.SetStartPositionFromMouseClick(Convert.ToDouble(p.X) / i.Width);
        }
        private void Smoothing_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            Image i = sender as Image;
            System.Windows.Point p = e.GetPosition(i);
            ImageView.SetEndPositionFromMouseClick(Convert.ToDouble(p.X) / i.Width);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;

            if (hwndSource != null)
                hwndSource.CompositionTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            base.OnSourceInitialized(e);
        }
    }
}
