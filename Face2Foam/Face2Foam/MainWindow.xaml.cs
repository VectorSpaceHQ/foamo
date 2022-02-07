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

namespace Face2Foam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Face2FoamLib.ImageProcessor imageProcessor;
        Face2FoamLib.ImageProcessor.ImageProcessorSettings imageProcessorSettings;

        public ImageProcessorView ImageView { get; protected set; }
        public MainWindow()
        {
            imageProcessor = new Face2FoamLib.ImageProcessor();
            imageProcessorSettings = new Face2FoamLib.ImageProcessor.ImageProcessorSettings();
            imageProcessorSettings.BackgroundFilters.Add(new Face2FoamLib.ImageProcessor.ImageProcessorSettings.ColorFilterSettings() { Color = new AForge.Imaging.RGB(160, 170, 180), Radius = 255 });
            ImageView = new ImageProcessorView(imageProcessor, imageProcessorSettings);
            DataContext = this;
            InitializeComponent();
            ImageView.ImageSourceFile = @"C:\Users\andre\OneDrive\Documents\Projects\Foam Cutter\Face Profiler\Sample Images 2022-02-06\PXL_20220206_020027263.MP.jpg";
        }
    }
}
