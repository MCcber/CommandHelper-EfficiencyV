using cbhk_environment.CommandClasses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace cbhk_environment.resources.MainFormDataContext
{
    public class CommandClasses: ObservableObject
    {
        /// <summary>
        /// 基础教程
        /// </summary>
        public RelayCommand BasicKnowledgeCommand { get; set; }

        /// <summary>
        /// 进阶教程
        /// </summary>
        public RelayCommand AdvancedKnowledgeCommand { get; set; }

        /// <summary>
        /// 原版模组教程
        /// </summary>
        public RelayCommand OriginalEditionModKnowledgeCommand { get; set; }

        public CommandClasses()
        {
            #region 绑定指令
            BasicKnowledgeCommand = new RelayCommand(basic_knowledge_command);
            AdvancedKnowledgeCommand = new RelayCommand(advanced_knowledge_command);
            OriginalEditionModKnowledgeCommand = new RelayCommand(originaledition_modknowledge_command);
            #endregion
        }

        private void basic_knowledge_command()
        {
            ClassicAndNew can_knowledge = new ClassicAndNew(0);
            can_knowledge.Show();
        }

        private void advanced_knowledge_command()
        {
            ClassicAndNew can_knowledge = new ClassicAndNew(1);
            can_knowledge.Show();
        }

        private void originaledition_modknowledge_command()
        {
            ClassicAndNew can_knowledge = new ClassicAndNew(2);
            can_knowledge.Show();
        }
    }
}
