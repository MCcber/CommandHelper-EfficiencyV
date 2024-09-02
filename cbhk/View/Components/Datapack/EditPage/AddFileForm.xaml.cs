using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace cbhk.View.Components.Datapack.EditPage
{
    /// <summary>
    /// AddFileForm.xaml 的交互逻辑
    /// </summary>
    public partial class AddFileForm
    {
        public AddFileForm()
        {
            InitializeComponent();
        }
    }

    public class NewItemStyle
    {
        public ImageSource Icon { get; set; }
        public string FunctionName { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public string NameSpace { get; set; }
    }

    public class NewItemGridWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
                return (double)value - 25;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public partial class AddFileFormDataContext:ObservableObject
    {
        private string FileTemplatesFolder = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\DatapackView\\data\\FileTemplates";
        private object tagItemsLock = new();
        public ObservableCollection<NewItemStyle> NewItemStyles { get; set; } = [];

        private CollectionViewSource NewItemStyleSource = null;
        public ObservableCollection<string> SortBySource { get; set; } = [];

        #region 与窗体绑定的属性
        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set
            {
                SetProperty(ref searchText, value);
                NewItemStyleSource?.View?.Refresh();
            }
        }

        private NewItemStyle selectedNewFile = null;
        public NewItemStyle SelectedNewFile
        {
            get => selectedNewFile;
            set
            {
                SetProperty(ref selectedNewFile, value);
                if (SelectedNewFile != null)
                {
                    SelectedTypeText = SelectedNewFile.TypeName;
                    SelectedDescriptionText = SelectedNewFile.Description;
                    NewFileName = SelectedNewFile.FunctionName + SelectedNewFile.TypeName;
                }
            }
        }

        public string DefaultSelectedNewFile = "";

        private TreeViewItem CurrentFileType = null;

        private ListView NewFileViewer = null;

        private string selectedTypeText = "";
        public string SelectedTypeText
        {
            get => selectedTypeText;
            set => SetProperty(ref selectedTypeText,value);
        }

        private string selectedDescriptionText = "";
        public string SelectedDescriptionText
        {
            get => selectedDescriptionText;
            set => SetProperty(ref selectedDescriptionText,value);
        }

        private string newFileName = "";
        public string NewFileName
        {
            get => newFileName;
            set => SetProperty(ref newFileName, value);
        }
        #endregion

        public AddFileFormDataContext()
        {
            #region 异步载入标签成员
            if(NewItemStyles.Count == 0)
            {
                BindingOperations.EnableCollectionSynchronization(NewItemStyles, tagItemsLock);
                Task.Run(() =>
                {
                    lock (tagItemsLock)
                    {
                        string mcfunctionIconPath = AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\item_and_block_images\\command_block.png";
                        if (Directory.Exists(FileTemplatesFolder))
                        {
                            string[] fileTemplates = Directory.GetFiles(FileTemplatesFolder, "*.json");
                            string type = "";
                            string description = "";
                            string path = "";
                            string nameSpace = "";
                            DrawingImage icon = new();
                            for (int i = 0; i < fileTemplates.Length; i++)
                            {
                                type = "";
                                description = "";
                                path = "";
                                nameSpace = "";
                                JObject fileData = JObject.Parse(File.ReadAllText(fileTemplates[i]));
                                if (fileData["type"] is JToken fileType)
                                    type = fileType.ToString();
                                if (fileData["description"] is JToken fileDescription)
                                    description = fileDescription.ToString();
                                if (fileData["path"] is JToken filePath)
                                {
                                    if (filePath.ToString().StartsWith("template:"))
                                        path = AppDomain.CurrentDomain.BaseDirectory + filePath.ToString()[(filePath.ToString().IndexOf(':') + 1)..];
                                    else
                                        if (File.Exists(filePath.ToString()))
                                        path = filePath.ToString();
                                }
                                if (fileData["nameSpace"] is JToken nameSpaceObj)
                                    nameSpace = nameSpaceObj.ToString();
                                if (Application.Current.Resources[Path.GetExtension(path)] is DrawingImage drawingImage)
                                    icon = drawingImage;
                                if (path.Length > 0)
                                    NewItemStyles.Add(new NewItemStyle() { Icon = icon, Path = path, Description = description, FunctionName = Path.GetFileNameWithoutExtension(fileTemplates[i]), TypeName = Path.GetExtension(path), NameSpace = nameSpace });
                            }
                        }
                    }
                });
            }
            #endregion

            #region 排序依据类型
            SortBySource.Add("默认");
            SortBySource.Add("名称升序");
            SortBySource.Add("名称降序");
            #endregion
        }

        /// <summary>
        /// 左侧文件类型树载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NewFileListViewer_Loaded(object sender,RoutedEventArgs e)
        {
            TreeView treeView = sender as TreeView;
            NewItemStyleSource = Window.GetWindow(treeView).FindResource("NewItemSourceCollection") as CollectionViewSource;
            NewItemStyleSource.Filter += NewItemStyleSource_Filter;
            CurrentFileType = treeView.Items[0] as TreeViewItem;
        }

        /// <summary>
        /// 新建文件视图载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NewFileViewer_Loaded(object sender,RoutedEventArgs e)
        {
            NewFileViewer = sender as ListView;

            if (DefaultSelectedNewFile.Length > 0)
            {
                NewFileViewer.Focus();
                SelectedNewFile = NewItemStyles.Where(item => Path.GetFileNameWithoutExtension(item.FunctionName) == DefaultSelectedNewFile).First();
                DefaultSelectedNewFile = "";
            }
            else
                SelectedNewFile = NewItemStyles.First();
        }

        /// <summary>
        /// 新建项选中文件类型更新事件
        /// </summary>
        public void NewFileType_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            CurrentFileType = e.NewValue as TreeViewItem;
            NewItemStyleSource?.View?.Refresh();
            NewFileViewer.SelectedIndex = 0;
        }

        /// <summary>
        /// 根据选定文件类型过滤新建项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewItemStyleSource_Filter(object sender, FilterEventArgs e)
        {
            NewItemStyle newItemStyle = e.Item as NewItemStyle;
            if(CurrentFileType != null)
            {
                if(!CurrentFileType.Uid.Contains('\\') && newItemStyle.NameSpace.Contains('\\'))
                    e.Accepted = (newItemStyle.NameSpace.StartsWith(CurrentFileType.Uid) || CurrentFileType.Uid.Length == 0) && (SearchText.Length == 0 || newItemStyle.FunctionName.Contains(SearchText));
                else
                    e.Accepted = (newItemStyle.NameSpace == CurrentFileType.Uid || CurrentFileType.Uid.Length == 0) && (SearchText.Length == 0 || newItemStyle.FunctionName.Contains(SearchText));
            }
        }

        [RelayCommand]
        /// <summary>
        /// 确定选择的文件并已命名
        /// </summary>
        /// <param name="window"></param>
        private void ClickTrue(Window window)
        {
            window.DialogResult = true;
        }

        [RelayCommand]
        /// <summary>
        /// 返回
        /// </summary>
        /// <param name="window"></param>
        private void ClickFalse(Window window)
        {
            window.DialogResult = false;
        }
    }
}