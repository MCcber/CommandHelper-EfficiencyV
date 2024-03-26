using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public class Enums
    {
        public enum DataTypes
        {
            String,
            BlockID,
            ItemID,
            BlockTag,
            ItemTag,
            Enum,
            EntityID,
            Byte,
            Short,
            Int,
            Float,
            Double,
            Long,
            Compound,
            Array
        }

        public enum IntProviderStructures
        {
            Constant,
            ConstantPlus,
            Uniform,
            BiasedToBottom,
            Clamped,
            ClampedNormal,
            WeightedList
        }

        public enum FloatProviderStructures
        {
            Constant,
            ConstantPlus,
            Uniform,
            ClampedNormal,
            trapezoid
        }

        public enum Dimensions
        {
            Dimension,
            DimensionType
        }

        public enum ModifyTypes
        {
            Value,
            Key,
            Object,
            Array,
            Error
        }
    }
}
