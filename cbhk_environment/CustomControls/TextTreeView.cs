using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.CustomControls
{
    public class TextTreeView:TreeView
    {
        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register("HeaderText", typeof(string), typeof(TextTreeView), new PropertyMetadata(default(string)));

        public string HeaderType
        {
            get { return (string)GetValue(HeaderTypeProperty); }
            set { SetValue(HeaderTypeProperty, value); }
        }

        public static readonly DependencyProperty HeaderTypeProperty =
            DependencyProperty.Register("HeaderType", typeof(string), typeof(TextTreeView), new PropertyMetadata(default(string)));
    }
}
