using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AStar;

namespace Face2FoamLib
{
    public class ImageProcessor
    {
        public static int imageWidth = 1000;
        public static int imageHeight = 1000;
        public static int imageDpi = 200;

        public UnmanagedImage OriginalImage { get; protected set; }
        public UnmanagedImage ForegroundRemovedImage { get; protected set; }//foreground-specific colors turned to pure black
        public UnmanagedImage BackgroundRemovedImage { get; protected set; }//background specific colors turned to pure white, all others turned to pure black
        public UnmanagedImage DiscreteImage { get; protected set; }
        public UnmanagedImage SmoothedImage { get; protected set; }
        public UnmanagedImage HeadImage { get; protected set; }
        public UnmanagedImage ProfileImage { get; protected set; }

        bool Busy;


        public void ProcessImage(string imageFile, ImageProcessorSettings settings)
        {
            ProcessImage(new Bitmap(imageFile), settings);
        }

        public void ProcessImage(Bitmap image, ImageProcessorSettings settings)
        {
            try
            {
                if (Busy) return;
                using (UnmanagedImage sourceImage = UnmanagedImage.FromManagedImage(image))
                {
                    ProcessOriginalImage(settings, sourceImage);
                }
            }catch (Exception ex)
            {

            } finally
            {
                image.Dispose();
            }
            
        }
        protected void ProcessOriginalImage(ImageProcessorSettings settings, UnmanagedImage sourceImage)
        {
            Busy = true;

            OriginalImage?.Dispose();
            ForegroundRemovedImage?.Dispose();
            BackgroundRemovedImage?.Dispose();
            DiscreteImage?.Dispose();
            SmoothedImage?.Dispose();
            HeadImage?.Dispose();
            ProfileImage?.Dispose();

            //resize and crop original image
            double factor = Math.Max( imageHeight/Convert.ToDouble(sourceImage.Height), imageWidth/Convert.ToDouble(sourceImage.Width) );
            ResizeBilinear resizer = new ResizeBilinear(Convert.ToInt32(factor * sourceImage.Width), Convert.ToInt32(factor * sourceImage.Height));
            UnmanagedImage resizedImage = resizer.Apply(sourceImage);
            sourceImage.Dispose();
            Crop croper = new Crop(new Rectangle((resizedImage.Width-imageWidth)/2, (resizedImage.Height-imageHeight)/2, imageWidth, imageHeight));
            OriginalImage = croper.Apply(resizedImage);
            resizedImage.Dispose();


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
            Dilatation dilationFilter = new Dilatation();
            Erosion erosionFilter = new Erosion();
            //Closing closingFilter = new Closing();
            for(short i = 0; i < settings.SmoothingFactor; i++)
            {
                //closingFilter.ApplyInPlace(SmoothedImage);
                dilationFilter.ApplyInPlace(SmoothedImage);
            }
            for (short i = 0; i < settings.SmoothingFactor; i++)
            {
                //closingFilter.ApplyInPlace(SmoothedImage);
                erosionFilter.ApplyInPlace(SmoothedImage);
            }

            //draw vertical white lines from bottom of picture until first white pixel at both start and end positions
            int horizontalStartPosition = Math.Min(SmoothedImage.Width - 1, Convert.ToInt32(Convert.ToDouble(settings.StartPosition) / 1000 * (SmoothedImage.Width - 1)));
            int lineEnd = SmoothedImage.Height - 2;
            while (SmoothedImage.GetPixel(horizontalStartPosition, lineEnd).R < 255 && lineEnd > 0) lineEnd--;
            Drawing.Line(SmoothedImage, new AForge.IntPoint(horizontalStartPosition, SmoothedImage.Height - 1), new AForge.IntPoint(horizontalStartPosition, lineEnd), Color.White);
            int horizontalEndPosition = Math.Min(SmoothedImage.Width - 1, Convert.ToInt32(Convert.ToDouble(settings.EndPosition) / 1000 * (SmoothedImage.Width - 1)));
            lineEnd = SmoothedImage.Height - 2;
            while (SmoothedImage.GetPixel(horizontalEndPosition, lineEnd).R < 255 && lineEnd > 0) lineEnd--;
            Drawing.Line(SmoothedImage, new AForge.IntPoint(horizontalEndPosition, SmoothedImage.Height - 1), new AForge.IntPoint(horizontalEndPosition, lineEnd), Color.White);


            BlobsFiltering blobFilter = new BlobsFiltering() { MinHeight = settings.BlobFilterSize, MinWidth = settings.BlobFilterSize, CoupledSizeFiltering=true };
            Invert invertingFilter = new Invert();

            HeadImage = SmoothedImage.Clone();
            blobFilter.ApplyInPlace(HeadImage);
            invertingFilter.ApplyInPlace(HeadImage);
            blobFilter.ApplyInPlace(HeadImage);
            invertingFilter.ApplyInPlace(HeadImage);

            //flood fill anything on bottom border outside of start/end
            PointedColorFloodFill floodFill = new PointedColorFloodFill(Color.White);
            for(int horizontalPixel = 0; horizontalPixel < HeadImage.Width; horizontalPixel++)
            {
                if(horizontalPixel < horizontalStartPosition || horizontalPixel > horizontalEndPosition)
                {
                    if (HeadImage.GetPixel(horizontalPixel, HeadImage.Height-1).R < 255)
                    {
                        floodFill.StartingPoint = new AForge.IntPoint(horizontalPixel, HeadImage.Height - 1);
                        floodFill.ApplyInPlace(HeadImage);
                    }
                }
            }


            //detect edges
            IFilter edgeDetector = new SobelEdgeDetector();
            ProfileImage = edgeDetector.Apply(HeadImage);
            //dilationFilter.ApplyInPlace(ProfileImage);

            Busy = false;
        }

        //public static System.Windows.Media.Imaging.BitmapImage GetBitmapFromUnmanaged(UnmanagedImage image, System.Drawing.Imaging.ImageFormat imageFormat = null)
        //{
        //    if (image == null) return null;

        //    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        //    { 
        //        Bitmap bitmap = image.ToManagedImage();
        //        bitmap.Save(ms, imageFormat ?? System.Drawing.Imaging.ImageFormat.Bmp);
        //        ms.Position = 0;
        //        System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.StreamSource = ms;
        //        bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        //        bitmapImage.EndInit();
        //        return bitmapImage;
        //    }
        //}

        public static void WriteToBitmap(UnmanagedImage source, System.Windows.Media.Imaging.WriteableBitmap dest)
        {
            dest.Lock();
            try
            {
                dest.WritePixels(new System.Windows.Int32Rect(0, 0, source.Width, source.Height), source.ImageData, System.Drawing.Image.GetPixelFormatSize(source.PixelFormat) / 8 * source.Width * source.Height, source.Stride);
            }
            catch (Exception ex) { }
            dest.Unlock();
        }

        public static System.Windows.Media.Imaging.WriteableBitmap InitializeWritableBitmap(System.Windows.Media.PixelFormat pixelFormat)
        {
            return new System.Windows.Media.Imaging.WriteableBitmap(imageWidth, imageHeight, imageDpi, imageDpi, pixelFormat, null);
        }

        public void ExportGCode(string file, string preamble, double scale)
        {
            int baseY = ProfileImage.Height - 2;//last row is all black, second to last row has start/stop
            int startX = 0;
            while(ProfileImage.GetPixel(startX,baseY).R == 0 && startX < ProfileImage.Width) startX++;
            int endX = ProfileImage.Width - 1;
            while(ProfileImage.GetPixel(endX,baseY).R == 0 && endX >= 0) endX--;

            //create astar grid
            short[,] tiles = new short[ProfileImage.Height, ProfileImage.Width];
            for(int x = 0; x < ProfileImage.Width; x++)
            {
                for(int y = 0; y < ProfileImage.Height; y++)
                {
                    tiles[y,x] = ProfileImage.GetPixel(x, y).R;
                }
            }
            WorldGrid worldGrid = new WorldGrid(tiles);
            PathFinder pathFinder = new PathFinder(worldGrid, new AStar.Options.PathFinderOptions() { PunishChangeDirection = true, UseDiagonals = true, SearchLimit = 20*ProfileImage.Width + 20*ProfileImage.Height }) ;
            System.Diagnostics.Debug.Assert(worldGrid[new Point(startX, baseY)] == 255);
            System.Diagnostics.Debug.Assert(worldGrid[new Point(endX, baseY)] == 255);
            Point[] points = pathFinder.FindPath(new Point(startX, baseY), new Point(endX, baseY));


            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(file))
            {
                sw.WriteLine(preamble);
                sw.WriteLine(PrintPoint(new Point(0, ProfileImage.Height-1), scale));
                sw.WriteLine(PrintPoint(new Point(startX, ProfileImage.Height - 1), scale));
                foreach(Point point in points)
                {
                    sw.WriteLine(PrintPoint(point,scale));
                }
                sw.WriteLine(PrintPoint(new Point(endX, ProfileImage.Height - 1), scale));
                sw.WriteLine(PrintPoint(new Point(ProfileImage.Width, ProfileImage.Height - 1), scale));
            }
        }

        //scale is mm per pixel
        protected string PrintPoint(Point point, double scale)
        {
            return string.Format("G1 X{0:F2} Y{1:F2}", point.X * scale, (ProfileImage.Height-1- point.Y) * scale) ;
        }



        public class ImageProcessorSettings
        {
            public List<ColorFilterSettings> ForegroundFilters { get; set; }
            public List<ColorFilterSettings> BackgroundFilters { get; set; }
            public byte MonochromeThreshold { get; set; }
            public byte SmoothingFactor { get; set; }
            public int BlobFilterSize { get; set; }
            public int StartPosition { get; set; } //0-1000
            public int EndPosition { get; set; } //0-1000

            public ImageProcessorSettings()
            {
                ForegroundFilters = new List<ColorFilterSettings>();
                BackgroundFilters = new List<ColorFilterSettings>();
                MonochromeThreshold = 254;
                SmoothingFactor = 1;
                BlobFilterSize = 300;
                StartPosition = 250;
                EndPosition = 750;
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
