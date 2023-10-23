using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.EntityGenerator;
using cbhk.Generators.EntityGenerator.Components;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.SpawnerGenerator.Components
{
    /// <summary>
    /// ReferenceEntity.xaml 的交互逻辑
    /// </summary>
    public partial class ReferenceEntity : UserControl
    {
        public ReferenceEntity()
        {
            InitializeComponent();
            Tag = "{id:\"minecraft:pig\"}";
            if(File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\pig_spawn_egg.png"))
            EntityIcon.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\pig_spawn_egg.png"));
        }

        /// <summary>
        /// 设置为空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetEmpty_Click(object sender, RoutedEventArgs e)
        {
            Tag = "";
            EntityIcon.Source = null;
        }

        /// <summary>
        /// 从剪切板导入实体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Spawner spawner = Window.GetWindow(button) as Spawner;
            string data = ExternalDataImportManager.GetEntityDataHandler(Clipboard.GetText(),false);
            Tag = data;

            try
            {
                string id = JObject.Parse(data)["id"].ToString().Replace("minecraft:", "");
                string iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + ".png";
                if (!File.Exists(iconPath))
                    iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + "_spawn_egg.png";
                if (File.Exists(iconPath))
                    EntityIcon.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                else
                    EntityIcon.Source = null;
            }
            catch
            {
                Message.PushMessage("导入失败！剪切板内容与实体无关或内容有误");
            }
        }

        /// <summary>
        /// 从文件导入实体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportFromFile_Click(object sender,RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                RestoreDirectory = true,
                Title = "请选择一个实体文件"
            };
            if (openFileDialog.ShowDialog().Value)
            {
                string data = ExternalDataImportManager.GetEntityDataHandler(openFileDialog.FileName);
                Tag = data;
                if (File.Exists(openFileDialog.FileName))
                {
                    try
                    {
                        string id = JObject.Parse(data)["id"].ToString().Replace("minecraft:", "");
                        string iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + ".png";
                        if (!File.Exists(iconPath))
                            iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + "_spawn_egg.png";
                        if (File.Exists(iconPath))
                            EntityIcon.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                        else
                            EntityIcon.Source = null;
                    }
                    catch
                    {
                        Button button = sender as Button;
                        Spawner spawner = Window.GetWindow(button) as Spawner;
                        Message.PushMessage("导入失败！剪切板内容与实体无关或内容有误");
                    }
                }
            }
        }

        /// <summary>
        /// 生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Spawn_Click(object sender, RoutedEventArgs e)
        {
            Entity entity = new();
            EntityDataContext context = entity.DataContext as EntityDataContext;
            EntityPagesDataContext pageContext = (context.EntityPageList[0].Content as EntityPages).DataContext as EntityPagesDataContext;
            pageContext.UseForTool = true;
            if (entity.ShowDialog().Value)
            {
                string data = ExternalDataImportManager.GetEntityDataHandler(pageContext.Result, false);
                Tag = pageContext.Result;
                string id = JObject.Parse(data)["id"].ToString().Replace("minecraft:", "");
                string iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + ".png";
                if (!File.Exists(iconPath))
                    iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + "_spawn_egg.png";
                if (File.Exists(iconPath))
                    EntityIcon.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                else
                    EntityIcon.Source = null;
            }
        }
    }
}
