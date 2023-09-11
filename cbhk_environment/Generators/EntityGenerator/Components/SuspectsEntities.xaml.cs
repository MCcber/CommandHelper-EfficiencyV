using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.EntityGenerator.Components
{
    /// <summary>
    /// SuspectsEntities.xaml 的交互逻辑
    /// </summary>
    public partial class SuspectsEntities : UserControl
    {
        #region 添加与清空可疑实体
        public RelayCommand<FrameworkElement> AddSuspectsEntities { get; set; }
        public RelayCommand<FrameworkElement> ClearSyspectsEntities { get; set; }
        #endregion
        public SuspectsEntities()
        {
            InitializeComponent();
            DataContext = this;
            AddSuspectsEntities = new RelayCommand<FrameworkElement>(AddSuspectsEntitiesCommand);
            ClearSyspectsEntities = new RelayCommand<FrameworkElement>(ClearSyspectsEntitiesCommand);
        }

        /// <summary>
        /// 添加可疑实体
        /// </summary>
        /// <param name="obj"></param>
        public void AddSuspectsEntitiesCommand(FrameworkElement obj)
        {
            Slider slider = new()
            {
                Style = Application.Current.Resources["NumberBoxStyle"] as Style
            };
            slider.ValueChanged += Slider_ValueChanged;
            UUIDOrPosGroup uUIDOrPosGroup = new();
            uUIDOrPosGroup.number0.ValueChanged += UUID_ValueChanged;
            StackPanel stackPanel = new();
            stackPanel.Children.Add(slider);
            stackPanel.Children.Add(uUIDOrPosGroup);
            Accordion accordion = obj as Accordion;
            ((accordion.Content as ScrollViewer).Content as StackPanel).Children.Add(stackPanel);
        }

        /// <summary>
        /// UUID数据更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UUID_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            UUIDOrPosGroup uUIDOrPosGroup = slider.FindParent<UUIDOrPosGroup>();
            StackPanel stackPanel = slider.FindParent<StackPanel>();
            Slider anger = stackPanel.FindChild<Slider>();
            stackPanel.Tag = "{anger:" + anger.Value + ",uuid:[" + uUIDOrPosGroup.number0.Value + "," + uUIDOrPosGroup.number1.Value + "," + uUIDOrPosGroup.number2.Value + "," + uUIDOrPosGroup.number3.Value + "]}";
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            StackPanel stackPanel = slider.Parent as StackPanel;
            UUIDOrPosGroup uUIDOrPosGroup = stackPanel.FindChild<UUIDOrPosGroup>();
            stackPanel.Tag = "{anger:" + slider.Value + ",uuid:[" + uUIDOrPosGroup.number0 + "," + uUIDOrPosGroup.number1 + "," + uUIDOrPosGroup.number2 + "," + uUIDOrPosGroup.number3 + "]}";
        }

        /// <summary>
        /// 清空可疑实体
        /// </summary>
        /// <param name="obj"></param>
        private void ClearSyspectsEntitiesCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            ((accordion.Content as ScrollViewer).Content as StackPanel).Children.Clear();
        }
    }
}
