using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CBHK.CustomControl.VectorButton
{
    public partial class TagBlock:Button
    {
        #region Property
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(TagBlock), new PropertyMetadata(default(string)));

        public IRelayCommand Delete
        {
            get { return (IRelayCommand)GetValue(DeleteProperty); }
            set { SetValue(DeleteProperty, value); }
        }

        public static readonly DependencyProperty DeleteProperty =
            DependencyProperty.Register("Delete", typeof(IRelayCommand), typeof(TagBlock), new PropertyMetadata(default(IRelayCommand)));
        #endregion

        #region Method
        public TagBlock()
        {
            Delete = new RelayCommand(DeleteButton);
        }
        #endregion

        #region Event
        public void DeleteButton()
        {
            InlineUIContainer parentContainer = Parent as InlineUIContainer;
            Paragraph paragraph = parentContainer.Parent as Paragraph;
            paragraph.Inlines.Remove(parentContainer);
        }
        #endregion
    }
}
