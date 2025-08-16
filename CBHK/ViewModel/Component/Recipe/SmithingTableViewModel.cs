using CBHK.GeneralTool.MessageTip;
using CBHK.Model.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Recipe
{
    public partial class SmithingTableViewModel : ObservableObject
    {
        #region 字段与引用
        /// <summary>
        /// 存储最终结果
        /// </summary>
        public string Result { get; set; } = "";
        /// <summary>
        /// 需要保存
        /// </summary>
        public bool NeedSave { get; set; } = true;

        #region 存储外部导入的数据
        public bool ImportMode { get; set; } = false;
        public JObject ExternalData { get; set; } = null;
        #endregion

        /// <summary>
        /// 当前物品
        /// </summary>
        private Image CurrentItem = null;
        /// <summary>
        /// 模板物品
        /// </summary>
        public Image TemplateItem = null;
        /// <summary>
        /// 基础物品你
        /// </summary>
        public Image BaseItem = null;
        /// <summary>
        /// 修饰物品
        /// </summary>
        public Image AdditionItem = null;
        /// <summary>
        /// 结果物品
        /// </summary>
        public Image ResultItem = null;
        BitmapImage emptyImage = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Recipe\Image\Empty.png"));
        SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        SolidColorBrush grayBrush = new((Color)ColorConverter.ConvertFromString("#484848"));
        SolidColorBrush greenBrush = new((Color)ColorConverter.ConvertFromString("#00FF00"));
        SolidColorBrush transparentBrush = new((Color)ColorConverter.ConvertFromString("Transparent"));

        #region 结果数量
        private double count = 1;
        public double Count
        {
            get => count;
            set => SetProperty(ref count, value);
        }
        #endregion
        #region 当前Tag
        private string currentTag = "";
        public string CurrentTag
        {
            get => currentTag;
            set => SetProperty(ref currentTag, value);
        }
        #endregion
        #region 模板的Tag
        private string templateTag = "";
        public string TemplateTag
        {
            get => templateTag;
            set => SetProperty(ref templateTag, value);
        }
        #endregion
        #region 基础的Tag
        private string baseTag = "";
        public string BaseTag
        {
            get => baseTag;
            set => SetProperty(ref baseTag, value);
        }
        #endregion
        #region 修饰物的Tag
        private string additionTag = "";
        public string AdditionTag
        {
            get => additionTag;
            set => SetProperty(ref additionTag, value);
        }
        #endregion
        #endregion
        #region 配方文件名
        private string fileName = "";
        public string FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        #endregion

        DataTable ItemTable = null;

        [RelayCommand]
        /// <summary>
        /// 执行配方
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Run()
        {
            #region 合成最终数据
            Result = ResultItem.Tag != null && ((ResultItem.Tag as ItemStructure).ImagePath != new BitmapImage(emptyImage.UriSource)) ? "{\r\n  \"type\": \"minecraft:smithing_transform\"," : "{\r\n  \"type\": \"minecraft:smithing_trim\",";
            #region 合并模板数据
            string templateItemID;
            string templateData = "";
            if (TemplateItem.Tag is ItemStructure templateStructure)
            {
                templateItemID = templateStructure.IDAndName[..templateStructure.IDAndName.IndexOf(':')];
                templateData = "\"template\":{\"item\":\"minecraft:" + templateItemID + "\"" + (templateTag.Length > 0 ? ",\"tag\":\"" + templateTag + "\"}," : "},");
            }
            #endregion
            #region 合并基础数据
            string baseItemID;
            string baseData = "";
            if (BaseItem.Tag is ItemStructure baseStructure)
            {
                baseItemID = baseStructure.IDAndName[..baseStructure.IDAndName.IndexOf(':')];
                baseData = "\"base\":{\"item\":\"minecraft:" + baseItemID + "\"" + (BaseTag.Length > 0 ? ",\"tag\":\"" + BaseTag + "\"}," : "},");
            }
            #endregion
            #region 合并修饰数据
            string additionItemID;
            string additionData = "";
            if (AdditionItem.Tag is ItemStructure additionStructure)
            {
                additionItemID = additionStructure.IDAndName[..additionStructure.IDAndName.IndexOf(':')];
                additionData = "\"addition\":{\"item\":\"minecraft:" + additionItemID + "\"" + (AdditionTag.Length > 0 ? ",\"tag\":\"" + AdditionTag + "\"}," : "},");
            }
            #endregion
            #region 合并结果数据
            string resultItemID;
            string resultData = "";
            if (ResultItem.Tag is ItemStructure resultItemStructure)
            {
                resultItemID = resultItemStructure.IDAndName[..resultItemStructure.IDAndName.IndexOf(':')];
                resultData = "\"result\":{\"item\":\"minecraft:" + resultItemID + "\",\"count\":" + int.Parse(Count.ToString()) + "}";
            }
            #endregion
            #region 合并最终结果
            Result += (templateData + baseData + additionData + resultData).TrimEnd(',') + "}";
            #endregion
            #endregion
            #region 选择生成路径，执行生成
            if (NeedSave)
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog = new()
                {
                    AddExtension = true,
                    Filter = "Json文件|*.json;",
                    DefaultExt = ".json",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                    RestoreDirectory = true,
                    Title = "选择配方文件存储路径"
                };
                if (saveFileDialog.ShowDialog().Value)
                {
                    _ = File.WriteAllTextAsync(saveFileDialog.FileName, Result);
                    Message.PushMessage("锻造台配方生成成功！", MessageBoxImage.Information);
                    //OpenFolderThenSelectFiles.ExplorerFile(saveFileDialog.FileName);
                }
            }
            #endregion
        }

        /// <summary>
        /// 载入模板材料
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TemplateMaterial_Loaded(object sender, RoutedEventArgs e)
        {
            TemplateItem = sender as Image;
            TemplateItem.Source ??= emptyImage;
            if (ImportMode)
            {
                if (ExternalData.SelectToken("template") is JObject ingredient)
                {
                    JToken itemIDObj = ingredient.SelectToken("item");
                    JToken itemTagObj = ingredient.SelectToken("tag");
                    if (itemIDObj != null)
                    {
                        string itemID = itemIDObj.ToString().Replace("minecraft:", "");
                        Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID.ToString() + ".png");
                        string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                        TemplateItem.Source = new BitmapImage(iconUri);
                        TemplateItem.Tag = new ItemStructure()
                        {
                            ImagePath = new BitmapImage(iconUri),
                            IDAndName = itemID + ":" + itemName
                        };
                        if (itemTagObj != null)
                            TemplateTag = itemTagObj.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// 载入基础材料
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BaseMaterial_Loaded(object sender, RoutedEventArgs e)
        {
            BaseItem = sender as Image;
            BaseItem.Source ??= emptyImage;
            if (ImportMode)
            {
                if (ExternalData.SelectToken("base") is JObject ingredient)
                {
                    JToken itemIDObj = ingredient.SelectToken("item");
                    JToken itemTagObj = ingredient.SelectToken("tag");
                    if (itemIDObj != null)
                    {
                        string itemID = itemIDObj.ToString().Replace("minecraft:", "");
                        Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID.ToString() + ".png");
                        string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                        BaseItem.Source = new BitmapImage(iconUri);
                        BaseItem.Tag = new ItemStructure()
                        {
                            ImagePath = new BitmapImage(iconUri),
                            IDAndName = itemID + ":" + itemName
                        };
                        if (itemTagObj != null)
                            BaseTag = itemTagObj.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// 载入修饰材料
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AdditionMaterial_Loaded(object sender, RoutedEventArgs e)
        {
            AdditionItem = sender as Image;
            AdditionItem.Source ??= emptyImage;
            if (ImportMode)
            {
                if (ExternalData.SelectToken("addition") is JObject ingredient)
                {
                    JToken itemIDObj = ingredient.SelectToken("item");
                    JToken itemTagObj = ingredient.SelectToken("tag");
                    if (itemIDObj != null)
                    {
                        string itemID = itemIDObj.ToString().Replace("minecraft:", "");
                        Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID.ToString() + ".png");
                        string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                        AdditionItem.Source = new BitmapImage(iconUri);
                        AdditionItem.Tag = new ItemStructure()
                        {
                            ImagePath = new BitmapImage(iconUri),
                            IDAndName = itemID + ":" + itemName
                        };
                        if (itemTagObj != null)
                            AdditionTag = itemTagObj.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// 载入结果物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ResultItem_Loaded(object sender, RoutedEventArgs e)
        {
            ResultItem = sender as Image;
            if (ImportMode)
            {
                if (ExternalData.SelectToken("result") is JObject ingredient)
                {
                    JToken itemIDObj = ingredient.SelectToken("item");
                    JToken itemCountObj = ingredient.SelectToken("count");
                    if (itemIDObj != null)
                    {
                        string itemID = itemIDObj.ToString().Replace("minecraft:", "");
                        Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID.ToString() + ".png");
                        string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                        ResultItem.Source = new BitmapImage(iconUri);
                        TemplateItem.Tag = new ItemStructure()
                        {
                            ImagePath = new BitmapImage(iconUri),
                            IDAndName = itemID + ":" + itemName
                        };
                        if (itemCountObj != null)
                            Count = int.Parse(itemCountObj.ToString());
                    }
                }
            }
            else
                ResultItem.Source ??= emptyImage;
        }

        /// <summary>
        /// 处理被拖拽的物品数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GetDropData(object sender, DragEventArgs e)
        {
            Image image = e.Data.GetData(typeof(Image).ToString()) as Image;
            Image currentImage = sender as Image;
            ItemStructure itemStructure = image.Tag as ItemStructure;

            ToolTip toolTip = new()
            {
                Foreground = whiteBrush,
                Background = grayBrush,
                Content = itemStructure.IDAndName
            };
            ToolTipService.SetBetweenShowDelay(currentImage, 0);
            ToolTipService.SetInitialShowDelay(currentImage, 0);
            currentImage.Source = image.Source;
            currentImage.Tag = itemStructure;
            if (Equals(currentImage, ResultItem))
                toolTip.Content += "(右击删除)";
            currentImage.ToolTip = toolTip;
        }

        /// <summary>
        /// 左击槽位打开槽位数据页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetSlotData_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CurrentItem != null && !Equals(CurrentItem, sender))
                (CurrentItem.Parent as Border).BorderBrush = transparentBrush;
            if (!Equals(CurrentItem, sender))
            {
                if (Equals(CurrentItem, TemplateItem))
                    TemplateTag = CurrentTag;
                else
                if (Equals(CurrentItem, BaseItem))
                    BaseTag = CurrentTag;
                else
                if (Equals(CurrentItem, AdditionItem))
                    AdditionTag = CurrentTag;
            }

            if (Equals(sender, TemplateItem))
            {
                CurrentTag = TemplateTag;
                CurrentItem = TemplateItem;
            }
            else
            if (Equals(sender, BaseItem))
            {
                CurrentTag = BaseTag;
                CurrentItem = BaseItem;
            }
            else
            if (Equals(sender, AdditionItem))
            {
                CurrentTag = AdditionTag;
                CurrentItem = AdditionItem;
            }

            (CurrentItem.Parent as Border).BorderBrush = greenBrush;
        }

        /// <summary>
        /// 删除结果数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DeleteResult_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ResultItem.Tag = null;
            ResultItem.Source = emptyImage;
        }
    }
}
