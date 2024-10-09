namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public class Enums
    {
        public enum ModifyType
        {
            Remove,
            Get
        }

        public enum ReplaceType
        {
            Direct,
            Input,
            String,
            AddElement,
            RemoveElement,
            AddArrayElement,
            RemoveArrayElement,
            Compound
        }

        public enum DataTypes
        {
            Input,
            Bool,
            String,
            BlockID,
            ItemID,
            BlockTag,
            ItemTag,
            Enum,
            EntityID,
            Byte,
            Decimal,
            Short,
            Int,
            Float,
            Double,
            Long,
            Compound,
            CustomCompound,
            OptionalCompound,
            NullableCompound,
            OptionalAndNullableCompound,
            EnumCompound,
            MultiType,
            ArrayElement,
            Array,
            InnerArray,
            ValueProvider
        }

        public enum ValueProviderTypes
        {
            IntProvider,
            BlockState,
            FloatProvider,
            HeightProvider,
            VerticalAnchor,
            ParameterPoint,
            PositionRuleTest,
            PositionSource,
            Processor,
            RuleTest,
            SurfaceRule,
            SurfaceRuleCondition
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
