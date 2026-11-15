using CBHK.CustomControl.VectorComboBox;
using CBHK.Domain;
using CBHK.Model.Data;
using CBHK.Model.Generator.Tag;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Generator
{
    /// <summary>
    /// 标签生成器。
    /// tag 不是 mcdoc 描述的 resource（vanilla 资产里只有一份没人引用的 `type Tag&lt;E&gt;`），
    /// 所以这页不走求值管线：候选一律来自 `Resource\Registries\&lt;版本&gt;.json` 的注册表成员，
    /// 形状按 tag 文件的真实结构拼（`{ replace, values }`，value 允许字符串 / `#tag` 引用 / `{id, required}`）。
    /// </summary>
    public partial class TagViewModel : ObservableObject
    {
        #region Field
        private const string DefaultNamespace = "minecraft";
        private readonly MessagePopup messagePopup = new();
        private readonly IContainerProvider container;
        private readonly DataService dataService;
        /// <summary>主页引用</summary>
        private readonly Window home;

        private ListView TagZone;
        /// <summary>对象数据源</summary>
        private CollectionViewSource TagViewSource;
        private int LastSelectedIndex;
        /// <summary>同一个实例只载入一次（重开窗口是新的 VM，展开态与输入都不带过来）</summary>
        private bool loaded;
        /// <summary>换版本时正在重建目录列表：期间的选择变化不各自触发一次重载</summary>
        private bool reloadSuspended;
        #endregion

        #region Property
        /// <summary>注册表数据里出现过的版本键（新的在前）</summary>
        [ObservableProperty]
        private ObservableCollection<VectorTextComboBoxItem> versionList = [];

        [ObservableProperty]
        private VectorTextComboBoxItem selectedVersion;

        /// <summary>tag 目录候选：所选版本里出现过的注册表名（item / block / entity_type / …）</summary>
        [ObservableProperty]
        private ObservableCollection<VectorTextComboBoxItem> tagRegistryList = [];

        [ObservableProperty]
        private VectorTextComboBoxItem selectedTagRegistry;

        /// <summary>是否替换</summary>
        [ObservableProperty]
        private bool replace;

        /// <summary>当前目录下的候选成员</summary>
        [ObservableProperty]
        private ObservableCollection<TagItemTemplate> tagItemList = [];

        [ObservableProperty]
        private TagItemTemplate selectedItem;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]

        private bool selectedAll;

        [ObservableProperty]
        private bool reverseAll;

        /// <summary>标签所在命名空间（决定 `ns:id` 的前缀与导出目录）</summary>
        [ObservableProperty]
        private string namespaceText = DefaultNamespace;

        /// <summary>标签文件名（不带 .json）</summary>
        [ObservableProperty]
        private string tagName;

        /// <summary>附加条目：每行一条，支持 `ns:path` / `#ns:tag` / 原始 `{"id":…,"required":false}`</summary>
        [ObservableProperty]
        private string extraValuesText;

        /// <summary>勾选项输出成 `{"id":…}` 而不是裸字符串</summary>
        [ObservableProperty]
        private bool useObjectForm;

        /// <summary>导出路径提示：data/&lt;ns&gt;/tags/&lt;目录&gt;/&lt;名字&gt;.json</summary>
        [ObservableProperty]
        private string targetPath;
        #endregion

        #region Method
        public TagViewModel(IContainerProvider Container, DataService DataService, MainView mainView)
        {
            home = mainView;
            container = Container;
            dataService = DataService;
            UpdateTargetPath();
        }

        partial void OnSelectedVersionChanged(VectorTextComboBoxItem value)
        {
            reloadSuspended = true;
            try
            {
                RebuildTagRegistryList();
            }
            finally
            {
                reloadSuspended = false;
            }
            UpdateTargetPath();
            ReloadCandidates();
        }

        partial void OnSelectedTagRegistryChanged(VectorTextComboBoxItem value)
        {
            if (reloadSuspended)
            {
                return;
            }
            UpdateTargetPath();
            ReloadCandidates();
        }

        partial void OnNamespaceTextChanged(string value) => UpdateTargetPath();

        partial void OnTagNameChanged(string value) => UpdateTargetPath();

        /// <summary>把所选版本里出现过的注册表名灌进 tag 目录下拉；默认挑 item（最常用的 tag 目录）。</summary>
        private void RebuildTagRegistryList()
        {
            string version = SelectedVersion?.Text;
            IReadOnlyList<string> names = string.IsNullOrEmpty(version) ? [] : dataService.GetRegistryNames(version);
            TagRegistryList = [.. names.Select(name => new VectorTextComboBoxItem { ItemID = name, Text = name })];
            SelectedTagRegistry = TagRegistryList.FirstOrDefault(item => item.Text == "item") ?? TagRegistryList.FirstOrDefault();
        }

        /// <summary>按"所选版本 + 所选 tag 目录"重建候选。整表替换，不做逐项增删。</summary>
        private void ReloadCandidates()
        {
            string version = SelectedVersion?.Text;
            string registryName = SelectedTagRegistry?.Text;
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(registryName))
            {
                TagItemList = [];
                return;
            }

            TagItemList = [.. dataService.GetIds(registryName, version).Select(id => new TagItemTemplate
            {
                DisplayId = id,
                DisplayName = string.Empty,
                DataType = registryName,
                Icon = ResolveIcon(id)
            })];
            TagViewSource?.View?.Refresh();
        }

        private void UpdateTargetPath()
        {
            string namespaceValue = string.IsNullOrWhiteSpace(NamespaceText) ? "<命名空间>" : NamespaceText.Trim();
            string registryText = SelectedTagRegistry?.Text ?? "<标签目录>";
            string name = string.IsNullOrWhiteSpace(TagName) ? "<标签名>" : TagName.Trim();
            TargetPath = "data/" + namespaceValue + "/tags/" + registryText + "/" + name + ".json";
        }

        /// <summary>
        /// 列表里的图标：仓里贴图是平铺的（`ImageSet\&lt;id&gt;.png`），先试平铺再试命名空间分层。
        /// 找不到就留空 —— 列表行有定宽图标列，不要因为缺贴图让文字左右跳。
        /// </summary>
        private static Uri ResolveIcon(string id)
        {
            string imageSet = AppDomain.CurrentDomain.BaseDirectory + "ImageSet" + Path.DirectorySeparatorChar;
            string bare = id.Contains(':') ? id[(id.IndexOf(':') + 1)..] : id;
            string[] candidates =
            [
                imageSet + bare + ".png",
                imageSet + bare + "_spawn_egg.png",
                imageSet + id.Replace(':', Path.DirectorySeparatorChar) + ".png"
            ];
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return new Uri(candidate, UriKind.RelativeOrAbsolute);
                }
            }
            return null;
        }

        /// <summary>把裸 id 补成 `ns:path`；tag 引用走 `#ns:path`。</summary>
        private string Qualify(string raw, bool asTag)
        {
            string value = (raw ?? string.Empty).Trim().Trim('"');
            if (value.Length == 0)
            {
                return value;
            }
            if (asTag && value.StartsWith('#'))
            {
                value = value[1..].Trim();
            }
            if (!value.Contains(':'))
            {
                string namespaceValue = string.IsNullOrWhiteSpace(NamespaceText) ? DefaultNamespace : NamespaceText.Trim();
                value = namespaceValue + ":" + value;
            }
            return asTag ? "#" + value : value;
        }

        private static string StripNamespace(string id)
        {
            string value = (id ?? string.Empty).Trim();
            int separator = value.IndexOf(':');
            return separator < 0 ? value : value[(separator + 1)..];
        }

        /// <summary>
        /// 拼 values：勾选项 + 附加条目，按最终 JSON 形态去重。
        /// 三种形态都落在这一个出口上：`"ns:id"` / `"#ns:tag"` / `{"id":…,"required":false}`。
        /// </summary>
        private bool TryBuildValues(out JArray values, out string error)
        {
            JArray built = [];
            values = built;
            error = null;
            HashSet<string> seen = new(StringComparer.Ordinal);

            void Add(JToken token)
            {
                if (seen.Add(token.ToString(Formatting.None)))
                {
                    built.Add(token);
                }
            }

            foreach (TagItemTemplate item in TagItemList)
            {
                if (item.BeChecked != true || string.IsNullOrWhiteSpace(item.DisplayId))
                {
                    continue;
                }
                string id = Qualify(item.DisplayId, asTag: false);
                if (!UseObjectForm)
                {
                    Add(new JValue(id));
                    continue;
                }
                Add(new JObject { ["id"] = id });
            }

            foreach (string rawLine in (ExtraValuesText ?? string.Empty).Split('\n'))
            {
                string line = rawLine.Trim().TrimEnd(',');
                if (line.Length == 0)
                {
                    continue;
                }
                if (line.StartsWith('{'))
                {
                    try
                    {
                        Add(JObject.Parse(line));
                    }
                    catch
                    {
                        error = "附加条目不是合法的 JSON 对象：" + line;
                        return false;
                    }
                    continue;
                }
                if (line.StartsWith('#'))
                {
                    Add(new JValue(Qualify(line, asTag: true)));
                    continue;
                }
                string qualified = Qualify(line, asTag: false);
                Add(UseObjectForm ? new JObject { ["id"] = qualified } : new JValue(qualified));
            }

            if (values.Count == 0)
            {
                error = "没有勾选任何成员，也没有附加条目";
                return false;
            }
            return true;
        }

        private void PushMessage(string message, string subMessage, string iconFileName)
        {
            messagePopup.PushMessage(new GeneratorMessage()
            {
                Message = message,
                MessageBrush = Brushes.Red,
                SubMessage = subMessage,
                SubMessageBrush = Brushes.DarkGray,
                Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + iconFileName, UriKind.RelativeOrAbsolute))
            });
        }

        /// <summary>当前视图里真正可见的成员（全选 / 反选只作用于过滤后的行）。</summary>
        private IEnumerable<TagItemTemplate> VisibleItems()
            => TagViewSource?.View is { } view ? view.Cast<TagItemTemplate>() : TagItemList;

        /// <summary>反转目标成员的值</summary>
        private void ReverseValue(TagItemTemplate currentItem)
        {
            if (currentItem is null || string.IsNullOrWhiteSpace(currentItem.DisplayId))
            {
                return;
            }
            currentItem.BeChecked = !(currentItem.BeChecked ?? false);
        }
        #endregion

        #region Event
        [RelayCommand]
        /// <summary>从剪切板导入标签数据</summary>
        private void ImportFromClipboard() => ImportTagData(Clipboard.GetText(), messagePopup, isPath: false);

        [RelayCommand]
        /// <summary>从文件导入标签数据</summary>
        private void ImportFromFile()
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Json文件|*.json;",
                AddExtension = true,
                DefaultExt = ".json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                RestoreDirectory = true,
                Title = "请选择一个标签文件"
            };
            if (dialog.ShowDialog() == true && File.Exists(dialog.FileName))
            {
                ImportTagData(dialog.FileName, messagePopup);
            }
        }

        /// <summary>按搜索词过滤成员（目录已经是单选，不再按数据类型过滤）。</summary>
        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not TagItemTemplate item)
            {
                return;
            }
            string search = SearchText?.Trim();
            e.Accepted = string.IsNullOrEmpty(search)
                || (item.DisplayId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        /// <summary>载入版本与 tag 目录，并挑一个默认目录建候选。</summary>
        public void TagView_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                UpdateTargetPath();
                return;
            }
            loaded = true;

            VersionList = [.. dataService.GetVersionList()
                .Reverse()
                .Select(version => new VectorTextComboBoxItem { ItemID = version, Text = version })];
            if (VersionList.Count == 0)
            {
                PushMessage("没有读到注册表数据", "Resource\\Registries 下没有可用的 *.json，无法枚举版本与候选", "name_tag.png");
                UpdateTargetPath();
                return;
            }

            SelectedVersion = VersionList[0];
        }

        /// <summary>背包视图载入事件</summary>
        public void ListViewLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ListView listView)
            {
                return;
            }
            TagZone = listView;
            if (listView.Parent is FrameworkElement parent)
            {
                TagViewSource = parent.FindResource("TagItemSource") as CollectionViewSource;
            }
            if (TagViewSource is not null)
            {
                TagViewSource.Filter -= CollectionViewSource_Filter;
                TagViewSource.Filter += CollectionViewSource_Filter;
                TagViewSource.View?.Refresh();
            }
        }

        [RelayCommand]
        private void Return(Window win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        [RelayCommand]
        /// <summary>执行生成</summary>
        private void Run()
        {
            if (!TryBuildValues(out JArray values, out string error))
            {
                PushMessage("生成失败", error, "name_tag.png");
                return;
            }

            JObject root = new()
            {
                ["replace"] = Replace,
                ["values"] = values
            };
            string result = root.ToString(Formatting.Indented);

            SaveFileDialog saveFileDialog = new()
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = "json",
                Filter = "JSON files (*.json;)|*.json;",
                FileName = string.IsNullOrWhiteSpace(TagName) ? "tag.json" : TagName.Trim() + ".json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Title = "保存为JSON文件"
            };
            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            string directory = Path.GetDirectoryName(saveFileDialog.FileName);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(saveFileDialog.FileName, result);
            PushMessage("标签生成成功！", "应放到 " + TargetPath, "firework_rocket.png");
        }

        /// <summary>左击选中成员</summary>
        public void ListBoxClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not Border)
            {
                LastSelectedIndex = TagZone?.SelectedIndex ?? 0;
                SelectedItem = null;
            }
            else if (SelectedItem is null && TagZone is not null && LastSelectedIndex > 0 && LastSelectedIndex < TagZone.Items.Count)
            {
                SelectedItem = TagZone.Items[LastSelectedIndex] as TagItemTemplate;
            }

            if (SelectedItem is not null)
            {
                ReverseValue(SelectedItem);
            }
        }

        /// <summary>更新搜索结果视图</summary>
        public void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => TagViewSource?.View?.Refresh();

        /// <summary>全选（只作用于当前过滤后可见的行）</summary>
        public void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (TagItemTemplate item in VisibleItems().ToList())
            {
                if (string.IsNullOrWhiteSpace(item.DisplayId))
                {
                    continue;
                }
                item.BeChecked = SelectedAll;
            }
        }

        /// <summary>反选（只作用于当前过滤后可见的行）</summary>
        public void ReverseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (TagItemTemplate item in VisibleItems().ToList())
            {
                if (string.IsNullOrWhiteSpace(item.DisplayId))
                {
                    continue;
                }
                item.BeChecked = !(item.BeChecked ?? false);
            }
        }
        #endregion

        #region 外部数据导入
        /// <summary>
        /// 导入一份已有 tag：`replace` 直接回填，能对上当前目录的字符串项勾上，
        /// 对不上的（`#tag` 引用、对象形态）原样进"附加条目"，导出时不会丢。
        /// </summary>
        public void ImportTagData(string filePathOrData, MessagePopup messagePopup, bool isPath = true)
        {
            try
            {
                string data = isPath ? File.ReadAllText(filePathOrData) : filePathOrData;
                JObject content = JObject.Parse(data);
                if (content["replace"] is JValue replaceValue && replaceValue.Value is bool replace)
                {
                    Replace = replace;
                }

                HashSet<string> ids = new(StringComparer.Ordinal);
                List<string> extras = [];
                if (content["values"] is JArray valuesArray)
                {
                    foreach (JToken token in valuesArray)
                    {
                        if (token is JValue { Value: string text })
                        {
                            if (text.StartsWith('#'))
                            {
                                extras.Add(text);
                            }
                            else
                            {
                                ids.Add(StripNamespace(text));
                            }
                            continue;
                        }
                        extras.Add(token.ToString(Formatting.None));
                    }
                }

                foreach (TagItemTemplate item in TagItemList)
                {
                    item.BeChecked = ids.Contains(StripNamespace(item.DisplayId));
                }
                ExtraValuesText = string.Join("\r\n", extras);
                TagViewSource?.View?.Refresh();
            }
            catch
            {
                messagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = "导入失败！文件内容格式不合法",
                    MessageBrush = Brushes.Red,
                    SubMessage = "标签生成器",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\ImageSet\name_tag.png"))
                });
            }
        }
        #endregion
    }
}
