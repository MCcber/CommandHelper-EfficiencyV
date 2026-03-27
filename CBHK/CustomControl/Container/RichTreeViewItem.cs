using CBHK.CustomControl.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class RichTreeViewItem:TreeViewItem
    {
        #region Property
        public Visibility TextBlockVisibility
        {
            get { return (Visibility)GetValue(TextBlockVisibilityProperty); }
            set { SetValue(TextBlockVisibilityProperty, value); }
        }

        public static readonly DependencyProperty TextBlockVisibilityProperty =
            DependencyProperty.Register("TextBlockVisibility", typeof(Visibility), typeof(RichTreeViewItem), new PropertyMetadata(default(Visibility)));

        public Visibility ToggleButtonVisibility
        {
            get { return (Visibility)GetValue(ToggleButtonVisibilityProperty); }
            set { SetValue(ToggleButtonVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ToggleButtonVisibilityProperty =
            DependencyProperty.Register("ToggleButtonVisibility", typeof(Visibility), typeof(RichTreeViewItem), new PropertyMetadata(default(Visibility)));

        public Visibility TextComboBoxVisibility
        {
            get { return (Visibility)GetValue(TextComboBoxVisibilityProperty); }
            set { SetValue(TextComboBoxVisibilityProperty, value); }
        }

        public static readonly DependencyProperty TextComboBoxVisibilityProperty =
            DependencyProperty.Register("TextComboBoxVisibility", typeof(Visibility), typeof(RichTreeViewItem), new PropertyMetadata(default(Visibility)));

        public Visibility IconComboBoxVisibility
        {
            get { return (Visibility)GetValue(IconComboBoxVisibilityProperty); }
            set { SetValue(IconComboBoxVisibilityProperty, value); }
        }

        public static readonly DependencyProperty IconComboBoxVisibilityProperty =
            DependencyProperty.Register("IconComboBoxVisibility", typeof(Visibility), typeof(RichTreeViewItem), new PropertyMetadata(default(Visibility)));

        public Visibility IconTextButtonVisibility
        {
            get { return (Visibility)GetValue(IconTextButtonVisibilityProperty); }
            set { SetValue(IconTextButtonVisibilityProperty, value); }
        }

        public static readonly DependencyProperty IconTextButtonVisibilityProperty =
            DependencyProperty.Register("IconTextButtonVisibility", typeof(Visibility), typeof(RichTreeViewItem), new PropertyMetadata(default(Visibility)));

        public Visibility ColorNumbericUpDownVisibility
        {
            get { return (Visibility)GetValue(ColorNumbericUpDownVisibilityProperty); }
            set { SetValue(ColorNumbericUpDownVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ColorNumbericUpDownVisibilityProperty =
            DependencyProperty.Register("ColorNumbericUpDownVisibility", typeof(Visibility), typeof(RichTreeViewItem), new PropertyMetadata(default(Visibility)));

        public Brush ConnectingLineFill
        {
            get { return (Brush)GetValue(ConnectingLineFillProperty); }
            set { SetValue(ConnectingLineFillProperty, value); }
        }

        public static readonly DependencyProperty ConnectingLineFillProperty =
            DependencyProperty.Register("ConnectingLineFill", typeof(Brush), typeof(RichTreeViewItem), new PropertyMetadata(default(Brush)));

        public Dictionary<string, ObservableCollection<RichTreeViewItem>> SubStructure
        {
            get { return (Dictionary<string, ObservableCollection<RichTreeViewItem>>)GetValue(SubStructureProperty); }
            set { SetValue(SubStructureProperty, value); }
        }

        public static readonly DependencyProperty SubStructureProperty =
            DependencyProperty.Register("SubStructure", typeof(Dictionary<string, ObservableCollection<RichTreeViewItem>>), typeof(RichTreeViewItem), new PropertyMetadata(default(Dictionary<string, ObservableCollection<RichTreeViewItem>>)));

        public TreeViewRun TextState
        {
            get { return (TreeViewRun)GetValue(TextStateProperty); }
            set { SetValue(TextStateProperty, value); }
        }

        public static readonly DependencyProperty TextStateProperty =
            DependencyProperty.Register("TextState", typeof(TreeViewRun), typeof(RichTreeViewItem), new PropertyMetadata(default(TreeViewRun))); 
        #endregion
    }
}
