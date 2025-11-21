using CBHK.CustomControl.MCDocument;
using CBHK.Domain.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CBHK.Utility.Modifier
{
    public class AttributeModifier : IComponentModifier
    {
        #region Field
        private List<string> AttributeList = [];
        private Dictionary<string, Type> AttributeDictionary = new()
        {
            { "canonical",typeof(UnionComboBox) },//联合体下拉框
            { "color",typeof(ColorButton) },//拾色器
            { "command",typeof(RuleTextBox) },//命令文本框
            { "deprecated",typeof(TipTextBlockInBorder) },//提示过期
            { "divisible_by",typeof(RuleNumberBox) },//能被指定数字整除
            { "entity",typeof(EnumComboBox) },//实体选择器
            { "game_rule",typeof(EnumComboBox) },//游戏规则
            { "id",typeof(EnumComboBox) },//资源定位符
            { "match_regex",typeof(RuleTextBox) },//匹配一个符合正则表达式规则的字符串
            { "nbt",typeof(RuleTextBox) },//SNBT字符串
            { "nbt_path",typeof(RuleTextBox) },//SNBT路径
            { "objective",typeof(EnumComboBox) },//记分板变量
            { "regex_pattern",typeof(RuleTextBox)},//匹配一个正则表达式字符串
            { "score_holder",typeof(EnumComboBox)},//记分对象，可以是实体或玩家名
            { "since",typeof(TipTextBlockInBorder)},//从某版本开始出现
            { "tag",typeof(RuleTextBox)},//Tag标签
            { "team",typeof(RuleTextBox)},//队伍名
            { "text_component",typeof(RuleTextBox)},//文本组件字符串
            { "until",typeof(TipTextBlockInBorder)},//直到某版本不可用
            { "bitfield",typeof(EnumComboBox)},//位域，用于进行逻辑或运算得出多选项总和的值
        };
        #endregion

        #region Method
        public List<IComponent> Modifier(IComponent targetComponent)
        {
            List<IComponent> result = [];
            string startPart = "#[";
            foreach (var attributeItem in AttributeList)
            {
                //调度器键
                if (attributeItem.Contains("dispatcher_key"))
                {

                }
                else
                {
                    Match notWordMatch = Regex.Match(attributeItem[startPart.Length..],@"\W");
                    if(notWordMatch is not null && notWordMatch.Index > 0 && AttributeDictionary.TryGetValue(attributeItem[startPart.Length..notWordMatch.Index],out Type type))
                    {
                        object instanceControl = Activator.CreateInstance(type);
                        if (instanceControl is IContainerComponent mcDocumentContainer)
                        {

                        }
                        else
                        if (instanceControl is IComponentModifier mcDocumentModifier)
                        {

                        }
                        else
                        if (instanceControl is IComponent mcDocumentComponent)
                        {

                        }
                    }
                }
            }
            return result;
        }

        public void SetRawdata(string data)
        {
            MatchCollection matchCollection = Regex.Matches(data,@"(#\[[()="":,\d\.]+])");
            AttributeList.Clear();
            AttributeList.AddRange(matchCollection.Select(item => item.Value));
        }

        public bool Verify(string targetData)
        {
            bool result = false;
            return result;
        }
        #endregion
    }
}