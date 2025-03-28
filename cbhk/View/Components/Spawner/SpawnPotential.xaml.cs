using CBHK.CustomControls;
using CBHK.GeneralTools;
using CBHK.View.Compoments.Spawner;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.Generators.SpawnerGenerator.Components
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
                        BlockLight = "block_light_limit:" + int.Parse(BlockLightValue.Value.ToString()) + ",";
                    else
                    {
                        BlockLight = "block_light_limit:[" + int.Parse(BlockLightMinValue.Value.ToString()) + "," + int.Parse(BlockLightMaxValue.Value.ToString()) + "],";
                    }
                }
                #endregion
                #region SkyLight
                string SkyLight = "";
                if(!UseDefaultSkyLightValue.IsChecked.Value)
                {
                    if (SkyLightValue.Visibility == Visibility.Visible)
                        SkyLight = "sky_light_limit:" + int.Parse(SkyLightValue.Value.ToString()) + ",";
                    else
                    {
                        SkyLight = "sky_light_limit:[" + int.Parse(SkyLightMinValue.Value.ToString()) + "," + int.Parse(SkyLightMaxValue.Value.ToString()) + "],";
                    }
                }
                #endregion

                string custom_spawn_rules = (BlockLight.Length + SkyLight.Length) > 0 ? "custom_spawn_rules:{" + (BlockLight + SkyLight).Trim(',')+ "}," : "";
                string entityData = entity.Tag != null ?"entity:"+entity.Tag.ToString():"";
                string data = "data:{"+ (custom_spawn_rules + entityData).Trim(',') + "},";
                result = "{weight:" + (int.Parse(weight.Value.ToString()) + "," + data).TrimEnd(',') + "}";
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
            RadiusToggleButtons radiusToggleButtons = sender as RadiusToggleButtons;
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
            RadiusToggleButtons radiusToggleButtons = sender as RadiusToggleButtons;
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
            IconTextButtons iconTextButtons = sender as IconTextButtons;
            SpawnerPageViewModel context = iconTextButtons.FindParent<SpawnerPageView>().DataContext as SpawnerPageViewModel;
            context.SpawnPotentials.Remove(this);
        }
    }
}
