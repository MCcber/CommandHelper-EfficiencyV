using cbhk.CustomControls.JsonTreeViewComponents;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace cbhk.Generators.CustomWorldGenerators
{
    public class CustomWorldBaseWindowDataContext:ObservableObject
    {
        #region Field
        public Window home = null;
        public string InitJson = "";
        public TextEditor editor = null;
        private CancellationTokenSource cancellationTokenSource = new();
        private string PresetDataDirectory = AppDomain.CurrentDomain.BaseDirectory + @"resources\configs";
        private string TitleBaseString = "自定义世界生成-";
        public enum CustomWorldGeneratorTypes
        {
            Biome,
            ConfiguredCarver,
            ConfiguredSurfaceBuilder,
            DensityFunction,
            FlatWorldPreset,
            Feature,
            WorldPreset,
            Noise,
            NoiseSettings,
            PlaceFeature,
            ProcessorList,
            Structures,
            StructureSet,
            TemplatePool
        }
        #endregion

        #region Property
        public ObservableCollection<JsonTreeViewItem> CustomWorldItemsSource { get; set; } = [];

        private string jsonData;

		public string JsonData
        {
            get => jsonData;

            set => SetProperty(ref jsonData, value);
		}
        private CustomWorldGeneratorTypes generatorType = CustomWorldGeneratorTypes.WorldPreset;

        public CustomWorldGeneratorTypes GeneratorType
        {
            get => generatorType;
            set => SetProperty(ref generatorType, value);
        }

        private string titleString;

        public string TitleString
        {
            get => titleString;
            set => SetProperty(ref titleString, value);
        }
        #endregion

        public CustomWorldBaseWindowDataContext()
        {
        }

        /// <summary>
        /// 代码编辑器载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            editor = sender as TextEditor;
            if (Directory.Exists(PresetDataDirectory))
            {
                string templatesDirectory = PresetDataDirectory + "\\" + GeneratorType.ToString() + @"\data\Templates";
                foreach (var item in Directory.GetFileSystemEntries(templatesDirectory))
                {
                    if (File.Exists(item) && item.EndsWith("json"))
                    {
                        editor.Text = File.ReadAllText(item);
                        TitleString = TitleBaseString + Path.GetFileNameWithoutExtension(item);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 扫描更大范围的Json文本
        /// </summary>
        private string ScanBiggerRangeJsonText()
        {
            StringBuilder result = new();

            int LeftIndex = editor.TextArea.Caret.Offset;
            int RightIndex = editor.TextArea.Caret.Offset;
            while (LeftIndex > 0)
            {
                LeftIndex--;
                editor.Document.GetText(LeftIndex,1);
            }
            return result.ToString();
        }

        /// <summary>
        /// Json更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void TextEditor_TextChanged(object sender,EventArgs e)
        {
            if (!cancellationTokenSource.IsCancellationRequested)
                cancellationTokenSource.Cancel();

            cancellationTokenSource = new();

            await Task.Delay(500);

            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    JsonData = editor.Text;
                    JArray jsonArray = null;
                    JObject jsonObject = null;
                    if (JsonData.TrimStart().StartsWith('['))
                        jsonArray = JArray.Parse(JsonData);
                    if (JsonData.TrimStart().StartsWith('{'))
                        jsonObject = JObject.Parse(JsonData);
                });
            }, cancellationTokenSource.Token);
        }
    }
}
