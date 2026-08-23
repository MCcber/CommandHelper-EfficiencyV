using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Data;
using CBHK.Utility.Visual;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CBHK.ViewModel.Generator
{
    public partial class AdvancementViewModel : ObservableObject
    {
        #region Field
        private Resource resource;
        private MetaTypeDTOValidator validator;
        private MCDocumentMetaTypeDTOHelper dtoHelper;
        private RapidJsonDataParser parser = new();
        private TextEditor textEditor = null;
        private FoldingManager foldingManager = null;
        private ValueTuple<string, int, List<KeyValueAnchors>> jsonParseResultMap = new();
        private IProgress<string> initReporter = null;
        private const string targetDispatchName = "Advancement";
        private const string targetDispatchPath = "::java::data::advancement::Advancement";
        //private const string targetDispatchPath = "::java::data::advancement::predicate::EntityPredicate";
        #endregion

        #region Property
        [ObservableProperty]
        private VectorTextComboBoxItem _currentVersion;

        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> versionList =
        [
            new VectorTextComboBoxItem()
            {
                Text = "1.20.5-1.20.6"
            }
        ];

        [ObservableProperty]
        private ObservableCollection<MetaTypeEditorFieldDTO> metaTypeDTOTreeViewItemList = [];

        [ObservableProperty]
        private string viewName = "进度编辑器";
        #endregion

        #region Method
        public AdvancementViewModel(MetaTypeDTOValidator metaTypeDTOValidator, MCDocumentMetaTypeDTOHelper documentMetaTypeDTOHelper,Resource resource)
        {
            validator = metaTypeDTOValidator;
            dtoHelper = documentMetaTypeDTOHelper;
            this.resource = resource;
            dtoHelper.Validator = validator;
            validator.DTOHelper = dtoHelper;
            validator.Registry = DocumentDTOBuildStrategyRegistry.Create(resource, dtoHelper);
        }
        #endregion

        #region Event
        public async void Advancement_Loaded(object sender, RoutedEventArgs e)
        {
            #region Field
            string templateFilePath = resource.GenertorConfiguration[targetDispatchName.ToLowerInvariant()] + Path.DirectorySeparatorChar + "empty.json";
            string baseFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            string currentMainDirectoryPath = Path.Combine(baseFolderPath + resource.MCDocumentLeadingPath + resource.MCDocumentEditorKey, targetDispatchName.ToLowerInvariant());

            initReporter = new Progress<string>(data =>
            {
                textEditor.Text = data;

                #region 填充Json视图
                jsonParseResultMap = parser.ParseFullText(data, textEditor.Document);
                if (jsonParseResultMap.Item2 > -1)
                {
                    textEditor.TextArea.TextView.BackgroundRenderers.Add(new WaveUnderlineBackgroundRenderer(jsonParseResultMap.Item3[0].ValueStart, jsonParseResultMap.Item3[0].ValueEnd, Brushes.Red));
                }
                #endregion

                #region 根据全文索引构建DTO树并建立当前生成器的调度器资源映射表
                var context = MetaTypeTreeViewDTOBuilder.BuildDTOTree(jsonParseResultMap.Item3);
                context = new([..context.dtoInstanceList[0].Children], context.anchorMap);

                var pair = resource.DocumentItemMap.FirstOrDefault(pair => pair.Value.TypeKind is MetaTypeKind.Dispatch && pair.Key == targetDispatchPath);
                MetaTypeEditorFieldDTO targetDispatchTemplate = pair.Value;

                if (targetDispatchTemplate is not null)
                {
                    validator.Verify(context, [.. targetDispatchTemplate.Children], CurrentVersion.Text, new("::java::data::advancement::Advancement"), true);
                    // 对当前节点执行展平/提升，去除内部可能残留的 Literal、Generic 或单子 Union
                    for (int i = 0; i < context.dtoInstanceList.Count; i++)
                    {
                        dtoHelper.HierarchicallyUpdateTreeStructuredData(context.dtoInstanceList[i], CurrentVersion.Text);
                    }
                }
                MetaTypeDTOTreeViewItemList = new(context.dtoInstanceList);
                #endregion

                //if (resource.DocumentItemMap.TryGetValue("::java::util::text::Text", out MetaTypeEditorFieldDTO javaTemplate))
                //{
                //    var instanceDTO = dtoHelper.InstantiateDTO(javaTemplate, CurrentVersion.Text);
                //    validator.Verify(context, [javaTemplate], CurrentVersion.Text, new("::java::util::text::Text"));
                //    MetaTypeDTOTreeViewItemList = new([instanceDTO]);
                //}
            });
            #endregion

            Task.Run(async () =>
            {
                #region 载入主结构树，然后处理依赖树
                if (File.Exists(baseFolderPath + templateFilePath))
                {
                    string data = await File.ReadAllTextAsync(templateFilePath);
                    //把初始的Json文本和DTO树结果通过调度器传回主线程，更新UI
                    initReporter.Report(data);
                }
                #endregion
            });
        }

        ///<summary>
        ///安装大纲、应用高亮规则
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        public void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            textEditor = sender as TextEditor;
            foldingManager = FoldingManager.Install(textEditor.TextArea);
            XshdSyntaxDefinition xshdSyntaxDefinition = HighlightingLoader.LoadXshd(new System.Xml.XmlTextReader(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\Json.xshd"));
            IHighlightingDefinition jsonHighlighting = HighlightingLoader.Load(xshdSyntaxDefinition, HighlightingManager.Instance);
            textEditor.SyntaxHighlighting = jsonHighlighting;
        }

        ///<summary>
        ///为树视图绑定节点展开事件
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        public void VectorTreeView_Loaded(object sender,RoutedEventArgs e)
        {
            if(sender is VectorTreeView treeView)
            {
                treeView.AddHandler(System.Windows.Controls.TreeViewItem.ExpandedEvent, new RoutedEventHandler(MetaTypeEditorFieldDTOItem_Expanded));
            }
        }

        ///<summary>
        ///处理引用类节点的展开
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        private void MetaTypeEditorFieldDTOItem_Expanded(object sender,RoutedEventArgs e)
        {
            var vectorTreeViewItem = e.OriginalSource as VectorTreeViewItem;
            MetaTypeEditorFieldDTO currentDTO = null;
            if(vectorTreeViewItem is not null && vectorTreeViewItem.Header is not null)
            {
                currentDTO = vectorTreeViewItem.Header as MetaTypeEditorFieldDTO;
            }
            DTOInstanceContext context = null;
            List<MetaTypeEditorFieldDTO> resultDTOList = [];
            //确认展开的节点是否为DTO实例
            if (currentDTO.Children is not null && currentDTO.Children.Count > 0 && currentDTO.Children[0].ID == "placeHolder")
            {
                #region 验证调度器或可选的结构体
                //处理调度器解释后的可选数据
                if (currentDTO.Value is ObservableCollection<MetaTypeEditorFieldDTO> valueList)
                {
                    context = new([..valueList], []);
                    validator.Verify(context, context.dtoInstanceList, CurrentVersion.Text, currentDTO.Path);
                    resultDTOList = [.. context.dtoInstanceList];
                }
                //使用Value的值来查找当前上下文是否有目标资源
                else if (currentDTO.Value is not null && !string.IsNullOrEmpty(currentDTO.Value.ToString()))
                {
                    string currentDocumentItemPath = currentDTO.Path.TargetPath.ToString();
                    var targetPair = resource.DocumentItemMap.FirstOrDefault(pair => pair.Key == currentDocumentItemPath);
                    if (targetPair.Value is MetaTypeEditorFieldDTO targetDTO)
                    {
                        //确认目标资源后再次模板实例化最后执行验证流程，然后把结果列表赋值给当前展开的节点的Children列表
                        MetaTypeEditorFieldDTO instancedDTO = dtoHelper.InstantiateDTO(targetDTO, CurrentVersion.Text);
                        context = new([.. instancedDTO.Children], []);
                        validator.Verify(context, [.. targetDTO.Children], CurrentVersion.Text, new(targetPair.Key), false);
                        resultDTOList = context.dtoInstanceList;
                    }
                }
                #endregion
            }
            else if (!currentDTO.IsRequired && currentDTO.TypeKind is MetaTypeKind.Struct && currentDTO.Children?.Count > 0)
            {
                for (int i = 0; i < currentDTO.Children.Count; i++)
                {
                    if (MCDocumentMetaTypeDTOHelper.IsIndirectType(currentDTO.Children[i].TypeKind) && currentDTO.Children[i].TypeKind is not MetaTypeKind.Union)
                    {
                        currentDTO.Children[i].Path ??= new(currentDTO.Path.TargetPath);
                        context = new([currentDTO.Children[i]], []);
                        validator.Verify(context, context.dtoInstanceList, CurrentVersion.Text, currentDTO.Children[i].Path);
                        currentDTO.Children[0] = context.dtoInstanceList[0];
                    }
                }
            }

            #region 剥壳并执行后处理
            if (resultDTOList?.Count > 0 && currentDTO?.Children?.Count > 0)
            {
                currentDTO.Children.Clear();
                string maxVersion = CurrentVersion.Text;
                if (maxVersion.Contains('-'))
                {
                    maxVersion = maxVersion.Split('-')[1];
                }
                //剥壳、设置父子关系、更新路径
                for (int i = 0; i < resultDTOList.Count; i++)
                {
                    dtoHelper.HierarchicallyUpdateTreeStructuredData(resultDTOList[i], maxVersion);
                    currentDTO.Children.Add(resultDTOList[i]);
                    resultDTOList[i].Path = currentDTO.Path;
                    resultDTOList[i].Parent = currentDTO;
                }
                //对必选的枚举类节点执行SelectedEnumItemUpdated操作，确保其子节点被正确实例化
                for (int i = 0; i < resultDTOList.Count; i++)
                {
                    //给必选的枚举类节点执行一次SelectedEnumItemUpdated操作，确保其子节点被正确实例化
                    if (resultDTOList[i].TypeKind is MetaTypeKind.Enum && resultDTOList[i].IsRequired)
                    {
                        dtoHelper.SelectedEnumItemUpdated(resultDTOList[i], maxVersion);
                    }
                }
            }
            //没有则添加报错节点
            else if (currentDTO.Value is not null && !string.IsNullOrEmpty(currentDTO.Value.ToString()))
            {
                currentDTO.Children.Clear();
                currentDTO.Children.AddRange([new() { ID = "", Path = null, TypeKind = MetaTypeKind.Any, FieldName = "Can't find target structure" }]);
            }
            #endregion
        }
        #endregion
    }
}