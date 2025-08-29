using CBHK.CustomControl;
using CBHK.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Text;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using System;
using SharpNBT;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Prism.Ioc;
using CBHK.View;
using CBHK.View.Generator;
using CBHK.Utility.MessageTip;
using CBHK.Common.Utility;

namespace CBHK.ViewModel.Generator
{
    public partial class OnlyOneCommandViewModel(IContainerProvider container, MainView mainView,RegexService RegexService) : ObservableObject
    {
        #region Field
        /// <summary>
        /// 主页引用
        /// </summary>
        public Window home = mainView;
        private IContainerProvider container = container;
        private RegexService regexService = RegexService;
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconCommandBlock.png";
        SolidColorBrush tranparentBrush = Brushes.Transparent;
        SolidColorBrush textBrush = Brushes.White;
        SolidColorBrush caretBrush = Brushes.White;
        #endregion

        #region Property
        /// <summary>
        /// 显示结果
        /// </summary>
        [ObservableProperty]
        public bool _showGeneratorResult = false;

        /// <summary>
        /// OOC标签页数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<RichTabItems> _oocTabSource = [
            new RichTabItems()
        {
            Style = Application.Current.Resources["RichTabItemStyle"] as Style,
            Header = "欢迎使用",
            FontWeight = FontWeights.Normal,
            IsContentSaved = true,
            BorderThickness = new(4, 4, 4, 0),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
            SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
            LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as ImageBrush,
            RightBorderTexture = Application.Current.Resources["TabItemRight"] as ImageBrush,
            TopBorderTexture = Application.Current.Resources["TabItemTop"] as ImageBrush,
            SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as ImageBrush,
            SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as ImageBrush,
            SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as ImageBrush
        }];

        /// <summary>
        /// 当前选中的标签页
        /// </summary>
        [ObservableProperty]
        private RichTabItems _selectedItem = null;
        #endregion

        #region Event
        [RelayCommand]
        /// <summary>
        /// 从文件导入
        /// </summary>
        private async Task ImportFromFile()
        {
            OpenFileDialog fileDialog = new()
            {
                DefaultExt = ".nbt",
                AddToRecent = true,
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "结构文件|*.nbt;",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                RestoreDirectory = true,
                Title = "请选择一个NBT文件"
            };

            CompoundTag tagCompound = null;
            ListTag blockCollection = null;
            if (fileDialog.ShowDialog().Value)
            {
                try
                {
                    using (FileStream stream = File.OpenRead(fileDialog.FileName))
                    {
                        tagCompound = await NbtFile.ReadAsync<CompoundTag>(fileDialog.FileName, FormatOptions.Java);
                    }
                    blockCollection = tagCompound["blocks"] as ListTag;
                    await Task.Run(async () =>
                    {
                        StringBuilder result = new();
                        List<string> targetBlockId = ["minecraft:command_block", "minecraft:repeating_command_block", "minecraft:chain_command_block"];
                        foreach (CompoundTag block in blockCollection.Cast<CompoundTag>())
                        {
                            string test = block.Type.ToString();
                            if (block.ContainsKey("nbt"))
                            {
                                CompoundTag blockNBT = block["nbt"] as CompoundTag;
                                if (targetBlockId.Contains(blockNBT["oldID"].ToString()))
                                {
                                    string commandContent = blockNBT["Command"].ToString();
                                    if (commandContent.Trim().Length > 0)
                                        result.Append(commandContent + "\r\n");
                                }
                            }
                        }
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            AddOneCommandPage();
                            (SelectedItem.Content as TextEditor).Text = result.ToString();
                        });
                    });
                }
                catch (Exception e)
                {
                    Message.PushMessage(e.Message, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 初始化标签页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OocTabItem_Loaded(object sender,RoutedEventArgs e)
        {
            OnlyOneCommandView onlyOneCommand = sender as OnlyOneCommandView;
            RichTextBox box = onlyOneCommand.Resources["welcomeBox"] as RichTextBox;
            OocTabSource[0].Content = box;
        }

        [RelayCommand]
        /// <summary>
        /// 清空ooc
        /// </summary>
        private void ClearCommandPage()
        {
            OocTabSource.Clear();
        }

        [RelayCommand]
        /// <summary>
        /// 添加ooc
        /// </summary>
        private void AddOneCommandPage()
        {
            TextEditor editControl = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 15,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                ShowLineNumbers = true,
                LineNumbersForeground = textBrush,
                Foreground = textBrush,
                Background = tranparentBrush
            };
            //McfunctionLanguage mcfunctionLanguage = new(editControl);
            //editControl.CustomLanguage = mcfunctionLanguage;
            RichTabItems tabItem = new()
            {
                FontSize = 12,
                Header = "OOC",
                FontWeight = FontWeights.Normal,
                Content = editControl,
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            OocTabSource.Add(tabItem);
            SelectedItem = tabItem;
        }

        [RelayCommand]
        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="obj"></param>
        private void Return(CommonWindow win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        [RelayCommand]
        /// <summary>
        /// 执行生成
        /// </summary>
        private void Run()
        {
            string resultStartpart = "summon falling_block ~ ~1.5 ~ {Time:1,Block:\"minecraft:redstone_block\",Motion:[0d,-1d,0d],Passengers:[{oldID:falling_block,Time:1,Block:\"minecraft:activator_rail\",Passengers:[{oldID:commandblock_minecart,Command:\"blockdata ~ ~-2 ~ {auto:0b,Command:\\\"\\\"}\"},{oldID:commandblock_minecart,Command:\"setblock ~1 ~-2 ~ repeating_command_block 5 replace {Command:\\\"\\\",auto:1b}\"},";
            string resultEndPart = "{oldID:commandblock_minecart,Command:\"setblock ~ ~1 ~ command_block 0 replace {auto:1b,Command:\\\"fill ~ ~ ~ ~ ~-2 ~ air\\\"}\"},{oldID:commandblock_minecart,Command:\"kill @e[type=commandblock_minecart,r=1]\"}]}]}";
            StringBuilder resultContent = new();

            int Offset = 2;

            foreach (RichTabItems tab in OocTabSource)
            {
                if(tab.Content is TextEditor editControl && tab.Uid == "")
                {
                    for (int i = 0; i < editControl.Document.LineCount; i++)
                    {
                        string lineContent = editControl.Document.GetText(editControl.Document.Lines[i]);
                        resultContent.Append("{oldID:commandblock_minecart,Command:\"setblock ~" + Offset + " ~-2 ~ chain_command_block 5 replace {Command:\\\"" + lineContent + "\\\",auto:1b}\"},");
                        Offset++;
                    }
                }
            }
            string Result = resultStartpart + resultContent.ToString() + resultEndPart;
            if(ShowGeneratorResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result, "OOC", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result);
                Message.PushMessage("Ooc生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }
        #endregion
    }
}