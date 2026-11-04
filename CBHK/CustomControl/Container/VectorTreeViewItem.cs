using CBHK.Utility.Data.Mcdoc;
using MinecraftLanguageModelLibrary.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorTreeViewItem : TreeViewItem
    {
        #region Property
        //public object Content { get; set; }
        //public TreeViewItemCollection<BaseTreeViewDataItem> Children { get; set; }
        public Visibility HorizontalTopLineVisibility
        {
            get { return (Visibility)GetValue(HorizontalTopLineVisibilityProperty); }
            set { SetValue(HorizontalTopLineVisibilityProperty, value); }
        }

        public static readonly DependencyProperty HorizontalTopLineVisibilityProperty =
            DependencyProperty.Register(nameof(HorizontalTopLineVisibility), typeof(Visibility), typeof(VectorTreeViewItem), new PropertyMetadata(default(Visibility)));

        public Visibility HorizontalBottomLineVisibility
        {
            get { return (Visibility)GetValue(HorizontalBottomLineVisibilityProperty); }
            set { SetValue(HorizontalBottomLineVisibilityProperty, value); }
        }

        public static readonly DependencyProperty HorizontalBottomLineVisibilityProperty =
            DependencyProperty.Register(nameof(HorizontalBottomLineVisibility), typeof(Visibility), typeof(VectorTreeViewItem), new PropertyMetadata(default(Visibility)));

        public Brush ConnectingLineBrush
        {
            get { return (Brush)GetValue(ConnectingLineBrushProperty); }
            set { SetValue(ConnectingLineBrushProperty, value); }
        }

        public static readonly DependencyProperty ConnectingLineBrushProperty =
            DependencyProperty.Register("ConnectingLineBrush", typeof(Brush), typeof(VectorTreeViewItem), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTreeViewItem()
        {
            Loaded += VectorTreeViewItem_Loaded;
            // 展开/折叠写回数据项，容器回收后也能恢复
            Expanded += (_, _) => SyncExpansion(true);
            Collapsed += (_, _) => SyncExpansion(false);
        }

        private void SyncExpansion(bool expanded)
        {
            if (Header is McdocTreeNode node)
            {
                node.IsExpanded = expanded;
            }
        }

        #endregion

        #region Event
        private void VectorTreeViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            var connectingLineBrushSource = DependencyPropertyHelper.GetValueSource(this, ConnectingLineBrushProperty);
            if (connectingLineBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || connectingLineBrushSource.BaseValueSource is BaseValueSource.Style || ConnectingLineBrush is null)
            {
                ConnectingLineBrush = new BrushConverter().ConvertFromString("#686868") as Brush;
            }
            HorizontalBottomLineVisibility = HorizontalTopLineVisibility = Visibility.Hidden;

            //自动展开必选项
            if (sender is VectorTreeViewItem vectorTreeViewItem && vectorTreeViewItem.Header is MetaTypeEditorFieldDTO headerDTO)
            {
                //返回VectorTreeViewItem
                if (headerDTO.IsRequired && (headerDTO.Children?.Count > 0 || headerDTO.SelectedUnionChildren?.Count > 0))
                {
                    IsExpanded = true;
                }
                if(headerDTO.TypeKind is MetaTypeKind.Dispatch && string.IsNullOrEmpty(headerDTO.FieldName) && !headerDTO.IsVisible)
                {
                    vectorTreeViewItem.Visibility = Visibility.Collapsed;
                }
                //定义键已并入 Tuple 管线：动态 map 的键由"输入框 + 加号"负责，加号里直接解析值类型，
                //不再产出 Definition 节点，所以这里原先绑定的回车提交事件一并去掉
            }
            //新求值管线的视图节点：只保留"必选有子级则自动展开"这一条 UX 规则。
            //旧分支里的 IsVisible 判断不需要复刻 —— 新管线在求值期就把 #[since]/#[until] 与不可见调度器过滤掉了
            else if (sender is VectorTreeViewItem item && item.Header is McdocTreeNode mdNode)
            {
                // 还原用户上次的展开状态；没有状态时按旧管线规则"必选且有子级就展开"。
                // 不能只靠样式 Setter 里的 IsExpanded 绑定：用户点击写的是本地值，会盖掉那条绑定，
                // 源节点永远收不到更新 —— 容器一旦回收就回落成 False（表现就是"展开后立刻被收回"）。
                var hasChildren = mdNode.Children?.Count > 0 || mdNode.SelectedUnionChildren?.Count > 0;
                IsExpanded = hasChildren && (mdNode.IsExpanded || mdNode.IsRequired);
                mdNode.IsExpanded = IsExpanded;
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VectorTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is VectorTreeViewItem;
        } 
        #endregion
    }
}