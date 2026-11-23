# 铁路大亨 2 · 大湾区 Demo（Unity）

> 项目正式名 / 内部代号：**山海铁道志（Railway2）**。一款竖屏箱庭式铁路经营模拟：每个区域是一张独立地图，长按车站拖线开线，列车往返结算，公司治理与工程科技两棵 DAG 树带你从 19 世纪蒸汽时代一路经营到 21 世纪高铁时代。

*A portrait tabletop-style railway tycoon sim built with Unity 6 + UGUI. Every screen, map layer and animation is generated at runtime from code — no prefabs, one scene — and shipped as a WebGL game for TapTap H5.*

本仓库是我的个人项目，现已公开：**欢迎提 Issue、发 PR、fork 学习**。仓库名沿用 Railway2-HK-Demo；游戏在代码与商店资料中的正式名为《山海铁道志》，内容覆盖汐港、镜屿、榕川三个大湾区风格箱庭，早期曾以「香港 Demo」起步。世界观与地名均为虚构（见文末说明），与真实城市、机构、企业无关。

---

## 项目状态

| 项目 | 说明 |
| --- | --- |
| 当前迭代 | v1.2.0（2026-11） |
| 引擎 | Unity 6000.6.8f1（Unity 6.0） |
| 渲染 / UI | UGUI，全部界面与地图元素在运行时由代码生成 |
| 目标平台 | WebGL（TapTap H5 为主），亦可导出 Windows / Android |
| 场景 | 只有 `Assets/Scenes/Main.unity`，里面只有一个挂 `DemoBootstrap` 的空物体 |
| 画面规格 | 竖屏，设计分辨率 1080×1920 |
| 存档 | 本地 PlayerPrefs + TapTap 云存档镜像 + 内置测试档热恢复 + 撤销 |
| 语言 | 简体中文（画面文案、注释、文档） |

---

## 玩法概览

### 三个可游玩区域（按顺序解锁）

| 区域 | 原型气质 | 内容 |
| --- | --- | --- |
| **汐港** | 港岛 / 九龙 / 新界 / 大屿山的海港城市 | 港口、跨海、山地地形；门户站通向镜屿、榕川 |
| **镜屿** | 濠江两岸的旧城与新城 | 紧凑老城、填海新区、口岸 |
| **榕川** | 珠三角冲积平原与丘陵 | 数据来自 `Assets/Resources/MapData/guangdong_stations.json`，114 座站 |

每区是独立箱庭：区内自由连线，区域之间不直接互通，靠**跨区门户站**衔接（如北桥关 / 东湾（西渡）、青藤（北关）/ 琴海口、跨海虹桥）。两侧线路在门户处同时接通后，即激活跨区联运补贴与客流加成；交界站可跨区衔接，进入下一区后在同一位置生成对应接口，形成伪互通。**每区线路上限为该区站点数的一半**，交界站计入所连区域。

### 开线

- 不设建线模式：**长按车站约 0.42 秒**触发震动 + 音效开始拖线，经过车站以 44px（随地图缩放反算）热区自动吸附，松手弹「开通确认」。
- **延长优先**：起手站已在某条线路上时，确认弹窗默认「延长 X 号线」；多条线经过该站则逐条列出，同时始终保留「开通新线路」。
- 拖回已连接站点会撤销后段；点线路弹出按线路颜色区分的小气泡：拆该段 / 拆整线 / 详情；拆线退回部分造价（默认 60%，公司特权可升至 72%）。

### 经营

- 时间：1× 速度下约 6 分钟为一个游戏日，日结时结算线路收益与维护。
- 列车在线路两端往返，每到一个站结一段收益；站上飘金色 `+N`，左上角显示每日收入。
- 线路详情：预计日收入 / 预计日利润 / 客流 / 单向区段运力、近 7 日净利润柱状曲线、沿线站点，以及加车与升级入口。
- 车库与调配：车型、编组、班次、分时段调度；涂装与机车商店。
- 经济系统：单段造价随图上距离与地形（山地、跨海）变化；票价、饱和、OD 客流、跨湾协同、维护费与周期性再平衡。

### 两棵 DAG 树

- **工程科技树**：机车 / 车辆 / 基建 / 电气化 / 经营等方向，三个纪元约 127 个节点，节点按年份与前置关系排列，研究消耗 RP，长按可看可研性、费用与效果预估。
- **公司治理树**：约 105 个节点、五个分支、多前置汇合（跨分支边约占 63%），从「雇员名册 / 站前货栈 / 汇票结算」起步，向特许、章程、分账、标准时、清算所等治理制度推进。
- 两棵树共用一套柔性力导向布局 + 矩形碰撞消解，支持环状年份带、收纳圆盘、缩放与二维视角记忆。

### 公司经营与文化内容

- 公司页四大板块：总览社务、高管人才（招募 / 训练 / 指派）、资本招投标（债券、市政标书、周期风向）、特权效率厅。
- 人才、合同、年度评奖、公司印章与等级、剧情章节、每日任务、每日驿报、节气提示、手帐文化日志、历史研究、图鉴打卡、景观明信片与收藏。
- 主题商店（含 3D 霓虹主题）、线路涂装、设置内的字体大小与音效开关。

### 存档与撤销

- 自动存档走主线程友好路径，避免 WebGL 线程限制；写入采用同目录临时文件 + 原子替换，防截断。
- 本地存档 + 云存档镜像（TapTap 环境下每 60 秒自动上传），可手动上传 / 拉取。
- 内置一份测试存档（`Assets/Resources/TestSaves`），可在设置里一键热恢复；`WebGLBuilder` 的正式构建会把它排除在包外。
- 撤销支持开线、升级、购买、拆除、人才与主题等操作，外部进度变化会使旧撤销快照失效。

### TapTap 能力

- 激励视频广告（领金币、收入倍率、主题解锁等）。
- 云存档镜像与排行榜（营收 / 站点 / 线路 / 客流维度）。
- 震动反馈（`haptic.jslib`）。
- **非 TapTap 环境（本地 / 普通浏览器）自动降级为本地逻辑**，不会卡在 SDK 回调上。

---

## 快速开始

1. 安装 **Unity 6000.6.8f1**（Unity Hub 可选安装）。
2. Clone 本仓库并打开工程目录（首次导入约几分钟；仓库含约 24 MB 的字体与若干 MB 音频，clone 体积约 90 MB）。
3. 打开 `Assets/Scenes/Main.unity`，按 **Play**。
4. **Scene 视图里看不到地图是正常的**：地图与全部 UI 都由 `DemoBootstrap` 在运行时生成。运行时自动创建 UICamera 与 AudioListener。
5. 想把 Game 视图调成手机原样：把左上角分辨率设为 **1080×1920**（或任意 9:16）。其他比例由 `CanvasLetterbox` 自适应，不会露相机底色、也不会裁掉顶栏。
6. 常用编辑器菜单：
   - `铁路大亨2 / 开始游戏（Play）`
   - `铁路大亨2 / 打开主场景 Main`
   - `铁路大亨2 / 打开 UI 脚本`
   - `铁路大亨2 / 打开测试存档 JSON`、`同步测试存档（跨设备开发）`

---

## 构建

### 编辑器内

- `Build / Export Android APK`：导出竖屏 APK。
- 其余构建入口集中在 `Assets/Editor/WebGLBuilder.cs`（可直接在 IDE 里调用，或加自己的 [MenuItem]）：
  - `WebGLBuilder.BuildWebGL()` → `Build/WebGL`
  - `WebGLBuilder.BuildWindows()` → `Build/Windows`
  - `WebGLBuilder.BuildAndroid()` → `Build/Android`
  - `WebGLBuilder.BuildTrailerWebGL()` → 带 `TRAILER_PREVIEW` 宏的预告片版本
- `WebGLBuilder` 的 WebGL / Windows / Android 构建会在构建期间临时移走 `Assets/Resources/TestSaves`，确保测试存档不进包；`DemoBuild.BuildWebGL` 是更早的简易入口，不做这一步。

### 命令行批量构建

```powershell
# WebGL
& "<UnityEditorPath>\Unity.exe" -batchmode -quit -projectPath "<ProjectPath>" ^
  -executeMethod WebGLBuilder.BuildWebGL -logFile "<ProjectPath>\Build\unity-webgl.log" -nographics

# Windows
& "<UnityEditorPath>\Unity.exe" -batchmode -quit -projectPath "<ProjectPath>" ^
  -executeMethod WebGLBuilder.BuildWindows -logFile "<ProjectPath>\Build\unity-win.log" -nographics
```

小提示：工程被 Unity 编辑器打开时，`Temp/UnityLockfile` 会阻止第二个 batchmode 实例使用同一工程；需要并行构建时请先关闭编辑器，或把工程复制到另一个目录再构建。

### 本地预览

WebGL 产物必须通过 HTTP 提供服务（`file://` 无法加载）：

```powershell
python -m http.server 8123 --directory Build/WebGL
# 浏览器打开 http://127.0.0.1:8123/
```

### 已知构建注意事项

- WebGL 压缩格式与解压回退由 `WebGLBuilder` / `DemoBuild` 设置；`DemoBuild.BuildWebGL` 还会用 `Assets/Editor/WebGLTemplate.html` 覆盖 `index.html`（全屏 CSS、加载遮罩）。
- 首次导入工程会自动生成 Library，属正常现象；仓库不包含 `Library/`、`Build/`、`Temp/`、`Logs/`。

---

## 目录结构

```
Assets/
  Editor/            编辑器工具、构建脚本、WebGL 模板、存档往返审计入口
  Plugins/WebGL/     TapTapWebGLBridge.jslib（广告/云存档/排行榜）、haptic.jslib
  Resources/
    Audio/           区域 BGM、经典曲目、SFX（运行时按区切换）
    Fonts/           霞鹜文楷 LXGWWenKai-Regular.ttf（SIL OFL 1.1）
    FleetArt/        车库、商店、调度页水彩背景
    MapArt/          汐港、镜屿、榕川的手绘插画底图
    MapData/         榕川站点数据（含站名、城市、投影坐标、跨区标记）
    MapIcons/        景观图标（自然 / 人文）
    PageArt/         各整页水彩底图、地区标题牌、纹样装饰
    Research/        机车与车厢图鉴（19–21 世纪约 20 张插画）
    Scenery/         景观明信片
    TestSaves/       内置测试存档（仅编辑器/测试流程使用）
  Scenes/Main.unity  唯一场景，仅含 Bootstrap
  Scripts/           运行时全部逻辑（Demo* 分模块，见下）
docs/                设计稿、区域与地图数据说明、实施记录、参考素材
scripts/             Python 审计脚本与存档往返测试驱动
```

### 运行时脚本怎么分工

`DemoBootstrap` 是一个 `partial class`，按领域拆分到多个文件（合计约 200 KB），便于并行改：

| 文件 | 负责 |
| --- | --- |
| `DemoBootstrap.cs` | 状态、HUD、地图构建、站点与线路、列车、时间与结算、Toast、焦点相机 |
| `DemoScreens.cs` | 线路、车库、公司、科技、图鉴、地区等整页与通用控件 |
| `DemoCompanySystems.cs` | 公司治理树 DAG、人才、合同、标书、特权 |
| `DemoResearch.cs` | 工程科技树、节点效果、RP 与预估 |
| `DemoTraffic.cs` | OD 客流、班次、时段、维护与收益结算 |
| `DemoLineInteractions.cs` | 拖线、吸附、点线气泡、拆段拆线 |
| `DemoMobile.cs` | 长按、触摸、拖拽、触感、设置页 |
| `DemoSave.cs` | 存档读写、旧档迁移、测试档同步、主线程自动存档 |
| `DemoUndo.cs` | 操作栈、快照与失效规则 |
| `DemoScreens` 配套 | `DemoBulletin`（驿报/排行榜）、`DemoJournalCulture`（手帐/节气）、`DemoHistoryStudies`、`DemoStoryDirector`、`DemoSceneryViewer`、`DemoEffects`、`DemoAudio`、`DemoBaySynergy` |
| `HistoryGraphLayout` / `HistoryGraphZoom` / `HistoryTreeEffects` | 双树布局、缩放、环带与收纳圆盘 |
| `TapTapSdkBridge.cs` | 与 jslib 通信的广告 / 云存档 / 排行榜 / 账号桥接 |
| `CanvasLetterbox` / `MapWheelZoom` / `PageHeaderLayout` | 画布适配、地图缩放、页眉布局 |

初次接触代码时的阅读顺序建议：`DemoBootstrap.BuildUI`（界面装配）→ `DemoLineInteractions`（拖线开线）→ `DemoTraffic`（收益结算）→ `DemoSave`（存档）→ `DemoResearch` / `DemoCompanySystems`（双树）。

### 地图是怎么画的

- 底图：`Assets/Resources/MapArt/*.jpg` 手绘插画（三个区域各一张），运行时挂为 `RawImage` 并跟随主题染色。
- 叠加层：代码绘制的海面/海岸渐变、等高线笔触、区域名标注（`DemoMapVector` 与 `DemoMapRaster`）。
- 站点、线路、列车、跨区箭头：全部是 UGUI 图形，按地图局部坐标摆放；地图缩放时站点图标与列车按 `1/scale` 反向缩放，保证屏幕尺寸稳定。
- 榕川的地图数据由 `docs/map-data/` 下的 Python 脚本从提取数据生成，站点投影与底图使用同一经纬范围。

---

## 审计与测试

项目里的非交互验证尽量做成可重复脚本（Python + 编译程序集），不需要打开编辑器：

```bash
python scripts/audit_company_dag.py            # 公司树 DAG：扇出、汇合、可达性、前六步穷举
python scripts/audit_research_incremental.py   # 科技树增量与预估
python scripts/audit_research_copy_layout.py   # 文案 + 字号布局度量
python scripts/audit_scroll_fleet.py           # 滚动记忆与调配页
python scripts/audit_save_integrity.py         # 存档字段读写对称性
./scripts/run_save_round_trip.ps1              # 存档 JSON 往返（托管 + 原生）
```

Unity 内另有菜单 `Railway2 / Audit / Save round trip (isolated)`，在隔离沙盒里跑 `CaptureSaveData` → JSON → `ApplySaveData` 往返。

---

## 如何参与

- **提意见 / 报 Bug**：开 Issue 即可，尽量带上 Unity 版本、复现步骤、Console 日志和相关截图；涉及数值/布局的问题请说明所在区域与页面。
- **PR**：欢迎小而专注的改动（修 Bug、文案、性能、适配）。请先开 Issue 讨论方向，避免大规模重构。个人项目的合并判断以维护者为准。
- **分支**：默认从 `main` 切 `feature/<主题>`；发布相关用 `release/<版本>`。历史备份分支 `backup/*` 只作归档，不要基于它们提 PR。
- **请不要提交**：`Library/`、`Build/`、`Temp/`、`Logs/`、`obj/`、`UserSettings/`，也不要提交包含个人数据的存档或任何密钥。
- **改动前**：因 UI 全为运行时程序化生成，避免引入 Prefab/场景内手动排版；改布局请先跑对应审计脚本，改数值请说明依据。
- **代码风格**：C# 以现有 `Demo*` 文件为参照，保持编译 0 error；中文文案统一在 `DemoBootstrap` 的文案常量/生成函数中维护。

---

## 素材与许可

- **代码**：本项目**暂不提供开源许可证**，即默认保留所有权利（All Rights Reserved）。你可以阅读、fork 学习、提 Issue 与 PR；未经作者许可，不得复制后另行发布或用于商业用途。需要授权请开 Issue 沟通。
- **字体**：霞鹜文楷（LXGW WenKai）采用 SIL Open Font License 1.1，可随游戏一同分发。
- **地图数据**：镜屿底图参考 OpenStreetMap 贡献者的海岸线数据（Geofabrik 的 Macau 提取）经简化与风格化处理，遵循 ODbL 1.0，画面标注 © OpenStreetMap contributors；数据与生成脚本见 `docs/map-data/`。示意底图不用于导航或测绘。
- **插画、音频、图鉴、纹样**：版权归作者所有，未经许可不得单独提取商用或在其他项目中再分发。
- **世界观说明**：本作使用完全虚构的地名、公司名与年份叙事（汐港 / 镜屿 / 榕川、山海铁道社等），与任何真实城市、行政区域、企业或历史事件无对应关系；游戏内数值为玩法平衡，非史实。

---

## 致谢

- [Unity](https://unity.com/) 6 与 UGUI。
- [霞鹜文楷 LXGW WenKai](https://github.com/lxgw/LxgwWenKai)（SIL OFL 1.1）。
- OpenStreetMap 贡献者与 [Geofabrik](https://download.geofabrik.de/) 的提取数据（ODbL 1.0）。
- TapTap 平台的 H5 / 小游戏能力（广告、云存档、排行榜）。