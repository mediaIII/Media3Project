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
using System.Windows.Media.Animation;
// Microsoft.Kinectの参照の追加
using Microsoft.Kinect;





namespace Media3Project
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        int number = 0;
        float[] xarray = new float[100];
        float[] yarray = new float[100];
        float[] grad = new float[100];
        // 特徴点のx,y座標
        float[] xf = new float[100];
        float[] yf = new float[100];
        int count2 = 1;
        // テンポ用配列
        float[] xf2 = new float[100];
        // フレーム数
        public int count = 0;
        // 計算用の配列

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            // ここからキネクトを記述
            // Kinectが接続されているかどうかを確認する
            try
            {

                if (KinectSensor.KinectSensors.Count == 0)
                {
                    throw new Exception("Kinectを接続してください");
                }

                // Kinectの動作を開始する
                StartKinect(KinectSensor.KinectSensors[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        private void StartKinect(KinectSensor kinectSensor)
        {
            kinectSensor.SkeletonStream.Enable();
            kinectSensor.SkeletonFrameReady +=new EventHandler<SkeletonFrameReadyEventArgs>(kinectSensor_SkeletonFrameReady);
            kinectSensor.Start();
        }


        /// <summary>
        /// スケルトンフレームの準備
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e) 
        {
            KinectSensor kinect = sender as KinectSensor;
            if (kinect == null)
            {
                return;
            } 
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    // スケルトンのジョイントの座標の取得
                    GetPointSkeleton(kinect, skeletonFrame);
                }
            }
        }
        /// <summary>
        /// スケルトンから座標の取得, 計算
        /// </summary>
        /// <param name="kinect"></param>
        /// <param name="skeletonFrame"></param>
        private void GetPointSkeleton(KinectSensor kinect, SkeletonFrame skeletonFrame)
        {
            Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
            skeletonFrame.CopySkeletonDataTo(skeletons);
            // トラッキングされているスケルトンのジョイントの座標表示
            foreach (Skeleton skeleton in skeletons)
            {
                // スケルトンがトラッキング状態(デフォルトモード)の場合は、ジョイントを描画する
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {

                    //// ジョイントがトラッキングされていなければ次へ  <--- トラッキングの判定用に使えそう
                    //if (joint.TrackingState == JointTrackingState.NotTracked)
                    //{
                    //    continue;
                    //}

                    // ジョイントの座標の表示

                    //Console.WriteLine("HeadPoint(X,Y):" + skeleton.Joints[JointType.Head].Position.X + "," + skeleton.Joints[JointType.Head].Position.Y);
                    //Console.WriteLine("RightHandPoint(X,Y):" + skeleton.Joints[JointType.HandRight].Position.X + "," + skeleton.Joints[JointType.HandRight].Position.Y);
                    //Console.WriteLine("HeadPoint(Z):" + skeleton.Joints[JointType.Head].Position.Z);

                    //１秒に１５フレーム表示
                    Console.WriteLine("FrameNumber:" + skeletonFrame.FrameNumber);

                    // 計算用のカウント数　フレーム数mod100
                    // int number = skeletonFrame.FrameNumber%100;

                    if (skeletonFrame.FrameNumber % 3 == 0)
                    {
                        number++;
                    }

                    number = number % 100;
                    // x,yの右手の座標
                    xarray[number] = skeleton.Joints[JointType.HandRight].Position.X;
                    yarray[number] = skeleton.Joints[JointType.HandRight].Position.Y;
                    //float xfeature;
                    //float yfeature;



                    float volume;
                    float tempo;

                    // x,yの増加量
                    if (number != 0)
                    {
                        float xgrad = xarray[number] - xarray[number - 1];
                        float ygrad = yarray[number] - yarray[number - 1];
                        grad[number] = xgrad / ygrad;
                        //Console.WriteLine("xgrad:" + xgrad);
                        //Console.WriteLine("ygrad:" + ygrad);
                        Console.WriteLine("grad[number]:" + grad[number]);
                        Console.WriteLine("grad[number-1]:" + grad[number - 1]);

                        if (grad[number] * grad[number - 1] < 0)
                        {   // 特徴点の検出
                            //xfeature = xarray[number];
                            //yfeature = yarray[number];
                            //Console.WriteLine("xfeature:" + xfeature);

                            // 特徴点ごとのx,y座標
                            xf[count2] = xarray[number];
                            yf[count2] = yarray[number];



                            // 特徴点間の距離による音量の計算
                            volume = (float)Math.Sqrt((double)((xf[count2] - xf[count2 - 1]) * (xf[count2] - xf[count2 - 1]) +
                                     (yf[count2] - yf[count2 - 1]) * (yf[count2] - yf[count2 - 1])));
                            Console.WriteLine("volume:" + volume);
                            Console.WriteLine("count2:" + count2);

                            xf2[count2] = skeletonFrame.FrameNumber;

                            tempo = xf2[count2] - xf2[count2 - 1];
                            Console.WriteLine("tempo:" + tempo);

                            count2++;
                            if (count2 > 98)
                            {
                                count2 = 1;
                            }
                        }

                    }

                    if (count < 10)
                    {
                        count++;
                    }
                    else
                    {
                        count = 0;
                    }

                    Console.WriteLine("PublicCount:" + count);

                    int body_part = 0;
                    if (skeleton.Joints[JointType.Head].Position.X < -0.2 && skeleton.Joints[JointType.Head].Position.Z < 1.7)
                    {
                        Console.WriteLine("左側検出");
                        body_part = 1;
                    }
                    else if (skeleton.Joints[JointType.Head].Position.X > 0.2 && skeleton.Joints[JointType.Head].Position.Z < 1.7)
                    {
                        Console.WriteLine("右側検出");
                        body_part = 2;
                    }

                }
            }

        }


        /// <summary>
        /// Kinectの動作を停止する
        /// </summary>
        /// <param name="kinect"></param>
        private void StopKinect(KinectSensor kinect)
        {
            if (kinect != null)
            {
                if (kinect.IsRunning)
                {
                    // スケルトンのフレーム更新イベントを削除する
                    kinect.SkeletonFrameReady -= kinectSensor_SkeletonFrameReady;

                    // Kinectの停止と、ネイティブリソースを解放する
                    kinect.Stop();
                    kinect.Dispose();
                }
            }
        }

        /// <summary>
        /// Windowが閉じられるときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect(KinectSensor.KinectSensors[0]);
        }

        /// <summary>
        /// 図形の初期化
        /// </summary>
        private void InitialzeFigure()
        {
            Volume.Height = Volume.MinHeight;
            // テストスライダーはデバッグ用
            TestSlider.Value = 0;
            // 回転中心の初期化
            Tempo.RenderTransformOrigin = new Point(0.5, 1.0);
            // テンポ角の初期化
            Tempo.RenderTransform = new RotateTransform(0);
        }
        /// <summary>
        /// スライダーの動きに合わせてボリュームが動く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VolumeChange(Volume.Height, TestSlider.Value * 12);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Volume.Height = 100;
            Ellipse ellipse = new Ellipse();
            Canvas.SetLeft(ellipse, 10);
            Canvas.SetTop(ellipse, 50);
            ellipse.Width = 20;
            ellipse.Height = 20;
            ellipse.Stroke = Brushes.Black;
            ellipse.StrokeThickness = 3;
            ellipse.Fill = Brushes.Red;
            canvas.Children.Add(ellipse);
        }
        /// <summary>
        /// ボリュームのアニメーション
        /// </summary>
        /// <param name="from"></param>　これ要らん
        /// <param name="to"></param>
        public void VolumeChange(double from, double to)
        {
            // ストーリボードクラスのインスタンス
            Storyboard storyboard = new Storyboard();
            storyboard.FillBehavior = FillBehavior.HoldEnd; // これいる？
            // 線形補間アニメーション
            DoubleAnimation animation = new DoubleAnimation { From = from, To = to, Duration = new Duration(TimeSpan.FromMilliseconds(100)) };
            animation.RepeatBehavior = new RepeatBehavior(1);
            Storyboard.SetTarget(animation, Volume);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        public void TempoChange(double tick)
        {
            Tempo.RenderTransform = new RotateTransform();
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation { From = 10, To = 100, Duration = new Duration(TimeSpan.FromMilliseconds(1000)) };
            animation.RepeatBehavior = new RepeatBehavior(1);
            Storyboard.SetTarget(animation, Tempo);
            //Storyboard.SetTargetProperty(animation, new PropertyPath();
        }

    }
}