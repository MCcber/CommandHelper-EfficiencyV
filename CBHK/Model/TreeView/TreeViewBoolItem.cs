using CBHK.Interface.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class TreeViewBoolItem : IBaseItem, IBaseKeyItem, IBoolItem, ITreeViewItem, INotifyPropertyChanged
    {
        #region Field
        private bool isTrue = true;
        private bool isFalse = false;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Property
        public string Key { get; set; }
        public bool IsRequired { get; set; }
        public Brush Foreground { get; set; } = Brushes.White;
        public Brush Background { get; set; } = Brushes.Transparent;

        public bool IsTrue
        {
            get
            {
                return isTrue;
            }
            set
            {
                if (!Equals(isTrue, value))
                {
                    isTrue = value;
                    OnPropertyChanged(nameof(IsTrue));
                    if (IsRequired)
                    {
                        IsFalse = !value;
                        OnPropertyChanged(nameof(IsFalse));
                    }
                }
            }
        }

        public bool IsFalse
        {
            get
            {
                return isFalse;
            }
            set
            {
                if (!Equals(isFalse, value))
                {
                    isFalse = value;
                    OnPropertyChanged(nameof(IsFalse));
                    if (IsRequired)
                    {
                        IsTrue = !value;
                        OnPropertyChanged(nameof(IsTrue));
                    }
                }
            }
        } 
        #endregion

        #region Event
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        } 
        #endregion
    }
}
