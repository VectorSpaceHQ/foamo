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

        

        public BitmapImage OriginalImage => Face2FoamLib.ImageProcessor.GetBitmapFromUnmanaged(ImageProcessor.OriginalImage);
        public BitmapImage ForegroundRemovedImage => Face2FoamLib.ImageProcessor.GetBitmapFromUnmanaged(ImageProcessor.ForegroundRemovedImage);
        public BitmapImage BackgroundRemovedImage => Face2FoamLib.ImageProcessor.GetBitmapFromUnmanaged(ImageProcessor.BackgroundRemovedImage);
        public BitmapImage DiscreteImage => Face2FoamLib.ImageProcessor.GetBitmapFromUnmanaged(ImageProcessor.DiscreteImage);
        public BitmapImage SmoothedImage => Face2FoamLib.ImageProcessor.GetBitmapFromUnmanaged(ImageProcessor.SmoothedImage);
        public BitmapImage HeadImage => Face2FoamLib.ImageProcessor.GetBitmapFromUnmanaged(ImageProcessor.HeadImage);

        protected Face2FoamLib.ImageProcessor.ImageProcessorSettings Settings;
        public ImageProcessorSettingsView SettingsView { get; protected set; }
        string imageSourceFile;
        public string ImageSourceFile { get { return imageSourceFile; } set { SetProperty(ref imageSourceFile, value); UpdateImagesCommand.NotifyCanExecuteChanged(); } }

        public RelayCommand UpdateImagesCommand { get; protected set; }
        public RelayCommand ConnectCameraCommand { get; protected set; }
        public RelayCommand StreamCameraCommand { get; protected set; }

        public ImageProcessorView(Face2FoamLib.ImageProcessor imageProcessor, Face2FoamLib.CameraConnector cameraConnector, Face2FoamLib.ImageProcessor.ImageProcessorSettings settings)
        {
            ImageProcessor = imageProcessor;
            Camera = cameraConnector;
            Camera.NewLiveViewImageAvailable += (c,s) => ImageProcessor.ProcessImage(s,Settings);
            Settings = settings;
            SettingsView = new ImageProcessorSettingsView(Settings);

            UpdateImagesCommand = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(UpdateImages,() => System.IO.File.Exists(ImageSourceFile) && !Camera.IsStreaming);
            ConnectCameraCommand = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(Camera.Connect, () =>true || !Camera.IsConnected);
            StreamCameraCommand = new RelayCommand(Camera.StartLiveCapture, () => true || Camera.IsConnected && !Camera.IsStreaming);
        }

        public void UpdateImages()
        {
            ImageProcessor.ProcessImage(ImageSourceFile, Settings);
            OnPropertyChanged(nameof(this.OriginalImage));
            OnPropertyChanged(nameof(this.ForegroundRemovedImage));
            OnPropertyChanged(nameof(this.BackgroundRemovedImage));
            OnPropertyChanged(nameof(this.DiscreteImage));
            OnPropertyChanged(nameof(this.SmoothedImage));
            OnPropertyChanged(nameof(this.HeadImage));
        }

        


    }

    public class ImageProcessorSettingsView : ObservableObject
    {
        Face2FoamLib.ImageProcessor.ImageProcessorSettings Settings;

        public ObservableCollection<ImageProcessorColorFilterSettingsView> ForegroundFilters { get; protected set; }
        public ObservableCollection<ImageProcessorColorFilterSettingsView> BackgroundFilters { get; protected set; }

        public RelayCommand AddForegroundFilterCommand { get; }
        public RelayCommand AddBackgroundFilterCommand { get; }

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
            set { SetProperty(Settings.BlobFilterSize, value, Settings, (o, v) => o.BlobFilterSize = Convert.ToInt32(Math.Min(Math.Max(0, v), 5000))); OnPropertyChanged(nameof(BlobFilterSizeText)); }
        }
        public string BlobFilterSizeText
        {
            get { return BlobFilterSize.ToString("F0"); }
            set { double test; if (double.TryParse(value, out test)) { BlobFilterSize = test; } }
        }

        public ImageProcessorSettingsView(Face2FoamLib.ImageProcessor.ImageProcessorSettings settings)
        {
            Settings = settings;
            ForegroundFilters = new ObservableCollection<ImageProcessorColorFilterSettingsView>(Settings.ForegroundFilters.Select(f => new ImageProcessorColorFilterSettingsView(f,ForegroundFilters)));
            BackgroundFilters = new ObservableCollection<ImageProcessorColorFilterSettingsView>(Settings.BackgroundFilters.Select(f => new ImageProcessorColorFilterSettingsView(f,BackgroundFilters)));
            ForegroundFilters.CollectionChanged += HandleForegroundCollectionChanged;
            BackgroundFilters.CollectionChanged += HandleBackgroundCollectionChanged;
            AddForegroundFilterCommand = new RelayCommand(() => ForegroundFilters.Add(new ImageProcessorColorFilterSettingsView(new Face2FoamLib.ImageProcessor.ImageProcessorSettings.ColorFilterSettings(),ForegroundFilters)));
            AddBackgroundFilterCommand = new RelayCommand(() => BackgroundFilters.Add(new ImageProcessorColorFilterSettingsView(new Face2FoamLib.ImageProcessor.ImageProcessorSettings.ColorFilterSettings(), BackgroundFilters)));
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
