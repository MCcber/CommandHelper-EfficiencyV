using CBHK.Common.Utility;
using CBHK.Domain.Interface;
using CBHK.Model.Common;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CBHK.Utility.Modifier
{
    public class IntegerRangeModifier(RegexService regexService) : IComponentModifier
    {
        #region Field
        private string range = string.Empty;
        private RegexService RegexService = regexService;
        #endregion

        #region Property
        public Enums.ModiferType ModiferType { get; set; } = Enums.ModiferType.Range;
        #endregion

        public void SetRawdata(string data)
        {
            range = data;
        }

        public bool Verify(string targetData)
        {
            MatchCollection valueArray = RegexService.GetIntegerData().Matches(range);
            int leftRangeValue = int.Parse(valueArray[0].Value);
            int rightRangeValue = 0;
            if (valueArray.Count > 1)
            {
                rightRangeValue = int.Parse(valueArray[1].Value);
            }
            if(rightRangeValue < leftRangeValue)
            {
                return false;
            }
            int targetValue = int.Parse(targetData);
            if (range.StartsWith(".."))
            {
                return targetValue <= leftRangeValue;
            }
            else
            if(range.EndsWith(".."))
            {
                return targetValue >= leftRangeValue;
            }
            else
            if(range.Contains(".."))
            {
                return targetValue >= leftRangeValue && targetValue <= rightRangeValue;
            }
            return false;
        }

        public List<IComponent> Modifier(IComponent targetComponent)
        {
            List<IComponent> result = [];
            return result;
        }
    }
}
