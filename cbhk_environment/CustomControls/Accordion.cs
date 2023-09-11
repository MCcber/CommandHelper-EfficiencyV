using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class Accordion:Expander
    {
        public RelayCommand<FrameworkElement> Modify
        {
            get { return (RelayCommand<FrameworkElement>)GetValue(ModifyProperty); }
            set { SetValue(ModifyProperty, value); }
        }

        public static readonly DependencyProperty ModifyProperty =
            DependencyProperty.Register("Modify", typeof(RelayCommand<FrameworkElement>), typeof(Accordion), new PropertyMetadata(null));

        public Brush ModifyForeground
        {
            get { return (Brush)GetValue(ModifyForegroundProperty); }
            set { SetValue(ModifyForegroundProperty, value); }
        }

        public static readonly DependencyProperty ModifyForegroundProperty =
            DependencyProperty.Register("ModifyForeground", typeof(Brush), typeof(Accordion), new PropertyMetadata(default(Brush)));

        public Geometry ModifyIconData
        {
            get { return (Geometry)GetValue(ModifyIconDataProperty); }
            set { SetValue(ModifyIconDataProperty, value); }
        }

        public static readonly DependencyProperty ModifyIconDataProperty =
            DependencyProperty.Register("ModifyIconData", typeof(Geometry), typeof(Accordion), new PropertyMetadata(default(Geometry)));

        public Visibility ModifyVisibility
        {
            get { return (Visibility)GetValue(ModifyVisibilityProperty); }
            set { SetValue(ModifyVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ModifyVisibilityProperty =
            DependencyProperty.Register("ModifyVisibility", typeof(Visibility), typeof(Accordion), new PropertyMetadata(default(Visibility)));

        public RelayCommand<FrameworkElement> Fresh
        {
            get { return (RelayCommand<FrameworkElement>)GetValue(FreshProperty); }
            set { SetValue(FreshProperty, value); }
        }

        public static readonly DependencyProperty FreshProperty =
            DependencyProperty.Register("Fresh", typeof(RelayCommand<FrameworkElement>), typeof(Accordion), new PropertyMetadata(null));

        public Geometry FreshIconData
        {
            get { return (Geometry)GetValue(FreshIconDataProperty); }
            set { SetValue(FreshIconDataProperty, value); }
        }

        public static readonly DependencyProperty FreshIconDataProperty =
            DependencyProperty.Register("FreshIconData", typeof(Geometry), typeof(Accordion), new PropertyMetadata(default(Geometry)));

        public Brush FreshForeground
        {
            get { return (Brush)GetValue(FreshForegroundProperty); }
            set { SetValue(FreshForegroundProperty, value); }
        }

        public static readonly DependencyProperty FreshForegroundProperty =
            DependencyProperty.Register("FreshForeground", typeof(Brush), typeof(Accordion), new PropertyMetadata(default(Brush)));



        public Visibility FreshVisibility
        {
            get { return (Visibility)GetValue(FreshVisibilityProperty); }
            set { SetValue(FreshVisibilityProperty, value); }
        }

        public static readonly DependencyProperty FreshVisibilityProperty =
            DependencyProperty.Register("FreshVisibility", typeof(Visibility), typeof(Accordion), new PropertyMetadata(default(Visibility)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(Accordion), new PropertyMetadata(default(string)));

        public SolidColorBrush TitleForeground
        {
            get { return (SolidColorBrush)GetValue(TitleForegroundProperty); }
            set { SetValue(TitleForegroundProperty, value); }
        }

        public static readonly DependencyProperty TitleForegroundProperty =
            DependencyProperty.Register("TitleForeground", typeof(SolidColorBrush), typeof(Accordion), new PropertyMetadata(default(SolidColorBrush)));

        public string ModifyName
        {
            get { return (string)GetValue(ModifyNameProperty); }
            set { SetValue(ModifyNameProperty, value); }
        }

        public static readonly DependencyProperty ModifyNameProperty =
            DependencyProperty.Register("ModifyName", typeof(string), typeof(Accordion), new PropertyMetadata(default(string)));

        public string FreshName
        {
            get { return (string)GetValue(FreshNameProperty); }
            set { SetValue(FreshNameProperty, value); }
        }

        public static readonly DependencyProperty FreshNameProperty =
            DependencyProperty.Register("FreshName", typeof(string), typeof(Accordion), new PropertyMetadata(default(string)));

        public ObservableCollection<string> Result
        {
            get { return (ObservableCollection<string>)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register("Result", typeof(ObservableCollection<string>), typeof(Accordion), new PropertyMetadata(default(ObservableCollection<string>)));
    }
}
