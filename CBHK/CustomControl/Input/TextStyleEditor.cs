using CBHK.Model.Common;
using CBHK.Utility.Common;
using CBHK.Utility.Visual;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class TextStyleEditor:ComboBox,INotifyPropertyChanged
    {
        #region Field
        private Thickness OriginMargin;
        private Brush OriginTopBorderBrush;
        private Brush OriginBorderCornerBrush;
        private Brush OriginForegroundBrush;
        private Brush OriginBackgroundBrush;
        private Style textblockStyle = Application.Current.TryFindResource("DefaultTextStyleEditorTextBlockStyle") as Style;
        private VectorRichTextBox editor = null;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Property
        public double OriginBottomHeight { get; set; }
        public bool IsSelectionBold { get; private set; }
        public bool IsSelectionItalic { get; private set; }
        public bool IsSelectionUnderlined { get; private set; }
        public bool IsSelectionStrikethrough { get; private set; }
        public bool IsSelectionObfuscated { get; private set; }

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

        public Brush RoundBorderBrush
        {
            get { return (Brush)GetValue(RoundBorderBrushProperty); }
            set { SetValue(RoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RoundBorderBrushProperty =
            DependencyProperty.Register("RoundBorderBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush TopBorderBrush
        {
            get { return (Brush)GetValue(TopBorderBrushProperty); }
            set { SetValue(TopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TopBorderBrushProperty =
            DependencyProperty.Register("TopBorderBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));
        private SolidColorBrush OriginBottomBrush;

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

        public Brush SearchBoxForeground
        {
            get { return (Brush)GetValue(SearchBoxForegroundProperty); }
            set { SetValue(SearchBoxForegroundProperty, value); }
        }

        public static readonly DependencyProperty SearchBoxForegroundProperty =
            DependencyProperty.Register("SearchBoxForeground", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));

        public Brush EditorBoxForeground
        {
            get { return (Brush)GetValue(EditorBoxForegroundProperty); }
            set { SetValue(EditorBoxForegroundProperty, value); }
        }

        public static readonly DependencyProperty EditorBoxForegroundProperty =
            DependencyProperty.Register("EditorBoxForeground", typeof(Brush), typeof(TextStyleEditor), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public TextStyleEditor()
        {
            RoundBorderBrush = Brushes.Black;
            Loaded += TextStyleEditor_Loaded;
            DropDownClosed += TextStyleEditor_DropDownClosed;
            MouseEnter += TextStyleEditor_MouseEnter;
            MouseLeave += TextStyleEditor_MouseLeave;
            PreviewMouseLeftButtonDown += TextStyleEditor_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += TextStyleEditor_PreviewMouseLeftButtonUp;
        }

        private TextBlock CloneTextBlock(TextBlock source)
        {
            TextBlock target = new()
            {
                Style = textblockStyle
            };

            foreach (var inline in source.Inlines)
            {
                target.Inlines.Add(CloneInline(inline));
            }
            return target;
        }

        private Inline CloneInline(Inline source)
        {
            if (source is Run run)
            {
                return new Run { Text = run.Text, Foreground = run.Foreground, FontSize = run.FontSize, FontWeight = run.FontWeight };
            }
            return null;
        }

        public void AddNewItem(List<Run> list,bool isSelected = false)
        {
            ComboBoxItem comboBoxItem = new()
            {
                IsSelected = isSelected
            };
            TextBlock textBlock = new()
            {
                Style = textblockStyle
            };
            textBlock.Inlines.AddRange(list);
            comboBoxItem.Content = textBlock;
            Items.Add(comboBoxItem);
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
        #endregion

        #region Event
        private void TextStyleEditor_Loaded(object sender, EventArgs e)
        {
            OriginMargin = Margin;

            if (Text == "")
            {
                Text = "Button";
            }

            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }

            SelectionChanged += TextStyleEditor_SelectionChanged;

            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }

            var titleBrushSource = DependencyPropertyHelper.GetValueSource(this, TitleBrushProperty);
            if (titleBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || titleBrushSource.BaseValueSource is BaseValueSource.Style || titleBrushSource.BaseValueSource is BaseValueSource.Default || TitleBrush is null)
            {
                TitleBrush = Brushes.White;
            }
            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || backgroundSource.BaseValueSource is BaseValueSource.Style || Background is null)
            {
                Background = new BrushConverter().ConvertFromString("#3c8527") as Brush;
            }
            var searchBoxforegroundSource = DependencyPropertyHelper.GetValueSource(this, SearchBoxForegroundProperty);
            if (searchBoxforegroundSource.BaseValueSource is BaseValueSource.DefaultStyle || searchBoxforegroundSource.BaseValueSource is BaseValueSource.Style)
            {
                SearchBoxForeground = Brushes.Gray;
            }
            var editorBoxforegroundSource = DependencyPropertyHelper.GetValueSource(this, EditorBoxForegroundProperty);
            if (editorBoxforegroundSource.BaseValueSource is BaseValueSource.DefaultStyle || editorBoxforegroundSource.BaseValueSource is BaseValueSource.Style || EditorBoxForeground is null)
            {
                EditorBoxForeground = Brushes.White;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                BorderBrush = Brushes.Black;
            }
            var originborderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (originborderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originborderCornerBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(color);
            }
            var originTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, TopBorderBrushProperty);
            if (originTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originTopBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                TopBorderBrush = OriginTopBorderBrush = new SolidColorBrush(color);
            }
            var originBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, BottomBorderBrushProperty);
            if (originBottomBrushSource.BaseValueSource is BaseValueSource.Default || originBottomBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.5f);
                OriginBottomBrush ??= new SolidColorBrush(color);
                BottomBorderBrush = OriginBottomBrush;
            }

            if (Application.Current.TryFindResource("DefaultInlineTextStylePreset") is string run)
            {
                AddNewItem([new Run()
                {
                    Text = run
                }], true);
            }

            OriginForegroundBrush = Foreground;
            OriginBackgroundBrush = Background;
        }

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
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            VectorColorPicker vectorColorPicker = GetTemplateChild("colorPicker") as VectorColorPicker;
            vectorColorPicker.CallBack = UpdateColor;
        }

        public void AddNewStyleItem_Click(object sender, RoutedEventArgs e) => AddNewStyleItem();

        private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter)
            {
                AddNewStyleItem();
            }
        }

        private void TextStyleEditor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedItem is ComboBoxItem comboBoxItem && comboBoxItem.Content is TextBlock sourceTb)
            {
                if (GetTemplateChild("contentPresenter") is ContentPresenter contentPresenter)
                {
                    ComboBoxItem displayItem = new()
                    {
                        Content = CloneTextBlock(sourceTb)
                    };
                    contentPresenter.Content = CloneTextBlock(sourceTb);
                }
            }
        }

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
            Color color = ColorTool.Darken((OriginBackgroundBrush as SolidColorBrush).Color, 0.4f);
            Background = new SolidColorBrush(color);

            var toggleButton = Template.FindName("toggleButton", sender as FrameworkElement);
            if (toggleButton is Control button)
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
            Background = OriginBackgroundBrush;
            var toggleButton = Template.FindName("toggleButton", sender as FrameworkElement);
            if (toggleButton is Control button)
            {
                object extraBottomLine = button.Template.FindName("extraBottomLine", button);
                if (extraBottomLine is RowDefinition row)
                {
                    row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
                }
            }
            TopBorderBrush = OriginTopBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
        }

        private void TextStyleEditor_MouseEnter(object sender, MouseEventArgs e)
        {
            Color darkColor = ColorTool.Darken((OriginBackgroundBrush as SolidColorBrush).Color, 0.2f);
            Background = new SolidColorBrush(darkColor);
            var toggleButton = Template.FindName("toggleButton", sender as FrameworkElement);
            if (toggleButton is Control button)
            {
                object extraBottomLine = button.Template.FindName("extraBottomLine", button as FrameworkElement);
                if (extraBottomLine is RowDefinition row)
                {
                    row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
                }
            }
            Margin = OriginMargin;
            Color lightBorderColor = ColorTool.Lighten((OriginTopBorderBrush as SolidColorBrush).Color, 0.4f);
            TopBorderBrush = new SolidColorBrush(lightBorderColor);
            Color lightCornerColor = ColorTool.Lighten((OriginBorderCornerBrush as SolidColorBrush).Color, 0.6f);
            BorderCornerBrush = new SolidColorBrush(lightCornerColor);
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
            selection.ApplyPropertyValue(System.Windows.Documents.TextElement.ForegroundProperty, EditorBoxForeground);
            selection.ApplyPropertyValue(System.Windows.Documents.TextElement.FontFamilyProperty, editor.FontFamily);
            selection.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
        }

        public void RemoveCurrentItem_Click(object sender,RoutedEventArgs e)
        {
            if (SelectedIndex > 0)
            {
                Items.RemoveAt(SelectedIndex);
            }
        }

        public void ClearItem_Click(object sender,RoutedEventArgs e)
        {
            while (Items.Count > 1)
            {
                Items.RemoveAt(1);
            }
        }
        #endregion
    }
}
