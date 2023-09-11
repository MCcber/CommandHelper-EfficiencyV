using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using cbhk_environment.GeneralTools.Time;
using cbhk_environment.Generators.DataPackGenerator.DatapackInitializationForms;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace cbhk_environment.Generators.DataPackGenerator.Components.HomePage
{
    /// <summary>
    /// RecentTreeItem.xaml 的交互逻辑
    /// </summary>
    public partial class RecentTreeItem : UserControl
    {
        public bool MouseEnterPinZone = false;
        public RecentTreeItem()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// 进入固定区域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Border_MouseEnter(object sender,MouseEventArgs e)
        {
            MouseEnterPinZone = true;
        }

        /// <summary>
        /// 离开固定区域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseEnterPinZone = false;
        }

        public void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            pinBox.Visibility = Visibility.Visible;
        }

        public void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            pinBox.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 切换锚定状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            HomePageDataContext context = border.FindParent<DatapackInitializationForms.HomePage>().DataContext as HomePageDataContext;
            RotateTransform rotateTransform = pinBox.RenderTransform as RotateTransform;
            //锚定
            if (rotateTransform.Angle == 90)
            {
                rotateTransform.Angle = 0;
                //保存当前内容节点
                RichTreeViewItems currentContentItem = border.FindParent<RichTreeViewItems>();
                //更新动态路径
                currentContentItem.Uid = context.StableRecentSolutionsFolderPath + "\\" + System.IO.Path.GetFileName(currentContentItem.Tag.ToString());
                //保存当前时间节点
                TreeViewItem currentDateItem = currentContentItem.Parent as TreeViewItem;
                currentDateItem.Items.Remove(currentContentItem);
                //如果没有子级则折叠
                if (currentDateItem.Items.Count == 0)
                    currentDateItem.Visibility = Visibility.Collapsed;
                context.RecentContentDateItemList[0].Items.Add(currentContentItem);
                context.RecentContentDateItemList[0].Visibility = Visibility.Visible;
            }
            else//取消锚定
            {
                rotateTransform.Angle = 90;
                //保存当前内容节点
                RichTreeViewItems currentContentItem = border.FindParent<RichTreeViewItems>();
                //更新动态路径
                currentContentItem.Uid = context.RecentSolutionsFolderPath + "\\" + System.IO.Path.GetFileName(currentContentItem.Tag.ToString());
                context.RecentContentDateItemList[0].Items.Remove(currentContentItem);
                //如果固定节点没有子级则折叠
                if (context.RecentContentDateItemList[0].Items.Count == 0)
                    context.RecentContentDateItemList[0].Visibility = Visibility.Collapsed;
                //获取指定文件的最后编辑时间
                DateTime currentTime = File.GetLastWriteTime(Path.Text);
                string timeMarker = TimeDifferenceCalculater.Calculate(currentTime);
                foreach (var item in context.RecentContentDateItemList)
                {
                    if(item.Header.ToString() == timeMarker)
                    {
                        item.Visibility = Visibility.Visible;
                        item.Items.Add(currentContentItem);
                    }
                }
            }
        }
    }
}
