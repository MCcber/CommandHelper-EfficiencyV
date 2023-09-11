using cbhk_environment.CustomControls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.EntityGenerator.Components
{
    /// <summary>
    /// VibrationMonitors.xaml 的交互逻辑
    /// </summary>
    public partial class VibrationMonitors : UserControl
    {
        #region 振动监听器类型数据源
        public ObservableCollection<string> VibrationMonitorType { get; set; } = new() { "block","entity" };
        #endregion

        #region 目标实体类型切换数据源
        public ObservableCollection<string> TargetEntitySwitcher { get; set; } = new() { "ID", "UUID", "Data" };
        #endregion

        public VibrationMonitors()
        {
            InitializeComponent();

            #region 处理控件的绑定
            VibrationMonitorsSourceType.ItemsSource = TargetEntitySwitcher;
            VibrationMonitorsSourceType.SelectedIndex = 0;
            VibrationMonitorsSourceType.SelectionChanged += VibrationMonitorsSourceType_SelectionChanged;

            VibrationMonitorTypeBox.ItemsSource = VibrationMonitorType;
            VibrationMonitorTypeBox.SelectedIndex = 0;
            VibrationMonitorTypeBox.SelectionChanged += VibrationMonitorTypeBox_SelectionChanged;
            EntityGroup0.Visibility = EntityGroup1.Visibility = Visibility.Collapsed;
            source_entityUUID.Visibility = sourceEntityDisplayer.Visibility = source_entityGenerator.Visibility = source_entityReference.Visibility = Visibility.Collapsed;
            #endregion
        }

        /// <summary>
        /// 确定指定控件的取值范围
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_Loaded(object sender, RoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            double minValue = 0;
            double maxValue = 0;
            switch (slider.Uid)
            {
                case "d":
                    minValue = double.MinValue;
                    maxValue = double.MaxValue;
                    break;
                case "f":
                    minValue = float.MinValue;
                    maxValue = float.MaxValue;
                    break;
                case "":
                    minValue = int.MinValue;
                    maxValue = int.MaxValue;
                    break;
            }
            slider.Minimum = minValue;
            slider.Maximum = maxValue;
        }

        /// <summary>
        /// 振动监听器类型切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VibrationMonitorTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (VibrationMonitorTypeBox.SelectedItem.ToString())
            {
                case "block":
                    BlockGroup.Visibility = Visibility.Visible;
                    EntityGroup0.Visibility = EntityGroup1.Visibility = Visibility.Collapsed;
                    break;
                case "entity":
                    BlockGroup.Visibility = Visibility.Collapsed;
                    EntityGroup0.Visibility = EntityGroup1.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// 振动监听器位置数据类型切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VibrationMonitorsSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedItem = TargetEntitySwitcher[VibrationMonitorsSourceType.SelectedIndex];
            switch (selectedItem)
            {
                case "ID":
                    source_entityValue.Visibility = Visibility.Visible;
                    sourceEntityDisplayer.Visibility = source_entityUUID.Visibility = source_entityReference.Visibility = source_entityGenerator.Visibility = Visibility.Collapsed;
                    break;
                case "UUID":
                    source_entityUUID.Visibility = Visibility.Visible;
                    sourceEntityDisplayer.Visibility = source_entityValue.Visibility = source_entityReference.Visibility = source_entityGenerator.Visibility = Visibility.Collapsed;
                    break;
                case "Data":
                    sourceEntityDisplayer.Visibility = source_entityReference.Visibility = source_entityGenerator.Visibility = Visibility.Visible;
                    source_entityValue.Visibility = source_entityUUID.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// 合并事件数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void event_LostFocus(object sender, RoutedEventArgs e)
        {
            TextTabItems textTabItems = sender as TextTabItems;
            List<string> results = new() { "", "", "", "", "" };
            if (game_event.Text.Length > 0)
                results[0] = "game_event:\"" + game_event.Text + "\"";
            else
                results[0] = "";
            if (distance.Value > 0)
                results[1] = "distance:" + distance.Value + "f";
            else
                results[1] = "";
            if (VibrationSourcePos.EnableButton.IsChecked.Value)
                results[2] = "pos:[" + VibrationSourcePos.number0.Value + "d," + VibrationSourcePos.number1.Value + "d," + VibrationSourcePos.number2.Value + "d]";
            else
                results[2] = "";
            if (TargetUUID.EnableButton.IsChecked.Value)
                results[3] = "source:[I;" + TargetUUID.number0.Value + "," + TargetUUID.number1.Value + "," + TargetUUID.number2.Value + "," + TargetUUID.number3.Value + "]";
            else
                results[3] = "";
            if (ProjectileUUID.EnableButton.IsChecked.Value)
                results[4] = "projectile_owner:[I;" + ProjectileUUID.number0.Value + "," + ProjectileUUID.number1.Value + "," + ProjectileUUID.number2.Value + "," + ProjectileUUID.number3.Value + "]";
            else
                results[4] = "";
            string result = string.Join(",", results).Trim(',');
            textTabItems.Tag = result.Length > 0 ?"event:{" + result + "}":"";
        }

        /// <summary>
        /// 合并振动选择器的数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void selector_LostFocus(object sender, RoutedEventArgs e)
        {
            TextTabItems textTabItems = sender as TextTabItems;
            List<string> results = new() { "", "", "", "", "" };
            if (game_eventC.Text.Length > 0)
                results[0] = "game_event:\"" + game_eventC.Text + "\"";
            else
                results[0] = "";
            if (distanceC.Value > 0)
                results[1] = "distance:" + distanceC.Value + "f";
            else
                results[1] = "";
            if (VibrationSourcePosC.EnableButton.IsChecked.Value)
                results[2] = "pos:[" + VibrationSourcePosC.number0.Value + "d," + VibrationSourcePosC.number1.Value + "d," + VibrationSourcePosC.number2.Value + "d]";
            else
                results[2] = "";
            if (TargetUUIDC.EnableButton.IsChecked.Value)
                results[3] = "source:[I;" + TargetUUIDC.number0.Value + "," + TargetUUIDC.number1.Value + "," + TargetUUIDC.number2.Value + "," + TargetUUIDC.number3.Value + "]";
            else
                results[3] = "";
            if (ProjectileUUIDC.EnableButton.IsChecked.Value)
                results[4] = "projectile_owner:[I;" + ProjectileUUIDC.number0.Value + "," + ProjectileUUIDC.number1.Value + "," + ProjectileUUIDC.number2.Value + "," + ProjectileUUIDC.number3.Value + "]";
            else
                results[4] = "";
            string result = string.Join(",", results).TrimEnd(',');
            textTabItems.Tag = "selector:{tick:" + tick.Value + (result.Length > 0?",event:{" + result + "}":"") + "}";
        }

        /// <summary>
        /// 合并振动监听器的位置数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void source_LostFocus(object sender, RoutedEventArgs e)
        {
            TextTabItems textTabItems = sender as TextTabItems;
            List<string> results = new() { "", "" };

            #region 振动监听器类型
            if (VibrationMonitorTypeBox.SelectedItem.ToString().Length > 0)
                results[0] = "type:\"" + game_eventC.Text + "\"";
            else
                results[0] = "";
            #endregion

            #region 振动监听器位置或目标实体
            if (BlockGroup.Visibility == Visibility.Visible)
                results[1] = "pos:[" + pos.number0.Value + "d," + pos.number1.Value + "d," + pos.number2.Value + "d]";
            else
            if (EntityGroup0.Visibility == Visibility.Visible && EntityGroup1.Visibility == Visibility.Visible)
            {
                string yOffset = offsetEnableButton.IsChecked.Value ? ",y_offset:" + y_offset.Value + "f" : "";
                if (VibrationMonitorsSourceType.SelectedItem.ToString() == "ID")
                    results[1] = "source_entity:" + source_entityValue.Value + yOffset;
                else
                    if (VibrationMonitorsSourceType.SelectedItem.ToString() == "UUID" && source_entityUUID.EnableButton.IsChecked.Value)
                    results[1] = "source_entity:[I;" + TargetUUIDC.number0.Value + "," + TargetUUIDC.number1.Value + "," + TargetUUIDC.number2.Value + "," + TargetUUIDC.number3.Value + "]" + yOffset;
                else
                    if (VibrationMonitorsSourceType.SelectedItem.ToString() == "Data" && sourceEntityDisplayer.Tag != null)
                    results[1] = "source_entity:" + sourceEntityDisplayer.Tag.ToString() + yOffset;
                else
                    results[1] = "";
            }
            #endregion

            string result = string.Join(",", results).TrimEnd(',');
            textTabItems.Tag = "source:{" + result + "}";
        }
    }
}
