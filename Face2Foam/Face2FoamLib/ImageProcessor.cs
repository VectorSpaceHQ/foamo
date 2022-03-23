using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace Face2FoamLib
{
    public class ImageProcessor
    {
        public UnmanagedImage OriginalImage { get; protected set; }
        public UnmanagedImage ForegroundRemovedImage { get; protected set; }//foreground-specific colors turned to pure black
        public UnmanagedImage BackgroundRemovedImage { get; protected set; }//background specific colors turned to pure white, all others turned to pure black
        public UnmanagedImage DiscreteImage { get; protected set; }
        public UnmanagedImage SmoothedImage { get; protected set; }
        public UnmanagedImage HeadImage { get; protected set; }

        bool Busy;


        public void ProcessImage(string imageFile, ImageProcessorSettings settings)
        {
            if (Busy) return;
            OriginalImage = UnmanagedImage.FromManagedImage(new Bitmap(imageFile));
            ProcessOriginalImage(settings);
        }

        public void ProcessImage(System.IO.Stream stream, ImageProcessorSettings settings)
        {
            if(Busy) return;
            using(WrapStream ws = new WrapStream(stream))
            {
                stream.Position = 0;
                OriginalImage = UnmanagedImage.FromManagedImage(new Bitmap(ws));
            }
            ProcessOriginalImage(settings);
        }
        protected void ProcessOriginalImage(ImageProcessorSettings settings)
        {
            Busy = true;
            EuclideanColorFiltering radiusFilter = new EuclideanColorFiltering();
            radiusFilter.FillOutside = false;
            ColorFiltering thresholdFilter = new ColorFiltering();
            thresholdFilter.FillOutsideRange = false;

            //turn anything matching foreground filters pure black
            ForegroundRemovedImage = OriginalImage.Clone();
            radiusFilter.FillColor = new RGB(0, 0, 0);
            thresholdFilter.FillColor = new RGB(0, 0, 0);
            foreach (var filter in settings.ForegroundFilters.Where(f => f.Enabled))
            {
                if(filter.Radius <= 0)
                {
                    thresholdFilter.Red = new AForge.IntRange(0, filter.Color.Red);
                    thresholdFilter.Green = new AForge.IntRange(0, filter.Color.Green);
                    thresholdFilter.Blue = new AForge.IntRange(0, filter.Color.Blue);
                    thresholdFilter.ApplyInPlace(ForegroundRemovedImage);
                } else if(filter.Radius >= 255)
                {
                    thresholdFilter.Red = new AForge.IntRange(filter.Color.Red, 255);
                    thresholdFilter.Green = new AForge.IntRange(filter.Color.Green, 255);
                    thresholdFilter.Blue = new AForge.IntRange(filter.Color.Blue, 255);
                    thresholdFilter.ApplyInPlace(ForegroundRemovedImage);
                } else
                {
                    radiusFilter.CenterColor = filter.Color;
                    radiusFilter.Radius = filter.Radius;
                    radiusFilter.ApplyInPlace(ForegroundRemovedImage);
                }
                
            }

            //turn anything matching background filters pure white
            BackgroundRemovedImage = ForegroundRemovedImage.Clone();
            radiusFilter.FillColor = new RGB(255, 255, 255);
            thresholdFilter.FillColor = new RGB(255, 255, 255);
            foreach (var filter in settings.BackgroundFilters.Where(f => f.Enabled))
            {
                if (filter.Radius <= 0)
                {
                    thresholdFilter.Red = new AForge.IntRange(0, filter.Color.Red);
                    thresholdFilter.Green = new AForge.IntRange(0, filter.Color.Green);
                    thresholdFilter.Blue = new AForge.IntRange(0, filter.Color.Blue);
                    thresholdFilter.ApplyInPlace(BackgroundRemovedImage);
                }
                else if (filter.Radius >= 255)
                {
                    thresholdFilter.Red = new AForge.IntRange(filter.Color.Red, 255);
                    thresholdFilter.Green = new AForge.IntRange(filter.Color.Green, 255);
                    thresholdFilter.Blue = new AForge.IntRange(filter.Color.Blue, 255);
                    thresholdFilter.ApplyInPlace(BackgroundRemovedImage);
                }
                else
                {
                    radiusFilter.CenterColor = filter.Color;
                    radiusFilter.Radius = filter.Radius;
                    radiusFilter.ApplyInPlace(BackgroundRemovedImage);
                }

            }


            //turn anything not pure white into pure black
            DiscreteImage = Grayscale.CommonAlgorithms.BT709.Apply(BackgroundRemovedImage);
            Threshold binaryFilter = new Threshold() { ThresholdValue = settings.MonochromeThreshold };
            binaryFilter.ApplyInPlace(DiscreteImage);


            //dilate than erode (white) image to get rid of stray (black) hairs
            SmoothedImage = DiscreteImage.Clone();
            Closing closingFilter = new Closing();
            for(short i = 0; i < settings.SmoothingFactor; i++)
            {
                closingFilter.ApplyInPlace(SmoothedImage);
            }

            BlobsFiltering blobFilter = new BlobsFiltering() { MinHeight = settings.BlobFilterSize, MinWidth = settings.BlobFilterSize, CoupledSizeFiltering=true };
            Invert invertingFilter = new Invert();

            HeadImage = SmoothedImage.Clone();
            blobFilter.ApplyInPlace(HeadImage);
            invertingFilter.ApplyInPlace(HeadImage);
            blobFilter.ApplyInPlace(HeadImage);
            invertingFilter.ApplyInPlace(HeadImage);

            Busy = false;
        }

        public static System.Windows.Media.Imaging.BitmapImage GetBitmapFromUnmanaged(UnmanagedImage image, System.Drawing.Imaging.ImageFormat imageFormat = null)
        {
            if (image == null) return null;

            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            Bitmap bitmap = image.ToManagedImage();
            bitmap.Save(ms, imageFormat ?? System.Drawing.Imaging.ImageFormat.Bmp);
            ms.Position = 0;
            System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        public class ImageProcessorSettings
        {
            public List<ColorFilterSettings> ForegroundFilters { get; set; }
            public List<ColorFilterSettings> BackgroundFilters { get; set; }
            public byte MonochromeThreshold { get; set; }
            public byte SmoothingFactor { get; set; }

            public int BlobFilterSize { get; set; }

            public ImageProcessorSettings()
            {
                ForegroundFilters = new List<ColorFilterSettings>();
                BackgroundFilters = new List<ColorFilterSettings>();
                MonochromeThreshold = 254;
                SmoothingFactor = 0;
                BlobFilterSize = 100;
            }

            public class ColorFilterSettings
            {
                public RGB Color { get; set; }
                public short Radius { get; set; }//<=0 : all belwo color; >= 255 : all above color; else, radius from color
                public bool Enabled { get; set; }

                public ColorFilterSettings()
                {
                    Color = new RGB(255, 255, 255);
                }

            }
        }
    }
}
