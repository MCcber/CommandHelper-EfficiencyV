using CBHK.CustomControl;
using CBHK.GeneralTool.MessageTip;
using CBHK.View.Component.Datapack.EditPage;
using CBHK.View.Generator;
using CBHK.ViewModel.Component.Datapack.EditPage;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace CBHK.ViewModel.Component.Datapack.DatapackInitializationForms
{
    /// <summary>
    /// 属性设置窗体逻辑处理
    /// </summary>
    public partial class DatapackGenerateSetupViewModel : ObservableObject
    {
        #region 存储解决方案的名称
        private string solutionName = "DatapackView";
        public string SolutionName
        {
            get => solutionName;
            set
            {
                SetProperty(ref solutionName, value);
                if (solutionName.Trim() == "")
                    SolutionNameIsNull = Visibility.Visible;
                else
                    SolutionNameIsNull = Visibility.Hidden;
            }
        }
        #endregion

        #region 存储解决方案的保存路径
        private TextComboBoxItem selectedSolutionPath;
        public TextComboBoxItem SelectedSolutionPath
        {
            get => selectedSolutionPath;
            set => SetProperty(ref selectedSolutionPath, value);
        }
        #endregion

        #region 解决方案名称为空时的提示可见性
        private Visibility solutionNameIsNull = Visibility.Hidden;
        public Visibility SolutionNameIsNull
        {
            get => solutionNameIsNull;
            set => SetProperty(ref solutionNameIsNull, value);
        }
        #endregion

        #region 生成路径、描述等数据
        public string SolutionTemplatePath = "";
        public ObservableCollection<TextComboBoxItem> GeneratorPathList { get; set; } = [];
        public string Description { get; set; } = "";

        private readonly string DatapackGeneratorFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Datapack\Data\GeneratorPathes.ini";
        /// <summary>
        /// 空白解决方案路径
        /// </summary>
        private readonly string BlankSolutionFolder = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Datapack\Data\Templates";
        #endregion

        public DatapackGenerateSetupViewModel()
        {
            #region 初始化数据
            List<string> generatorList = [.. File.ReadAllLines(DatapackGeneratorFilePath)];
            foreach (var item in generatorList)
                GeneratorPathList.Add(new TextComboBoxItem() { Text = item });
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 设置解决方案的路径
        /// </summary>
        private void SetSolutionPath()
        {
            OpenFolderDialog openFolderDialog = new()
            {
                Title = "请选择当前解决方案生成路径",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                ShowHiddenItems = true
            };

            if (openFolderDialog.ShowDialog().Value)
            {
                if (Directory.Exists(openFolderDialog.FolderName))
                {
                    string selectedPath = openFolderDialog.FolderName;
                    if (!GeneratorPathList.Any(item => item.Text == selectedPath))
                    {
                        GeneratorPathList.Insert(0, new TextComboBoxItem() { Text = selectedPath });
                    }
                    else
                    {
                        SelectedSolutionPath = GeneratorPathList.Where(item => item.Text == selectedPath).First();
                    }

                    if (GeneratorPathList.Count > 100)
                    {
                        GeneratorPathList.Remove(GeneratorPathList.Last());
                    }
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 属性设置窗体进入下一步
        /// </summary>
        private async Task AttributeNextStep(FrameworkElement ele)
        {
            #region 解决方案无名称不生成
            if (SolutionName.Trim().Length == 0 || GeneratorPathList.Count == 0)
            {
                Message.PushMessage("生成失败！解决方案未设置名称",MessageBoxImage.Error);
                return;
            }
            #endregion
            #region 复制解决方案到指定目录,并转移数据包，新建编辑页数据包节点
            if (File.Exists(SolutionTemplatePath))
                File.Copy(SolutionTemplatePath,SelectedSolutionPath.Text + "\\" + SolutionName + ".sln",true);

            string solutionContent = File.ReadAllText(SolutionTemplatePath);
            DatapackView datapack = Window.GetWindow(ele) as DatapackView;
            DatapackViewModel context = datapack.DataContext as DatapackViewModel;
            context.editPage ??= new();
            EditPageViewModel editContext = context.editPage.DataContext as EditPageViewModel;
            if (solutionContent.Length > 0)
            {
                JObject data = JObject.Parse(solutionContent);
                if(data.SelectToken("Datapacks") is JArray Datapacks)
                {
                    //把数据包转移到生成目录
                    if (Datapacks.Count == 0)
                    {
                        string solutionName = Path.GetFileNameWithoutExtension(SolutionTemplatePath);
                        CopyFolder(Path.GetDirectoryName(SolutionTemplatePath) + "\\" + solutionName, SelectedSolutionPath + "\\" + SolutionName);
                        #region 为编辑页添加数据包节点
                        DatapackTreeItem datapackTreeItems = new();
                        datapackTreeItems.HeadText.Text = SolutionName;
                        TreeViewItem treeViewItem = new()
                        {
                            Header = datapackTreeItems,
                            Uid = SelectedSolutionPath + "\\" + SolutionName
                        };
                        if (Directory.GetFileSystemEntries(SelectedSolutionPath + "\\" + SolutionName).Length > 0)
                        {
                            treeViewItem.Items.Add(new TreeViewItem());
                            treeViewItem.Expanded += ContentReader.DatapackTreeItems_Expanded;
                        }
                        editContext.DatapackTreeViewItems.Add(treeViewItem);
                        #endregion
                    }
                    else
                    {
                        foreach (JValue item in Datapacks.Cast<JValue>())
                        {
                            string solutionPath = item.ToString();
                            string DatapackName = solutionPath[(solutionPath.LastIndexOf('\\') + 1)..];
                            if (DatapackName.Trim().Length == 0)
                                DatapackName = "DatapackView";
                            //把数据包转移到生成目录
                            CopyFolder(solutionPath, SelectedSolutionPath + "\\" + SolutionName);
                            #region 为编辑页添加数据包节点
                            DatapackTreeItem datapackTreeItems = new();
                            datapackTreeItems.HeadText.Text = SolutionName;
                            TreeViewItem treeViewItem = new()
                            {
                                Header = datapackTreeItems,
                                Uid = SelectedSolutionPath + "\\" + SolutionName
                            };
                            if (Directory.GetFileSystemEntries(SelectedSolutionPath + "\\" + SolutionName).Length > 0)
                            {
                                treeViewItem.Items.Add(new TreeViewItem());
                                treeViewItem.Expanded += ContentReader.DatapackTreeItems_Expanded;
                            }
                            editContext.DatapackTreeViewItems.Add(treeViewItem);
                            #endregion
                        }
                    }
                }
            }
            #endregion
            #region 把当前生成路径集合写入指定文件
            await File.WriteAllLinesAsync(DatapackGeneratorFilePath, GeneratorPathList.Select(item=>item.Text));
            #endregion
            #region 导航到编辑页
            NavigationService.GetNavigationService(context.frame).Navigate(context.editPage);
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 属性设置窗体进入上一步
        /// </summary>
        private void AttributeLastStep(FrameworkElement ele)
        {
            //返回模板选择页
            DatapackView datapack = Window.GetWindow(ele) as DatapackView;
            DatapackViewModel context = datapack.DataContext as DatapackViewModel;
            NavigationService.GetNavigationService(context.frame).Navigate(context.templateSelectPage);
        }

        /// <summary>
        /// 复制文件夹及文件
        /// </summary>
        /// <param name="sourceFolder">原文件路径</param>
        /// <param name="destFolder">目标文件路径</param>
        /// <returns></returns>
        public void CopyFolder(string sourceFolder, string destFolder)
        {
            try
            {
                //如果目标路径不存在,则创建目标路径
                if (!Directory.Exists(destFolder))
                {
                    Directory.CreateDirectory(destFolder);
                }
                //得到原文件根目录下的所有文件
                string[] files = Directory.GetFiles(sourceFolder);
                foreach (string file in files)
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(destFolder, name);
                    File.Copy(file, dest);//复制文件
                }
                //得到原文件根目录下的所有文件夹
                string[] folders = Directory.GetDirectories(sourceFolder);
                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    string dest = Path.Combine(destFolder, name);
                    CopyFolder(folder, dest);//构建目标路径,递归复制文件
                }
            }
            catch{}
        }
    }
}