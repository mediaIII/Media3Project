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
using Microsoft.Kinect;
using System.Threading;

namespace Media3Project
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {   
        /// <summary>
        /// テンポ棒の右端
        /// </summary>
        const double MinAngle = -25;
        /// <summary>
        /// テンポ棒の左端
        /// </summary>
        const double MaxAngle = 25;
        /// <summary>
        /// tickの更新用
        /// </summary>
        double tickUpdated = 100000;
        const double Degree = 60;
        int number = 1;
        float[] xarray = new float[100];
        float[] yarray = new float[100];
        float[] grad = new float[100];
        /// <summary>
        /// 特徴点のx,y座標
        /// </summary>
        float[] featureX = new float[100];
        float[] featureY = new float[100];
        /// <summary>
        /// 特徴点のカウント
        /// </summary>
        int featurecount = 1;
        /// <summary>
        /// テンポ用配列
        /// </summary>
        float[] tempoarray = new float[100];
        // フレーム数
        public int flamenum = 0;
        // 計算用の配列
        /// <summary>
        /// 平均値計算用のカウント(3点取り出す)
        /// </summary>
        int meancount = 0;
        /// <summary>
        /// 平均値のx,y座標
        /// </summary>
        float[] xmean = new float[100];
        float[] ymean = new float[100];
        float Volume_max;
        float frame;
        /// <summary>
        /// 平均値の数
        /// </summary>
        int meannum = 0;
        /// <summary>
        /// 2直線の角度
        /// </summary>
        float angleBetween;
        /// <summary>
        ///  特徴点の５フレーム以内は特徴点を検出しない
        /// </summary>
        int FrameDetect = 5;
        /// <summary>
        /// 左側検出
        /// </summary>
        int leftfrag = 0;
        /// <summary>
        /// 右側検出
        /// </summary>
        int rightfrag = 0;
        /// <summary>
        /// 
        /// </summary>
        int InitialCount = 0;
        /// <summary>
        /// 初期位置を判定するときの誤差
        /// </summary>
        float BaseDirection;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 図の初期化
            InitialzeFigure();
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
            kinectSensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinectSensor_SkeletonFrameReady);
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
                    // ジョイントの座標の表示

                    //１秒に１５フレーム表示
                   // Console.WriteLine("FrameNumber:" + skeletonFrame.FrameNumber);

                    // 計算用のカウント数　フレーム数mod100
                    // int number = skeletonFrame.FrameNumber%100;

                    SkeletonPoint FramePoint;

                    number++;
                    number = number % 100;

                    xarray[number] = skeleton.Joints[JointType.HandRight].Position.X;
                    yarray[number] = skeleton.Joints[JointType.HandRight].Position.Y;
                    // x,yの右手の座標
                    FramePoint = skeleton.Joints[JointType.HandRight].Position;

         //          if (Math.Abs(xarray[number]-xarray[number-1])<BaseDirection)
           //         {
             //           InitialCount += 1;
               //     }

                    float volume;
                    float tempo;
                    // 2直線の角度を求める
                    if (number > 2)
                    {

                        Vector vector1 = new Vector(xarray[number] - xarray[number - 1], yarray[number] - yarray[number - 1]);
                        Vector vector2 = new Vector(xarray[number - 1] - xarray[number - 2], yarray[number - 1] - yarray[number - 2]);
                        angleBetween = (float)Vector.AngleBetween(vector1, vector2);
                        
                    }
                    else
                    {
                        // ここはなんか記述？
                    }

                    if (meancount > 2 && number > 2)
                    {
                        xmean[meannum] = (xarray[number] + xarray[number - 1] + xarray[number - 2]) / 3;
                        ymean[meannum] = (yarray[number] + yarray[number - 1] + yarray[number - 2]) / 3;
                        meannum++;
                        meannum = meannum % 100;
                        meancount = 0;
                    }

                    meancount++;
                     Volume_max = yarray.Max();

                    // x,yの増加量
                    if (number != 0)
                    {
                        //float xgrad = xarray[number] - xarray[number - 1];
                        //float ygrad = yarray[number] - yarray[number - 1];
                        //grad[meannum] = xgrad / ygrad;

                        // 特徴点の検出
                        if (angleBetween > Degree && frame + FrameDetect < skeletonFrame.FrameNumber)
                        {
                            frame = skeletonFrame.FrameNumber;
                            //xfeature = xarray[number];
                            //yfeature = yarray[number];
                            //Console.WriteLine("xfeature:" + xfeature);

                            // 特徴点ごとのx,y座標
                            featureX[featurecount] = xmean[meannum];
                            featureY[featurecount] = ymean[meannum];

                            // 青色のマーカー
                            DrawEllipse(kinect, FramePoint, 1);

                            // 特徴点間の距離による音量の計算
                            //volume = (float)Math.Sqrt((double)((xarray[number] - xarray[number - 1]) * (xarray[number] - xarray[number-1]) +
                            //         (yarray[number] - yarray[number - 1]) * (yarray[number] - yarray[number-1])));

                            volume = Volume_max;
                       
                            volume = volume * 500;
                            // 0～128に変更しなければならない(次回)
                           // Console.WriteLine("volume:" + volume);

                            VolumeChange((double)volume);

                            tempoarray[featurecount] = skeletonFrame.FrameNumber;
                            if (featurecount % 6 == 0 || featurecount % 6 == 2)
                            {
                            }
                            else if(featurecount % 6 == 1 || featurecount % 6 == 3){ 
                            tempo = 2*(tempoarray[featurecount] - tempoarray[featurecount - 1]);
                            tempo = 900 / tempo;

                            Console.WriteLine("tempo:" + tempo);
                            tickUpdated = tempo;
                            }
                            else
                            {
                                tempo = tempoarray[featurecount] - tempoarray[featurecount - 1];
                                //bpmに変更する 900は一分間のフレーム数
                                tempo = 900 / tempo;
                                tickUpdated = tempo;
                                Console.WriteLine("tempo:" + tempo);

                            }

                            featurecount++;
                            if (featurecount > 98)
                            {
                                featurecount = 1;
                            }
                        }
                        else
                        {
                            // 赤色のマーカー
                            DrawEllipse(kinect, FramePoint, 0);
                        }

                    }

                    if (flamenum < 10)
                    {
                        flamenum++;
                    }
                    else
                    {
                        flamenum = 0;
                    }

                    //}

                   // Console.WriteLine("PublicCount:" + flamenum);

                     // int body_part = 0;
                    if (skeleton.Joints[JointType.Head].Position.X < skeleton.Joints[JointType.HipCenter].Position.X - 0.1 && skeleton.Joints[JointType.Head].Position.Z < skeleton.Joints[JointType.HipCenter].Position.Z - 0.1)
                    {
                        Console.WriteLine("左側検出");
                        //    body_part = 1;
                        leftfrag = 1;
                    }
                    else if (skeleton.Joints[JointType.Head].Position.X > skeleton.Joints[JointType.HipCenter].Position.X + 0.1 && skeleton.Joints[JointType.Head].Position.Z < skeleton.Joints[JointType.HipCenter].Position.Z - 0.1)
                    {
                        Console.WriteLine("右側検出");
                        //     body_part = 2;
                        rightfrag = 1;
                    }
                    else
                    {
                        leftfrag = 0;
                        rightfrag = 0;
                    }
                }
            }
        }

        private void DrawEllipse(KinectSensor kinect, SkeletonPoint position, int flag)
        {
            const int R = 5;

            // スケルトンの座標を、RGBカメラの座標に変換する
            ColorImagePoint point = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(position, kinect.ColorStream.Format);
            //var point = new Point();
            //// 座標を画面のサイズに変換する
            //point = SkeletonPointToScreen(kinect, position);
            point.X = (int)ScaleTo(point.X, kinect.ColorStream.FrameWidth, canvas1.Width);
            point.Y = (int)ScaleTo(point.Y, kinect.ColorStream.FrameHeight, canvas1.Height);
            //            canvas1.Children.Clear();
            // 円を描く
            Ellipse ellipse = new Ellipse();
            if (flag == 1)
            {
                ellipse.Fill = new SolidColorBrush(Colors.Blue);
            }
            else
            {
                ellipse.Fill = new SolidColorBrush(Colors.Red);
            }
            ellipse.Margin = new Thickness(point.X - R, point.Y - R, 0, 0);
            ellipse.Width = R * 2;
            ellipse.Height = R * 2;
            canvas1.Children.Add(ellipse);
        }

        double ScaleTo(double value, double source, double dest)
        {
            return (value * dest) / source;
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
            // テンポの動作開始
            fromRighttoLeft(tickUpdated, MinAngle, MaxAngle);
        }

        /// <summary>
        /// スライダーの動きに合わせてボリュームが動く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 12 は調整用の係数
            VolumeChange(TestSlider.Value * 12);
        }

        /// <summary>
        /// デバッグ用ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            fromRighttoLeft(1000, MinAngle, MaxAngle);
        }

        /// <summary>
        /// ボリュームのアニメーション
        /// </summary>
        /// <param name="to"></param>
        public void VolumeChange(double to)
        {
            // ストーリボードクラスのインスタンス
            Storyboard storyboard = new Storyboard();
            // 線形補間アニメーション
            DoubleAnimation animation = new DoubleAnimation { To = to, Duration = new Duration(TimeSpan.FromMilliseconds(100)) };
            animation.RepeatBehavior = new RepeatBehavior(1);
            Storyboard.SetTarget(animation, Volume);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        /// <summary>
        /// テンポのアニメーション(反時計回り方向)
        /// </summary>
        /// <param name="milliTime">RightからLeftまでの移動時間(msec)</param>
        /// <param name="Right"></param>
        /// <param name="Left"></param>
        public void fromRighttoLeft(double milliTime, double Right, double Left)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation { From = Right, To = Left, Duration = new Duration(TimeSpan.FromMilliseconds(milliTime)) };
            animation.RepeatBehavior = new RepeatBehavior(1);
            storyboard.Completed += byLeft;
            Storyboard.SetTarget(animation, Tempo);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Rectangle.RenderTransform).(RotateTransform.Angle)"));
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        /// <summary>
        /// 到達時に時計回り方向のアニメーションの実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void byLeft(object sender, EventArgs e)
        {
            fromLefttoRight(tickUpdated, MaxAngle, MinAngle);
        }

        /// <summary>
        /// テンポのアニメーション(時計回り方向)
        /// </summary>
        /// <param name="milliTime">LeftからRightまでの移動時間(msec)</param>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        public void fromLefttoRight(double milliTime, double Left, double Right)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation { From = Left, To = Right, Duration = new Duration(TimeSpan.FromMilliseconds(milliTime)) };
            animation.RepeatBehavior = new RepeatBehavior(1);
            storyboard.Completed += byRight;
            Storyboard.SetTarget(animation, Tempo);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Rectangle.RenderTransform).(RotateTransform.Angle)"));
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        /// <summary>
        /// 到達時に反時計回り方向のアニメーションの実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void byRight(object sender, EventArgs e)
        {
            fromRighttoLeft(tickUpdated, MinAngle, MaxAngle);
        }

        private void TempoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tickUpdated = TempoSlider.Value;
        }

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            canvas1.Children.Clear();
        }
    }
}