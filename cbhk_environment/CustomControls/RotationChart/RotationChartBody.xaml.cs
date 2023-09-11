using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace cbhk_environment.CustomControls.RotationChart
{
    /// <summary>
    /// RotationChartBody.xaml 的交互逻辑
    /// </summary>
    public partial class RotationChartBody : UserControl
    {
        public DispatcherTimer SwitchTimer = new()
        {
            Interval = new TimeSpan(0, 0, 0, 0, 1000),
            IsEnabled = false
        };

        public DispatcherTimer StopTimer = new()
        {
            Interval = new TimeSpan(0, 0, 0, 0, 1000),
            IsEnabled = false
        };

        DoubleAnimation doubleAnimation = new()
        {
            From = 0,
            Duration = TimeSpan.FromSeconds(1),
            RepeatBehavior = new RepeatBehavior(1),
            FillBehavior = FillBehavior.Stop
        };

        Storyboard storyboard = new();
        SolidColorBrush redBrush = new((Color)ColorConverter.ConvertFromString("#FF0000"));
        SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));

        private int selectedIndex = 0;
        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                selectedIndex = value;
                LastIndex = selectedIndex;
            }
        }
        int LastIndex = 0;

        private object[] propertyChain = new object[] { RenderTransformProperty,
            TranslateTransform.XProperty };

        public RotationChartBody()
        {
            InitializeComponent();

            SwitchTimer.Tick += SwitchTimer_Tick;
            StopTimer.Tick += StopTimer_Tick;
            storyboard.Completed += MoveCompleted;
        }

        /// <summary>
        /// 点击切换按钮后停滞指定时间再启动切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopTimer_Tick(object sender, EventArgs e)
        {
            StopTimer.IsEnabled = false;
            SwitchTimer.IsEnabled = true;
        }

        /// <summary>
        /// 执行切换动画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchTimer_Tick(object sender, EventArgs e)
        {
            if (SwitchButtonPanel.Children.Count > 1)
            {
                int NextIndex = SelectedIndex + 1 >= DisplayZone.Children.Count ? 0 : SelectedIndex + 1;
                Image image = DisplayZone.Children[SelectedIndex] as Image;
                Image nextImage = DisplayZone.Children[NextIndex] as Image;
                image.RenderTransform = new TranslateTransform();

                Panel.SetZIndex(image, 1);
                Panel.SetZIndex(nextImage, 0);
                image.Visibility = Visibility.Visible;
                nextImage.Visibility = Visibility.Visible;

                double byValue = DisplayZone.ActualWidth * (-1);
                doubleAnimation.By = byValue;
                Storyboard.SetTarget(doubleAnimation, image);
                Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(0).(1)", propertyChain));
                storyboard.Children.Add(doubleAnimation);
                storyboard.Begin();
            }
            SwitchTimer.IsEnabled = false;
        }

        /// <summary>
        /// 动画结束后回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveCompleted(object sender, EventArgs e)
        {
            DisplayZone.Children[SelectedIndex].Visibility = Visibility.Collapsed;
            Canvas.SetLeft(DisplayZone.Children[SelectedIndex], 0);
            Panel.SetZIndex(DisplayZone.Children[SelectedIndex], 0);
            (SwitchButtonPanel.Children[SelectedIndex] as ImageWithBorder).mark.BorderBrush = whiteBrush;

            #region 控制下标范围
            SelectedIndex++;
            if (SelectedIndex >= DisplayZone.Children.Count)
                SelectedIndex = 0;
            #endregion

            (SwitchButtonPanel.Children[SelectedIndex] as ImageWithBorder).mark.BorderBrush = redBrush;

            SwitchTimer.IsEnabled = true;
        }

        /// <summary>
        /// 后置添加一个成员
        /// </summary>
        /// <param name="imageSource">图像绝对路径</param>
        /// <param name="webUrl">目标网址</param>
        public void Append(string imageSource, string webUrl,string description)
        {
            ImageWithBorder imageWithBorder = new()
            {
                Tag = webUrl
            };
            Image image = new()
            {
                Source = new BitmapImage(new Uri(imageSource, UriKind.Absolute)),
                ToolTip = description
            };
            ToolTipService.SetBetweenShowDelay(image,0);
            ToolTipService.SetInitialShowDelay(image, 0);
            image.MouseLeftButtonUp += (a, b) => {
                if (System.Text.RegularExpressions.Regex.IsMatch(webUrl, @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$"))
                    Process.Start(webUrl);
            };
            imageWithBorder.ImageContent.Source = new BitmapImage(new Uri(imageSource, UriKind.Absolute));
            imageWithBorder.MouseLeftButtonUp += ImageWithBorder_MouseLeftButtonUp;
            SwitchButtonPanel.Children.Add(imageWithBorder);
            DisplayZone.Children.Add(image);
        }

        /// <summary>
        /// 设置全部内容
        /// </summary>
        /// <param name="imageSources">图像绝对路径链表</param>
        /// <param name="webUrls">目标网址链表</param>
        public void SetAll(List<string> imageSources)
        {
            SwitchTimer.IsEnabled = false;

            #region 清空数据
            SwitchButtonPanel.Children.Clear();
            DisplayZone.Children.Clear();
            #endregion

            string linkFile = "";
            for (int i = 0; i < imageSources.Count; i++)
            {
                linkFile = Path.GetDirectoryName(imageSources[i]) + "\\" + Path.GetFileNameWithoutExtension(imageSources[i]) + ".txt";

                #region 添加切换按钮和显示成员
                ImageWithBorder imageWithBorder = new();
                Image image = new()
                {
                    Source = new BitmapImage(new Uri(imageSources[i], UriKind.Absolute)),
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand,
                    Visibility = Visibility.Collapsed
                };
                imageWithBorder.ImageContent.Source = new BitmapImage(new Uri(imageSources[i], UriKind.Absolute));
                imageWithBorder.MouseLeftButtonUp += ImageWithBorder_MouseLeftButtonUp;
                SwitchButtonPanel.Children.Add(imageWithBorder);
                DisplayZone.Children.Add(image);
                #endregion

                if (File.Exists(linkFile))
                {
                    image.Tag = File.ReadAllText(linkFile);
                    var psi = new ProcessStartInfo
                    {
                        FileName = image.Tag.ToString(),
                        UseShellExecute = true
                    };
                    image.MouseLeftButtonUp += (a, b) => { Process.Start(psi); };
                }
            }
            Panel.SetZIndex(DisplayZone.Children[0], 1);
            DisplayZone.Children[0].Visibility = Visibility.Visible;
            (SwitchButtonPanel.Children[SelectedIndex] as ImageWithBorder).mark.BorderBrush = redBrush;
            SwitchTimer.IsEnabled = true;
        }

        /// <summary>
        /// 更新选中成员
        /// </summary>
        private void UpdateSelectedItem()
        {
            for (int i = 0; i < DisplayZone.Children.Count; i++)
            {
                if (i != SelectedIndex)
                {
                    DisplayZone.Children[i].Visibility = Visibility.Collapsed;
                    Canvas.SetLeft(DisplayZone.Children[i], 0);
                }
            }
            if (SelectedIndex >= DisplayZone.Children.Count)
                SelectedIndex = 0;
            Panel.SetZIndex(DisplayZone.Children[SelectedIndex], 1);
            DisplayZone.Children[SelectedIndex].Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 点击下方切换按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageWithBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SwitchTimer.IsEnabled = false;
            StopTimer.IsEnabled = true;
            int LastInex = SelectedIndex;
            SelectedIndex = SwitchButtonPanel.Children.IndexOf(sender as ImageWithBorder);
            (SwitchButtonPanel.Children[SelectedIndex] as ImageWithBorder).mark.BorderBrush = redBrush;
            (SwitchButtonPanel.Children[LastInex] as ImageWithBorder).mark.BorderBrush = whiteBrush;
            UpdateSelectedItem();
        }

        /// <summary>
        /// 向左移动一个单位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Turnleft_Click(object sender, RoutedEventArgs e)
        {
            SwitchTimer.IsEnabled = false;
            (SwitchButtonPanel.Children[SelectedIndex] as ImageWithBorder).mark.BorderBrush = whiteBrush;
            #region 控制下标
            SelectedIndex--;
            if (SelectedIndex < 0)
                SelectedIndex = SwitchButtonPanel.Children.Count - 1;
            #endregion
            (SwitchButtonPanel.Children[SelectedIndex] as ImageWithBorder).mark.BorderBrush = redBrush;
            UpdateSelectedItem();
            StopTimer.IsEnabled = true;
        }

        /// <summary>
        /// 向右移动一个单位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnRight_Click(object sender, RoutedEventArgs e)
        {
            SwitchTimer.IsEnabled = false;
            (SwitchButtonPanel.Children[SelectedIndex] as ImageWithBorder).mark.BorderBrush = whiteBrush;
            #region 控制下标
            SelectedIndex++;
            if (SelectedIndex >= SwitchButtonPanel.Children.Count)
                SelectedIndex = 0;
            #endregion
            (SwitchButtonPanel.Children[SelectedIndex] as ImageWithBorder).mark.BorderBrush = redBrush;
            UpdateSelectedItem();
            StopTimer.IsEnabled = true;
        }
    }
}
