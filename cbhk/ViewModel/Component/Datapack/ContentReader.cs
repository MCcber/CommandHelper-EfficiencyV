using CBHK.CustomControl;
using CBHK.GeneralTool;
using CBHK.View.Component.Datapack.EditPage;
using CBHK.ViewModel.Component.Datapack.EditPage;
using CBHK.ViewModel.Generator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.ViewModel.Component.Datapack
{
    public class ContentReader
    {
        #region 数据包元数据的数据结构
        public struct DataPackMetaStruct
        {
            /// <summary>
            /// 版本
            /// </summary>
            public string Version;
            /// <summary>
            /// 字符描述
            /// </summary>
            public string Description;
            /// <summary>
            /// 对象或数组描述
            /// </summary>
            public RichParagraph DescriptionObjectOrArray;
            /// <summary>
            /// 描述的类型
            /// </summary>
            public string DescriptionType;
            /// <summary>
            /// 过滤器
            /// </summary>
            public RichParagraph Filter;
        };
        #endregion

        #region 内容的类型
        public enum ContentType
        {
            Solution,
            Datapack,
            Folder,
            File,
            UnKnown
        }
        #endregion

        /// <summary>
        /// 白色
        /// </summary>
        private static SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));

        //目标类型目录配置文件路径
        static string TargetFolderNameListFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Datapack\data\targetFolderNameList.ini";
        //能够读取的文件类型配置文件路径
        static string ReadableFileExtensionListFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Datapack\data\ReadableFileExtensionList.ini";
        //原版命名空间配置文件路径
        static string OriginalTargetFolderNameListFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Datapack\data\minecraftNameSpaceList.ini";
        //存储目标目录类型列表
        public static List<string> TargetFolderNameList = [];
        //存储能够读取的文件类型列表
        public static List<string> ReadableFileExtensionList = [];
        //存储原版命名空间类型列表
        public static List<string> OriginalTargetFolderNameList = [];
        /// <summary>
        /// 编辑页数据上下文
        /// </summary>
        //private static EditPageViewModel editContext = null;

        static ContentReader()
        {
            //读取可读取文件类型列表
            ReadableFileExtensionList = File.ReadAllLines(ReadableFileExtensionListFilePath).ToList();
            //读取原版命名空间列表
            OriginalTargetFolderNameList = File.ReadAllLines(OriginalTargetFolderNameListFilePath).ToList();
            //读取可用命名空间列表
            TargetFolderNameList = File.ReadAllLines(TargetFolderNameListFilePath).ToList();
        }

        /// <summary>
        ///为文件夹添加users，everyone用户组的完全控制权限
        /// </summary>
        /// <param name="dirPath"></param>
        private static void AddSecurity(string dirPath)
        {
            //获取文件夹信息
            var dir = new DirectoryInfo(dirPath);
            //获得该文件夹的所有访问权限
            var dirSecurity = dir.GetAccessControl(AccessControlSections.All);
            //设定文件ACL继承
            var inherits = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            //添加ereryone用户组的访问权限规则 完全控制权限
            var everyoneFileSystemAccessRule = new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
            //添加Users用户组的访问权限规则 完全控制权限
            var usersFileSystemAccessRule = new FileSystemAccessRule("Users", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
            dirSecurity.ModifyAccessRule(AccessControlModification.Add, everyoneFileSystemAccessRule, out var isModified);
            dirSecurity.ModifyAccessRule(AccessControlModification.Add, usersFileSystemAccessRule, out isModified);
            //设置访问权限
            dir.SetAccessControl(dirSecurity);
        }

        /// <summary>
        /// 判断读取内容的类型
        /// </summary>
        public static ContentType JudgeTheTypeOfReadContent(string path)
        {
            ContentType result = ContentType.UnKnown;

            #region 判断是否为解决方案
            if (File.Exists(path))
            {
                string fileContent = File.ReadAllText(path);
                try
                {
                    JObject data = JObject.Parse(fileContent);
                    if (data.SelectToken("Datapacks") is JArray)
                        result = ContentType.Solution;
                }
                catch { }
            }
            #endregion

            if (result == ContentType.UnKnown)
            {
                if (File.Exists(path))
                {
                    #region 判断是否为.json或.mcfunction等文件
                    string extension = Path.GetExtension(path);
                    if (ReadableFileExtensionList.Contains(extension))
                        result = ContentType.File;
                    #endregion
                }
                if (Directory.Exists(path))
                {
                    result = ContentType.Folder;
                    #region 判断是否为数据包
                    if (!path.EndsWith("\\"))
                        path += "\\";
                    if (File.Exists(path + "pack.mcmeta") && Directory.Exists(path + "data"))
                        result = ContentType.Datapack;
                    #endregion
                }
                //同一目录下有同名子文件和子文件夹
                if (Path.GetExtension(path).Length == 0 && File.Exists(path) && Directory.Exists(path))
                    result = ContentType.Folder;
            }
            return result;
        }

        /// <summary>
        /// 读取指定路径的内容
        /// </summary>
        /// <returns></returns>
        public static List<TreeViewItem> ReadTheContentOfTheSpecifiedPath(string targetPath, EditPageViewModel editContext = null)
        {
            List<TreeViewItem> result = [];
            List<TreeViewItem> folderResult = [];
            List<TreeViewItem> fileResult = [];
            ContentType contentType = JudgeTheTypeOfReadContent(targetPath);
            switch (contentType)
            {
                case ContentType.Solution:
                    {
                        JObject data = JObject.Parse(File.ReadAllText(targetPath));
                        if (data.SelectToken("Datapacks") is JArray datapacks)
                        {
                            foreach (JToken datapack in datapacks)
                            {
                                string datapackPath = datapack.ToString();
                                if (Directory.Exists(datapackPath))
                                {
                                    DatapackTreeItem datapackHeaderItems = new();
                                    datapackHeaderItems.HeadText.Text = datapackPath[(datapackPath.LastIndexOf("\\") + 1)..];
                                    RichTreeViewItems datapackItem = new()
                                    {
                                        Margin = new Thickness(0, 2, 0, 2),
                                        Header = datapackHeaderItems,
                                        Foreground = whiteBrush,
                                        Uid = datapackPath,
                                        Tag = data.SelectToken("ModifiedFiles")
                                    };
                                    //添加一个空节点当作展开的引子
                                    if (Directory.Exists(datapackPath) && Directory.GetFileSystemEntries(datapackPath).Length > 0)
                                        datapackItem.Items.Add(new RichTreeViewItems());
                                    datapackItem.Expanded += DatapackTreeItems_Expanded;
                                    if (Directory.Exists(datapackPath))
                                        folderResult.Add(datapackItem);
                                    else
                                        fileResult.Add(datapackItem);
                                }
                            }
                        }
                    }
                    break;
                case ContentType.Datapack:
                    {
                        DatapackTreeItem datapackHeaderItems = new();
                        datapackHeaderItems.HeadText.Text = targetPath[(targetPath.LastIndexOf("\\") + 1)..];
                        RichTreeViewItems datapackItem = new()
                        {
                            Margin = new Thickness(0, 2, 0, 2),
                            Header = datapackHeaderItems,
                            Foreground = whiteBrush,
                            Uid = targetPath
                        };
                        //添加一个空节点当作展开的引子
                        if (Directory.Exists(targetPath) && Directory.GetFileSystemEntries(targetPath).Length > 0)
                            datapackItem.Items.Add(new RichTreeViewItems());
                        datapackItem.Expanded += DatapackTreeItems_Expanded;
                        if (Directory.Exists(targetPath))
                            folderResult.Add(datapackItem);
                        else
                            fileResult.Add(datapackItem);
                    }
                    break;
                case ContentType.Folder:
                    {
                        string[] subContent = Directory.GetFileSystemEntries(targetPath);
                        foreach (string item in subContent)
                        {
                            DatapackTreeItem headerItems = new();
                            if (File.Exists(item))
                                headerItems.HeadText.Text = Path.GetFileName(item);
                            else
                                headerItems.HeadText.Text = item[(item.LastIndexOf('\\') + 1)..];
                            headerItems.DatapackMarker.Visibility = Visibility.Collapsed;
                            headerItems.Icon.Visibility = Visibility.Visible;
                            if (Directory.Exists(item))
                                headerItems.Icon.Source = Application.Current.Resources["FolderClosed"] as ImageSource;
                            RichTreeViewItems richTreeViewItems = new()
                            {
                                Margin = new Thickness(0, 2, 0, 2),
                                Foreground = whiteBrush,
                                Uid = item
                            };
                            //添加一个空节点当作展开的引子
                            if (Directory.Exists(item) && Directory.GetFileSystemEntries(item).Length > 0)
                            {
                                richTreeViewItems.Items.Add(new RichTreeViewItems());
                                richTreeViewItems.Expanded += DatapackTreeItems_Expanded;
                            }
                            if (Directory.Exists(item))
                                folderResult.Add(richTreeViewItems);
                            else
                            {
                                headerItems.HeadText.Text = Path.GetFileName(item);
                                fileResult.Add(richTreeViewItems);
                                if (editContext != null)
                                    richTreeViewItems.MouseDoubleClick += editContext.DoubleClickAnalysisAndOpenAsync;
                            }
                            richTreeViewItems.Header = headerItems;
                        }
                    }
                    break;
                case ContentType.File:
                    {
                        DatapackTreeItem headerItems = new();
                        headerItems.HeadText.Text = Path.GetFileName(targetPath);
                        headerItems.DatapackMarker.Visibility = Visibility.Collapsed;
                        headerItems.Icon.Visibility = Visibility.Visible;
                        if (File.Exists(targetPath))
                        {
                            string extension = Path.GetExtension(targetPath);
                            if (extension != ".mcfunction")
                                headerItems.Icon.Source = Application.Current.Resources["UnknownFile"] as ImageSource;
                            int extersionIndex = ReadableFileExtensionList.IndexOf(extension);
                            if (extersionIndex != -1)
                                headerItems.Icon.Source = Application.Current.Resources[ReadableFileExtensionList[extersionIndex]] as ImageSource;
                        }
                        RichTreeViewItems richTreeViewItems = new()
                        {
                            Margin = new Thickness(0, 2, 0, 2),
                            Header = headerItems,
                            Foreground = whiteBrush,
                            Uid = targetPath
                        };
                        richTreeViewItems.MouseDoubleClick += editContext.DoubleClickAnalysisAndOpenAsync;
                        fileResult.Add(richTreeViewItems);
                    }
                    break;
                case ContentType.UnKnown:
                    break;
            }
            result.AddRange(folderResult);
            result.AddRange(fileResult);
            return result;
        }

        /// <summary>
        /// 数据包视图节点展开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static async void DatapackTreeItems_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem currentItem = sender as TreeViewItem;
            Window window = Window.GetWindow(currentItem);
            List<TreeViewItem> folderResult = [];
            List<TreeViewItem> fileResult = [];
            //退订，返回
            if (currentItem.Items.Count > 0 && (currentItem.Items[0] as TreeViewItem).Header != null)
            {
                currentItem.Expanded -= DatapackTreeItems_Expanded;
                return;
            }
            else
                currentItem.Items.Clear();

            #region 处理子数据
            string targetPath = currentItem.Uid;
            ContentType contentType = JudgeTheTypeOfReadContent(targetPath);

            await window.Dispatcher.InvokeAsync(new Action(() =>
            {
                switch (contentType)
                {
                    case ContentType.Datapack:
                        {
                            string[] subContent = Directory.GetFileSystemEntries(targetPath + "\\data");
                            foreach (string item in subContent)
                            {
                                DatapackTreeItem headerItems = new();
                                headerItems.DatapackMarker.Visibility = Visibility.Collapsed;
                                headerItems.Icon.Visibility = Visibility.Visible;
                                headerItems.HeadText.Text = item[(item.LastIndexOf('\\') + 1)..];
                                if (Directory.Exists(item))
                                    headerItems.Icon.Source = Application.Current.Resources["FolderClosed"] as ImageSource;
                                TreeViewItem richTreeViewItems = new()
                                {
                                    Margin = new Thickness(0, 2, 0, 2),
                                    Header = headerItems,
                                    Foreground = whiteBrush,
                                    Uid = item
                                };
                                if (Directory.Exists(item))
                                    folderResult.Add(richTreeViewItems);
                                else
                                    fileResult.Add(richTreeViewItems);
                                //添加一个空节点当作展开的引子
                                if (Directory.Exists(item) && Directory.GetFileSystemEntries(item).Length > 0)
                                    richTreeViewItems.Items.Add(new TreeViewItem());
                                richTreeViewItems.Expanded += DatapackTreeItems_Expanded;
                            }
                        }
                        break;
                    case ContentType.Folder:
                        {
                            string[] subContent = Directory.GetFileSystemEntries(targetPath);
                            foreach (string item in subContent)
                            {
                                DatapackTreeItem headerItems = new();
                                if (File.Exists(item))
                                    headerItems.HeadText.Text = Path.GetFileName(item);
                                else
                                    headerItems.HeadText.Text = item[(item.LastIndexOf('\\') + 1)..];
                                headerItems.DatapackMarker.Visibility = Visibility.Collapsed;
                                headerItems.Icon.Visibility = Visibility.Visible;
                                TreeViewItem richTreeViewItems = new()
                                {
                                    Margin = new Thickness(0, 2, 0, 2),
                                    Header = headerItems,
                                    Foreground = whiteBrush,
                                    Uid = item
                                };
                                if (File.Exists(item))
                                {
                                    string extension = Path.GetExtension(item);
                                    if (extension != ".mcfunction")
                                        headerItems.Icon.Source = Application.Current.Resources["UnknownFile"] as ImageSource;
                                    int extersionIndex = ReadableFileExtensionList.IndexOf(extension);
                                    if (extersionIndex != -1)
                                    {
                                        headerItems.Icon.Source = Application.Current.Resources[ReadableFileExtensionList[extersionIndex]] as ImageSource;
                                        DatapackViewModel datapackContext = Window.GetWindow(currentItem).DataContext as DatapackViewModel;
                                        EditPageViewModel editContext = datapackContext.EditPage.DataContext as EditPageViewModel;
                                        richTreeViewItems.MouseDoubleClick += editContext.DoubleClickAnalysisAndOpenAsync;
                                    }
                                }
                                else
                                    headerItems.Icon.Source = Application.Current.Resources["FolderClosed"] as ImageSource;
                                //添加一个空节点当作展开的引子
                                if (Directory.Exists(item) && Directory.GetFileSystemEntries(item).Length > 0)
                                {
                                    richTreeViewItems.Items.Add(new TreeViewItem());
                                    richTreeViewItems.Expanded += DatapackTreeItems_Expanded;
                                }
                                if (Directory.Exists(item))
                                    folderResult.Add(richTreeViewItems);
                                else
                                    fileResult.Add(richTreeViewItems);
                            }
                        }
                        break;
                    case ContentType.File:
                        {
                            DatapackTreeItem headerItems = new();
                            headerItems.HeadText.Text = Path.GetFileName(targetPath);
                            headerItems.DatapackMarker.Visibility = Visibility.Collapsed;
                            headerItems.Icon.Visibility = Visibility.Visible;
                            if (File.Exists(targetPath))
                            {
                                string extension = Path.GetExtension(targetPath);
                                if (extension != ".mcfunction")
                                    headerItems.Icon.Source = Application.Current.Resources["UnknownFile"] as ImageSource;
                                int extersionIndex = ReadableFileExtensionList.IndexOf(extension);
                                if (extersionIndex != -1)
                                    headerItems.Icon.Source = Application.Current.Resources[ReadableFileExtensionList[extersionIndex]] as ImageSource;
                            }
                            TreeViewItem richTreeViewItems = new()
                            {
                                Margin = new Thickness(0, 2, 0, 2),
                                Header = headerItems,
                                Foreground = whiteBrush,
                                Uid = targetPath
                            };
                            EditPageViewModel context = currentItem.FindParent<EditPageView>().DataContext as EditPageViewModel;
                            richTreeViewItems.MouseDoubleClick += context.DoubleClickAnalysisAndOpenAsync;
                            currentItem.Items.Add(richTreeViewItems);
                        }
                        break;
                }
                foreach (var item in folderResult)
                    currentItem.Items.Add(item);
                foreach (var item in fileResult)
                    currentItem.Items.Add(item);
            }));
            #endregion
        }
    }
}