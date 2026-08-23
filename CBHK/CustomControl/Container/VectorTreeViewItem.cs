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
                //自定义节点按下回车事件
                if(headerDTO.TypeKind is MetaTypeKind.Definition)
                {
                    vectorTreeViewItem.PreviewKeyDown += (s, e) =>
                    {
                        if(e.Key is System.Windows.Input.Key.Enter && vectorTreeViewItem.Header is MetaTypeEditorFieldDTO dto)
                        {
                            dto.DefinitionEnterKeyDown?.Invoke();
                        }
                    };
                }
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