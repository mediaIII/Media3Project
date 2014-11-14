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

namespace Media3Project
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitialzeFigure();
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
            VolumeChange(Volume.Height, TestSlider.Value * 12);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Volume.Height = 100;
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