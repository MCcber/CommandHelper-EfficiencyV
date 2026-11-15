"""校验 SupplementalData.json 只装"注册表目录里没有"的数据。

这个文件存在的唯一理由是补注册表与 mcdoc 资产库都拿不到的那几组命令语法枚举，
所以它必须一直很小。一旦有人把注册表数据（item / block / entity_type / ...）抄进来，
就是又造了一份会漂移的副本——这个脚本负责把这种改动拦下来。

用法：
    python check-supplement.py [Registries 目录] [SupplementalData.json 路径]

Registries 目录缺省按 ../../../bin/Debug/net8.0-windows10.0.22621.0/Resource/Registries 推断，
找不到就只做形状校验。
"""

import json
import os
import sys

ALLOWED_META_KEYS = {"_comment", "Version"}


def load_registry_names(directory):
    """返回注册表目录里出现过的全部注册表名（跨版本取并集）。"""
    names = set()
    if not directory or not os.path.isdir(directory):
        return names
    for entry in os.listdir(directory):
        if not entry.endswith(".json"):
            continue
        try:
            with open(os.path.join(directory, entry), encoding="utf-8") as handle:
                data = json.load(handle)
        except Exception:
            continue
        if isinstance(data, dict):
            names.update(data.keys())
    return names


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    supplement_path = sys.argv[2] if len(sys.argv) > 2 else os.path.join(here, "SupplementalData.json")
    registries = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
        here, "..", "..", "..", "bin", "Debug", "net8.0-windows10.0.22621.0", "Resource", "Registries")

    with open(supplement_path, encoding="utf-8") as handle:
        supplement = json.load(handle)

    registry_names = load_registry_names(os.path.abspath(registries))
    keys = [key for key in supplement if key not in ALLOWED_META_KEYS]

    duplicated = sorted(key for key in keys if key in registry_names)
    size = os.path.getsize(supplement_path)
    entries = sum(len(value) for value in supplement.values() if isinstance(value, list))
    entries += sum(len(value) for value in supplement.values() if isinstance(value, dict))

    print(f"file       : {supplement_path}")
    print(f"size       : {size} bytes")
    print(f"keys       : {len(keys)} -> {', '.join(keys)}")
    print(f"entries    : {entries}")
    print(f"registries : {len(registry_names)} names loaded from {os.path.abspath(registries)}")

    if duplicated:
        print(f"FAIL 这些键在注册表目录里已经有数据，不该再存一份：{duplicated}")
        return 1
    if size > 64 * 1024:
        print(f"FAIL 文件已经 {size} 字节，说明它不再是'注册表缺的那几组'了")
        return 1

    print("OK 只包含注册表目录里没有的数据")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
