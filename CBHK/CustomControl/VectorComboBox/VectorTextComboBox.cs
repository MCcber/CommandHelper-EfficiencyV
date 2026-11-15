using CBHK.Model.Constant;
using CBHK.Utility.Common;
using CBHK.Utility.Data;
using CBHK.Utility.Visual;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CBHK.CustomControl.VectorComboBox
{
    public partial class VectorTextComboBox : ComboBox
    {
        #region Field
        private CollectionViewSource itemView = new();
        /// <summary>模板里的弹层：判断"焦点还在弹层里"要靠它，Popup 内容在独立窗口里，不是本控件的可视子树。</summary>
        private Popup popup;
        #endregion

        #region Property
        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(VectorTextComboBox),
                new PropertyMetadata(default(string), (d, _) => ((VectorTextComboBox)d).OnSearchTextChanged()));

        /// <summary>搜索词一变就重过滤：以前只在回车时 Refresh，等于"边敲边搜"根本没实现。</summary>
        private void OnSearchTextChanged()
        {
            RefreshActiveView();
            UpdateSearchBoxVisibility();
        }

        public Visibility SearchBoxVisibility
        {
            get { return (Visibility)GetValue(SearchBoxVisibilityProperty); }
            set { SetValue(SearchBoxVisibilityProperty, value); }
        }

        public static readonly DependencyProperty SearchBoxVisibilityProperty =
            DependencyProperty.Register("SearchBoxVisibility", typeof(Visibility), typeof(VectorTextComboBox), new PropertyMetadata(default(Visibility)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VectorTextComboBox), new PropertyMetadata(default(string)));

        public Thickness ArrowMargin
        {
            get { return (Thickness)GetValue(ArrowMarginProperty); }
            set { SetValue(ArrowMarginProperty, value); }
        }

        public static readonly DependencyProperty ArrowMarginProperty =
            DependencyProperty.Register("ArrowMargin", typeof(Thickness), typeof(VectorTextComboBox), new PropertyMetadata(default(Thickness)));

        public Brush ThemeTitleBorderBackground
        {
            get { return (Brush)GetValue(ThemeTitleBorderBackgroundProperty); }
            set { SetValue(ThemeTitleBorderBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeTitleBorderBackgroundProperty =
            DependencyProperty.Register("ThemeTitleBorderBackground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleBorderBackground
        {
            get { return (Brush)GetValue(TitleBorderBackgroundProperty); }
            set { SetValue(TitleBorderBackgroundProperty, value); }
        }

        public static readonly DependencyProperty TitleBorderBackgroundProperty =
            DependencyProperty.Register("TitleBorderBackground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleLeftTopBorderBrush
        {
            get { return (Brush)GetValue(TitleLeftTopBorderBrushProperty); }
            set { SetValue(TitleLeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleLeftTopBorderBrushProperty =
            DependencyProperty.Register("TitleLeftTopBorderBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleRightBottomBorderBrush
        {
            get { return (Brush)GetValue(TitleRightBottomBorderBrushProperty); }
            set { SetValue(TitleRightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleRightBottomBorderBrushProperty =
            DependencyProperty.Register("TitleRightBottomBorderBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleBorderCornerBrush
        {
            get { return (Brush)GetValue(TitleBorderCornerBrushProperty); }
            set { SetValue(TitleBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleBorderCornerBrushProperty =
            DependencyProperty.Register("TitleBorderCornerBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush ArrowBrush
        {
            get { return (Brush)GetValue(ArrowBrushProperty); }
            set { SetValue(ArrowBrushProperty, value); }
        }

        public static readonly DependencyProperty ArrowBrushProperty =
            DependencyProperty.Register("ArrowBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleBrush
        {
            get { return (Brush)GetValue(TitleBrushProperty); }
            set { SetValue(TitleBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleBrushProperty =
            DependencyProperty.Register("TitleBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush SearchBoxForeground
        {
            get { return (Brush)GetValue(SearchBoxForegroundProperty); }
            set { SetValue(SearchBoxForegroundProperty, value); }
        }

        public static readonly DependencyProperty SearchBoxForegroundProperty =
            DependencyProperty.Register("SearchBoxForeground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush PopupItemPanelBackground
        {
            get { return (Brush)GetValue(PopupItemPanelBackgroundProperty); }
            set { SetValue(PopupItemPanelBackgroundProperty, value); }
        }

        public static readonly DependencyProperty PopupItemPanelBackgroundProperty =
            DependencyProperty.Register("PopupItemPanelBackground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTextComboBox()
        {
            BorderBrush = Brushes.Black;
            Loaded += VectorTextComboBox_Loaded;
            DropDownOpened += VectorTextComboBox_DropDownOpened;
            DropDownClosed += VectorTextComboBox_DropDownClosed;
            itemView.Filter += ItemView_Filter;
            AddHandler(Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnAnyLostKeyboardFocus), true);
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeTitleBorderBackground is SolidColorBrush solidColorBrush)
            {
                BorderBrush = new SolidColorBrush(ColorTool.Darken(solidColorBrush.Color,0.6f));
                TitleBorderBackground = new SolidColorBrush(solidColorBrush.Color);
                TitleLeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.2f));
                TitleRightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.2f));
                TitleBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.4f));
            }
        }
        #endregion

        #region Event
        private void VectorTextComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            SetResourceReference(ThemeTitleBorderBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(TitleBrushProperty,Theme.CommonForeground);
            SetResourceReference(ArrowBrushProperty, Theme.CommonForeground);
            SetResourceReference(SearchBoxForegroundProperty,Theme.CommonForeground);
            SetResourceReference(PopupItemPanelBackgroundProperty,Theme.CommonBackground);

            UpdateBorderColorByBackgroundColor();

            // 有绑定的 ItemsSource 绝对不许覆盖：赋值会给 DP 写本地值，绑定当场被拆掉，
            // 之后视图模型换掉集合（换版本/换目录）下拉就永远停在最早那一批，搜索也搜的是旧数据。
            // 这种情况把过滤交给 OnItemsSourceChanged（挂到源集合的默认视图上）。
            if (BindingOperations.GetBindingExpression(this, ItemsSourceProperty) is not null)
            {
                UpdateSearchBoxVisibility();
                return;
            }

            // Loaded 可能多次触发（控件重新挂载/换父级时），第二次 ItemsSource 已经是 itemView.View，
            // 直接跳过避免重复包装（重复包装会让 ItemsSource 变成视图再赋给 Source 而抛异常，也会破坏选择）。
            if (ReferenceEquals(ItemsSource, itemView.View))
            {
                UpdateSearchBoxVisibility();
                return;
            }

            // 将 ItemsSource 包装进 itemView，以便搜索框按 SearchText 过滤
            if (ItemsSource is not null)
            {
                itemView.Source = ItemsSource;
                ItemsSource = itemView.View;
            }
            UpdateSearchBoxVisibility();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            popup = GetTemplateChild("PART_Popup") as Popup;
            AttachPopupFocusHandler();
            UpdateSearchBoxVisibility();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeTitleBorderBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VectorComboBoxItemContainer();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is VectorComboBoxItemContainer;
        }

        private void ItemView_Filter(object sender, FilterEventArgs e)
        {
            string text = e.Item switch
            {
                VectorTextComboBoxItem v => v.Text,
                string s => s,
                _ => e.Item?.ToString() ?? ""
            };
            e.Accepted = StringTool.IsMatchSearchText(text, SearchText);
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            // 当外部 XAML 绑定设置 ItemsSource 时，将过滤器挂到 WPF 自动创建的 CollectionView 上
            if (newValue is not null && newValue != itemView.View)
            {
                var defaultView = CollectionViewSource.GetDefaultView(newValue);
                if (defaultView is not null)
                {
                    defaultView.Filter = item =>
                    {
                        string text = item switch
                        {
                            VectorTextComboBoxItem v => v.Text,
                            string s => s,
                            _ => item?.ToString() ?? ""
                        };
                        return StringTool.IsMatchSearchText(text, SearchText);
                    };
                    defaultView.Refresh();
                }
            }
            UpdateSearchBoxVisibility();
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            UpdateSearchBoxVisibility();
        }

        /// <summary>
        /// 搜索框的可见性只看"源集合有多少条"，不看过滤后剩下几条 ——
        /// 按过滤结果判会让搜索框在敲到只剩 ≤10 条时当场消失（焦点跟着丢，搜索等于被掐断）。
        /// </summary>
        private void UpdateSearchBoxVisibility()
        {
            SearchBoxVisibility = SourceItemCount() > 10 ? Visibility.Visible : Visibility.Collapsed;
        }

        private int SourceItemCount()
        {
            object source = ItemsSource;
            // 尽量退到未被过滤的那一层
            while (source is CollectionView view && view.SourceCollection is not null)
            {
                source = view.SourceCollection;
            }
            return source switch
            {
                ICollection collection => collection.Count,
                IEnumerable enumerable => enumerable.Cast<object>().Count(),
                _ => 0
            };
        }

        /// <summary>刷新当前真正生效的那个视图（itemView 包装路径或绑定路径）。</summary>
        private void RefreshActiveView()
        {
            if (ItemsSource == itemView.View)
            {
                itemView.View?.Refresh();
            }
            else if (ItemsSource is not null)
            {
                CollectionViewSource.GetDefaultView(ItemsSource)?.Refresh();
            }
        }

        private void VectorTextComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            // 查找模板中的按钮
            var toggleButton = FindSomeThingByType.FindVisualChildByName<ToggleButton>(this, "toggleButton");
            if (toggleButton != null)
            {
                // 创建并触发MouseLeave事件
                var args = new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
                {
                    RoutedEvent = MouseLeaveEvent
                };
                toggleButton.RaiseEvent(args);
            }
        }

        /// <summary>搜索框里的回车：只重算结果，绝不收弹层，也不移动焦点。</summary>
        public void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter)
            {
                e.Handled = true;
                IsDropDownOpen = true;
                RefreshActiveView();
            }
        }

        /// <summary>控件自己的回车也压住：基类的"回车提交"会把弹层一起收掉。</summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key is Key.Enter && IsDropDownOpen)
            {
                e.Handled = true;
                RefreshActiveView();
            }
        }

        /// <summary>展开时把焦点送进搜索框：搜索框只在"有焦点且不丢焦点"时才可用。</summary>
        private void VectorTextComboBox_DropDownOpened(object sender, EventArgs e)
        {
            AttachPopupFocusHandler();
            if (SearchBoxVisibility != Visibility.Visible)
            {
                return;
            }
            // 第一次展开时弹层的可视子树可能还没建好，等这一拍过去再找搜索框
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsDropDownOpen)
                {
                    FindSearchBox()?.Focus();
                }
            }), DispatcherPriority.Loaded);
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            // 用户在下方的列表里选中某个成员 → 收弹层（这条是唯一的"选中即收"入口）
            if (IsDropDownOpen && e.AddedItems.Count > 0)
            {
                IsDropDownOpen = false;
            }
        }

        /// <summary>
        /// 关弹层只有一个判据：焦点真的离开了"控件 + 弹层"这一整块。
        /// 鼠标移出搜索框、焦点在搜索框与列表之间挪动，都不算离开。
        /// </summary>
        private void OnAnyLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!IsDropDownOpen)
            {
                return;
            }

            // 焦点还在弹层/控件内部：什么都不做
            if (IsFocusInside(e.NewFocus))
            {
                return;
            }

            // 焦点切换有过渡期（Alt+Tab、点按钮先失焦再得焦）：等这一拍安定下来再判，避免误关
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsDropDownOpen && !IsFocusInside(Keyboard.FocusedElement))
                {
                    IsDropDownOpen = false;
                }
            }), DispatcherPriority.Input);
        }

        /// <summary>把"焦点离开弹层"的监听挂到弹层内容上（Popup 内容在独立窗口，控件的路由事件到不了）。</summary>
        private void AttachPopupFocusHandler()
        {
            if (popup?.Child is not UIElement popupContent)
            {
                return;
            }
            KeyboardFocusChangedEventHandler handler = OnAnyLostKeyboardFocus;
            popupContent.RemoveHandler(Keyboard.LostKeyboardFocusEvent, handler);
            popupContent.AddHandler(Keyboard.LostKeyboardFocusEvent, handler, true);
        }

        private bool IsFocusInside(IInputElement focus)
        {
            DependencyObject target = focus as DependencyObject;
            if (target is null)
            {
                return false;
            }
            if (IsAncestorOf(target))
            {
                return true;
            }
            DependencyObject popupRoot = popup?.Child;
            if (popupRoot is null)
            {
                return false;
            }
            for (DependencyObject node = target; node is not null; node = GetParentNode(node))
            {
                if (ReferenceEquals(node, popupRoot) || ReferenceEquals(node, this))
                {
                    return true;
                }
            }
            return false;
        }

        private static DependencyObject GetParentNode(DependencyObject node)
        {
            if (node is Visual or System.Windows.Media.Media3D.Visual3D)
            {
                return VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node);
            }
            return LogicalTreeHelper.GetParent(node);
        }

        /// <summary>在弹层里找那个搜索输入框（模板里只有一个）。</summary>
        private Input.VectorTextBox FindSearchBox()
        {
            DependencyObject root = popup?.Child;
            return root is null ? null : FindDescendant<Input.VectorTextBox>(root);
        }

        private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    return match;
                }
                if (FindDescendant<T>(child) is { } nested)
                {
                    return nested;
                }
            }
            return null;
        }
        #endregion
    }
}