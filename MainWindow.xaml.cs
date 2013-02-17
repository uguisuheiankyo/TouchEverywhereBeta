using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Diagnostics;
using System.IO;

namespace TouchEveryWhereBeta
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Active Kinect Sensor
        /// </summary>
        private KinectSensor sensor;

        // For Color ////////////////////////////////////////
        #region COLOR
        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the rgb camera
        /// </summary>
        private byte[] colorPixels;
        #endregion


        // For Depth ////////////////////////////////////////
        #region DEPTH
        ///// <summary>
        ///// Bitmap that will hold depth information
        ///// </summary>
        private WriteableBitmap depthBitmap;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private DepthImagePixel[] depthPixels;

        /// <summary>
        /// Intermediate storage for the color data received from the depth camera
        /// </summary>
        private byte[] depthPixelsOnImage;
        #endregion

        // For TouchArea ////////////////////////////////////
        #region TOUCHAREA
        /// <summary>
        /// タッチ領域を構成する頂点のリスト
        /// </summary>
        private TouchArea touchArea = new TouchArea();

        /// <summary>
        /// 複数のタッチ領域の頂点を管理するリスト
        /// </summary>
        private List<TouchArea> touchAreas = new List<TouchArea>();
#endregion

        // For InputKeyEvent ////////////////////////////////
        InputEventPublisher iep = new InputEventPublisher();

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">objct sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Look throug all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup
            // To make your app robust against plug/unplug,
            // it is recommended to use kinectSensorChooser provided in Microsoft.Kinect.Tookit
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the streams to receive color and depth frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.sensor.SkeletonStream.Enable();

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                this.depthPixelsOnImage = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                // 距離画像はRGB座標系に合わせてある
                this.depthBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                this.ColorImage.Source = this.colorBitmap;
                this.DepthImage.Source = this.depthBitmap;

                // Near&上半身トラッキングモードにする
                //this.EnableNearModeHalfSkeletalTracking();
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated; // Use seated tracking

                // Add an event handler to be called whenever there is new frames data
                this.sensor.AllFramesReady += this.SensorAllFramesReady;


                // Start the sensor
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's AllFramesReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event agruments</param>
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            #region ColorImageFrame
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the iamge to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    //Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
            #endregion

            #region DepthImageFrame
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    // Convert from Depth Coordinate to Color Coordinate
                    ColorImagePoint[] colorPoints = new ColorImagePoint[depthFrame.PixelDataLength];
                    this.sensor.CoordinateMapper.MapDepthFrameToColorFrame(this.sensor.DepthStream.Format, this.depthPixels, this.sensor.ColorStream.Format, colorPoints);

                    // Get the min and max reliable depth for the current frame
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert the depth to RGB
                    for (int i = 0; i < this.depthPixels.Length; ++i)
                    {
                        // Get the depth for this pixel
                        int depth = depthPixels[i].Depth;

                        // 変換した結果が、フレームサイズを超えることがあるため、小さいほうを使う
                        int x = Math.Min(colorPoints[i].X, this.sensor.ColorStream.FrameWidth - 1);
                        int y = Math.Min(colorPoints[i].Y, this.sensor.ColorStream.FrameHeight - 1);
                        int colorPixelIndex = ((y * depthFrame.Width) + x) * sizeof(int);
                        //int colorPixelIndex = ((colorPoints[i].Y * depthFrame.Width) + colorPoints[i].X) * sizeof(int);

                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).

                        // Note: Using conditionals in this loop could degrade performance.
                        // Consider using a lookup table instead when writing production code.
                        // See the KinectDepthViewer class used by the KinectExplorer sample
                        // for a lookup table example.
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                        // Write out blue byte
                        this.depthPixelsOnImage[colorPixelIndex] = intensity;

                        // Write out green byte
                        this.depthPixelsOnImage[colorPixelIndex + 1] = intensity;

                        // Write out red byte                        
                        this.depthPixelsOnImage[colorPixelIndex + 2] = intensity;

                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        //++colorPixelIndex;
                    }

                    // Write the pixel data into our bitmap
                    //this.colorBitmap.WritePixels(
                    //    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    //    this.colorPixels,
                    //    this.colorBitmap.PixelWidth * sizeof(int),
                    //    0);
                    this.depthBitmap.WritePixels(
                        new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                        this.depthPixelsOnImage,
                        this.depthBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
            #endregion

            #region SkeletonFrame
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skeleton in skeletons)
                    {
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            foreach (Joint joint in skeleton.Joints)
                            {
                                // ジョイントがトラッキングされていなければ次へ
                                if (joint.TrackingState == JointTrackingState.NotTracked)
                                {
                                    continue;
                                }

                                // ジョイントの座標を描く
                                if (joint.JointType == JointType.HandLeft || joint.JointType == JointType.HandRight)
                                {
                                    HandTracking(joint.Position, joint.JointType);
                                }
                            }
                        }
                        // スケルトンが位置追跡だけの場合は、
                        //else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                        //{
                            // スケルトン位置(Center hip)を描画する
                            //DrawEllipse(skeleton.Position);

                        //}
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// ユーザがイメージをクリックした時のハンドル
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Clicked");

            // ユーザがクリックした座標を頂点とするタッチ領域を生成する
            // タッチ領域は4点から構成される
            // すでに生成したタッチ領域内に別のタッチ領域の頂点を設定できない
            // Createセッション以外でクリックが行われた場合，
            // すでにあるタッチ領域内をクリックした場合は半透明色で塗りつぶし

            // クリックされた座標を取得
            Point mousePointOnImage = Mouse.GetPosition(ColorImage);

            // タッチ領域とクリックされた点との内外判定
            int id = InsideOutsideDetections(mousePointOnImage);
            if (id != 0)
            {
                Debug.WriteLine("Inside of the TouchArea" + id);

                foreach (TouchArea touchArea in touchAreas)
                {
                    if (touchArea.ID != id) // 選択の解除
                    {
                        touchArea.Isselected = false;
                    }
                    else // 選択
                    {
                        touchArea.Isselected = true;

                        // 選択されたタッチ領域を塗りつぶす
                        DrawSelectedTouchArea(touchArea.Points);
                    }
                }
                return;
            }
            else // 選択の解除
            {
                foreach (TouchArea touchArea in touchAreas)
                {
                    touchArea.Isselected = false;

                }
                selectedTouchArea.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Createボタンのクリックハンドラー
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ButtonCreateClick(object sender, RoutedEventArgs e)
        {
            // タッチ領域の頂点リストをリセット
            touchArea = new TouchArea();

            // RGB表示領域にタッチ領域生成用クリックハンドラーを追加
            this.rgbImage.Click -= this.ImageClick;
            this.rgbImage.Click += this.CreateTouchAreaClick;
        }

        /// <summary>
        /// 選択されたタッチ領域に対して指定のキーを割り当てる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAssignmentClick(object sender, RoutedEventArgs e)
        {
            string assignKey = cmbAssignKeys.Text;
            if (assignKey != "")
            {
                foreach (TouchArea touchArea in this.touchAreas)
                {
                    if (touchArea.Isselected)
                    {
                        touchArea.AssignKey = assignKey;
                        Debug.WriteLine("The AssignKey of TouchArea" + touchArea.ID + " is " + assignKey);
                    }
                }
            }
        }

        /// <summary>
        /// タッチ領域生成用クリックハンドラー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateTouchAreaClick(object sender, RoutedEventArgs e)
        {

            // クリックされた座標を取得
            Point mousePointOnImage = Mouse.GetPosition(ColorImage);

            // タッチ領域とクリックされた点との内外判定
            int id = InsideOutsideDetections(mousePointOnImage);
            if (id != 0)
            {
                Debug.WriteLine(" TouchArea" + id);
                return;
            }

            if (touchArea.Points.Count < 4)
            {
                // タッチ領域の頂点にボタンを配置
                Button btn = new Button();
                btn.Style = (Style)(this.Resources["PointButton"]);
                Canvas.SetLeft(btn, mousePointOnImage.X - (btn.Width / 2));
                Canvas.SetTop(btn, mousePointOnImage.Y - (btn.Height / 2));
                btn.Visibility = Visibility.Visible;

                // ボタンをキャンバス（映像上）に追加
                ColorCanvas.Children.Add(btn);

                // タッチ領域を構成する頂点として格納
                this.touchArea.Points.Add(mousePointOnImage);
            }
            else
            {
                // RGB表示領域におけるタッチ領域生成用クリックハンドラーを削除
                this.rgbImage.Click -= this.CreateTouchAreaClick;
                this.rgbImage.Click += this.ImageClick;

                // タッチ領域をタッチ領域リストに追加
                this.touchAreas.Add(this.touchArea);

                // タッチ領域にID割り当て
                this.touchArea.ID = this.touchAreas.Count;

                // タッチ領域を描画
                DrawTouchArea(this.touchArea.Points);
            }
        }

        /// <summary>
        /// タッチ領域と点との内外判定
        /// </summary>
        /// <param name="touchAreas"></param>
        /// <param name="clickPoint"></param>
        /// <returns></returns>
        private int InsideOutsideDetections(Point p)
        {
            foreach (TouchArea touchArea in touchAreas)
            {
                if (touchArea.InsideOutsideDetection(p))
                {
                    return touchArea.ID;
                }
            }
            return 0;
        }

        /// <summary>
        /// タッチ領域をキャンバスに追加
        /// </summary>
        /// <param name="touchAreas">タッチ領域の頂点集合</param>
        private void DrawTouchArea(PointCollection Points)
        {
            Polygon polygon = new Polygon();
            polygon.Stroke = Brushes.Red;
            polygon.StrokeThickness = 2;
            polygon.Points = Points;
            ColorCanvas.Children.Add(polygon);
        }

        /// <summary>
        /// 選択されたタッチ領域を塗りつぶす
        /// </summary>
        /// <param name="Points">タッチ領域の頂点集合</param>
        private void DrawSelectedTouchArea(PointCollection Points)
        {
            // 事前に用意したPolygonをタッチ領域にそって塗りつぶす
            selectedTouchArea.Points = Points;
            selectedTouchArea.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 手を追跡し，TouchArea内に入った時にキーイベントを発行する
        /// また，手の位置に円を描画する
        /// </summary>
        /// <param name="kinect"></param>
        /// <param name="position"></param>
        private void HandTracking(SkeletonPoint position, JointType jointType)
        {
            // スケルトンの座標を、RGBカメラの座標に変換する
            ColorImagePoint point = this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(position, this.sensor.ColorStream.Format);

            // 座標を画面のサイズに変換する
            point.X = (int)ScaleTo(point.X, this.sensor.ColorStream.FrameWidth, ColorCanvas.Width);
            point.Y = (int)ScaleTo(point.Y, this.sensor.ColorStream.FrameHeight, ColorCanvas.Height);

            // タッチ領域と手の位置との内外判定
            Point p = new Point(point.X, point.Y);
            int id = this.InsideOutsideDetections(p);

            if (id != 0)
            {
                this.PushKey(touchAreas[id - 1].AssignKey);
            }
            else
            {
                //
            }

            #region 円の描画
            //const int R = 2;

            // 円を描く
            if (jointType == JointType.HandLeft)
            {
                HandLeftPosition.Margin = new Thickness(point.X - 2, point.Y - 2, 0, 0);
                //Debug.WriteLine(point.X + " " + point.Y);
            }
            else if (jointType == JointType.HandRight)
            {
                HandRightPosition.Margin = new Thickness(point.X - 2, point.Y - 2, 0, 0);
            }
            //ColorCanvas.Children.Add(ellipse);
            #endregion
        }

        private void PushKey(string key)
        {
            switch (key)
            {
                case "←":
                    //Debug.WriteLine("←");
                    iep.SendLeftKey();
                    break;
                case "↑":
                    //Debug.WriteLine("↑");
                    iep.SendUpKey();
                    break;
                case "→":
                    //Debug.WriteLine("→");
                    iep.SendRightKey();
                    break;
                case "↓":
                    //Debug.WriteLine("↓");
                    iep.SendDownKey();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 座標の変換
        /// </summary>
        /// <param name="value"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        public double ScaleTo(double value, double source, double dest)
        {
            return (value * dest) / source;
        }

        /// <summary>
        /// Nearモード&上半身トラッキング
        /// </summary>
        /// <param name="kinect"></param>
        private void EnableNearModeHalfSkeletalTracking()
        {
            if (this.sensor != null && this.sensor.DepthStream != null && this.sensor.SkeletonStream != null)
            {
                try
                {
                    this.sensor.DepthStream.Range = DepthRange.Near; // Depth in near range enabled
                    this.sensor.SkeletonStream.EnableTrackingInNearRange = true; // enable returning skeletons while depth is in Near Range
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated; // Use seated tracking
                }
                catch (Exception)
                {
                    Debug.WriteLine("モード切り替えエラーです");
                }
            }
        }
    }
}
