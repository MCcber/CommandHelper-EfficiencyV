using CBHK.Interface.TreeView;
using CBHK.Model.TreeView;
using CBHK.Utility.Visual;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorTreeView : TreeView
    {
        #region Field
        private Point lastMouseDown;
        private VectorTreeViewItem dragTreeViewItem;
        private BaseTreeViewDataItem lastDataItem;
        private BaseTreeViewDataItem draggedDataItem;
        #endregion

        #region Property
        #endregion

        #region Method
        public VectorTreeView()
        {
            AllowDrop = true;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            } 
            while (current is not null);

            return null;
        }

        /// <summary>
        /// 检测当前被拖拽节点是否为目标节点的若干层父级
        /// </summary>
        /// <returns></returns>
        private bool IsAllowDragToParent(BaseTreeViewDataItem currentDraggedData, BaseTreeViewDataItem currentTargetData)
        {
            bool result = true;
            BaseTreeViewDataItem loopParent = currentTargetData.Parent as BaseTreeViewDataItem;

            result = loopParent == currentDraggedData.Parent;
            if(result)
            {
                return result;
            }

            //后续因个别特殊情况可能需要允许将节点拖拽到其父级节点上
            while (loopParent is not null)
            {
                if (loopParent == currentDraggedData)
                {
                    result = false;
                    break;
                }
                loopParent = loopParent.Parent as BaseTreeViewDataItem;
            }
            return result;
        }
        #endregion

        #region Event
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VectorTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is VectorTreeViewItem;
        }

        #region 处理拖拽与拖拽预览
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            lastMouseDown = e.GetPosition(this);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Keyboard.FocusedElement is TextBox)
                {
                    return;
                }

                Point currentPosition = e.GetPosition(this);

                // 判断移动距离是否超过了系统的防抖阈值（防止误触）
                if (Math.Abs(currentPosition.X - lastMouseDown.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPosition.Y - lastMouseDown.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // 找到鼠标当前所在的 VectorTreeViewItem
                    dragTreeViewItem = FindAncestor<VectorTreeViewItem>((DependencyObject)e.OriginalSource);

                    if (dragTreeViewItem is not null)
                    {
                        // 拿到绑定的 ViewModel 数据
                        draggedDataItem = dragTreeViewItem.DataContext as BaseTreeViewDataItem;
                        Grid grid = dragTreeViewItem.Template.FindName("grid", dragTreeViewItem) as Grid;
                        if (grid is not null)
                        {
                            DragDropHelper.StartDrag(dragTreeViewItem, draggedDataItem, grid.RowDefinitions[0].Height.Value);
                        }
                    }
                }
            }
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            var targetItem = FindAncestor<VectorTreeViewItem>((DependencyObject)e.OriginalSource);

            // 如果目标是一个节点，且不是自己本身，则允许放置 (Move)
            if (targetItem is not null && draggedDataItem is not null && targetItem.DataContext != draggedDataItem)
            {
                e.Effects = DragDropEffects.Move;

                if(targetItem.DataContext is BaseTreeViewDataItem targetDataItem)
                {
                    bool isAllowDragToParent = IsAllowDragToParent(draggedDataItem, targetDataItem);
                    if (!isAllowDragToParent)
                    {
                        targetDataItem.HorizontalTopLineVisibility = targetDataItem.HorizontalBottomLineVisibility = Visibility.Collapsed;
                        if (lastDataItem is not null)
                        {
                            lastDataItem.HorizontalTopLineVisibility = lastDataItem.HorizontalBottomLineVisibility = Visibility.Collapsed;
                        }
                        return;
                    }
                    int dragIndex = draggedDataItem.Parent.Children.IndexOf(draggedDataItem);
                    int targetIndex = targetDataItem.Parent.Children.IndexOf(targetDataItem);

                    if (lastDataItem != targetDataItem)
                    {
                        if (lastDataItem is not null)
                        {
                            lastDataItem.HorizontalTopLineVisibility = Visibility.Hidden;
                            lastDataItem.HorizontalBottomLineVisibility = Visibility.Hidden;
                        }

                        lastDataItem = targetDataItem;
                    }

                    if (dragIndex > targetIndex)
                    {
                        targetDataItem.HorizontalTopLineVisibility = Visibility.Visible;
                    }
                    else
                    {
                        targetDataItem.HorizontalBottomLineVisibility = Visibility.Visible;
                    }
                }
            }
            else// 不允许放置 (显示禁止图标)
            {
                e.Effects = DragDropEffects.None;
                if (lastDataItem is not null)
                {
                    lastDataItem.HorizontalTopLineVisibility = Visibility.Hidden;
                    lastDataItem.HorizontalBottomLineVisibility = Visibility.Hidden;
                }
            }

            e.Handled = true;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            var targetItem = FindAncestor<VectorTreeViewItem>((DependencyObject)e.OriginalSource);

            if (targetItem is not null && draggedDataItem is not null)
            {
                var targetData = targetItem.DataContext;
                if (draggedDataItem != targetData)
                {
                    if (draggedDataItem is BaseTreeViewDataItem currentDraggedData && targetData is BaseTreeViewDataItem currentTargetData && currentDraggedData.Parent is not null && currentTargetData.Parent is not null)
                    {
                        bool isAllowDragToParent = IsAllowDragToParent(currentDraggedData, currentTargetData);
                        if(!isAllowDragToParent)
                        {
                            currentTargetData.HorizontalTopLineVisibility = currentTargetData.HorizontalBottomLineVisibility = Visibility.Collapsed;
                            if(lastDataItem is not null)
                            {
                                lastDataItem.HorizontalTopLineVisibility = lastDataItem.HorizontalBottomLineVisibility = Visibility.Collapsed;
                            }
                            return;
                        }
                        if (currentTargetData.Parent == currentDraggedData.Parent)
                        {
                            IContainerItem parent = currentTargetData.Parent;
                            int targetIndex = parent.Children.IndexOf(currentTargetData);
                            int currentIndex = parent.Children.IndexOf(currentDraggedData);

                            if(targetIndex > currentIndex)
                            {
                                if (targetIndex == parent.Children.Count - 1)
                                {
                                    parent.Children.Remove(currentDraggedData);
                                    parent.Children.Add(currentDraggedData);
                                }
                                else
                                {
                                    parent.Children.Remove(currentDraggedData);
                                    parent.Children.Insert(targetIndex, currentDraggedData);
                                }
                            }
                            else
                            if(targetIndex < currentIndex)
                            {
                                parent.Children.Remove(currentDraggedData);
                                parent.Children.Insert(targetIndex, currentDraggedData);
                            }
                        }
                        //else
                        //{
                        //    currentDraggedData.Parent.Children.Remove(currentDraggedData);
                        //    currentTargetData.Children.Add(currentDraggedData);
                        //}

                        currentTargetData.IsExpanded = true;
                        currentTargetData.HorizontalTopLineVisibility = Visibility.Hidden;
                        currentTargetData.HorizontalBottomLineVisibility = Visibility.Hidden;
                        currentDraggedData.Parent.IsExpanded = currentDraggedData.Parent.Children.Count > 0;
                        currentDraggedData.IsSelected = true;
                    }
                }
            }

            draggedDataItem = null;
            e.Handled = true;
        }  
        #endregion

        #endregion
    }
}