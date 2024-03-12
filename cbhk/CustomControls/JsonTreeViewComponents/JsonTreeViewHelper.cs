using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Chat;

namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public static class JsonTreeViewHelper
    {
        public enum ModifyTypes
        {
            Value,
            Key,
            Object,
            Array,
            Error
        }

        public static ModifyTypes ScanAndDetermineTheOperationObject (ref string json,int caretOffset)
        {
            return ModifyTypes.Error;
        }
    }
}
