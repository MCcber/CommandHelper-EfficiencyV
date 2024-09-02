using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.EntityGenerator;
using cbhk.Generators.EntityGenerator.Components;
using cbhk.View.Generators;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            {
                ImageBrush imageBrush = new(new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\pig_spawn_egg.png")));
                EntityIcon.Background = imageBrush;
            }
        }

        /// <summary>
        /// 设置为空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetEmpty_Click(object sender, RoutedEventArgs e)
        {
            Tag = "";
            EntityIcon.Background = null;
        }

        /// <summary>
        /// 从剪切板导入实体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            SpawnerView spawner = Window.GetWindow(button) as SpawnerView;
            string data = ExternalDataImportManager.GetEntityDataHandler(Clipboard.GetText(),false);
            Tag = data;

            try
            {
                string id = JObject.Parse(data)["id"].ToString().Replace("minecraft:", "").Replace("\"","");
                string iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + ".png";
                ImageBrush imageBrush = null;
                if (!File.Exists(iconPath))
                    iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + "_spawn_egg.png";
                if (File.Exists(iconPath))
                    imageBrush = new(new BitmapImage(new Uri(iconPath, UriKind.Absolute)));
                EntityIcon.Background = imageBrush;
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
                        ImageBrush imageBrush = null;
                        if (!File.Exists(iconPath))
                            iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + "_spawn_egg.png";
                        if (File.Exists(iconPath))
                            imageBrush = new(new BitmapImage(new Uri(iconPath, UriKind.Absolute)));
                            EntityIcon.Background = imageBrush;
                        EntityIcon.Background = imageBrush;
                    }
                    catch
                    {
                        Button button = sender as Button;
                        SpawnerView spawner = Window.GetWindow(button) as SpawnerView;
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
            EntityView entity = new();
            EntityViewModel context = entity.DataContext as EntityViewModel;
            context.IsCloseable = false;
            if (entity.ShowDialog().Value)
            {
                EntityPagesViewModel entityPagesDataContext = (context.EntityPageList[0].Content as EntityPagesView).DataContext as EntityPagesViewModel;
                string data = ExternalDataImportManager.GetEntityDataHandler(entityPagesDataContext.Result, false);
                Tag = entityPagesDataContext.Result;
                string entityID = "";
                JObject json = JObject.Parse(data);
                if (json.SelectToken("id") is JToken entityIDToken)
                    entityID = entityIDToken.ToString().Replace("minecraft:", "").Replace("_spawn_egg:", "");
                if (entityID.Length == 0)
                {
                    entityIDToken = json.SelectToken("ItemView.id");
                    if (entityIDToken is not null)
                        entityID = entityIDToken.ToString().Replace("minecraft:", "").Replace("_spawn_egg:", "");
                }
                if (entityID.Length > 0)
                {
                    string iconPath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + entityID + ".png";
                    ImageBrush imageBrush = null;
                    if (!File.Exists(iconPath))
                        iconPath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + entityID + "_spawn_egg.png";
                    if (File.Exists(iconPath))
                        imageBrush = new(new BitmapImage(new Uri(iconPath, UriKind.Absolute)));
                    EntityIcon.Background = imageBrush;
                }
            }
        }
    }
}