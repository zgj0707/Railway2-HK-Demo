# 铁路大亨 2 · 香港 Demo 资产生图 Prompt

> 目标：为 1080×1920 竖屏 Unity WebGL Demo 生成可导入的美术资产。
> 风格：日式手绘纸感 + 香港城市地图 + 低饱和水彩 + 墨线 + 米白纸底 + 朱红/靛蓝/抹茶/金色点缀。
> 所有图片 Prompt 默认不生成文字；中文、数字、按钮文案由 Unity 运行时 Text 渲染。

---

## 0. 全局 Style Bible

### 0.1 风格关键词

~~~text
Japanese hand-drawn game UI, Hong Kong travel map, soft watercolor and gouache,
warm cream paper texture, fine ink outlines, muted vintage palette,
risograph / letterpress grain, gentle imperfect edges, cozy tycoon game,
flat 2D illustration, top-down map, mobile portrait, no text
~~~

### 0.2 固定调色板

| 名称 | Hex | 用途 |
|---|---|---|
| Paper 米白纸 | #F6EFE3 | 全屏底、面板底 |
| Paper2 暖灰纸 | #ECE2D0 | 次级面板、卡片 |
| Ink 墨色 | #2F2A26 | 文字、线条、图标描边 |
| InkSoft 淡墨 | #6B6359 | 次级文字 |
| Vermilion 朱红 | #C8553D | 主按钮、1 号线、警告 |
| Indigo 靛蓝 | #3B5A8C | 2 号线、科技、加车按钮 |
| Matcha 抹茶 | #6FA05A | 3 号线、成功、升级 |
| Gold 金 | #D9A94E | 金币、列车、高亮 |
| Sakura 樱粉 | #E8A0B4 | 钻石、活动 |
| Sea 海水 | #A9C9CE | 维多利亚港、海面 |

### 0.3 统一 Negative Prompt

~~~text
text, letters, words, numbers, watermark, signature, logo, UI screenshot,
photorealistic, 3D render, CGI, glossy plastic, chrome, neon cyberpunk,
oversaturated colors, harsh black outlines, cluttered composition,
low resolution, blurry, jpeg artifacts, distorted perspective,
extra objects, duplicated shapes, cropped subject, frame, border
~~~

### 0.4 出图与导入约定

- 先出 2 倍尺寸（如 2160×3840），再缩到目标尺寸，边缘更干净。
- 图标、列车、徽章：透明背景 PNG，主体占画布 70%–80%，四周留安全边。
- 地图、页面背景：不透明，满画布，不留白边。
- 9-slice 面板：四角结构简单，上下/左右边缘可平铺，不要斜角或方向性强的渐变。
- 同一批资产固定一个 style reference 或 seed，保证纸感、线宽、饱和度一致。
- 生成后统一叠 5%–10% 纸纹，饱和度 -5 ~ -10。

---

## 1. 资产清单与优先级

| ID | 资产 | 目标尺寸 | 透明 | 优先级 | 用途 |
|---|---|---|---:|---|---|
| M1 | 香港主地图底图 | 2160×3840 | 否 | P0 | 全屏背景，运行时叠加站点/线路 |
| M2 | 纸纹平铺 | 1024×1024 | 否 | P1 | 地图/页面叠加 |
| S1 | 车站标记 Sprite Sheet | 1024×1024 | 是 | P0 | 普通站/换乘站/口岸站/选中环 |
| T1 | 金色列车侧面 | 1024×512 | 是 | P0 | 线路上的列车 |
| C1–C6 | 景观打卡插画 ×6 | 1024×768 | 否 | P1 | 打卡弹窗/图鉴卡片 |
| U1 | HUD 图标 Sheet | 1024×1024 | 是 | P0 | 金币/钻石/问号/齿轮等 |
| U2 | 奶油纸面板 9-slice | 512×512 | 是 | P0 | 弹窗/卡片底 |
| U3 | 按钮 9-slice 三色 | 512×256 | 否 | P1 | 朱红/靛蓝/抹茶 |
| U4 | 底栏 Tab 图标 | 1024×512 | 是 | P1 | 线路/列车/公司/科技 |
| P1 | 科技页纸张背景 | 1080×1920 | 否 | P2 | 科技树底 |
| P2 | 公司页空白插画 | 1024×768 | 是 | P2 | 空状态 |
| K1 | App 图标 | 1024×1024 | 否 | P1 | 商店/桌面 |
| K2 | 启动 Key Art | 1080×1920 | 否 | P2 | 启动/分享图 |

---

## 2. 地图

### M1. 香港主地图底图

文件：Assets/Art/Map/hk_map_base.png · 2160×3840 · 不透明

用法：BuildMap() 的 RawImage 底图。代码会叠加 17 个车站、6 个景观和线路，所以底图不要画站点圆点、线路、文字、图例。

~~~text
[STYLE] Japanese hand-drawn travel map, soft watercolor and gouache on warm cream paper,
fine ink outlines, muted vintage palette, subtle paper grain, cozy tycoon game map,
flat top-down illustration, no text.

[SUBJECT] Stylized map of Hong Kong and surrounding waters, portrait orientation.
Hong Kong Island at lower center, Kowloon peninsula above it, New Territories and green
hills toward the top, Lantau Island and the airport at lower left, outlying islands
scattered in the sea. Victoria Harbour as a calm blue-grey inlet between the island and
Kowloon. A few mountain ridges, country parks, small urban blocks and tiny harbor ferries
as decorative details. Simplified and readable, not geographically exact.

[COMPOSITION] Full-bleed top-down map, 9:16 portrait, land and sea fill the frame,
no border, no title block. Keep calm low-detail areas around the middle and lower center
where station labels and route lines will be drawn.

[COLOR] Cream paper #F6EFE3 land, pale sage green hills, desaturated sea #A9C9CE to
#7FA7AD, ink #2F2A26 thin outlines, tiny vermilion and gold accents for roofs and ferries.

[OUTPUT] 2160x3840, high detail, clean edges, no text, no icons, no route lines,
no station dots, no UI.
~~~

Negative：text, labels, station icons, metro lines, route lines, compass, legend, border, frame, satellite photo, 3D terrain, realistic map, bright blue ocean, neon.

备注：
- 形状不必和真实香港完全一致；游戏坐标是手调的，底图只要像香港即可。
- 不要生成中文地名，避免和运行时文字重叠。

### M2. 纸纹平铺

文件：Assets/Art/Map/paper_grain.png · 1024×1024 · 可平铺

~~~text
Seamless tileable warm cream paper texture, subtle fibers, soft deckle grain,
very light ink speckles, Japanese washi paper, low contrast, no pattern motifs,
no text, flat scan, 1024x1024, seamless edges.
~~~

Unity：Wrap Mode = Repeat；全屏 RawImage 叠加，透明度 5%–10%。

---

## 3. 车站与列车

### S1. 车站标记 Sprite Sheet

文件：Assets/Art/Stations/station_markers.png · 1024×1024 · 透明
布局：2×2 网格，每格 512×512；游戏内显示 20–56 px。

~~~text
[STYLE] Japanese hand-drawn game icon sheet, fine ink linework, warm cream fill,
muted vintage palette, paper grain, flat 2D, transparent background.

[SUBJECT] Four separate station marker icons in a clean grid with generous padding:
1) ordinary station: cream circle with ink outline and a tiny ink center dot;
2) interchange station: cream circle with double ink ring and small gold center;
3) boundary/portal station: cream diamond with ink outline and tiny vermilion accent;
4) selected ring: open gold ring with small hand-drawn ticks, no fill.

[OUTPUT] 1024x1024, each icon centered in its own cell, crisp edges, no text,
no shadows crossing cell boundaries, transparent PNG.
~~~

Negative：text, numbers, map, metro line, background, drop shadow outside icon, 3D, glossy, neon.

### T1. 金色列车侧面

文件：Assets/Art/Trains/train_gold_side.png · 1024×512 · 透明
用法：线路上的小列车，运行时旋转/缩放。

~~~text
[STYLE] Japanese hand-drawn side-view train sprite, soft gouache and ink,
warm cream paper texture, vintage tycoon game asset, flat 2D, transparent background.

[SUBJECT] Cute compact two-car metro train, side view, brass and warm gold body #D9A94E,
cream window band, dark ink outline, small round headlights, tiny vermilion stripe,
subtle paper grain. Friendly toy-like proportions, slightly rounded nose, no operator,
no text, no route number.

[COMPOSITION] Centered horizontally, full train visible, 2:1 aspect,
generous transparent margin, side profile exactly horizontal.

[OUTPUT] 1024x512, transparent PNG, crisp silhouette, no shadow, no ground, no text.
~~~

Negative：text, logo, number, photorealistic train, 3D render, perspective, front view, tracks, station, shadow, background.

备注：游戏里列车只有约 40–60 px 长，轮廓比细节重要；车窗和金色高光要清晰。

---

## 4. 景观打卡插画（6 张）

统一规格：Assets/Art/Scenery/ · 1024×768 · 不透明（弹窗内圆角裁切）
统一风格：日式旅行明信片 / 水彩小景 / 轻微纸纹 / 无文字 / 主体居中偏下。

### C1. 维多利亚港

~~~text
Japanese hand-drawn travel postcard illustration of Victoria Harbour, Hong Kong,
seen from the Kowloon side at dusk. A small green-star ferry crossing calm blue-grey water,
layered city skyline and green peaks behind, warm cream sky, soft watercolor and gouache,
fine ink outlines, muted vintage palette, paper grain, cozy tycoon game art, no text, 4:3.
~~~

### C2. 太平山

~~~text
Japanese hand-drawn travel postcard illustration of Victoria Peak, Hong Kong.
A winding hillside path, tropical trees, a small vintage tram silhouette, layered green
hills and mist, warm cream paper sky, soft watercolor and gouache, fine ink lines,
muted sage and gold palette, no text, 4:3.
~~~

### C3. 天坛大佛

~~~text
Japanese hand-drawn travel postcard illustration of the Tian Tan Buddha on Lantau Island,
Hong Kong. Calm seated bronze Buddha statue on a lotus base, green mountains and soft
clouds, warm cream paper, muted gold and sage palette, soft watercolor and ink,
peaceful composition, no text, 4:3.
~~~

### C4. 庙街夜市

~~~text
Japanese hand-drawn travel postcard illustration of Temple Street Night Market, Hong Kong.
Rows of small warm lanterns, simple market stalls, steam from food carts, distant tenement
facades, cozy evening atmosphere, muted vermilion and gold, soft gouache and ink,
paper grain, no text, 4:3.
~~~

### C5. 浅水湾

~~~text
Japanese hand-drawn travel postcard illustration of Repulse Bay, Hong Kong.
Gentle curved beach, calm turquoise-grey water, palm trees, small tiled pavilion,
soft cream sand, warm paper sky, muted watercolor palette, fine ink outlines, no text, 4:3.
~~~

### C6. 狮子山

~~~text
Japanese hand-drawn travel postcard illustration of Lion Rock, Hong Kong.
Dramatic ridgeline silhouette above a layer of city rooftops and green slopes,
soft morning haze, cream paper sky, muted sage and ink palette, watercolor and fine ink,
no text, 4:3.
~~~

统一 Negative：text, caption, stamp, frame, photorealistic, HDR, neon, cyberpunk, oversaturated, 3D render, people close-up, logo.

---

## 5. HUD 与 UI

### U1. HUD 图标 Sprite Sheet

文件：Assets/Art/UI/hud_icons.png · 1024×1024 · 透明
布局：4×3 网格，每格 256×256；游戏内显示 24–56 px。

~~~text
[STYLE] Japanese hand-drawn game UI icon sheet, fine dark ink outlines,
warm cream and muted gold fills, subtle paper texture, flat 2D,
transparent background, cozy tycoon game.

[SUBJECT] Twelve separate icons in a clean 4x3 grid, each centered with padding:
1 coin, 2 diamond, 3 question mark, 4 gear, 5 daily task checklist,
6 collection postcards, 7 ranking podium, 8 speed / fast-forward,
9 ad reward play badge, 10 plus, 11 check, 12 close x.

[OUTPUT] 1024x1024, consistent line weight, no text, no numbers, no background,
crisp edges, transparent PNG.
~~~

Negative：text, labels, numbers, gradient mesh, 3D, glossy, neon, background, shadow outside cell.

### U2. 奶油纸面板 9-slice

文件：Assets/Art/UI/panel_paper.png · 512×512 · 透明
Unity：Image Type = Sliced，Border 64。

~~~text
Single square UI panel, warm cream paper #F6EFE3, very subtle paper fiber,
softly rounded corners, thin hand-drawn ink edge, faint warm shadow inside the bottom edge,
Japanese stationery / letterpress feel, flat front view, no text, no icons,
no background, 512x512, perfectly centered, uniform border thickness,
suitable for 9-slice.
~~~

Negative：text, logo, pattern, strong gradient, torn edge, photo, 3D, thick shadow, asymmetric corners.

### U3. 按钮 9-slice 三色

文件：Assets/Art/UI/btn_vermilion.png / btn_indigo.png / btn_matcha.png · 512×256 · 透明
把 [COLOR] 换成 vermilion #C8553D / indigo #3B5A8C / matcha #6FA05A。

~~~text
Single rounded rectangle game button, [COLOR] watercolor fill, subtle paper grain,
thin darker ink edge, soft matte finish, hand-drawn Japanese stationery style,
flat front view, centered, no text, no icon, transparent background, 512x256,
uniform rounded corners, suitable for 9-slice.
~~~

Unity：Border 48–64；文字由运行时 Text 叠加。

### U4. 底栏 Tab 图标

文件：Assets/Art/UI/tab_icons.png · 1024×512 · 透明
布局：4×1，每格 256×512。

~~~text
Four hand-drawn game tab icons in a horizontal sheet, fine ink linework,
muted vermilion / indigo / matcha / gold accents, transparent background, 1024x512:
1 route / metro line: small train ticket with a curved line and two dots;
2 train / garage: front view of a tiny gold train;
3 company: small office building with a flag;
4 technology: small lightbulb over a gear.
Consistent line weight, centered in four equal cells, no text, no numbers.
~~~

---

## 6. 页面与品牌

### P1. 科技页纸张背景

文件：Assets/Art/UI/tech_paper_bg.png · 1080×1920 · 不透明

~~~text
Portrait mobile game technology page background, warm cream paper,
very faint grid and blueprint lines, subtle ink smudges,
small hand-drawn gear and circuit doodles in the margins,
lots of empty center space for cards, Japanese stationery / HOI4 tech tree influence,
muted palette, no text, no icons in the center, flat 2D, 9:16.
~~~

### P2. 公司页空白插画

文件：Assets/Art/UI/company_empty.png · 1024×768 · 透明

~~~text
Small hand-drawn illustration of an empty vintage office desk: a blank ledger, an ink stamp,
a cup of tea, a tiny potted plant, warm cream paper, soft watercolor and ink,
cozy tycoon game, centered composition, lots of negative space,
transparent background, no text, no logo, 4:3.
~~~

### K1. App 图标

文件：Assets/Art/Brand/app_icon.png · 1024×1024 · 不透明

~~~text
Mobile game app icon, square with rounded corners, warm cream paper background,
a cute gold two-car metro train curving around a small green Hong Kong island,
thin ink outline, tiny vermilion route line, Japanese hand-drawn gouache style,
centered, bold readable silhouette at small size, no text, no letters, 1024x1024.
~~~

### K2. 启动 Key Art

~~~text
Portrait key art for a cozy Hong Kong railway tycoon game, illustrated map of
Victoria Harbour with a tiny gold train crossing a red route line, cream paper texture,
Japanese hand-drawn watercolor and ink, warm gold and muted teal palette,
space at top and bottom for title/logo, no text, 9:16, 1080x1920.
~~~

---

## 7. 一致性检查清单

- [ ] 所有图同一套纸色、线宽、饱和度，不出现写实 3D / 霓虹。
- [ ] 地图没有文字、站点圆点、线路，给运行时留叠加空间。
- [ ] 图标缩小到 32 px 仍能辨认轮廓。
- [ ] 列车侧面在 48 px 时长宽比正确。
- [ ] 9-slice 面板四角一致，拉伸后不变形。
- [ ] 所有资产无 watermark、无签名、无乱码文字。
- [ ] 透明图没有白边/黑边。
