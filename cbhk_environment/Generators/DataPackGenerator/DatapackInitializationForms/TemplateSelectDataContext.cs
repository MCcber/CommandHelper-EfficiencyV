using cbhk_environment.Generators.DataPackGenerator.Components.TemplateSelectPage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace cbhk_environment.Generators.DataPackGenerator.DatapackInitializationForms
{
    public class TemplateSelectDataContext: ObservableObject
    {
        #region 字段
        /// <summary>
        /// 模板存放路径
        /// </summary>
        private string TemplateDataFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Datapack\\data\\SolutionTemplates.json";
        /// <summary>
        /// 空白解决方案路径
        /// </summary>
        private string BlankSolutionFolder = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Datapack\\data\\Templates";
        /// <summary>
        /// 解决方案模板数组对象
        /// </summary>
        private JArray SolutionTemplateArray = new();
        /// <summary>
        /// 近期使用的模板存放路径
        /// </summary>
        private string RecentTemplateDataFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Datapack\\data\\RecentTemplates";
        /// <summary>
        /// 包版本映射表路径
        /// </summary>
        private string PackVersionMapFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Datapack\\data\\VersionList.ini";
        private SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        private SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));
        private SolidColorBrush grayBrush = new((Color)ColorConverter.ConvertFromString("#3D3D3D"));
        /// <summary>
        /// 载入完毕
        /// </summary>
        private bool Loaded = false;
        #endregion

        /// <summary>
        /// 近期使用的解决方案模板数据源
        /// </summary>
        public ObservableCollection<RecentSolutionTemplateItems> RecentSolutionTemplateList { get; set; } = new();
        /// <summary>
        /// 包模板数据源
        /// </summary>
        public ObservableCollection<SolutionTemplateItems> SolutionTemplateList { get; set; } = new();
        /// <summary>
        /// 解决方案视图集合对象
        /// </summary>
        private CollectionViewSource SolutionTemplateSource { get; set; }
        /// <summary>
        /// 存放版本列表
        /// </summary>
        public ObservableCollection<string> VersionList { get; set; } = new();
        /// <summary>
        /// 存放模板类型列表
        /// </summary>
        public ObservableCollection<string> DeveloperNameList { get; set; } = new();
        /// <summary>
        /// 包功能类型列表
        /// </summary>
        public ObservableCollection<string> FunctionTypeList { get; set; } = new();

        #region 存储已选择的版本
        private string selectedVersion = "";
        public string SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion, value);
                ClearAllParametersVisibility = SelectedVersionIndex <= 0 && SelectedDeveloperNameIndex <= 0 && SelectedFunctionTypeIndex <= 0 && SearchText.Length == 0 ? Visibility.Hidden : Visibility.Visible;
                if (Loaded)
                SolutionTemplateSource.View?.Refresh();
            }
        }
        public int SelectedVersionIndex { get; set; } = 0;
        private int RealSelectedVersion
        {
            get => VersionList.Count - 1 - VersionList.IndexOf(SelectedVersion) + 3;
        }
        #endregion

        #region 存储已选择的开发者名称
        private string selectedDeveloperName = "";
        public string SelectedDeveloperName
        {
            get => selectedDeveloperName;
            set
            {
                SetProperty(ref selectedDeveloperName, value);
                ClearAllParametersVisibility = SelectedVersionIndex <= 0 && SelectedDeveloperNameIndex <= 0 && SelectedFunctionTypeIndex <= 0 && SearchText.Length == 0 ? Visibility.Hidden : Visibility.Visible;
                if (Loaded)
                    SolutionTemplateSource.View?.Refresh();
            }
        }
        public int SelectedDeveloperNameIndex { get; set; } = 0;
        #endregion

        #region 存储已选择的功能类型
        private string selectedFunctionType = "";
        public string SelectedFunctionType
        {
            get => selectedFunctionType;
            set
            {
                SetProperty(ref selectedFunctionType, value);
                ClearAllParametersVisibility = SelectedVersionIndex <= 0 && SelectedDeveloperNameIndex <= 0 && SelectedFunctionTypeIndex <= 0 && SearchText.Length == 0 ? Visibility.Hidden : Visibility.Visible;
                if (Loaded)
                    SolutionTemplateSource.View?.Refresh();
            }
        }
        public int SelectedFunctionTypeIndex { get; set; } = 0;
        #endregion

        #region 存储已选择的解决方案
        private SolutionTemplateItems LastSelectedSolution = null;
        private SolutionTemplateItems selectedSolution = null;
        public SolutionTemplateItems SelectedSolution
        {
            get => selectedSolution;
            set
            {
                selectedSolution = value;
                if(SelectedSolution != null)
                    LastSelectedSolution = SelectedSolution;
            }
        }
        #endregion

        #region 存储搜索模板搜索文本
        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set
            {
                SetProperty(ref searchText, value);
                ClearAllParametersVisibility = SelectedVersionIndex <= 0 && SelectedDeveloperNameIndex <= 0 && SelectedFunctionTypeIndex <= 0 && SearchText.Length == 0 ? Visibility.Hidden : Visibility.Visible;
                if (Loaded)
                    SolutionTemplateSource.View?.Refresh();
            }
        }
        #endregion

        #region 上一步、下一步命令
        public RelayCommand<Page> TemplateLastStep { get; set; }
        public RelayCommand<Page> TemplateNextStep { get; set; }
        #endregion

        #region 清除过滤参数可见性
        private Visibility clearAllParametersVisibility = Visibility.Hidden;
        public Visibility ClearAllParametersVisibility
        {
            get => clearAllParametersVisibility;
            set => SetProperty(ref clearAllParametersVisibility,value);
        }
        #endregion

        #region 主窗体引用
        Datapack dataPack = null;
        #endregion


        public TemplateSelectDataContext()
        {
            #region 链接命令
            TemplateLastStep = new RelayCommand<Page>(TemplateLastStepCommand);
            TemplateNextStep = new RelayCommand<Page>(TemplateNextStepCommand);
            #endregion
        }

        /// <summary>
        /// 异步加载数据
        /// </summary>
        private async void InitData()
        {
            await Task.Run(() =>
            {
                dataPack.Dispatcher.InvokeAsync(() =>
                {
                    datapack_datacontext context = dataPack.DataContext as datapack_datacontext;
                    SolutionTemplateSource = context.templateSelectPage.FindResource("SolutionTemplateSource") as CollectionViewSource;
                    #region 清除数据
                    VersionList.Clear();
                    SolutionTemplateList.Clear();
                    DeveloperNameList.Clear();
                    FunctionTypeList.Clear();
                    VersionList.Add("全部");
                    DeveloperNameList.Add("全部");
                    FunctionTypeList.Add("全部");
                    #endregion
                    #region 载入版本
                    if (File.Exists(PackVersionMapFilePath))
                    {
                        string[] versions = File.ReadAllLines(PackVersionMapFilePath);
                        for (int i = versions.Length - 1; i >= 0; i--)
                            VersionList.Add(versions[i]);
                    }
                    #endregion
                    #region 载入解决方案模板
                    string solutionTemplateContent = File.ReadAllText(TemplateDataFilePath);
                    List<string> BlankSolutions = Directory.GetFiles(BlankSolutionFolder).ToList();
                    BlankSolutions.Sort(new SolutionNameComparer());
                    for (int i = 0; i < BlankSolutions.Count; i++)
                        SolutionTemplateArray.Add(BlankSolutions[i]);
                    if (solutionTemplateContent.Length > 0)
                        SolutionTemplateArray = JArray.Parse(solutionTemplateContent);
                    if (SolutionTemplateArray != null)
                    {
                        //读取空白解决方案
                        foreach (var item in SolutionTemplateArray.Cast<JValue>())
                        {
                            string path = item.ToString();
                            JObject data = JObject.Parse(File.ReadAllText(path));
                            string iconPath = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + ".png";
                            if (File.Exists(path))
                            {
                                SolutionTemplateItems solutionTemplateItems = new()
                                {
                                    Margin = new Thickness(0, 0, -10, 10),
                                    Uid = path
                                };
                                SolutionTemplateList.Add(solutionTemplateItems);
                                if (File.Exists(iconPath))
                                {
                                    Uri iconUri = new(iconPath, UriKind.Absolute);
                                    solutionTemplateItems.Icon.Source = new BitmapImage(iconUri);
                                }
                                if (data != null)
                                {
                                    if (data.SelectToken("Name") is JToken name)
                                        solutionTemplateItems.SolutionName.Text = name.ToString();
                                    if (data.SelectToken("Description") is JToken description)
                                        solutionTemplateItems.Description.Text = description.ToString();
                                    string versionData = "";
                                    if (data.SelectToken("Version") is JToken version)
                                        versionData += version.ToString();
                                    if (data.SelectToken("SolutionVersion") is JToken solutionVersion)
                                        versionData += "-" + solutionVersion.ToString();
                                    solutionTemplateItems.Version.Text = versionData;
                                    if (data.SelectToken("Developer") is JToken developer)
                                    {
                                        solutionTemplateItems.Developer.Text = developer.ToString();
                                        if (!DeveloperNameList.Contains(developer.ToString()))
                                            DeveloperNameList.Add(developer.ToString());
                                    }
                                    if (data.SelectToken("Type") is JToken type)
                                    {
                                        if (!FunctionTypeList.Contains(type.ToString()))
                                            FunctionTypeList.Add(type.ToString());
                                        TextBlock textBlock = new()
                                        {
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Text = type.ToString(),
                                            Foreground = whiteBrush,
                                            Padding = new Thickness(5, 2, 5, 2)
                                        };
                                        Border border = new()
                                        {
                                            Margin = new Thickness(0, 2, 0, 0),
                                            CornerRadius = new CornerRadius(5),
                                            HorizontalAlignment = HorizontalAlignment.Left,
                                            BorderThickness = new Thickness(1),
                                            BorderBrush = blackBrush,
                                            Background = grayBrush,
                                            Child = textBlock
                                        };
                                        solutionTemplateItems.TypePanel.Children.Add(border);
                                    }
                                }
                            }
                        }
                        SelectedVersionIndex = SelectedDeveloperNameIndex = SelectedFunctionTypeIndex = 0;
                    }
                    #endregion
                    #region 载入近期使用的解决方案模板

                    #endregion
                });
                Loaded = true;
            });
        }

        /// <summary>
        /// 模板窗体载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TemplateWindowLoaded(object sender,RoutedEventArgs e)
        {
            dataPack = Window.GetWindow(sender as Page) as Datapack;
            InitData();
        }

        /// <summary>
        /// 导航至参数设置页
        /// </summary>
        private async Task NavigationToGenerateSetupPage()
        {
            datapack_datacontext context = dataPack.DataContext as datapack_datacontext;
            await dataPack.Dispatcher.InvokeAsync(() =>
            {
                context.datapackGenerateSetupPage ??= new();
                DatapackGenerateSetupDataContext setUpContext = context.datapackGenerateSetupPage.DataContext as DatapackGenerateSetupDataContext;
                setUpContext.SolutionTemplatePath = LastSelectedSolution.Uid;
                NavigationService.GetNavigationService(context.frame).Navigate(context.datapackGenerateSetupPage);
            });
        }

        /// <summary>
        /// 清空过滤参数
        /// </summary>
        public void ClearFilterParameters(object sender, MouseButtonEventArgs e)
        {
            SearchText = "";
            SelectedVersion = VersionList[0];
            SelectedDeveloperName = DeveloperNameList[0];
            SelectedFunctionType = FunctionTypeList[0];
            ClearAllParametersVisibility = Visibility.Hidden;
        }

        /// <summary>
        /// 返回起始页
        /// </summary>
        /// <param name="page"></param>
        private void TemplateLastStepCommand(Page page)
        {
            datapack_datacontext context = dataPack.DataContext as datapack_datacontext;
            NavigationService.GetNavigationService(context.frame).Navigate(context.homePage);
        }

        /// <summary>
        /// 进入包参数设置页
        /// </summary>
        /// <param name="page"></param>
        private async void TemplateNextStepCommand(Page page)
        {
            await NavigationToGenerateSetupPage();
        }

        /// <summary>
        /// 解决方案模板视图过滤事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SolutionTemplateViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (!Loaded) return;
            SolutionTemplateItems solutionTemplateItems = e.Item as SolutionTemplateItems;
            bool name = false;
            bool description = false;
            bool version;
            string versionValue = RealSelectedVersion + "";
            string currentVersion = solutionTemplateItems.Version.Text;
            if (currentVersion == "Version") return;
            if (SearchText.Length > 0)
            {
                name = solutionTemplateItems.SolutionName.Text.StartsWith(SearchText) || solutionTemplateItems.SolutionName.Text.Contains(SearchText);
                description = solutionTemplateItems.Description.Text.StartsWith(SearchText) || solutionTemplateItems.Description.Text.Contains(SearchText);
            }
            version = versionValue == currentVersion[..currentVersion.IndexOf('-')] || SelectedVersion == "全部";
            string developerValue = solutionTemplateItems.Developer.Text;
            List<string> typeValue = new();
            foreach (Border item in solutionTemplateItems.TypePanel.Children)
            {
                if (item.Child is TextBlock textBlock)
                    typeValue.Add(textBlock.Text);
            }
            bool developer = false;
            if(SelectedDeveloperName != null)
            developer = SelectedDeveloperName == "全部" ? true : SelectedDeveloperName.StartsWith(developerValue) || SelectedDeveloperName.Contains(developerValue);
            bool type = false;
            if(SelectedFunctionType != null)
            type = SelectedFunctionType == "全部" ? true : typeValue.Contains(SelectedFunctionType);
            e.Accepted = SearchText.Length > 0?((name || description) && version && developer && type) : (name || description || (version && developer && type));
        }

        /// <summary>
        /// 近期方案模板视图过滤事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RecentSolutionTemplateViewSource_Filter(object sender, FilterEventArgs e)
        {

        }

        /// <summary>
        /// 双击解决方案视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void SolutionViewer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await NavigationToGenerateSetupPage();
        }
    }

    public class SolutionNameComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            string xNumber = Regex.Match(Path.GetFileNameWithoutExtension(x),@"\d+$").ToString();
            string yNumber = Regex.Match(Path.GetFileNameWithoutExtension(y), @"\d+$").ToString();
            int xInt = int.Parse(xNumber);
            int yInt = int.Parse(yNumber);
            return yInt.CompareTo(xInt);
        }
    }
}
