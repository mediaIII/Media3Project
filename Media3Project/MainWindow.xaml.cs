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
        const double MinAngle=-25;
        /// <summary>
        /// テンポ棒の左端
        /// </summary>
        const double MaxAngle=25;
        /// <summary>
        /// tickの更新用
        /// </summary>
        double tickUpdated = 100000;

        public MainWindow()
        {
            InitializeComponent();
            InitialzeFigure();

            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ここからキネクトを記述
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
            TempoChange(1000, MinAngle, MaxAngle);
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
        /// テンポのアニメーション(1往復)
        /// </summary>
        /// <param name="milliTime"></param>
        /// <param name="StartAngle"></param>
        /// <param name="FinishAngle"></param>
        public void TempoChange(double milliTime, double StartAngle, double FinishAngle)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation { From = StartAngle, To = FinishAngle, Duration = new Duration(TimeSpan.FromMilliseconds(milliTime)) };
            animation.RepeatBehavior = new RepeatBehavior(1);
            animation.AutoReverse = true;
            storyboard.Completed += storyboard_Completed;
            Storyboard.SetTarget(animation, Tempo);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Rectangle.RenderTransform).(RotateTransform.Angle)"));
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        void storyboard_Completed(object sender, EventArgs e)
        {
            TempoChange(tickUpdated, MinAngle, MaxAngle);
        }

        private void TempoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tickUpdated = TempoSlider.Value;
        }
    }
}