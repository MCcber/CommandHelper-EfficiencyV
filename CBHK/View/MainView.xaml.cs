using CBHK.Common.Utility.Event;
using Prism.Events;

namespace CBHK.View
{
    /// <summary>
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView
    {
        #region Field
        private IEventAggregator eventAggregator;
        #endregion

        public MainView(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            this.eventAggregator = eventAggregator;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            eventAggregator.GetEvent<CloseWindowEvent>().Subscribe(Close, ThreadOption.UIThread, false);
        }
    }
}