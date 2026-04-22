using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Common;
using CBHK.Model.Constant;
using CBHK.Utility.Common;
using CBHK.Utility.Data;
using CBHK.Utility.Visual;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class TextStyleEditor : ComboBox
    {
        #region Field
        private Thickness OriginMargin;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginBorderCornerBrush;
        private Brush OriginBottomBrush;
        private Brush OriginRightBottomBorderBrush;
        private Style textblockStyle = Application.Current.TryFindResource("DefaultTextStyleEditorTextBlockStyle") as Style;
        private VectorRichTextBox editor = null;
        private bool isEnterKeyDown = false;
        #endregion

        #region Property
        public double OriginBottomHeight { get; set; }
        public bool IsSelectionBold { get; private set; }
        public bool IsSelectionItalic { get; private set; }
        public bool IsSelectionUnderlined { get; private set; }
        public bool IsSelectionStrikethrough { get; private set; }
        public bool IsSelectionObfuscated { get; private set; }

        private ObservableCollection<TextStyleEditorItem> DataList { get; set; } = [];
        private CollectionViewSource ItemView = null;

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(TextStyleEditor), new PropertyMetadata(default(string)));

        public Visibility SearchBoxVisibility
        {
            get { return (Visibility)GetValue(SearchBoxVisibilityProperty); }
            set { SetValue(SearchBoxVisibilityProperty, value); }
        }

        public static readonly DependencyProperty SearchBoxVisibilityProperty =
            DependencyProperty.Register("SearchBoxVisibility", typeof(Visibility), typeof(TextStyleEditor), new PropertyMetadata(default(Visibility)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TextStyleEditor), new PropertyMetadata(default(string)));

        public FormattingState SelectionFormattingState
        {
            get { return (FormattingState)GetValue(SelectionFormattingStateProperty); }
            set { SetValue(SelectionFormattingStateProperty, value); }
        }

        public static readonly DependencyProperty SelectionFormattingStateProperty =
            DependencyProperty.Register("SelectionFormattingState", typeof(FormattingState), typeof(TextStyleEditor), new PropertyMetadata(default(FormattingState)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush TitleLeftTopBorderBrush
        {
            get { return (Brush)GetValue(TitleLeftTopBorderBrushProperty); }
            set { SetValue(TitleLeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleLeftTopBorderBrushProperty =
            DependencyProperty.Register("TitleLeftTopBorderBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush TitleRightBottomBorderBrush
        {
            get { return (Brush)GetValue(TitleRightBottomBorderBrushProperty); }
            set { SetValue(TitleRightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleRightBottomBorderBrushProperty =
            DependencyProperty.Register("TitleRightBottomBorderBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush TitleBorderCornerBrush
        {
            get { return (Brush)GetValue(TitleBorderCornerBrushProperty); }
            set { SetValue(TitleBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleBorderCornerBrushProperty =
            DependencyProperty.Register("TitleBorderCornerBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush BottomBorderBrush
        {
            get { return (Brush)GetValue(BottomBorderBrushProperty); }
            set { SetValue(BottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BottomBorderBrushProperty =
            DependencyProperty.Register("BottomBorderBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush ItemContainerBackground
        {
            get { return (Brush)GetValue(ItemContainerBackgroundProperty); }
            set { SetValue(ItemContainerBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ItemContainerBackgroundProperty =
            DependencyProperty.Register("ItemContainerBackground", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush ItemSelectedMarkerBrush
        {
            get { return (Brush)GetValue(ItemSelectedMarkerBrushProperty); }
            set { SetValue(ItemSelectedMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty ItemSelectedMarkerBrushProperty =
            DependencyProperty.Register("ItemSelectedMarkerBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush PopupItemPanelBackground
        {
            get { return (Brush)GetValue(PopupItemPanelBackgroundProperty); }
            set { SetValue(PopupItemPanelBackgroundProperty, value); }
        }

        public static readonly DependencyProperty PopupItemPanelBackgroundProperty =
            DependencyProperty.Register("PopupItemPanelBackground", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush TitleBrush
        {
            get { return (Brush)GetValue(TitleBrushProperty); }
            set { SetValue(TitleBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleBrushProperty =
            DependencyProperty.Register("TitleBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public TextStyleEditor()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(ForegroundProperty, Theme.CommonForeground);
            SetResourceReference(TitleBrushProperty, Theme.CommonForeground);

            DropDownClosed += TextStyleEditor_DropDownClosed;
            MouseEnter += TextStyleEditor_MouseEnter;
            MouseLeave += TextStyleEditor_MouseLeave;
            PreviewMouseLeftButtonDown += TextStyleEditor_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += TextStyleEditor_PreviewMouseLeftButtonUp;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
                PopupItemPanelBackground = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));

                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));
                TitleBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));

                LeftTopBorderBrush = OriginLeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                TitleLeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));

                RightBottomBorderBrush = OriginRightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
                TitleRightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.3f));

                BottomBorderBrush = OriginBottomBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.6f));
            }
        }

        public void AddNewItem(List<Run> list,bool isSelected = false)
        {
            DataList.Add(new TextStyleEditorItem()
            {
                InlineList = new ObservableCollection<InlineData>(list.Select(run => new InlineData()
                {
                    Text = run.Text,
                    Foreground = run.Foreground,
                    FontWeight = run.FontWeight,
                    TextDecorationCollection = run.TextDecorations,
                })),
                IsSelected = isSelected
            });
        }

        private void UpdateColor(Color color)
        {
            editor.Selection.ApplyPropertyValue(System.Windows.Documents.TextElement.ForegroundProperty, new SolidColorBrush(color));
        }

        private void AddNewStyleItem()
        {
            if (editor.Document.Blocks.FirstBlock is Paragraph paragraph)
            {
                if(paragraph.Inlines.Count == 1 && paragraph.Inlines.FirstInline is Run firstRun && string.IsNullOrEmpty(firstRun.Text))
                {
                    return;
                }
                List<Run> runList = [..paragraph.Inlines.Select(inline =>
                {
                    if(inline is Run run)
                    {
                        return run;
                    }
                    return null;
                })];
                runList.RemoveAll(item => item is null);
                AddNewItem(runList, true);
            }
        }

        /// <summary>
        /// 强制重新渲染
        /// </summary>
        private void ForceRenderUpdate()
        {
            if (GetTemplateChild("contentPresenter") is ContentPresenter contentPresenter && SelectedItem is not null)
            {
                var temp = SelectedItem;
                contentPresenter.Content = null;
                contentPresenter.Content = temp;
            }
        }
        #endregion

        #region Event
        public void Editor_Loaded(object sender,RoutedEventArgs e)
        {
            if (sender is VectorRichTextBox vectorRichTextBox)
            {
                editor = vectorRichTextBox;

                //注入装饰层
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(editor);
                layer?.Add(new ObfuscatedAdorner(editor));

                //订阅键盘事件
                editor.PreviewKeyDown += Editor_PreviewKeyDown;
                editor.PreviewKeyUp += Editor_PreviewKeyUp;
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

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        private void ItemView_Filter(object sender, FilterEventArgs e)
        {
            if(e.Item is TextStyleEditorItem textStyleEditorItem)
            {
                bool result = StringTool.IsMatchSearchText(textStyleEditorItem.FullTextCache, SearchText);
                e.Accepted = result;
            }
        }

        public void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter && sender is TextBox && ItemView is not null)
            {
                IsDropDownOpen = true;
                e.Handled = true;
                ItemView.View?.Refresh();
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (DataList.Count > 10)
            {
                SearchBoxVisibility = Visibility.Visible;
            }
            else
            {
                SearchBoxVisibility = Visibility.Collapsed;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("colorPicker") is VectorColorPicker vectorColorPicker)
            {
                vectorColorPicker.UpdateSelectedColorCallBack = UpdateColor;
            }

            OriginMargin = Margin;

            if (ItemView is null)
            {
                ItemView = Application.Current.Resources["TextStyleEditorBoxItemView"] as CollectionViewSource;
                ItemView.Filter += ItemView_Filter;
                ItemView.Source = DataList;
                if (DataList.Count > 0)
                {
                    SelectedItem = DataList[0];
                }
            }

            if (string.IsNullOrEmpty(Text))
            {
                Text = "Button";
            }

            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }

            SelectionChanged += TextStyleEditor_SelectionChanged;

            if(GetTemplateChild("extraBottomLine") is RowDefinition rowDefinition)
            {
                rowDefinition.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }

            if (Application.Current.TryFindResource("DefaultInlineTextStylePreset") is string run)
            {
                AddNewItem([new Run()
                {
                    Text = run
                }], true);
            }

            for (int i = 0; i < 10; i++)
            {
                AddNewItem([new Run()
                {
                    Text = i + ""
                }]);
            }
            
            if(DataList is not null && DataList.Count > 0)
            {
                SelectedItem = DataList[0];
            }

            ForceRenderUpdate();

            UpdateBorderColorByBackgroundColor();
        }

        public void AddNewStyleItem_Click(object sender, RoutedEventArgs e) => AddNewStyleItem();

        private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter && !isEnterKeyDown)
            {
                isEnterKeyDown = true;
                AddNewStyleItem();
            }
        }

        private void Editor_PreviewKeyUp(object sender, KeyEventArgs e) => isEnterKeyDown = false;

        private void TextStyleEditor_SelectionChanged(object sender, SelectionChangedEventArgs e) => ForceRenderUpdate();

        public void Editor_MouseLeave(object sender, MouseEventArgs e)
        {
            SelectionFormattingState = RichTextFormattingHelper.GetFormattingState(editor);
            Focus();
        }

        private void TextStyleEditor_DropDownClosed(object sender, EventArgs e)
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

        private void TextStyleEditor_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TextStyleEditor_MouseEnter(sender, null);
        }

        private void TextStyleEditor_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(GetTemplateChild("toggleButton") is Control button)
            {
                object extraBottomLine = button.Template.FindName("extraBottomLine", button as FrameworkElement);
                if (extraBottomLine is RowDefinition row)
                {
                    row.Height = new(0, GridUnitType.Pixel);
                }
            }
            Margin = new(Margin.Left, Margin.Top + 2, Margin.Right, Margin.Bottom);
        }

        private void TextStyleEditor_MouseLeave(object sender, MouseEventArgs e)
        {
            var toggleButton = Template.FindName("toggleButton", sender as FrameworkElement);
            if (toggleButton is Control button)
            {
                object extraBottomLine = button.Template.FindName("extraBottomLine", button);
                if (extraBottomLine is RowDefinition row)
                {
                    row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
                }
            }
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
        }

        private void TextStyleEditor_MouseEnter(object sender, MouseEventArgs e)
        {
            if (GetTemplateChild("toggleButton") is Control button)
            {
                object extraBottomLine = button.Template.FindName("extraBottomLine", button as FrameworkElement);
                if (extraBottomLine is RowDefinition row)
                {
                    row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
                }
            }
            Margin = OriginMargin;

            if(OriginLeftTopBorderBrush is SolidColorBrush originLeftTopBorderBrush)
            {
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(originLeftTopBorderBrush.Color, 0.4f));
            }
            if(OriginBorderCornerBrush is SolidColorBrush originBorderCornerBrush)
            {
                BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(originBorderCornerBrush.Color, 0.6f));
            }
        }

        public void SetBold_Click(object sender,RoutedEventArgs e)
        {
            if(editor is not null)
            {
                RichTextFormattingHelper.ToggleBold(editor);
                SelectionFormattingState = RichTextFormattingHelper.GetFormattingState(editor);
            }
        }

        public void SetItalic_Click(object sender,RoutedEventArgs e)
        {
            if (editor is not null)
            {
                RichTextFormattingHelper.ToggleItalic(editor);
                SelectionFormattingState = RichTextFormattingHelper.GetFormattingState(editor);
            }
        }

        public void SetUnderlined_Click(object sender, RoutedEventArgs e)
        {
            if (editor is not null)
            {
                RichTextFormattingHelper.ToggleUnderline(editor);
                SelectionFormattingState = RichTextFormattingHelper.GetFormattingState(editor);
            }
        }

        public void SetStrikethrough_Click(object sender, RoutedEventArgs e)
        {
            if (editor is not null)
            {
                RichTextFormattingHelper.ToggleStrikethrough(editor);
                SelectionFormattingState = RichTextFormattingHelper.GetFormattingState(editor);
            }
        }

        public void SetObfuscated_Click(object sender, RoutedEventArgs e)
        {
            if (editor is not null)
            {
                RichTextFormattingHelper.ToggleObfuscated(editor);
                // 重新获取状态并更新 UI 绑定属性
                var state = RichTextFormattingHelper.GetFormattingState(editor);
                SelectionFormattingState = state;
                IsSelectionObfuscated = state.Obfuscated ?? false;
            }
        }

        public void ResetStyle_Click(object sender,RoutedEventArgs e)
        {
            var selection = editor.Selection;
            if(selection.IsEmpty)
            {
                return;
            }
            RichTextFormattingHelper.ApplyCustomPropertyToSelection(selection, ObfuscatedProvider.IsObfuscatedProperty, false);
            selection.ApplyPropertyValue(System.Windows.Documents.TextElement.FontWeightProperty, FontWeights.Normal);
            selection.ApplyPropertyValue(System.Windows.Documents.TextElement.FontStyleProperty, FontStyles.Normal);
            selection.ApplyPropertyValue(System.Windows.Documents.TextElement.FontFamilyProperty, editor.FontFamily);
            selection.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
        }

        public void RemoveCurrentItem_Click(object sender,RoutedEventArgs e)
        {
            if (SelectedIndex > 0 && ItemsSource is ListCollectionView listCollectionView)
            {
                listCollectionView.RemoveAt(SelectedIndex);
            }
        }

        public void ClearItem_Click(object sender,RoutedEventArgs e)
        {
            if (ItemsSource is ListCollectionView listCollectionView)
            {
                while (listCollectionView.Count > 1)
                {
                    listCollectionView.RemoveAt(1);
                }
            }
        }
        #endregion
    }
}