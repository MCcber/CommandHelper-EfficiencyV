// 属性系统：等价 spyglass 的 runtime/attribute（handleAttributes / shouldKeepAccordingToAttributeFilters）。
// 取舍：spyglass 用 McdocAttribute 接口挂 8 个回调 + validator 体系，这里压成一个 handler 的三个可选委托，
// 只保留求值真正用到的 filterElement / mapType / mapField。
using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data.Mcdoc
{
    public sealed class McdocAttributeHandler
    {
        /// <summary>返回 false 表示该元素（字段/联合成员/枚举项）在当前版本应被丢弃。</summary>
        public Func<McdocAttrValue, McdocRuntime, bool> FilterElement;
        public Func<McdocAttrValue, McdocType, McdocType> MapType;
        public Func<McdocAttrValue, McdocStructField, McdocStructField> MapField;
    }

    public sealed class McdocAttributeRegistry
    {
        private readonly Dictionary<string, McdocAttributeHandler> handlers = new(StringComparer.Ordinal);

        public void Register(string name, McdocAttributeHandler handler)
        {
            if (string.IsNullOrEmpty(name) || handler == null) return;
            handlers[name] = handler;
        }

        /// <summary>遍历已注册的属性处理器；未注册的属性静默跳过（与 spyglass 一致）。</summary>
        public void Handle(IEnumerable<McdocAttribute> attributes, McdocRuntime runtime, Action<McdocAttributeHandler, McdocAttrValue> action)
        {
            if (attributes == null || action == null) return;
            foreach (var attribute in attributes)
            {
                if (attribute == null || !handlers.TryGetValue(attribute.Name ?? "", out var handler)) continue;
                action(handler, attribute.Value);
            }
        }

        public bool ShouldKeep(IEnumerable<McdocAttribute> attributes, McdocRuntime runtime)
        {
            var keep = true;
            Handle(attributes, runtime, (handler, config) =>
            {
                if (!keep || handler.FilterElement == null) return;
                if (!handler.FilterElement(config, runtime)) keep = false;
            });
            return keep;
        }

        /// <summary>
        /// 内置属性：since / until / deprecated / id。
        /// id 的 mapType 对应 spyglass runtime/attribute/builtin.js:124-137：literal(string) 的值与
        /// enum(string) 的每个值都过一遍 ResourceLocation.lengthen；其它类型原样，config 不参与判定。
        /// since 为包含下限（≥），until 为排除上限（&lt;），与 misode 中 ReleaseVersion.cmp 的注册一致。
        /// </summary>
        public static McdocAttributeRegistry CreateDefault(string gameVersion)
        {
            var registry = new McdocAttributeRegistry();
            registry.Register("since", new McdocAttributeHandler
            {
                FilterElement = (config, _) => CompareVersions(gameVersion, config?.AsString()) >= 0
            });
            registry.Register("until", new McdocAttributeHandler
            {
                FilterElement = (config, _) => CompareVersions(gameVersion, config?.AsString()) < 0
            });
            registry.Register("deprecated", new McdocAttributeHandler
            {
                MapField = (config, field) =>
                {
                    if (config == null || string.IsNullOrEmpty(config.AsString()))
                    {
                        var clone = field.Clone();
                        clone.Deprecated = true;
                        return clone;
                    }
                    if (CompareVersions(gameVersion, config.AsString()) >= 0)
                    {
                        var clone = field.Clone();
                        clone.Deprecated = true;
                        return clone;
                    }
                    return field;
                }
            });
            // #[canonical]：spyglass 侧是空 handler（builtin.js:58-60），真正的行为硬编码在
            // simplifyUnion 里（requireCanonical 时只留带 canonical 的成员）。这里注册只为"可识别"。
            registry.Register("canonical", new McdocAttributeHandler());
            // #[dispatcher_key="<registry>"]：候选来自该 dispatcher 的成员键（builtin.js:61-77 的 stringMocker），
            // 没有 FilterElement / MapType，取值由 McdocSelectOptions.Apply 处理。
            registry.Register("dispatcher_key", new McdocAttributeHandler());
            registry.Register("id", new McdocAttributeHandler
            {
                // 只实现 mapType：spyglass 的 #[id] 没有 filterElement（版本过滤与它无关）。
                MapType = (config, type) =>
                {
                    if (type is null) return type;
                    if (type.Kind == McdocTypeKind.Literal && type.LiteralKind == McdocLiteralKind.String)
                    {
                        var clone = type.Clone();
                        clone.Value = McdocIdSpec.LengthenResourceLocation(type.LiteralText());
                        return clone;
                    }
                    if (type.Kind == McdocTypeKind.Enum && type.EnumKind == McdocTypeKind.String)
                    {
                        var clone = type.Clone();
                        clone.EnumValues = [];
                        foreach (var value in type.EnumValues ?? [])
                        {
                            if (value is null) continue;
                            clone.EnumValues.Add(new McdocEnumValue
                            {
                                Identifier = value.Identifier,
                                Value = McdocIdSpec.LengthenResourceLocation(Convert.ToString(value.Value)),
                                Desc = value.Desc,
                                Attributes = value.Attributes,
                            });
                        }
                        return clone;
                    }
                    return type;
                }
            });
            return registry;
        }

        /// <summary>按 "1.20.5" 这类版本号逐段比较；缺失段按 0 处理。任一侧为空 → 视为无约束。</summary>
        public static int CompareVersions(string left, string right)
        {
            if (string.IsNullOrEmpty(right)) return 0;
            if (string.IsNullOrEmpty(left)) return 0;
            var a = left.Split('.');
            var b = right.Split('.');
            var length = Math.Max(a.Length, b.Length);
            for (var i = 0; i < length; i++)
            {
                var av = i < a.Length && int.TryParse(a[i], out var parsedA) ? parsedA : 0;
                var bv = i < b.Length && int.TryParse(b[i], out var parsedB) ? parsedB : 0;
                if (av != bv) return av < bv ? -1 : 1;
            }
            return 0;
        }
    }
}
