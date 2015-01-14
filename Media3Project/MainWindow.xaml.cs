using Microsoft.Kinect;
using NextMidi.Data;
using NextMidi.Data.Domain;
using NextMidi.Data.Score;
using NextMidi.DataElement;
using NextMidi.DataElement.MetaData;
using NextMidi.Filing.Midi;
using NextMidi.MidiPort.Output;
using NextMidi.Time;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


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
        public static double tickUpdated = 1;
        /// <summary>
        /// ???
        /// </summary>
        const double Degree = 50;
        int number = 1;
        float[] xarray = new float[75];
        float[] yarray = new float[75];
        float[] grad = new float[100];
        int maxtemponumber;
        /// <summary>
        /// 特徴点のx,y座標
        /// </summary>
        float[] featureX = new float[100];
        float[] featureY = new float[100];
        /// <summary>
        /// 特徴点のカウント
        /// </summary>
        int featurecount = 0;
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
        float[] betweentempo = new float[100];
        float[] maxtempo = new float[2];
        float[] TotalDistance = new float[75];
        float[] tempo2 = new float[2];
        float rate;
        int tempocount = 0;
        int maxtempocount = 0;
        int startfrag = 0;
        double Headposition = 0;
        float[] ytempoarray = new float[100];
        StreamWriter w = new StreamWriter("kekka.txt");
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
        int FrameDetect = 30;
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
        MyMidiOutPort MyMidiOutPort;
        MidiData MidiData;
        MidiPlayer Player;
        /// <summary>
        /// グループ(パート)の配列
        /// </summary>
        static public int[] group = new int[128];
        /// <summary>
        /// チャンネルの番号番目に楽器番号が入った配列
        /// </summary>
        static public int[] value = new int[128];
        /// <summary>
        /// 原曲のTickが入った一次元配列
        /// </summary>
        List<int> tick_org = new List<int>();
        /// <summary>
        /// 原曲のGateが入った一次元配列
        /// </summary>
        List<int> gate_org = new List<int>();
        /// <summary>
        /// Tickをいじる比率
        /// </summary>
        double Coef = 1.0;
        /// <summary>
        /// 前のTickの比率
        /// </summary>
        double OldCoef = 1.0;
        MyQueue Queue = new MyQueue();


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += Window_Closing;
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


            //Midiの参照

            // ポートの指定
            MyMidiOutPort = new MyMidiOutPort(new MidiOutPort(0));
            Console.WriteLine(MyMidiOutPort.IsOpen);

            // 指定したポートを開く
            try
            {
                MyMidiOutPort.Open();
            }
            catch
            {
                Console.WriteLine("no such port exists");
                return;
            }
            // ファイルパスの指定
            //            string path = "test.mid";
            string path = "C:\\Users\\media3\\Downloads\\DQ.mid";
            if (!File.Exists(path))
            {
                Console.WriteLine("File dose not exist");
                return;
            }
            // midiファイルの読み込み
            MidiData = MidiReader.ReadFrom(path, Encoding.GetEncoding("shift-jis"));
            MidiFileDomain domain = new MidiFileDomain(MidiData);

            //曲に使われている楽器を5つのグループに分ける
            MakeGroup();

            //原曲のTickとGateを一次元配列に格納
            Store();

            // Playerの作成
            Player = new MidiPlayer(MyMidiOutPort);
            Player.Stopped += Player_Stopped;
            // 別スレッドでの演奏開始

            Player.Play(domain);
        }
        /// <summary>
        /// 原曲のTickGateを一次元配列に格納
        /// </summary>
        void Store()
        {
            foreach (var track in MidiData.Tracks)
            {
                foreach (var note in track.GetData<NoteEvent>())
                {
                    tick_org.Add(note.Tick);
                    gate_org.Add(note.Gate);
                }
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
                    SkeletonPoint FramePoint;

                    number++;
                    number = number % 75;

                    xarray[number] = skeleton.Joints[JointType.HandRight].Position.X;
                    yarray[number] = skeleton.Joints[JointType.HandRight].Position.Y;
                    // x,yの右手の座標
                    FramePoint = skeleton.Joints[JointType.HandRight].Position;

                    float tempo;

                    if (meancount > 2 && number > 2)
                    {
                        xmean[meannum] = (xarray[number] + xarray[number - 1] + xarray[number - 2]) / 3;
                        ymean[meannum] = (yarray[number] + yarray[number - 1] + yarray[number - 2]) / 3;
                        meannum++;
                        meannum = meannum % 100;
                        meancount = 0;
                    }


                    Queue.Add(skeleton.Joints[JointType.HandRight].Position.X);
                    meancount++;

                    // 100はマイナスにしないための初期値
                    // Volume_max = 100 + 200 * (yarray.Max() - skeleton.Joints[JointType.Head].Position.Y);
                    Volume_max = 120 * (yarray.Max() - yarray.Min());

                    if (flamenum < 10)
                    {
                        flamenum++;
                    }
                    else
                    {
                        flamenum = 0;
                    }


                    if (skeleton.Joints[JointType.Head].Position.X < skeleton.Joints[JointType.HipCenter].Position.X - 0.1
                        && skeleton.Joints[JointType.Head].Position.Z < skeleton.Joints[JointType.HipCenter].Position.Z - 0.1)
                    {
                        Headposition = -0.08;
                        leftfrag = 1;
                        rightfrag = 0;
                    }
                    else if (skeleton.Joints[JointType.Head].Position.X > skeleton.Joints[JointType.HipCenter].Position.X + 0.1
                        && skeleton.Joints[JointType.Head].Position.Z < skeleton.Joints[JointType.HipCenter].Position.Z - 0.1)
                    {
                        Headposition = 0.05;
                        rightfrag = 1;
                        leftfrag = 0;
                    }
                    else if (skeleton.Joints[JointType.Head].Position.Z < skeleton.Joints[JointType.HipCenter].Position.Z - 0.1)
                    {
                        Headposition = 0;
                        leftfrag = 1;
                        rightfrag = 1;
                    }
                    else
                    {
                        Headposition = 0;
                        leftfrag = 0;
                        rightfrag = 0;
                    }

                    if (skeleton.Joints[JointType.HandRight].Position.X < (skeleton.Joints[JointType.Head].Position.X + Headposition)
                           && Queue.Fetch(0) > Queue.Fetch(1) && Queue.Fetch(1) < Queue.Fetch(2) && frame + FrameDetect < skeletonFrame.FrameNumber)
                    {
                        frame = skeletonFrame.FrameNumber;
                        tempo2[tempocount] = frame;
                        tempocount++;
                        startfrag = 1;

                        if (tempocount > 1)
                        {
                            tempocount = 0;
                        }

                        tempo = Math.Abs(tempo2[0] - tempo2[1]);
                        if (tempo > 40 && tempo < 100)
                        {
                            tickUpdated = 8 * tempo;
                        }
                        Kinect_tempo_Change((double)tempo);
                        Console.WriteLine("tempo:" + tempo);
                        Console.WriteLine("volume:" + Volume_max);
                        DrawEllipse(kinect, FramePoint, 1);
                    }
                }
            }
        }

        private void DrawEllipse(KinectSensor kinect, SkeletonPoint position, int flag)
        {
            const int R = 7;

            // スケルトンの座標を、RGBカメラの座標に変換する
            ColorImagePoint point = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(position, kinect.ColorStream.Format);
            point.X = (int)ScaleTo(point.X, kinect.ColorStream.FrameWidth, canvas1.Width);
            point.Y = (int)ScaleTo(point.Y, kinect.ColorStream.FrameHeight, canvas1.Height);
            //            canvas1.Children.Clear();
            // 円を描く
            Ellipse ellipse = new Ellipse();
            if (flag == 1)
            {
                ellipse.Fill = new SolidColorBrush(Colors.Blue);
                ellipse.Margin = new Thickness(point.X - R, point.Y - R, 0, 0);
                ellipse.Width = R * 2;
                ellipse.Height = R * 2;
            }
            else
            {
                ellipse.Fill = new SolidColorBrush(Colors.Red);
                ellipse.Margin = new Thickness(point.X - 2, point.Y - 2, 0, 0);
                ellipse.Width = 2 * 2;
                ellipse.Height = 2 * 2;
            }
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
                    w.Close();
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
            fromRighttoLeft(0.1, MinAngle, MaxAngle);
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
        /// <param name="millisecTime">RightからLeftまでの移動時間(msec)</param>
        /// <param name="Right"></param>
        /// <param name="Left"></param>
        public void fromRighttoLeft(double millisecTime, double Right, double Left)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation { From = Right, To = Left, Duration = new Duration(TimeSpan.FromMilliseconds(millisecTime)) };
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
        /// <param name="millisecTime">LeftからRightまでの移動時間(msec)</param>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        public void fromLefttoRight(double millisecTime, double Left, double Right)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation { From = Left, To = Right, Duration = new Duration(TimeSpan.FromMilliseconds(millisecTime)) };
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

        /// <summary>
        /// [note.value]番目にnote.valueの楽器がどのグループに入るか0〜4の数字が入った配列をつくる
        /// </summary>
        void MakeGroup()
        {
            foreach (var track in MidiData.Tracks)
            {
                foreach (var note in track.GetData<ProgramEvent>())
                {
                    //0?7, 16?24番の楽器番号ならパート0
                    if ((0 <= note.Value && note.Value <= 7) || (16 <= note.Value && note.Value <= 23))
                    {
                        group[note.Value] = 0;
                        value[(int)note.Channel] = note.Value;
                        Console.WriteLine("strument{0} group{1}", note.Value, group[note.Value]);
                    }
                    //8?15, 108, 112?119番の楽器番号ならパート1
                    if ((8 <= note.Value && note.Value <= 15) || (112 <= note.Value && note.Value <= 119) || note.Value == 108)
                    {
                        group[note.Value] = 1;
                        value[(int)note.Channel] = note.Value;
                        Console.WriteLine("strument{0} group{1}", note.Value, group[note.Value]);
                    }
                    //24?55, 104?107, 110番の楽器番号ならパート2
                    if ((24 <= note.Value && note.Value <= 55) || (104 <= note.Value && note.Value <= 107) || note.Value == 110)
                    {
                        group[note.Value] = 2;
                        value[(int)note.Channel] = note.Value;
                        Console.WriteLine("strument{0} group{1}", note.Value, group[note.Value]);
                    }
                    //56?79, 109, 111番の楽器番号ならパート3
                    if ((56 <= note.Value && note.Value <= 79) || note.Value == 109 || note.Value == 111)
                    {
                        group[note.Value] = 3;
                        value[(int)note.Channel] = note.Value;
                        Console.WriteLine("strument{0} group{1}", note.Value, group[note.Value]);
                    }
                    //80〜103,120?127番の楽器番号ならパート4
                    if ((80 <= note.Value && note.Value <= 103) || (120 <= note.Value && note.Value <= 127))
                    {
                        group[note.Value] = 4;
                        value[(int)note.Channel] = note.Value;
                        Console.WriteLine("strument{0} group{1}", note.Value, group[note.Value]);
                    }

                }
            }
        }

        /// <summary>
        /// チャンネル内の楽器番号を羅列
        /// </summary>
        void CheckStrument()
        {
            foreach (var track in MidiData.Tracks)
            {
                foreach (var note in track.GetData<ProgramEvent>())
                {
                    Console.WriteLine("strument {0} channel {1}", note.Value, note.Channel);
                }
            }
        }

        void Player_Stopped(object sender, EventArgs e)
        {
            MyMidiOutPort.Close();
        }
        /// <summary>
        /// キネクトでのボリューム変更の反映
        /// </summary>
        /// <param name="leftflag"></param>
        /// <param name="rightflag"></param>
        /// <param name="volume"></param>
        void Kinect_Volume_Change(int leftflag, int rightflag, int volume)
        {
            if (MyMidiOutPort != null)
            {
                if (leftflag == 0 && rightflag == 0)
                {
                    MyMidiOutPort.deltaVelocity0 = volume - 60;
                    MyMidiOutPort.deltaVelocity1 = volume - 60;
                    MyMidiOutPort.deltaVelocity2 = volume - 60;
                }
                else if (leftflag == 1 && rightflag == 0)
                {
                    MyMidiOutPort.deltaVelocity1 = (volume - 100) * 10;
                }
                else if (leftflag == 0 && rightflag == 1)
                {
                    MyMidiOutPort.deltaVelocity2 = (volume - 100) * 10;
                }
                else if (leftflag == 1 && rightflag == 1)
                {
                    MyMidiOutPort.deltaVelocity0 = (volume - 100) * 10;
                }
            }
        }

        private void Kinect_tempo_Change(double tempo)
        {
            if (MyMidiOutPort != null)
            {
                int index = 0;
                Coef = tempo / 60;
                if (Coef - OldCoef > 0.3) Coef = OldCoef + 0.3;

                if (Coef - OldCoef < -0.3) Coef = OldCoef - 0.3;

                foreach (var track in MidiData.Tracks)
                {
                    foreach (var note in track.GetData<NoteEvent>())
                    {
                        if (Player.Tick < note.Tick)
                        {
                            note.Tick = (int)((double)(tick_org[index] - Player.Tick) * Coef + Player.Tick);
                            note.Gate = (int)((double)gate_org[index] * Coef);
                        }
                        index++;
                    }
                }
                OldCoef = Coef;
            }
        }
    }
    /// <summary>
    /// MidiOutPortのSend改良版
    /// </summary>
    class MyMidiOutPort : IMidiOutPort
    {
        /// <summary>
        ///  パート(グループ)
        /// </summary>
        int[] MainWindowGroup = Media3Project.MainWindow.group;
        /// <summary>
        /// 楽器番号
        /// </summary>
        int[] MainWindowValue = Media3Project.MainWindow.value;
        MidiOutPort Delegate;
        /// <summary>
        /// Tickの比率
        /// </summary>
        public double Coef = 1.0;
        /// <summary>
        /// Velocityの増減量
        /// </summary>
        public int deltaVelocity = 0;
        /// <summary>
        /// グループ0のVelocityの増減量
        /// </summary>
        public int deltaVelocity0 = 0;
        /// <summary>
        /// グループ1のVelocityの増減量
        /// </summary>
        public int deltaVelocity1 = 0;
        /// <summary>
        /// グループ2のVelocityの増減量
        /// </summary>
        public int deltaVelocity2 = 0;
        /// <summary>
        /// Velocityの最大
        /// </summary>
        private const byte MaxVelocity = 127;
        /// <summary>
        /// MyMidiOutPort のインスタンス
        /// </summary>
        /// <param name="index"></param>
        public MyMidiOutPort(MidiOutPort MidiOutPort)
        {
            Delegate = MidiOutPort;
        }
        /// <summary>
        /// MidiOutPortのIsOpen
        /// </summary>
        /// 
        public bool IsOpen
        {
            get
            {
                return Delegate.IsOpen;
            }
            set
            {
                Delegate.IsOpen = value;
            }
        }
        /// <summary>
        /// MidiOutPortのName
        /// </summary>
        public string Name
        {
            get
            {
                return Delegate.Name;
            }
        }
        /// <summary>
        /// MidiOutPortのClose()
        /// </summary>
        public void Close()
        {
            Delegate.Close();
        }
        /// <summary>
        /// MidiOutPortのOpen()
        /// </summary>
        public void Open()
        {
            Delegate.Open();
        }
        /// <summary>
        /// dataを加工し, MidiOutPortのSendを使う
        /// </summary>
        /// <param name="data"></param>
        public void Send(IMidiEvent data)
        {
            //ここでデータ加工
            if (data.RequireToSend)
            {
                modifyData(data);
            }
            Delegate.Send(data);
        }
        private void modifyData(IMidiEvent data)
        {
            if (data is NoteOnEvent)
            {
                var Note = (NoteOnEvent)data;
                /*現在いじってるチャンネルの楽器番号がどのグループ(0~2)に属するか調べ、
                 それぞれの音量を変更*/
                switch (MainWindowGroup[MainWindowValue[(int)Note.Channel]])
                {
                    case 0:
                        Note.Velocity = (int)(Note.Velocity) + deltaVelocity0 > 0 ? (byte)Math.Min(127, (int)(Note.Velocity) + deltaVelocity0) : (byte)0;
                        //Note.Velocity = 0;
                        break;
                    case 3:
                        Note.Velocity = (int)(Note.Velocity) + deltaVelocity1 > 0 ? (byte)Math.Min(127, (int)(Note.Velocity) + deltaVelocity1) : (byte)0;
                        //Note.Velocity = 0;
                        break;
                    case 2:
                        Note.Velocity = (int)(Note.Velocity) + deltaVelocity2 > 0 ? (byte)Math.Min(127, (int)(Note.Velocity) + deltaVelocity2) : (byte)0;
                        //Note.Velocity = 0;
                        break;
                    default:
                        Note.Velocity = (int)(Note.Velocity) + deltaVelocity > 0 ? (byte)Math.Min(127, (int)(Note.Velocity) + deltaVelocity) : (byte)0;
                        //Note.Velocity = 0;
                        break;
                }
            }
        }
    }
}