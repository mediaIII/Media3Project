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
        const double MAXTEMPOCOUNT = 50;
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
        public static double tickUpdated = 1000;
        int number = 1;
        /// <summary>
        /// 右腕のy座標を入れるための配列
        /// </summary>
        float[] YofRighthand = new float[75];
        /// <summary>
        /// テンポ用配列
        /// </summary>
        float[] tempoarray = new float[100];
        float Volume_max;
        float frame;
        /// <summary>
        /// テンポを計算する用の配列
        /// </summary>
        float[] TimeBetweenPoints = new float[2];
        /// <summary>
        /// テンポを計算するためのカウンタ
        /// </summary>
        int TempoCount = 0;
        /// <summary>
        /// 頭の位置の誤差値
        /// </summary>
        double Headposition = 0;
        /// <summary>
        /// 左側検出
        /// </summary>
        int leftflag = 0;
        /// <summary>
        /// 右側検出
        /// </summary>
        int rightflag = 0;
        /// <summary>
        /// modifydata追加済みのMidiOutPort
        /// </summary>
        MyMidiOutPort MyMidiOutPort;
        /// <summary>
        /// Midiの格納先
        /// </summary>
        MidiData MidiData;
        /// <summary>
        /// Midiのデータ境界
        /// </summary>
        MidiFileDomain domain;
        /// <summary>
        /// Midiの演奏
        /// </summary>
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
        /// <summary>
        /// 右手の3フレーム分の座標を格納
        /// </summary>
        MyQueue RightHand = new MyQueue();
        /// <summary>
        /// 30フレーム間は動作しない
        /// </summary>
        int FrameDetect = 30;


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
            //Midiの参照
            
                // ポートの指定
                MyMidiOutPort = new MyMidiOutPort(new MidiOutPort(0));

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
                string path = "C:\\Users\\media3\\Downloads\\DQ.mid";
                if (!File.Exists(path))
                {
                    Console.WriteLine("File dose not exist");
                    return;
                }
                // midiファイルの読み込み
                MidiData = MidiReader.ReadFrom(path, Encoding.GetEncoding("shift-jis"));
            domain = new MidiFileDomain(MidiData);

                //曲に使われている楽器を5つのグループに分ける
                MakeGroup();
                //原曲のTickとGateを一次元配列に格納
                Store();

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


                // Playerの作成
                Player = new MidiPlayer(MyMidiOutPort);
                Player.Stopped += Player_Stopped;
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
        /// <summary>
        /// キネクトの開始
        /// </summary>
        /// <param name="kinectSensor"></param>
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
        /// スケルトンから座標の取得, 計算(15Frame/sec)
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
                    SkeletonPoint FramePoint;

                    number++;
                    //　指揮一周分(約75こ分)のy座標を配列に入れる
                    number = number % 75;

                    YofRighthand[number] = skeleton.Joints[JointType.HandRight].Position.Y;
                    // 右手のx,y座標
                    FramePoint = skeleton.Joints[JointType.HandRight].Position;

                    float tempo;

                    RightHand.Add(skeleton.Joints[JointType.HandRight].Position.X);

                    // ????
                    Volume_max = 120 * (YofRighthand.Max() - YofRighthand.Min());

                    VolumeChange((double)Volume_max);
                    Kinect_Volume_Change(leftflag, rightflag, (int)Volume_max);


                    if (skeleton.Joints[JointType.Head].Position.X < skeleton.Joints[JointType.HipCenter].Position.X - 0.1
                        && skeleton.Joints[JointType.Head].Position.Z < skeleton.Joints[JointType.HipCenter].Position.Z - 0.1)
                    {
                        // 左側検出
                        Headposition = -0.08;
                        leftflag = 1;
                        rightflag = 0;
                        DrawEllipseOnDisplay(InstrumentLeft);
                        
                    }
                    else if (skeleton.Joints[JointType.Head].Position.X > skeleton.Joints[JointType.HipCenter].Position.X + 0.1
                        && skeleton.Joints[JointType.Head].Position.Z < skeleton.Joints[JointType.HipCenter].Position.Z - 0.1)
                    {
                        // 右側検出
                        Headposition = 0.05;
                        rightflag = 1;
                        leftflag = 0;
                        DrawEllipseOnDisplay(InstrumentRight);
                    }
                    else if (skeleton.Joints[JointType.Head].Position.Z < skeleton.Joints[JointType.HipCenter].Position.Z - 0.1)
                    {
                        // 正面検出
                        Headposition = 0;
                        leftflag = 1;
                        rightflag = 1;
                        DrawEllipseOnDisplay(InstrumentCenter);
                    }
                    else
                    {
                        // 基本位置
                        Headposition = 0;
                        leftflag = 0;
                        rightflag = 0;
                        DrawEllipseOnDisplay(InstrumentStandard);
                    }

                    if (skeleton.Joints[JointType.HandRight].Position.X < (skeleton.Joints[JointType.Head].Position.X + Headposition)
                           && RightHand.Fetch(0) > RightHand.Fetch(1) && RightHand.Fetch(1) < RightHand.Fetch(2) && frame + FrameDetect < skeletonFrame.FrameNumber)
                    {
                        frame = skeletonFrame.FrameNumber;
                        
                        TimeBetweenPoints[TempoCount] = frame;
                        TempoCount++;

                        if (TempoCount > 1)
                        {
                            TempoCount = 0;
                        }

                        tempo = Math.Abs(TimeBetweenPoints[0] - TimeBetweenPoints[1]);
                        if (tempo > 40 && tempo < 100)
                        {
                            tickUpdated = 8 * tempo;
                        }
                        Kinect_tempo_Change((double)tempo);
                    }
                }
            }
        }

        private void DrawEllipseOnDisplay(Ellipse Instrument)
        {
            Canvas.Children.Clear();
            Instrument.Fill = new SolidColorBrush(Colors.Yellow);
            Canvas.Children.Add(Instrument);
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
            // ボリューム値の表示の初期化
            Volume.Height = Volume.MinHeight;
            // 回転中心の初期化
            Tempo.RenderTransformOrigin = new Point(0.5, 1.0);
            // テンポ角の初期化
            Tempo.RenderTransform = new RotateTransform(0);
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
                    }
                    //8?15, 108, 112?119番の楽器番号ならパート1
                    if ((8 <= note.Value && note.Value <= 15) || (112 <= note.Value && note.Value <= 119) || note.Value == 108)
                    {
                        group[note.Value] = 1;
                        value[(int)note.Channel] = note.Value;
                    }
                    //24?55, 104?107, 110番の楽器番号ならパート2
                    if ((24 <= note.Value && note.Value <= 55) || (104 <= note.Value && note.Value <= 107) || note.Value == 110)
                    {
                        group[note.Value] = 2;
                        value[(int)note.Channel] = note.Value;
                    }
                    //56?79, 109, 111番の楽器番号ならパート3
                    if ((56 <= note.Value && note.Value <= 79) || note.Value == 109 || note.Value == 111)
                    {
                        group[note.Value] = 3;
                        value[(int)note.Channel] = note.Value;
                    }
                    //80〜103,120?127番の楽器番号ならパート4
                    if ((80 <= note.Value && note.Value <= 103) || (120 <= note.Value && note.Value <= 127))
                    {
                        group[note.Value] = 4;
                        value[(int)note.Channel] = note.Value;
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
        /// <summary>
        /// プレイヤー停止時に発生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tempo"></param>
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
        /// <summary>
        /// プレイヤーのスタート, テンポ棒の動作開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Start_Button(object sender, RoutedEventArgs e)
        {
            // 別スレッドでの演奏開始
            Player.Play(domain);
            fromRighttoLeft(tickUpdated, MinAngle, MaxAngle);
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