using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace cbhk_environment.Generators.SignGenerator.Components
{
    public class SignPageDataContext : ObservableObject
    {
        #region 字段

        /// <summary>
        /// 告示牌类型
        /// </summary>
        public string SignType = "oak";

        #region 告示牌背景
        public BitmapImage signPanelSource = null;
        public BitmapImage SignPanelSource
        {
            get => signPanelSource;
            set => SetProperty(ref signPanelSource, value);
        }
        #endregion

        #region 已选择的版本
        private string selectedVersion = "";
        public string SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion,value);
                VersionMask();
            }
        }
        #endregion

        /// <summary>
        /// 版本数据源
        /// </summary>
        public ObservableCollection<string> VersionSource { get; set; } = new() { "1.16-", "1.17~1.19.4", "1.20+" };

        /// <summary>
        /// 正反面文档
        /// </summary>
        public List<FlowDocument> SignDocuments { get; set; } = new() { new FlowDocument(),new FlowDocument() };

        /// <summary>
        /// 告示牌编辑器
        /// </summary>
        private RichTextBox SignTextEditor = null;

        /// <summary>
        /// 是否被裱
        /// </summary>
        public bool IsWaxed { get; set; } = true;

        #region 是否为反面
        public bool isBack = false;
        public bool IsBack
        {
            get => isBack;
            set
            {
                SetProperty(ref isBack, value);
                FaceSwitcher();
            }
        }
        #endregion

        /// <summary>
        /// 切换告示牌正反面
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void FaceSwitcher()
        {
            if (SignTextEditor is null) return;
            SignTextEditor.Document = IsBack ? SignDocuments[1] : SignDocuments[0];
        }

        #region 正反面发光
        private bool isFrontGlowing;

        public bool IsFrontGlowing
        {
            get => isFrontGlowing;
            set => SetProperty(ref isFrontGlowing,value);
        }
        private bool isBackGlowing;

        public bool IsBackGlowing
        {
            get => isBackGlowing;
            set => SetProperty(ref isBackGlowing, value);
        }
        #endregion

        #region 显示结果
        private bool showResult;

        public bool ShowResult
        {
            get => showResult;
            set => SetProperty(ref showResult, value);
        }

        #endregion

        #endregion

        public SignPageDataContext()
        {
            SignPanelSource = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + SignType + "SignPanel.png", UriKind.Absolute))
            {
                CacheOption = BitmapCacheOption.OnLoad
            };
            for (int i = 0; i < 4; i++)
            {
                SignDocuments[0].Blocks.Add(new Paragraph() { TextAlignment = TextAlignment.Center, Margin = new(0, 2.5, 0, 2.5) });
                SignDocuments[1].Blocks.Add(new Paragraph() { TextAlignment = TextAlignment.Center, Margin = new(0, 2.5, 0, 2.5) });
            }
        }

        /// <summary>
        /// 版本蒙版,关闭指定版本或版本区间里不存在的功能
        /// </summary>
        private void VersionMask()
        {

        }

        /// <summary>
        /// 文本编辑器载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignTextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            SignTextEditor = sender as RichTextBox;
            SignTextEditor.Document = SignDocuments[0];
        }
    }
}
