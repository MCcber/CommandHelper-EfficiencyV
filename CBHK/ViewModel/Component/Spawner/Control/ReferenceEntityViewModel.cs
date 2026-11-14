using CBHK.Model.Data;
using CBHK.Utility.Common;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View.Component.Entity;
using CBHK.View.Generator;
using CBHK.ViewModel.Component.Entity;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Spawner.Control
{
    /// <summary>
    /// 引用实体槽（SpawnerPageView 里的 entity）的全部业务：NBT 文本、图标、剪贴板/文件导入、生成。
    /// 视图侧只剩 InitializeComponent，事件改成命令绑定。
    /// </summary>
    public partial class ReferenceEntityViewModel : ObservableObject
    {
        private const string DefaultTag = "{id:\"minecraft:pig\"}";

        private readonly MessagePopup messagePopup = new();

        /// <summary>实体 NBT 文本；空串 = "设置为空"。</summary>
        [ObservableProperty]
        private string tag = DefaultTag;

        /// <summary>图标（视图里 Border.Background 直接绑它）。</summary>
        [ObservableProperty]
        private Brush icon;

        public ReferenceEntityViewModel() => ApplyTag(DefaultTag, reportFailure: false);

        [RelayCommand]
        private void SetEmpty()
        {
            Tag = "";
            Icon = null;
        }

        [RelayCommand]
        private void ImportFromClipboard()
            => ImportText(Clipboard.GetText(), "导入失败！剪切板内容与实体无关或内容有误");

        [RelayCommand]
        private void ImportFromFile()
        {
            OpenFileDialog dialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                RestoreDirectory = true,
                Title = "请选择一个实体文件",
            };
            if (dialog.ShowDialog() != true) return;
            ImportText(ExternalCommandParser.Read(dialog.FileName, true), "导入失败！文件内容与实体无关或内容有误");
        }

        /// <summary>导入一段命令/数据：解析出实体 NBT 就回填，否则保留原状并提示。</summary>
        private void ImportText(string text, string failureMessage)
        {
            if (!ExternalCommandParser.TryGetEntityNbt(text, out var nbt, out _, out _) || string.IsNullOrWhiteSpace(nbt))
            {
                Report(failureMessage);
                return;
            }
            ApplyTag(nbt, reportFailure: true, failureMessage);
        }

        /// <summary>把 NBT 文本落到 Tag + 图标上；解析不出 id 时只保留文本、不换图标。</summary>
        private void ApplyTag(string nbt, bool reportFailure, string failureMessage = null)
        {
            Tag = nbt ?? "";
            var id = EntityIdOf(Tag);
            if (id.Length == 0)
            {
                if (reportFailure) Report(failureMessage ?? "导入失败！内容里没有实体 id");
                return;
            }
            Icon = IconFor(id);
        }

        /// <summary>实体 id：id → itemView.id（刷怪蛋写法），去掉命名空间、引号与 _spawn_egg 后缀。</summary>
        private static string EntityIdOf(string nbt)
        {
            if (string.IsNullOrWhiteSpace(nbt)) return "";
            try
            {
                var json = JObject.Parse(nbt);
                // 刷怪笼导入的 SpawnData.entity 用的是 oldID，实体命令用 id，刷怪蛋用 itemView.id
                var token = json.SelectToken("id") ?? json.SelectToken("oldID") ?? json.SelectToken("itemView.id");
                return token is null ? "" : NormalizeEntityId(token.ToString());
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string NormalizeEntityId(string id)
            => (id ?? "").Replace("minecraft:", "").Replace("\"", "")
                .Replace("_spawn_egg:", "").Replace("_spawn_egg", "").Trim();

        /// <summary>
        /// 外部（刷怪笼导入）灌实体数据：唯一入口，内部走和手工导入同一条落 Tag + 找图标的路径。
        /// 以前是窗口 VM 直接给控件赋 Tag/Background，控件与 ViewModel 各存一份。
        /// </summary>
        public void ApplyEntityData(string nbt) => ApplyTag(nbt, reportFailure: false);

        /// <summary>实体图标：ImageSet 与 data_sourcesentityImages 两处都看，&lt;id&gt;.png 优先于 &lt;id&gt;_spawn_egg.png。</summary>
        private static Brush IconFor(string entityId)
        {
            var roots = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageSet"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "data_sources", "entityImages"),
            };
            foreach (var root in roots)
            {
                foreach (var name in new[] { entityId + ".png", entityId + "_spawn_egg.png" })
                {
                    var brush = LoadBrush(Path.Combine(root, name));
                    if (brush is not null) return brush;
                }
            }
            return null;
        }

        /// <summary>读一张 png 当画刷；文件不存在或图坏了返回 null。</summary>
        private static Brush LoadBrush(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return new ImageBrush(bitmap);
            }
            catch (Exception)
            {
                return null;   // 坏图当没有图标，不抛
            }
        }

        private void Report(string message)
        {
            try
            {
                messagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = message,
                    MessageBrush = Brushes.Red,
                    SubMessage = "引用实体",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = (IconFor("barrier") as ImageBrush)?.ImageSource,   // GeneratorMessage.Icon 是 ImageSource，不是 Brush
                });
            }
            catch (Exception)
            {
                // 没有挂到窗口上的 MessagePopup 本身会抛：提示失败不该把导入/生成流程一起带崩
            }
        }
    }
}
