using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class RichTreeViewItems:TreeViewItem
    {
        public Visibility TextBlockVisibility
        {
            get { return (Visibility)GetValue(TextBlockVisibilityProperty); }
            set { SetValue(TextBlockVisibilityProperty, value); }
        }

        public static readonly DependencyProperty TextBlockVisibilityProperty =
            DependencyProperty.Register("TextBlockVisibility", typeof(Visibility), typeof(RichTreeViewItems), new PropertyMetadata(default(Visibility)));

        public Visibility ToggleButtonVisibility
        {
            get { return (Visibility)GetValue(ToggleButtonVisibilityProperty); }
            set { SetValue(ToggleButtonVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ToggleButtonVisibilityProperty =
            DependencyProperty.Register("ToggleButtonVisibility", typeof(Visibility), typeof(RichTreeViewItems), new PropertyMetadata(default(Visibility)));

        public Visibility TextComboBoxVisibility
        {
            get { return (Visibility)GetValue(TextComboBoxVisibilityProperty); }
            set { SetValue(TextComboBoxVisibilityProperty, value); }
        }

        public static readonly DependencyProperty TextComboBoxVisibilityProperty =
            DependencyProperty.Register("TextComboBoxVisibility", typeof(Visibility), typeof(RichTreeViewItems), new PropertyMetadata(default(Visibility)));

        public Visibility IconComboBoxVisibility
        {
            get { return (Visibility)GetValue(IconComboBoxVisibilityProperty); }
            set { SetValue(IconComboBoxVisibilityProperty, value); }
        }

        public static readonly DependencyProperty IconComboBoxVisibilityProperty =
            DependencyProperty.Register("IconComboBoxVisibility", typeof(Visibility), typeof(RichTreeViewItems), new PropertyMetadata(default(Visibility)));

        public Visibility IconTextButtonVisibility
        {
            get { return (Visibility)GetValue(IconTextButtonVisibilityProperty); }
            set { SetValue(IconTextButtonVisibilityProperty, value); }
        }

        public static readonly DependencyProperty IconTextButtonVisibilityProperty =
            DependencyProperty.Register("IconTextButtonVisibility", typeof(Visibility), typeof(RichTreeViewItems), new PropertyMetadata(default(Visibility)));

        public Visibility ColorNumbericUpDownVisibility
        {
            get { return (Visibility)GetValue(ColorNumbericUpDownVisibilityProperty); }
            set { SetValue(ColorNumbericUpDownVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ColorNumbericUpDownVisibilityProperty =
            DependencyProperty.Register("ColorNumbericUpDownVisibility", typeof(Visibility), typeof(RichTreeViewItems), new PropertyMetadata(default(Visibility)));

        public Brush ConnectingLineFill
        {
            get { return (Brush)GetValue(ConnectingLineFillProperty); }
            set { SetValue(ConnectingLineFillProperty, value); }
        }

        public static readonly DependencyProperty ConnectingLineFillProperty =
            DependencyProperty.Register("ConnectingLineFill", typeof(Brush), typeof(RichTreeViewItems), new PropertyMetadata(default(Brush)));

        public Dictionary<string,ObservableCollection<RichTreeViewItems>> SubStructure
        {
            get { return (Dictionary<string, ObservableCollection<RichTreeViewItems>>)GetValue(SubStructureProperty); }
            set { SetValue(SubStructureProperty, value); }
        }

        public static readonly DependencyProperty SubStructureProperty =
            DependencyProperty.Register("SubStructure", typeof(Dictionary<string, ObservableCollection<RichTreeViewItems>>), typeof(RichTreeViewItems), new PropertyMetadata(default(Dictionary<string, ObservableCollection<RichTreeViewItems>>)));

        public TreeViewRun TextState
        {
            get { return (TreeViewRun)GetValue(TextStateProperty); }
            set { SetValue(TextStateProperty, value); }
        }

        public static readonly DependencyProperty TextStateProperty =
            DependencyProperty.Register("TextState", typeof(TreeViewRun), typeof(RichTreeViewItems), new PropertyMetadata(default(TreeViewRun)));
    }
}
