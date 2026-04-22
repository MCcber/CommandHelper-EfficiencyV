using CBHK.CustomControl;
using CBHK.CustomControl.VectorButton;
using CBHK.Utility.Common;
using CBHK.ViewModel.Component.Spawner;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.View.Component.Spawner
{
    /// <summary>
    /// SpawnPotential.xaml 的交互逻辑
    /// </summary>
    public partial class SpawnPotential : UserControl
    {
        #region 合并数据
        public string Result
        {
            get
            {
                string result;

                #region BlockLight
                string BlockLight = "";
                if(!UseDefaultBlockLightValue.IsChecked.Value)
                {
                    if (BlockLightValue.Visibility == Visibility.Visible)
                        BlockLight = "block_light_limit:" + int.Parse(BlockLightValue.Text.ToString()) + ",";
                    else
                    {
                        BlockLight = "block_light_limit:[" + int.Parse(BlockLightMinValue.Text.ToString()) + "," + int.Parse(BlockLightMaxValue.Text.ToString()) + "],";
                    }
                }
                #endregion

                #region SkyLight
                string SkyLight = "";
                if(!UseDefaultSkyLightValue.IsChecked.Value)
                {
                    if (SkyLightValue.Visibility == Visibility.Visible)
                        SkyLight = "sky_light_limit:" + int.Parse(SkyLightValue.Text.ToString()) + ",";
                    else
                    {
                        SkyLight = "sky_light_limit:[" + int.Parse(SkyLightMinValue.Text.ToString()) + "," + int.Parse(SkyLightMaxValue.Text.ToString()) + "],";
                    }
                }
                #endregion

                string custom_spawn_rules = (BlockLight.Length + SkyLight.Length) > 0 ? "custom_spawn_rules:{" + (BlockLight + SkyLight).Trim(',')+ "}," : "";
                string entityData = entity.Tag is not null ?"entity:"+entity.Tag.ToString():"";
                string data = "data:{"+ (custom_spawn_rules + entityData).Trim(',') + "},";
                result = "{weight:" + (int.Parse(weight.Text.ToString()) + "," + data).TrimEnd(',') + "}";
                return result;
            }
        }
        #endregion

        public SpawnPotential()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 切换方块光照限制数据类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlockLight_Click(object sender, RoutedEventArgs e)
        {
            VectorToggleTextButton radiusToggleButtons = sender as VectorToggleTextButton;
            if(radiusToggleButtons.IsChecked.Value)
            {
                BlockLightValue.Visibility = Visibility.Visible;
                BlockLightRange.Visibility = Visibility.Collapsed;
            }
            else
            {
                BlockLightValue.Visibility = Visibility.Collapsed;
                BlockLightRange.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 切换天空光照限制数据类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkyLight_Click(object sender, RoutedEventArgs e)
        {
            VectorToggleTextButton radiusToggleButtons = sender as VectorToggleTextButton;
            if (radiusToggleButtons.IsChecked.Value)
            {
                SkyLightValue.Visibility = Visibility.Visible;
                SkyLightRange.Visibility = Visibility.Collapsed;
            }
            else
            {
                SkyLightValue.Visibility = Visibility.Collapsed;
                SkyLightRange.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 删除此潜在实体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IconTextButtons_Click(object sender, RoutedEventArgs e)
        {
            Button iconTextButtons = sender as Button;
            SpawnerPageViewModel context = iconTextButtons.FindParent<SpawnerPageView>().DataContext as SpawnerPageViewModel;
            context.SpawnPotentialList.Remove(this);
        }
    }
}
