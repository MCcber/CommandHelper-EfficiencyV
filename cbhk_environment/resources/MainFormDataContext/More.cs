using cbhk_environment.More;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace cbhk_environment.resources.MainFormDataContext
{
    public class More:ObservableObject
    {
        /// <summary>
        /// 进群交流
        /// </summary>
        public RelayCommand ConversationGroupCommand { set; get; }

        /// <summary>
        /// 反馈bug
        /// </summary>
        public RelayCommand FeedBackBugsCommand { get; set; }

        /// <summary>
        /// 关于编辑器
        /// </summary>
        public RelayCommand AboutUsCommand { get; set; }

        public RelayCommand NoticeToUsersCommand { get; set; }

        public RelayCommand DonateUsCommand { get; set; }

        public More()
        {
            ConversationGroupCommand = new RelayCommand(conversationGroupCommand);
            FeedBackBugsCommand = new RelayCommand(feedbackBugsCommand);
            AboutUsCommand = new RelayCommand(aboutUsCommand);
            NoticeToUsersCommand = new(noticeToUsersCommand);
            DonateUsCommand = new(donateUsCommand);
        }

        /// <summary>
        /// 关于赞助
        /// </summary>
        private void donateUsCommand()
        {
            DonateUs donateUs = new();
            donateUs.Topmost = true;
            donateUs.Show();
            donateUs.Focus();
            donateUs.Topmost = false;
        }

        /// <summary>
        /// 进群交流
        /// </summary>
        private void conversationGroupCommand()
        {
            ConversationGroup conversation_group = new();
            conversation_group.Show();
        }

        /// <summary>
        /// 反馈bug
        /// </summary>
        private void feedbackBugsCommand()
        {
            FeedBackBugs feedback_bugs = new();
            feedback_bugs.Show();
        }

        /// <summary>
        /// 关于编辑器
        /// </summary>
        private void aboutUsCommand()
        {
            AboutUs about_us = new();
            about_us.Show();
        }

        /// <summary>
        /// 打开用户须知窗体
        /// </summary>
        private void noticeToUsersCommand()
        {
            NoticeToUsers noticeToUsers = new();
            noticeToUsers.ShowDialog();
        }
    }
}
