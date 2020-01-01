using Accord.Imaging;
using Splat;
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
using OpenCvSharp;
using System.Drawing;
using System.IO;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace ShapeChecker.DemoCore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ComboBox1.ItemsSource = System.IO.Directory.GetFiles("../../../Images")
                .ToDictionary(a => System.IO.Path.GetFileNameWithoutExtension(a), a=>a);

            ComboBox1.SelectionChanged += ComboBox1_SelectionChanged;
            // IBitmap is a type that provides basic image information such as dimensions
            //IBitmap profileImage = BitmapLoader.Current.LoadFromResource("../../../Images/demo.png", null, null).Result;

            this.Loaded += MainWindow_Loaded;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox1.SelectedIndex = 0;
        }

        private void ComboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            //Bitmap image = new Bitmap(assembly.GetManifestResourceStream("ShapeChecker." + embeddedFileName));
            (Bitmap b1, Bitmap b2, System.Windows.Rect rect) = HoughLinesSample.SampleCpp(e.AddedItems.Cast<KeyValuePair<string,string>>().First().Value);
            myGreyImage.Source = ConvertBitmapToBitmapImage.Convert(b1);
            detectCircle.Source = ConvertBitmapToBitmapImage.Convert(b2);


            var rect2 = new System.Windows.Shapes.Rectangle
            {
                Stroke = System.Windows.Media.Brushes.LightBlue,
                StrokeThickness = 2,
                Width = rect.Width,
                Height = rect.Height
            };

            Canvas.SetLeft(rect2, rect.X);
            Canvas.SetTop(rect2, rect.Y);
            Canvas2.Children.Add(rect2);
        }




        // Process image
        private void ProcessImage(IBitmap bitmap)
        {

        }

        private void testButton_Click(object sender, RoutedEventArgs e)
        {

        }


        //public Image<Bgr, Byte> fsd(string fileName)
        //{

        //    //Load the image from file and resize it for display
        //    Image<Bgr, Byte> img =
        //       new Image<Bgr, byte>(fileName)
        //       .Resize(400, 400, Emgu.CV.CvEnum.Inter.Linear, true);

        //    //Convert the image to grayscale and filter out the noise
        //    UMat uimage = new UMat();
        //    CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);

        //    double cannyThreshold = 180.0;
        //    double circleAccumulatorThreshold = 120;
        //    CircleF[] circles = CvInvoke.HoughCircles(uimage, HoughType.Gradient, 2.0, 20.0, cannyThreshold, circleAccumulatorThreshold, 5);

        //    Image<Bgr, Byte> circleImage = img.CopyBlank();
        //    foreach (CircleF circle in circles)
        //        circleImage.Draw(circle, new Bgr(Color.Brown), 2);
        //    return circleImage;

        //}


        private System.Windows.Point startPoint;
        private Rectangle rect;

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(Canvas2);

            rect = new Rectangle
            {
                Stroke = System.Windows.Media.Brushes.RosyBrown,
                StrokeThickness = 2
            };
            Canvas.SetLeft(rect, startPoint.X);
            Canvas.SetTop(rect, startPoint.Y);
            Canvas2.Children.Add(rect);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released || rect == null)
                return;

            var pos = e.GetPosition(Canvas2);

            var x = Math.Min(pos.X, startPoint.X);
            var y = Math.Min(pos.Y, startPoint.Y);

            var w = Math.Max(pos.X, startPoint.X) - x;
            var h = Math.Max(pos.Y, startPoint.Y) - y;

            rect.Width = w;
            rect.Height = h;

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            rect = null;
        }
    }



    /// <summary>
    /// Hough Transform Sample / ハフ変換による直線検出
    /// </summary>
    /// <remarks>http://opencv.jp/sample/special_transforms.html#hough_line</remarks>
    class HoughLinesSample
    {
        //public void Run()
        //{
        //    SampleCpp();
        //}

        /// <summary>
        /// sample of new C++ style wrapper
        /// </summary>
        public static (Bitmap, Bitmap, System.Windows.Rect) SampleCpp(string file)
        {
            // (1) Load the image
            using (Mat imgGray = new Mat(file, ImreadModes.Grayscale))
            using (Mat imgStd = new Mat(file, ImreadModes.Color))
            using (Mat imgProb = imgStd.Clone())
            {
                // Preprocess
                Cv2.Canny(imgGray, imgGray, 50, 200, 3, false);

                // (3) Run Standard Hough Transform 
                LineSegmentPolar[] segStd = Cv2.HoughLines(imgGray, 1, Math.PI / 180, 50, 0, 0);


                var circles = Cv2.HoughCircles(imgGray, HoughMethods.Gradient, 2, 40);
                System.Windows.Rect rect = default;
                foreach (var circle in circles.Take(1))
                {
                    var point = new OpenCvSharp.Point { X = (int)circle.Center.X, Y = (int)circle.Center.Y };
                    imgStd.Circle(point, (int)circle.Radius, Scalar.AliceBlue, 2);

                    rect = new System.Windows.Rect(point.X - circle.Radius, point.Y - circle.Radius, circle.Radius * 2, circle.Radius * 2);
                }

                int limit = Math.Min(segStd.Length, 10);
                for (int i = 0; i < limit; i++)
                {
                    // Draws result lines
                    float rho = segStd[i].Rho;
                    float theta = segStd[i].Theta;
                    double a = Math.Cos(theta);
                    double b = Math.Sin(theta);
                    double x0 = a * rho;
                    double y0 = b * rho;
                    OpenCvSharp.Point pt1 = new OpenCvSharp.Point { X = (int)Math.Round(x0 + 1000 * (-b)), Y = (int)Math.Round(y0 + 1000 * (a)) };
                    OpenCvSharp.Point pt2 = new OpenCvSharp.Point { X = (int)Math.Round(x0 - 1000 * (-b)), Y = (int)Math.Round(y0 - 1000 * (a)) };
                    imgStd.Line(pt1, pt2, Scalar.Red, 3, LineTypes.AntiAlias, 0);
                }

                // (4) Run Probabilistic Hough Transform
                LineSegmentPoint[] segProb = Cv2.HoughLinesP(imgGray, 1, Math.PI / 180, 50, 50, 10);
                foreach (LineSegmentPoint s in segProb)
                {
                    imgProb.Line(s.P1, s.P2, Scalar.Red, 3, LineTypes.AntiAlias, 0);
                }

                var bitmapGray = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(imgStd);

                var bitmapGra2y = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(imgProb);

                return (bitmapGray, bitmapGra2y, rect);
                // (5) Show results
                //using (new Window("Hough_line_standard", WindowMode.AutoSize, imgStd))
                //using (new Window("Hough_line_probabilistic", WindowMode.AutoSize, imgProb))
                //{
                //    Window.WaitKey(0);
                //}
            }

        }
        private static Bitmap MatToBitmap(Mat mat)
        {
            using (var ms = mat.ToMemoryStream())
            {
                return (Bitmap)System.Drawing.Image.FromStream(ms);
            }
        }





    }


    public class ConvertBitmapToBitmapImage
    {
        /// <summary>
        /// Takes a bitmap and converts it to an image that can be handled by WPF ImageBrush
        /// </summary>
        /// <param name="src">A bitmap image</param>
        /// <returns>The image as a BitmapImage for WPF</returns>
        public static BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
    }
}

