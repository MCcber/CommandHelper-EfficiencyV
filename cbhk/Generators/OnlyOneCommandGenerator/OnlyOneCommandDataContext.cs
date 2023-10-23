using cbhk.CustomControls;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.Windows.Edit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cyotek.Data.Nbt;
using System.Collections.ObjectModel;
using cbhk.GeneralTools.MessageTip;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using System.Collections.Generic;
using System;

namespace cbhk.Generators.OnlyOneCommandGenerator
{
    public partial class OnlyOneCommandDataContext : ObservableObject
    {
        #region 生成、返回等命令
        /// <summary>
        /// 执行生成
        /// </summary>
        public RelayCommand RunCommand { get; set; }

        /// <summary>
        /// 返回主页
        /// </summary>
        public RelayCommand<CommonWindow> ReturnCommand { get; set; }

        /// <summary>
        /// 添加一条ooc
        /// </summary>
        public RelayCommand AddOneCommandPage { get; set; }

        /// <summary>
        /// 清空ooc
        /// </summary>
        public RelayCommand ClearCommandPage { get; set; }

        /// <summary>
        /// 从剪切板导入
        /// </summary>
        public RelayCommand ImportFormClipBoard { get; set; }

        /// <summary>
        /// 从文件导入
        /// </summary>
        public RelayCommand ImportFromFile { get; set; }
        #endregion

        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconCommandBlock.png";

        SolidColorBrush tranparentBrush = Brushes.Transparent;
        SolidColorBrush textBrush = Brushes.White;
        SolidColorBrush caretBrush = Brushes.White;

        #region 显示结果
        private bool showGeneratorResult = false;
        public bool ShowGeneratorResult
        {
            get => showGeneratorResult;
            set => SetProperty(ref showGeneratorResult, value);
        }
        #endregion

        /// <summary>
        /// OOC标签页数据源
        /// </summary>
        public ObservableCollection<RichTabItems> OocTabSource { get; set; } = new();

        /// <summary>
        /// 主页引用
        /// </summary>
        public Window home = null;

        #region 当前选中的标签页
        private RichTabItems selectedItem = null;
        public RichTabItems SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }
        #endregion

        //[GeneratedRegex(@"summon falling_block ~ ~[-+]?[0-9]*\.?[0-9]+ ~ {""Time"":1,""Block"":""minecraft:redstone_block"",""Motion"":\[0,-1,0\],""Passengers"":\[{""id"":""falling_block"",""Time"":""1"",""Block"":""minecraft:activator_rail"",""Passengers"":\[{""id"":""commandblock_minecart"",""Command"":""blockdata ~ ~\-2 ~ {\\\""auto\\\"":0,""Command"":\""\""}""},{""id"":""commandblock_minecart"",""Command"":""setblock ~1 ~\-2 ~ repeating_command_block 5 replace {""Command"":\\\""\\\"",\\\""auto\\\"":""1""}""},")]
        //private static partial Regex OocStart();

        //[GeneratedRegex(@"{""id"":""commandblock_minecart"",""Command"":""setblock ~ ~1 ~ command_block 0 replace {\\\""auto\\\"":""1"",""Command"":\""fill ~ ~ ~ ~ ~-2 ~ air\""}""},{""id"":""commandblock_minecart"",""Command"":""kill @e\[type=commandblock_minecart,r=1\]""}]}]}", RegexOptions.RightToLeft)]
        //private static partial Regex OocEnd();

        [GeneratedRegex(@"(?<=Command:\\\"")([\w \:""\[\]'()!@#$%^&*\-=+\\|;,./?<>`~]*)+(?=\\\"")")]
        private static partial Regex GetCommand();

        OnlyOneCommand onlyOneCommand = null;
        public OnlyOneCommandDataContext()
        {
            #region 绑定指令
            RunCommand = new RelayCommand(run_command);
            ReturnCommand = new RelayCommand<CommonWindow>(return_command);
            AddOneCommandPage = new RelayCommand(AddOneCommandPageCommand);
            ClearCommandPage = new RelayCommand(ClearCommandPageCommand);
            //ImportFormClipBoard = new RelayCommand(ImportFormClipBoardCommand);
            ImportFromFile = new RelayCommand(ImportFromFileCommand);
            #endregion
        }

        /// <summary>
        /// 从文件导入
        /// </summary>
        private async void ImportFromFileCommand()
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
            TagCompound tagCompound = null;
            TagCollection blockCollection = null;
            if (fileDialog.ShowDialog().Value)
            {
                try
                {
                    NbtDocument document = new();
                    using (FileStream stream = File.OpenRead(fileDialog.FileName))
                    {
                        document.Load(stream);
                        tagCompound = document.DocumentRoot;
                    }
                    blockCollection = (tagCompound.Value["blocks"] as TagList).Value;
                    await Task.Run(async () =>
                    {
                        StringBuilder result = new();
                        List<string> targetBlockId = new() { "minecraft:command_block", "minecraft:repeating_command_block", "minecraft:chain_command_block" };
                        foreach (TagCompound block in blockCollection.Cast<TagCompound>())
                        {
                            string test = block.Type.ToString();
                            if (block.Value.Contains("nbt"))
                            {
                                TagCompound blockNBT = block.Value["nbt"] as TagCompound;
                                if (targetBlockId.Contains(blockNBT.Value["id"].ToValueString()))
                                {
                                    string commandContent = blockNBT.Value["Command"].ToValueString();
                                    if (commandContent.Trim().Length > 0)
                                        result.Append(commandContent + "\r\n");
                                }
                            }
                        }
                        await onlyOneCommand.Dispatcher.InvokeAsync(() =>
                        {
                            AddOneCommandPageCommand();
                            (SelectedItem.Content as EditControl).Text = result.ToString();
                        });
                    });
                }
                catch(Exception e)
                {
                    Message.PushMessage(e.Message,MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 从剪切板导入
        /// </summary>
        //private async void ImportFormClipBoardCommand()
        //{
            //string nbt = Clipboard.GetText();
            //await Task.Run(async () =>
            //{
            //    nbt = nbt[nbt.IndexOf("{")..(nbt.LastIndexOf("}") + 1)];
            //    List<Match> result = GetCommand().Matches(nbt).Where(item=>!string.IsNullOrEmpty(item.Value)).ToList();
            //    if(result.Count > 1)
            //    result.RemoveAt(result.Count - 1);
            //    await onlyOneCommand.Dispatcher.InvokeAsync(() =>
            //    {
            //        AddOneCommandPageCommand();
            //        StringBuilder commandContent = new();
            //        for (int i = 0; i < result.Count; i++)
            //        {
            //            commandContent.Append(result[i].Value + "\r\n");
            //        }
            //            (SelectedItem.Content as EditControl).Text = commandContent.ToString();
            //    });
            //});
        //}

        /// <summary>
        /// 初始化标签页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OocTabItemLoaded(object sender,RoutedEventArgs e)
        {
            onlyOneCommand = sender as OnlyOneCommand;
            RichTabItems initItem = onlyOneCommand.Resources["initItem"] as RichTabItems;
            OocTabSource.Add(initItem);
        }

        /// <summary>
        /// 清空ooc
        /// </summary>
        private void ClearCommandPageCommand()
        {
            OocTabSource.Clear();
        }

        /// <summary>
        /// 添加ooc
        /// </summary>
        private void AddOneCommandPageCommand()
        {
            EditControl editControl = new()
            {
                EnableIntellisense = true,
                EnableOutlining = true,
                IntellisenseMode = IntellisenseMode.Auto,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 15,
                CaretBrush = caretBrush,
                DocumentLanguage = Languages.Custom,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                ShowLineNumber = true,
                LineNumberAreaBackground = tranparentBrush,
                LineNumberTextForeground = textBrush,
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

        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="obj"></param>
        private void return_command(CommonWindow win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        /// <summary>
        /// 执行生成
        /// </summary>
        private void run_command()
        {
            string resultStartpart = "summon falling_block ~ ~1.5 ~ {Time:1,Block:\"minecraft:redstone_block\",Motion:[0d,-1d,0d],Passengers:[{id:falling_block,Time:1,Block:\"minecraft:activator_rail\",Passengers:[{id:commandblock_minecart,Command:\"blockdata ~ ~-2 ~ {auto:0b,Command:\\\"\\\"}\"},{id:commandblock_minecart,Command:\"setblock ~1 ~-2 ~ repeating_command_block 5 replace {Command:\\\"\\\",auto:1b}\"},";
            string resultEndPart = "{id:commandblock_minecart,Command:\"setblock ~ ~1 ~ command_block 0 replace {auto:1b,Command:\\\"fill ~ ~ ~ ~ ~-2 ~ air\\\"}\"},{id:commandblock_minecart,Command:\"kill @e[type=commandblock_minecart,r=1]\"}]}]}";
            StringBuilder resultContent = new();

            int Offset = 2;

            foreach (RichTabItems tab in OocTabSource)
            {
                if(tab.Content is EditControl editControl && tab.Uid == "")
                {
                    for (int i = 0; i < editControl.Lines.Count; i++)
                    {
                        string lineContent = editControl.Lines[i].Text;
                        resultContent.Append("{id:commandblock_minecart,Command:\"setblock ~" + Offset + " ~-2 ~ chain_command_block 5 replace {Command:\\\"" + lineContent + "\\\",auto:1b}\"},");
                        Offset++;
                    }
                }
            }
            string result = resultStartpart + resultContent.ToString() + resultEndPart;
            if(ShowGeneratorResult)
            {
                GenerateResultDisplayer.Displayer displayer = GenerateResultDisplayer.Displayer.GetContentDisplayer();
                displayer.GeneratorResult(result, "OOC", icon_path);
                displayer.Show();
            }
            else
            {
                Clipboard.SetText(result);
                Message.PushMessage("Ooc生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }
    }
}
