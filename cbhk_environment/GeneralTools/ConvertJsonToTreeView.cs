using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace cbhk_environment.GeneralTools
{
    public class ConvertJsonToTreeView
    {
        //用于对应Json对象类型的格式化字符
        const string NULL_TEXT = "<null>";
        const string ARRAY = "[{0}]";
        const string OBJECT = "[{0}]";
        const string PROPERTY = "{0}";
        //用于界面绑定的属性定义
        public string Header { get; private set; }
        public IEnumerable<ConvertJsonToTreeView> Children { get; private set; }
        public JToken Token { get; private set; }
        //内部构造函数，使用FromJToken来创建ConvertJsonToTreeView

        public ConvertJsonToTreeView(JToken token, string header, IEnumerable<ConvertJsonToTreeView> children)
        {
            Token = token;
            Header = header;
            Children = children;
        }

        //外部的从JToken创建ConvertJsonToTreeView的方法

        public static ConvertJsonToTreeView FromJToken(JToken jtoken)
        {
            if (jtoken == null)
            {
                throw new ArgumentNullException("jtoken");
            }
            var type = jtoken.GetType();
            if (typeof(JValue).IsAssignableFrom(type))
            {
                var jvalue = (JValue)jtoken;
                var value = jvalue.Value;
                if (value == null)
                {
                    value = NULL_TEXT;
                }
                return new ConvertJsonToTreeView(jvalue, value.ToString(), null);
            }
            else if (typeof(JContainer).IsAssignableFrom(type))
            {
                var jcontainer = (JContainer)jtoken;
                var children = jcontainer.Children().Select(c => FromJToken(c));
                string header = "";
                if (typeof(JProperty).IsAssignableFrom(type))
                    header = ((JProperty)jcontainer).Name;
                return new ConvertJsonToTreeView(jcontainer, header, children);
            }
            else
            {
                throw new Exception("不支持的JToken类型");
            }

        }

    }
}