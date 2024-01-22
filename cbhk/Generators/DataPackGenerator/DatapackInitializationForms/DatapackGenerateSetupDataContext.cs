using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.DataPackGenerator.Components;
using cbhk.Generators.DataPackGenerator.Components.EditPage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace cbhk.Generators.DataPackGenerator.DatapackInitializationForms
{
    /// <summary>
    /// 属性设置窗体逻辑处理
    /// </summary>
    public class DatapackGenerateSetupDataContext: ObservableObject
    {
        #region 属性设置窗体:上一步、下一步和设置路径等指令
        public RelayCommand<FrameworkElement> AttributeLastStep { get; set; }
        public RelayCommand<FrameworkElement> AttributeNextStep { get; set; }
        public RelayCommand SetSolutionPath { get; set; }
        #endregion

        #region 存储解决方案的名称
        private string solutionName = "Datapack";
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
        private string selectedSolutionPath = "";
        public string SelectedSolutionPath
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
        public ObservableCollection<string> GeneratorPathList { get; set; } = [];
        public string Description { get; set; } = "";

        private readonly string DatapackGeneratorFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Datapack\\data\\GeneratorPathes.ini";
        /// <summary>
        /// 空白解决方案路径
        /// </summary>
        private readonly string BlankSolutionFolder = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Datapack\\data\\Templates";
        #endregion

        public DatapackGenerateSetupDataContext()
        {
            #region 链接指令
            AttributeLastStep = new RelayCommand<FrameworkElement>(AttributeLastStepCommand);
            AttributeNextStep = new RelayCommand<FrameworkElement>(AttributeNextStepCommand);
            SetSolutionPath = new RelayCommand(SetSolutionPathCommand);
            #endregion
            #region 初始化数据
            List<string> generatorList = [.. File.ReadAllLines(DatapackGeneratorFilePath)];
            foreach (var item in generatorList)
                GeneratorPathList.Add(item);
            #endregion
        }

        /// <summary>
        /// 设置解决方案的路径
        /// </summary>
        private void SetSolutionPathCommand()
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
                    SelectedSolutionPath = selectedPath;
                    if (GeneratorPathList.Count > 0)
                    {
                        if (!GeneratorPathList.Contains(selectedPath))
                        {
                            GeneratorPathList.Insert(0,selectedPath);
                            if (GeneratorPathList.Count > 100)
                                GeneratorPathList.Remove(GeneratorPathList.Last());
                        }
                    }
                    else
                        GeneratorPathList.Insert(0,selectedPath);
                }
            }
        }

        /// <summary>
        /// 属性设置窗体进入下一步
        /// </summary>
        private async void AttributeNextStepCommand(FrameworkElement ele)
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
                File.Copy(SolutionTemplatePath,SelectedSolutionPath + "\\" + SolutionName + ".sln",true);

            string solutionContent = File.ReadAllText(SolutionTemplatePath);
            Datapack datapack = Window.GetWindow(ele) as Datapack;
            DatapackDataContext context = datapack.DataContext as DatapackDataContext;
            context.editPage ??= new();
            EditPageDataContext editContext = context.editPage.DataContext as EditPageDataContext;
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
                        DatapackTreeItems datapackTreeItems = new();
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
                                DatapackName = "Datapack";
                            //把数据包转移到生成目录
                            CopyFolder(solutionPath, SelectedSolutionPath + "\\" + SolutionName);
                            #region 为编辑页添加数据包节点
                            DatapackTreeItems datapackTreeItems = new();
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
            await File.WriteAllLinesAsync(DatapackGeneratorFilePath, GeneratorPathList);
            #endregion
            //导航到编辑页
            NavigationService.GetNavigationService(context.frame).Navigate(context.editPage);
        }

        /// <summary>
        /// 属性设置窗体进入上一步
        /// </summary>
        private void AttributeLastStepCommand(FrameworkElement ele)
        {
            //返回模板选择页
            Datapack datapack = Window.GetWindow(ele) as Datapack;
            DatapackDataContext context = datapack.DataContext as DatapackDataContext;
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
