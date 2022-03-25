using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Face2Foam
{
    public class ImageProcessorView : ObservableObject
    {
        protected Face2FoamLib.ImageProcessor ImageProcessor;
        protected Face2FoamLib.CameraConnector Camera;

        
        public WriteableBitmap OriginalImage { get; protected set; }
        public WriteableBitmap ForegroundRemovedImage { get; protected set; }
        public WriteableBitmap BackgroundRemovedImage { get; protected set; }
        public WriteableBitmap DiscreteImage { get; protected set; }
        public WriteableBitmap SmoothedImage { get; protected set; }
        public WriteableBitmap HeadImage { get; protected set; }
        public WriteableBitmap ProfileImage { get; protected set; }

        protected Face2FoamLib.ImageProcessor.ImageProcessorSettings Settings;
        public ImageProcessorSettingsView SettingsView { get; protected set; }
        string imageSourceFile;
        public string ImageSourceFile { get { return imageSourceFile; } set { SetProperty(ref imageSourceFile, value); UpdateImagesFromFile(); } }
        string gcodeFolder;
        int imageSize;//millimeters of each edge of the square
        public string ImageSize { get { return imageSize.ToString(); } set { try { SetProperty(ref imageSize, Convert.ToInt32(value)); } catch(Exception ex) { SetProperty(ref imageSize, Convert.ToInt32(0)); } } }
        public string GCodeFolder { get { return gcodeFolder; } set { SetProperty(ref gcodeFolder, value); RecalculateButtonPermissives(); } }
        string gcodeFile;
        public string GCodeFile { get { return gcodeFile; } set { SetProperty(ref gcodeFile, value); } }
        string gcodePreamble;
        public string GCodePreamble { get { return gcodePreamble; } set { SetProperty(ref gcodePreamble, value); } }

        Action<System.Drawing.Bitmap> ProcessImageAction;

        public RelayCommand ConnectCameraCommand { get; protected set; }
        public RelayCommand DisconnectCameraCommand { get; protected set; }
        public RelayCommand StartCameraStreamCommand { get; protected set; }
        public RelayCommand StopCameraStreamCommand { get; protected set; }
        public RelayCommand GCodeExportCommand { get; protected set; }

        public ImageProcessorView(Face2FoamLib.ImageProcessor imageProcessor, Face2FoamLib.CameraConnector cameraConnector, Face2FoamLib.ImageProcessor.ImageProcessorSettings settings)
        {
            

            ImageProcessor = imageProcessor;
            Camera = cameraConnector;

            OriginalImage = Face2FoamLib.ImageProcessor.InitializeWritableBitmap(System.Windows.Media.PixelFormats.Bgr24);
            ForegroundRemovedImage = Face2FoamLib.ImageProcessor.InitializeWritableBitmap(System.Windows.Media.PixelFormats.Bgr24);
            BackgroundRemovedImage = Face2FoamLib.ImageProcessor.InitializeWritableBitmap(System.Windows.Media.PixelFormats.Bgr24);
            DiscreteImage = Face2FoamLib.ImageProcessor.InitializeWritableBitmap(System.Windows.Media.PixelFormats.Gray8);
            SmoothedImage = Face2FoamLib.ImageProcessor.InitializeWritableBitmap(System.Windows.Media.PixelFormats.Gray8);
            HeadImage = Face2FoamLib.ImageProcessor.InitializeWritableBitmap(System.Windows.Media.PixelFormats.Gray8);
            ProfileImage = Face2FoamLib.ImageProcessor.InitializeWritableBitmap(System.Windows.Media.PixelFormats.Gray8);

            ProcessImageAction = (System.Drawing.Bitmap b) => { ImageProcessor.ProcessImage(b, Settings); UpdateDisplayedImages(); };
            Camera.NewLiveViewImageAvailable +=  HandleNewLiveViewImage;
            Camera.SomethingChanged += (o, e) => { System.Windows.Application.Current.Dispatcher.Invoke(RecalculateButtonPermissives); };
            Settings = settings;
            SettingsView = new ImageProcessorSettingsView(Settings);
            SettingsView.PropertyChanged += (o, e) => { if(!cameraConnector.IsStreaming) System.Windows.Application.Current.Dispatcher.Invoke(UpdateImagesFromFile); };
            SettingsView.ChildPropertyChanged += (o, e) => { if (!cameraConnector.IsStreaming) System.Windows.Application.Current.Dispatcher.Invoke(UpdateImagesFromFile); };


            GCodeFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            GCodeFile = "Profile";
            imageSize = 600;

            ConnectCameraCommand = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(Camera.Connect, () =>!Camera.IsConnected);
            DisconnectCameraCommand = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(Camera.Dispose, () => Camera.IsConnected);
            StartCameraStreamCommand = new RelayCommand(Camera.StartLiveCapture, () => Camera.IsConnected && !Camera.IsStreaming);
            StopCameraStreamCommand = new RelayCommand(Camera.StopLiveCapture, () => Camera.IsConnected && Camera.IsStreaming);
            GCodeExportCommand = new RelayCommand(ExportGCode, () => System.IO.Directory.Exists(GCodeFolder));
        }

        public void HandleNewLiveViewImage(EOSDigital.API.Camera sender, System.IO.Stream img)
        {
            

            //BitmapImage EvfImage = new BitmapImage();
            System.Drawing.Bitmap bitmap;
            //using (Face2FoamLib.WrapStream s = new Face2FoamLib.WrapStream(img))
            //{
            //    img.Position = 0;
            //    EvfImage.BeginInit();
            //    EvfImage.StreamSource = s;
            //    EvfImage.CacheOption = BitmapCacheOption.OnLoad;
            //    EvfImage.EndInit();
            //    EvfImage.Freeze();
            //}
            //using (System.IO.MemoryStream outStream = new System.IO.MemoryStream())
            //{
            //    BitmapEncoder enc = new BmpBitmapEncoder();
            //    enc.Frames.Add(BitmapFrame.Create(EvfImage));
            //    enc.Save(outStream);
            //    bitmap = new System.Drawing.Bitmap(outStream);

            //}

            bitmap = new System.Drawing.Bitmap(img);
            System.Windows.Application.Current.Dispatcher.BeginInvoke(ProcessImageAction,bitmap);
        }

        public void UpdateImagesFromFile()
        {
            if (!System.IO.File.Exists(ImageSourceFile)) return;
            ImageProcessor.ProcessImage(ImageSourceFile, Settings);
            UpdateDisplayedImages();
        }

        

        public void UpdateDisplayedImages()
        {
            Face2FoamLib.ImageProcessor.WriteToBitmap(ImageProcessor.OriginalImage, OriginalImage);
            OnPropertyChanged(nameof(this.OriginalImage));
            Face2FoamLib.ImageProcessor.WriteToBitmap(ImageProcessor.ForegroundRemovedImage, ForegroundRemovedImage);
            OnPropertyChanged(nameof(this.ForegroundRemovedImage));
            Face2FoamLib.ImageProcessor.WriteToBitmap(ImageProcessor.BackgroundRemovedImage, BackgroundRemovedImage);
            OnPropertyChanged(nameof(this.BackgroundRemovedImage));
            Face2FoamLib.ImageProcessor.WriteToBitmap(ImageProcessor.DiscreteImage, DiscreteImage);
            OnPropertyChanged(nameof(this.DiscreteImage));
            Face2FoamLib.ImageProcessor.WriteToBitmap(ImageProcessor.SmoothedImage, SmoothedImage);
            OnPropertyChanged(nameof(this.SmoothedImage));
            Face2FoamLib.ImageProcessor.WriteToBitmap(ImageProcessor.HeadImage, HeadImage);
            OnPropertyChanged(nameof(this.HeadImage));
            Face2FoamLib.ImageProcessor.WriteToBitmap(ImageProcessor.ProfileImage, ProfileImage);
            OnPropertyChanged(nameof(this.ProfileImage));
        }

        public static void UpdateDisplayedImage(WriteableBitmap displayedImage, AForge.Imaging.UnmanagedImage processedImage)
        {
            System.Drawing.Bitmap bitmap = processedImage.ToManagedImage();

        }

        public void RecalculateButtonPermissives()
        {
            ConnectCameraCommand?.NotifyCanExecuteChanged();
            DisconnectCameraCommand?.NotifyCanExecuteChanged();
            StartCameraStreamCommand?.NotifyCanExecuteChanged();
            StopCameraStreamCommand?.NotifyCanExecuteChanged();
            GCodeExportCommand?.NotifyCanExecuteChanged();
        }

        public void ExportGCode()
        {
            double scale = Convert.ToDouble(imageSize)/ProfileImage.PixelWidth;
            if (System.IO.Directory.Exists(GCodeFolder))
            {

                ImageProcessor.ExportGCode(System.IO.Path.Combine(GCodeFolder, GCodeFile)+".gcode", GCodePreamble, scale);
            }
        }


    }

    public class ImageProcessorSettingsView : ObservableObject
    {
        Face2FoamLib.ImageProcessor.ImageProcessorSettings Settings;

        public ObservableCollection<ImageProcessorColorFilterSettingsView> ForegroundFilters { get; protected set; }
        public ObservableCollection<ImageProcessorColorFilterSettingsView> BackgroundFilters { get; protected set; }

        public RelayCommand AddForegroundFilterCommand { get; }
        public RelayCommand AddBackgroundFilterCommand { get; }
        public event EventHandler ChildPropertyChanged;

        public double MonochromeThreshold
        {
            get { return Settings.MonochromeThreshold; }
            set { SetProperty(Settings.MonochromeThreshold, value, Settings, (o, v) => o.MonochromeThreshold = Convert.ToByte(Math.Min(Math.Max(0, v), 255))); OnPropertyChanged(nameof(MonochromeThresholdText)); }
        }
        public string MonochromeThresholdText
        {
            get { return MonochromeThreshold.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { MonochromeThreshold = test; } }
        }
        public double SmoothingFactor
        {
            get { return Settings.SmoothingFactor; }
            set { SetProperty(Settings.SmoothingFactor, value, Settings, (o, v) => o.SmoothingFactor = Convert.ToByte(Math.Min(Math.Max(0, v), 10))); OnPropertyChanged(nameof(SmoothingFactorText)); }
        }
        public string SmoothingFactorText
        {
            get { return SmoothingFactor.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { SmoothingFactor = test; } }
        }

        public double BlobFilterSize
        {
            get { return Settings.BlobFilterSize; }
            set { SetProperty(Settings.BlobFilterSize, value, Settings, (o, v) => o.BlobFilterSize = Convert.ToInt32(Math.Min(Math.Max(0, v), 1000))); OnPropertyChanged(nameof(BlobFilterSizeText)); }
        }
        public string BlobFilterSizeText
        {
            get { return BlobFilterSize.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { BlobFilterSize = test; } }
        }

        public double StartPosition
        {
            get { return Settings.StartPosition; }
            set { SetProperty(Settings.StartPosition, value, Settings, (o, v) => o.StartPosition = Convert.ToInt32(Math.Min(Math.Max(0, v), 1000))); OnPropertyChanged(nameof(StartPositionText)); }
        }
        public string StartPositionText
        {
            get { return StartPosition.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { StartPosition = test; } }
        }
        public double EndPosition
        {
            get { return Settings.EndPosition; }
            set { SetProperty(Settings.EndPosition, value, Settings, (o, v) => o.EndPosition = Convert.ToInt32(Math.Min(Math.Max(0, v), 1000))); OnPropertyChanged(nameof(EndPositionText)); }
        }
        public string EndPositionText
        {
            get { return EndPosition.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { EndPosition = test; } }
        }

        public ImageProcessorSettingsView(Face2FoamLib.ImageProcessor.ImageProcessorSettings settings)
        {
            Settings = settings;
            ForegroundFilters = new ObservableCollection<ImageProcessorColorFilterSettingsView>(Settings.ForegroundFilters.Select(f => new ImageProcessorColorFilterSettingsView(f,ForegroundFilters)));
            BackgroundFilters = new ObservableCollection<ImageProcessorColorFilterSettingsView>(Settings.BackgroundFilters.Select(f => new ImageProcessorColorFilterSettingsView(f,BackgroundFilters)));
            ForegroundFilters.CollectionChanged += HandleForegroundCollectionChanged;
            BackgroundFilters.CollectionChanged += HandleBackgroundCollectionChanged;
            ForegroundFilters.ToList().ForEach(f => f.PropertyChanged += (o, e) => NotifyChildPropertyChanged());
            BackgroundFilters.ToList().ForEach(f => f.PropertyChanged += (o, e) => NotifyChildPropertyChanged());
            AddForegroundFilterCommand = new RelayCommand(() => ForegroundFilters.Add(new ImageProcessorColorFilterSettingsView(new Face2FoamLib.ImageProcessor.ImageProcessorSettings.ColorFilterSettings(),ForegroundFilters)));
            AddBackgroundFilterCommand = new RelayCommand(() => BackgroundFilters.Add(new ImageProcessorColorFilterSettingsView(new Face2FoamLib.ImageProcessor.ImageProcessorSettings.ColorFilterSettings(), BackgroundFilters)));
        }

        protected void NotifyChildPropertyChanged()
        {
            ChildPropertyChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void HandleForegroundCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        { 
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                Settings.ForegroundFilters.Clear();
            } else {
                if (e.OldItems != null)
                    foreach (ImageProcessorColorFilterSettingsView item in e.OldItems)
                    {
                        Settings.ForegroundFilters.Remove(item.ColorFilterSettings);
                    }
                if (e.NewItems != null)
                    foreach (ImageProcessorColorFilterSettingsView item in e.NewItems)
                    {
                        Settings.ForegroundFilters.Add(item.ColorFilterSettings);
                        item.PropertyChanged += (o, ee) => NotifyChildPropertyChanged();
                    }
            }
        }

        protected void HandleBackgroundCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                Settings.ForegroundFilters.Clear();
            }
            else
            {
                if(e.OldItems != null)
                    foreach (ImageProcessorColorFilterSettingsView item in e.OldItems)
                    {
                        Settings.BackgroundFilters.Remove(item.ColorFilterSettings);
                    }
                if(e.NewItems != null)
                    foreach (ImageProcessorColorFilterSettingsView item in e.NewItems)
                    {
                        Settings.BackgroundFilters.Add(item.ColorFilterSettings);
                        item.PropertyChanged += (o, ee) => NotifyChildPropertyChanged();
                    }
            }
        }

    }

    public class ImageProcessorColorFilterSettingsView : ObservableObject
    {
        ObservableCollection<ImageProcessorColorFilterSettingsView> FilterList;
        public Face2FoamLib.ImageProcessor.ImageProcessorSettings.ColorFilterSettings ColorFilterSettings { get; protected set; }

        public double Red {  
            get { return ColorFilterSettings.Color.Red; }
            set { SetProperty(ColorFilterSettings.Color.Red, value, ColorFilterSettings, (o, v) => o.Color.Red = Convert.ToByte(Math.Min(Math.Max(0, v), 255))); OnPropertyChanged(nameof(RedText)); } 
        }
        public double Green
        {
            get { return ColorFilterSettings.Color.Green; }
            set { SetProperty(ColorFilterSettings.Color.Green, value, ColorFilterSettings, (o, v) => o.Color.Green = Convert.ToByte(Math.Min(Math.Max(0, v), 255))); OnPropertyChanged(nameof(GreenText)); }
        }
        public double Blue
        {
            get { return ColorFilterSettings.Color.Blue; }
            set { SetProperty(ColorFilterSettings.Color.Blue, value, ColorFilterSettings, (o, v) => o.Color.Blue = Convert.ToByte(Math.Min(Math.Max(0, v), 255)) ); OnPropertyChanged(nameof(BlueText)); }
        }
        public double Radius
        {
            get { return ColorFilterSettings.Radius; }
            set { SetProperty(ColorFilterSettings.Radius, value, ColorFilterSettings, (o, v) => o.Radius = Convert.ToInt16(Math.Min(Math.Max(0, v), 255))); OnPropertyChanged(nameof(RadiusText)); }
        }

        public string RedText
        {
            get { return Red.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { Red = test; } }
        }
        public string GreenText
        {
            get { return Green.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { Green = test; } }
        }
        public string BlueText
        {
            get { return Blue.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { Blue = test; } }
        }
        public string RadiusText
        {
            get { return Radius.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { Radius = test; } }
        }

        public bool Enabled
        {
            get { return ColorFilterSettings.Enabled; }
            set { SetProperty(ColorFilterSettings.Enabled, value, ColorFilterSettings, (o, v) => o.Enabled = v); OnPropertyChanged(nameof(ToggleEnableText)); }
        }

        public RelayCommand RemoveCommand { get; }
        public RelayCommand ToggleEnableCommand { get; }
        public string ToggleEnableText { get { return Enabled ? "Disable" : "Enable"; } }

        public ImageProcessorColorFilterSettingsView(Face2FoamLib.ImageProcessor.ImageProcessorSettings.ColorFilterSettings colorFilterSettings, ObservableCollection<ImageProcessorColorFilterSettingsView> filterList)
        {
            ColorFilterSettings = colorFilterSettings;
            FilterList = filterList;
            Enabled = true;

            RemoveCommand = new RelayCommand(() => FilterList.Remove(this));
            ToggleEnableCommand = new RelayCommand(() => Enabled = !Enabled);
        }

    }


}
