# CBHK.Probe

判据探针：不参与产品构建，只用来回答「这条行为对不对」「这次改快了多少」。

退出码约定：`0` 全绿，`1` 有红，`2` 环境不足（不许当成 SKIP 混过去）。

## 构建

```powershell
dotnet build CBHK.Probe\CBHK.Probe.csproj -c Debug /p:SkipComInterop=true `
  /p:CustomBeforeMicrosoftCommonTargets="$PWD\CBHK.Probe\skip-com-interop.targets"
```

`skip-com-interop.targets` 让被引用的 CBHK 项目跳过 tlbimp，改用已生成的 Interop 程序集（没有它每次都要重新跑 COM 引用解析）。

## 全量回归

```powershell
.\CBHK.Probe\regress.ps1
```

按 `Program.cs` 里注册的 `--case` 逐个跑一遍，再跑三个内置 gate，末行 `ALL GREEN <条数>` 或 `FAILED: <清单>`。

## 入口

| 入口 | 用途 |
|---|---|
| `--case <名字>` | 单条行为门禁（名字见 `Program.cs` 的 switch） |
| `--gate sync\|parse\|caret` | 文本同步 / 解析 / 光标三个内置 gate |
| `--check-trace <日志>` | 校验 `%TEMP%\mcdoc-trace.log` 的写入轨迹 |
| `--render <目录>` / `--render-dimension <目录>` | 离屏渲染整棵树出 PNG（像素核对用） |
| `--render-window <目录>` | 真窗口首帧计时：分档消融 + 视觉节点普查（见下） |
| `--virtualization <目录>` | 大行数下量「实到容器数 / 视觉节点数」，验虚拟化与 Recycling |
| `--perf item` | 物品页建页耗时 |

## 性能回归怎么读

- `--render-window`：先打印 `[计时] A/B/C`（资源字典 / schema+注册表 / VM 构造）、`[资源]`（行内图标 key 是否命中）、`[计数]`（建页期间注册表成员表 `GetIds` 被调用几次），再逐档 `ShowOnce` 打印 `构造=…ms Show=…ms 泵=…ms`，末尾 `[视觉] 整窗 visual 合计=… 行=… 每行=…` 与 `[视觉] 图标按钮：已摊开模板 N / 只有元素没有模板 M`。
- `--virtualization`：618 / 3018 / 15018 行三档，打印 `实到容器`、`视觉节点`、`可容纳≈N 行`，滚到底再数一次看容器是否被复用（下标跳跃而不是新增）。
- 本机单次测量波动可达 50%，一律「同一档跑多次取最小」；比较改造前后必须同一天同一批跑。

## 基线（2026-11-19，1080x700 屏外窗口）

| 指标 | 值 |
|---|---|
| 维度页 17 行：整窗 visual | 1573（92.5/行） |
| 维度页 17 行：只树 Show | ≈159–186ms |
| 维度页 17 行：真窗口 Show | ≈469–536ms（进程内首批窗口另有一次 ~270ms 一次性开销） |
| 15018 行虚拟化档 | 实到容器 35、visual 3103、Show ≈369ms |
| 行内动作按钮 | 每行一颗（一颗动作就地常驻 / 两颗以上进 ⋮） |
| 1MB JSON 击键（`--case sync-typing`） | p50 ≈ 20–53ms、p95 ≈ 53–86ms；0 次整篇重建 |
