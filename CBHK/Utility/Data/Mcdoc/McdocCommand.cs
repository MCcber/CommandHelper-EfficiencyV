// 极简 ICommand：树模板上的 ⊕/⊖ 等按钮只需要"能执行"与"可执行"。
// 注意：CanExecuteChanged 必须是**真的**事件 —— 树里按钮的可用性随输入变化，
// 用 add{} remove{} 空实现的话，键一改按钮不会重新求值，表现就是"按钮始终灰着"。
using System;
using System.Windows.Input;

namespace CBHK.Utility.Data.Mcdoc
{
    public sealed class McdocCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        public McdocCommand(Action execute, Func<bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => execute();

        /// <summary>状态变了（如键被输入）由数据层调用，让绑定到这个命令的按钮重新求值。</summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
