using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk_environment.Generators.EntityGenerator.Components
{
    /// <summary>
    /// PassengerItems.xaml 的交互逻辑
    /// </summary>
    public partial class PassengerItems : UserControl
    {
        public PassengerItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 删除当前乘客
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButtons_Click(object sender, RoutedEventArgs e)
        {
            IconTextButtons iconTextButtons = sender as IconTextButtons;
            StackPanel stackPanel = iconTextButtons.FindParent<StackPanel>();
            stackPanel.Children.Remove(this);
            Accordion accordion = (stackPanel.Parent as ScrollViewer).Parent as Accordion;
            accordion.FindChild<IconButtons>().Focus();
        }

        /// <summary>
        /// 引用存档
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReferenceSaveClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                RestoreDirectory = true,
                Title = "请选择一个实体文件"
            };
            if(openFileDialog.ShowDialog().Value)
            {
                if(File.Exists(openFileDialog.FileName))
                {
                    string entityData = ExternalDataImportManager.GetEntityDataHandler(openFileDialog.FileName);
                    JToken entityTag = JObject.Parse(entityData)["EntityTag"];
                    if (entityTag != null)
                        entityData = entityTag.ToString();
                    else
                        entityTag = JObject.Parse(entityData);
                    JToken entityID = entityTag["id"];
                    DisplayEntity.Tag = entityData;
                    if(entityID != null)
                    (DisplayEntity.Child as Image).Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\entityImages\\" + entityID + ".png", UriKind.RelativeOrAbsolute));
                }
            }
        }

        /// <summary>
        /// 从剪切板导入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportFromClipboardClick(object sender, RoutedEventArgs e)
        {
            string entityData = ExternalDataImportManager.GetEntityDataHandler(Clipboard.GetText(),false);
            entityData = entityData[entityData.IndexOf('{')..entityData.LastIndexOf('}')];
            //补齐缺失双引号对的key
            entityData = Regex.Replace(entityData, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
            //清除数值型数据的单位
            entityData = Regex.Replace(entityData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
            if (entityData.Trim().Length == 0) return;
            JToken entityTag = JObject.Parse(entityData)["EntityTag"];
            if (entityTag != null)
                entityData = entityTag.ToString();
            else
                entityTag = JObject.Parse(entityData);
            JToken entityID = entityTag["id"];
            DisplayEntity.Tag = entityData;
            if(entityID != null)
            (DisplayEntity.Child as Image).Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\entityImages\\" + entityID + ".png", UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// 更新引用的实体索引
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReferenceIndex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            entity_datacontext entityContext = (Window.GetWindow(slider) as Entity).DataContext as entity_datacontext;
            RichTabItems richTabItems = this.FindParent<RichTabItems>();
            int currentIndex = entityContext.EntityPageList.IndexOf(richTabItems);
            int index = int.Parse(slider.Value.ToString());
            if (index >= 0 && index < entityContext.EntityPageList.Count)
            {
                entityPagesDataContext pageContext = (entityContext.EntityPageList[index].Content as EntityPages).DataContext as entityPagesDataContext;
                if (ReferenceMode.IsChecked.Value)
                {
                    pageContext.UseForReference = true;
                    if (slider.Value < entityContext.EntityPageList.Count && currentIndex != index)
                    {
                        DisplayEntity.Tag = pageContext.run_command(false);
                        (DisplayEntity.Child as Image).Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\entityImages\\" + pageContext.SelectedEntityIdString + "_spawn_egg.png", UriKind.RelativeOrAbsolute));
                    }
                    else
                    {
                        DisplayEntity.Tag = "";
                        (DisplayEntity.Child as Image).Source = new BitmapImage();
                    }
                }
                else
                {
                    DisplayEntity.Tag = "";
                    (DisplayEntity.Child as Image).Source = new BitmapImage();
                    pageContext.UseForReference = false;
                }
            }
        }

        /// <summary>
        /// 已开启引用模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReferenceMode_Checked(object sender, RoutedEventArgs e)
        {
            ReferenceIndex_ValueChanged(ReferenceIndex,null);
        }
    }
}
