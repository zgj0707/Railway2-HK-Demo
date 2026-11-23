# 铁路大亨 2 · 大湾区 Demo（Unity）

## 打开与运行

1. Unity Hub 里打开本工程（`E:\aigc\Railway2_unity`），或直接打开 `Assets/Scenes/Main.unity`。
2. 按顶部 **Play**。地图和全部 UI 都是运行时由 `DemoBootstrap` 生成的，Scene 视图里只有 Bootstrap，看不到地图是正常的。
3. Game 视图不会再报“无相机”：运行时自动创建 `UICamera` 和 `AudioListener`。

> **预览比例**：UI 按 1080×1920 竖屏设计，画布由 `CanvasLetterbox` 自适应——窗口比 9:16 更宽时按高度适配（两侧留海面），更窄时按宽度适配（上下留海面）。任何窗口比例下 UI 都完整可见，不会露相机底色、也不会裁掉顶部 HUD。
> 想在 Game 视图看到“手机原样”，把左上角分辨率从 `Free Aspect` 改成 `1080×1920` 即可。

## 在哪里编辑

UI 不是场景里可拖拽的对象，而是代码生成的：

- `Assets/Scripts/DemoBootstrap.cs`：车站、顶部 HUD、弹窗、线路/列车逻辑；地图底图在 `BuildCodeMap` 中用代码矢量绘制
- `Assets/Scripts/DemoMapVector.cs`：地图矢量组件（陆地填色、海岸线、等高线、海浪笔触），不依赖底图图片
- `Assets/Scripts/DemoScreens.cs`：线路、车库、公司、科技、图鉴等全屏页面
- `Assets/Scripts/DemoMobile.cs`：长按、拖线、触感等移动端输入

在 Hierarchy 选中 `Bootstrap`，Inspector 里有快捷按钮可直接打开这些脚本；菜单 `铁路大亨2` 下也有“打开 UI 脚本 / 开始游戏（Play）/ 打开主场景 Main”。

> 当前不做 Scene 可视化编辑；UI 仍以代码生成，方便快速迭代和统一手机比例。

## 当前交互

- 长按车站 0.42s 开始拖线，44px 小热区吸附；拖回已连接站点会撤销后段。
- 点线路弹出按线路颜色区分的小气泡：拆该段 / 拆整线 / 详情；多条线并行时同时弹多个。
- 详情大弹窗显示近 7 日实收、今日实收、客流、运力、列车与升级情况。
- 景观打卡不再自动弹窗，图鉴图标出现红点，进入图鉴点击待打卡卡片领取。

## 构建

`.tmp/build_throttled.ps1` 会调用 Unity batchmode 的 `DemoBuild.BuildWebGL`，产物在 `Build/WebGL`。
