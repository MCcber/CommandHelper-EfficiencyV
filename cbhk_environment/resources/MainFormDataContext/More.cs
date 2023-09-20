using cbhk_environment.More;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace cbhk_environment.resources.MainFormDataContext
{
    public class More:ObservableObject
    {
        public RelayCommand NoticeToUsersCommand { get; set; }


        public More()
        {
            NoticeToUsersCommand = new(noticeToUsersCommand);
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
