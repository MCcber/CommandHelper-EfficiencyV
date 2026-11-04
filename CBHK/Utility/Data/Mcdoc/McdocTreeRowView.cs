using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 行视图：Reset 会重建容器并把滚动位置打回顶部，
    /// 这里在 Reset 前记下像素偏移、布局后恢复 —— 展开/折叠在滚动中段也不跳。
    /// 虚拟化配置也在这里统一设好，页面不用关心。
    /// </summary>
    public sealed class McdocTreeRowView : ListBox
    {
        private INotifyCollectionChanged observed;
        private ScrollViewer scrollViewer;
        private double anchorOffset = -1;

        public McdocTreeRowView()
        {
            BorderThickness = new Thickness(0);
            Background = Brushes.Transparent;
            SelectionMode = SelectionMode.Single;
            VirtualizingPanel.SetIsVirtualizing(this, true);
            VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
            VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
            VirtualizingPanel.SetCacheLength(this, new VirtualizationCacheLength(1.0, 1.0));
            VirtualizingPanel.SetCacheLengthUnit(this, VirtualizationCacheLengthUnit.Page);
            ScrollViewer.SetCanContentScroll(this, true);
        }

        private string lastSelectedPath;

        /// <summary>页面级文档动作（复制/另存 JSON）：快捷键统一在这里接，页面不用各写按钮。</summary>
        private IMcdocDocumentActions DocumentActions => DataContext as IMcdocDocumentActions;

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            var modifiers = System.Windows.Input.Keyboard.Modifiers;
            var actions = DocumentActions;
            if (actions is not null && (modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                if (e.Key == System.Windows.Input.Key.S && actions.SaveJsonCommand is { } save && save.CanExecute(null))
                {
                    save.Execute(null);
                    e.Handled = true;
                    return;
                }
                if (e.Key == System.Windows.Input.Key.C && (modifiers & System.Windows.Input.ModifierKeys.Shift) != 0
                    && actions.CopyJsonCommand is { } copy && copy.CanExecute(null))
                {
                    copy.Execute(null);
                    e.Handled = true;
                    return;
                }
            }
            if (Items.Count == 0) { base.OnPreviewKeyDown(e); return; }

            // Preview 走隧道阶段：行内编辑器（键输入框 / 值编辑器 / 可输入下拉）还没轮到处理这个键。
            // 归属必须按"事件目标"判：编辑器有焦点时左右键是光标、回车是提交，
            // 当成行导航就会落到别的行上（现场：重命名框里按回车，第一行 display 的子结构被摊开）。
            var target = e.OriginalSource as DependencyObject;
            if (IsInlineEditor(target))
            {
                if (e.Key == System.Windows.Input.Key.Enter && CommitKeyEntry(target))
                {
                    e.Handled = true;
                    return;
                }
                base.OnPreviewKeyDown(e);
                return;
            }

            // 行导航只认"事件目标所在的那一行"：没有目标行就不许猜第一行
            // （行上的按钮不选中行，SelectedIndex 常是 -1 —— 兜底到 0 就是"按键全作用到 display"）。
            var row = RowFrom(target);
            var index = row is null ? -1 : Items.IndexOf(row);
            if (index < 0) { base.OnPreviewKeyDown(e); return; }
            switch (e.Key)
            {
                case System.Windows.Input.Key.Down: SelectRow(Math.Min(Items.Count - 1, index + 1)); e.Handled = true; break;
                case System.Windows.Input.Key.Up: SelectRow(Math.Max(0, index - 1)); e.Handled = true; break;
                case System.Windows.Input.Key.Home: SelectRow(0); e.Handled = true; break;
                case System.Windows.Input.Key.End: SelectRow(Items.Count - 1); e.Handled = true; break;
                case System.Windows.Input.Key.Enter:
                    row.IsExpanded = !row.IsExpanded;
                    e.Handled = true; break;
                case System.Windows.Input.Key.Right:
                    if (row.HasChildren && !row.IsExpanded) row.IsExpanded = true;
                    else SelectRow(Math.Min(Items.Count - 1, index + 1));
                    e.Handled = true; break;
                case System.Windows.Input.Key.Left:
                    if (row.IsExpanded) row.IsExpanded = false;
                    else
                    {
                        for (var i = index - 1; i >= 0; i--)
                        {
                            if (Items[i] is McdocRow candidate && candidate.Depth < row.Depth)
                            {
                                SelectRow(i);
                                break;
                            }
                        }
                    }
                    e.Handled = true; break;
            }
            base.OnPreviewKeyDown(e);
        }

        private void SelectRow(int index)
        {
            if (index < 0 || index >= Items.Count) return;
            SelectedIndex = index;
            ScrollIntoView(Items[index]);
        }

        /// <summary>事件目标是不是行内编辑器：键输入框 / 值编辑器 / 可输入下拉（到行容器为止，行本身不算）。</summary>
        private static bool IsInlineEditor(DependencyObject target)
        {
            for (var current = target; current is not null; current = ParentOf(current))
            {
                if (current is McdocKeyBox or TextBoxBase or PasswordBox or ComboBox) return true;
                if (current is ListBoxItem or McdocTreeRowView) return false;
            }
            return false;
        }

        /// <summary>键输入框里回车 = 点行上的「加入」：新键提交与条目改名走同一个命令出口。</summary>
        private static bool CommitKeyEntry(DependencyObject target)
        {
            for (var current = target; current is not null; current = ParentOf(current))
            {
                if (current is not McdocKeyBox box)
                {
                    if (current is ListBoxItem or McdocTreeRowView) return false;
                    continue;
                }
                if (box.DataContext is not McdocRow row) return false;
                var command = row.Node?.AddEntryCommand;
                if (command?.CanExecute(null) != true) return false;
                command.Execute(null);
                return true;
            }
            return false;
        }

        /// <summary>事件目标归属的那一行（找不到就返回 null，调用方不许兜底）。</summary>
        private McdocRow RowFrom(DependencyObject target)
        {
            for (var current = target; current is not null; current = ParentOf(current))
            {
                if (current is ListBoxItem item) return item.DataContext as McdocRow;
                if (ReferenceEquals(current, this)) return null;
            }
            return null;
        }

        /// <summary>整表 Reset 后 WPF 会清掉选中项：按路径恢复。</summary>
        private void RestoreSelection()
        {
            if (string.IsNullOrEmpty(lastSelectedPath)) return;
            foreach (var item in Items)
            {
                if (item is McdocRow row && row.Node.JsonPath == lastSelectedPath)
                {
                    SelectedItem = row;
                    return;
                }
            }
        }

        /// <summary>Ctrl+点击展开按钮 = 展开/折叠整棵子树（misode 的 expand_all / collapse_all）。</summary>
        protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == 0) return;
            McdocRow row = null;
            var onExpander = false;
            for (var current = e.OriginalSource as DependencyObject; current is not null; current = ParentOf(current))
            {
                if (current is System.Windows.Controls.Primitives.ToggleButton) onExpander = true;
                if (current is ListBoxItem { DataContext: McdocRow hit })
                {
                    row = hit;
                    break;
                }
            }
            if (row is null || !onExpander) return;
            var command = row.IsExpanded ? row.CollapseAllCommand : row.ExpandAllCommand;
            if (command.CanExecute(null)) command.Execute(null);
            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            ActivateFrom(e.OriginalSource as DependencyObject);
        }

        protected override void OnLostKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            // 焦点离开行视图：把"打字期间推迟掉的重建"补上（树不能永远停在上一代）
            if (!IsKeyboardFocusWithin)
                (DataContext as CBHK.ViewModel.Generator.McdocGeneratorViewModelBase)?.FlushDeferredRebuild();
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if (SelectedItem is McdocRow selected)
            {
                selected.ActivateEditor();
                lastSelectedPath = selected.Node.JsonPath;
            }
        }

        /// <summary>
        /// 往上找父级。doc 面板的富文本命中的是 Run（FrameworkContentElement，不是 Visual），
        /// 直接 VisualTreeHelper.GetParent 会抛"Run 不是 Visual 或 Visual3D"，鼠标划过标签就崩。
        /// </summary>
        private static DependencyObject ParentOf(DependencyObject current)
        {
            if (current is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D)
            {
                return VisualTreeHelper.GetParent(current);
            }
            if (current is ContentElement content)
            {
                return ContentOperations.GetParent(content) ?? (content as FrameworkContentElement)?.Parent;
            }
            return null;
        }

        /// <summary>鼠标移到哪一行，就把哪一行的编辑器实化（其余行保持摘要）。</summary>
        private static void ActivateFrom(DependencyObject source)
        {
            for (var current = source; current is not null; current = ParentOf(current))
            {
                if (current is ListBoxItem { DataContext: McdocRow row })
                {
                    row.ActivateEditor();
                    return;
                }
            }
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
            Hook();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Hook();
        }

        private void Hook()
        {
            if (observed is not null) observed.CollectionChanged -= OnRowsChanged;
            observed = ItemsSource as INotifyCollectionChanged;
            if (observed is not null) observed.CollectionChanged += OnRowsChanged;
            scrollViewer ??= FindScroll(this);
        }

        private static ScrollViewer FindScroll(DependencyObject root)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is ScrollViewer hit) return hit;
                var nested = FindScroll(child);
                if (nested is not null) return nested;
            }
            return null;
        }

        private void OnRowsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action != NotifyCollectionChangedAction.Reset) return;
            scrollViewer ??= FindScroll(this);
            if (scrollViewer is null || scrollViewer.VerticalOffset <= 0) return;
            anchorOffset = scrollViewer.VerticalOffset;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (anchorOffset < 0) return;
                scrollViewer?.ScrollToVerticalOffset(anchorOffset);
                anchorOffset = -1;
            }), DispatcherPriority.Loaded);
        }
    }
}
