# 铁路大亨 2 · 香港 Demo（Unity）

## 打开与运行

1. Unity Hub 里打开本工程（`E:\aigc\Railway2_unity`），或直接打开 `Assets/Scenes/Main.unity`。
2. 按顶部 **Play**。地图和全部 UI 都是运行时由 `DemoBootstrap` 生成的，Scene 视图里只有 Bootstrap，看不到地图是正常的。
3. Game 视图不会再报“无相机”：运行时自动创建 `UICamera` 和 `AudioListener`。

## 在哪里编辑

UI 不是场景里可拖拽的对象，而是代码生成的：

- `Assets/Scripts/DemoBootstrap.cs`：地图、车站、顶部 HUD、弹窗、线路/列车逻辑
- `Assets/Scripts/DemoScreens.cs`：线路、车库、公司、科技、图鉴等全屏页面
- `Assets/Scripts/DemoMobile.cs`：长按、拖线、触感等移动端输入

在 Hierarchy 选中 `Bootstrap`，Inspector 里有快捷按钮可直接打开这些脚本；菜单 `铁路大亨2` 下也有“打开 UI 脚本 / 开始游戏（Play）/ 打开主场景 Main”。

> 如果你希望像普通 Unity 项目那样在 Scene 里直接拖 UI，需要把现在的代码生成 UI 改成场景/预制体驱动，这是结构性改造，可另行安排。

## 构建

`.tmp/build_throttled.ps1` 会调用 Unity batchmode 的 `DemoBuild.BuildWebGL`，产物在 `Build/WebGL`。
