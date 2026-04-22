using CBHK.Interface.TreeView;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CBHK.Model.TreeView
{
    public class BaseTreeViewDataItem : IContainerItem, INotifyPropertyChanged
    {
        #region Property
        public IContainerItem Parent { get; private set; }
        public object Content { get; set; }
        public TreeViewItemCollection<BaseTreeViewDataItem> Children { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool isExpanded;
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        private Visibility horizontalTopLineVisibility = Visibility.Hidden;
        public Visibility HorizontalTopLineVisibility
        {
            get => horizontalTopLineVisibility;
            set
            {
                if (horizontalTopLineVisibility != value)
                {
                    horizontalTopLineVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        private Visibility horizontalBottomLineVisibility = Visibility.Hidden;
        public Visibility HorizontalBottomLineVisibility
        {
            get => horizontalBottomLineVisibility;
            set
            {
                if (horizontalBottomLineVisibility != value)
                {
                    horizontalBottomLineVisibility = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region Method
        public BaseTreeViewDataItem()
        {
            Children = new TreeViewItemCollection<BaseTreeViewDataItem>(this, (child, parent) =>
            {
                child.Parent = parent;
            });
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion
    }
}