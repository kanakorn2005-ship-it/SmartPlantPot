# Smart Plant Pot – Front End / UX-UI (CFC00002)

Unity 6.5 · URP · new Input System · uGUI. Everything lives in `Assets/SmartPlantPot/`.

## Run it
Two scenes, both rebuilt from the **Smart Plant Pot** menu (safe to re-run, your edited assets are kept):

| Scene | Menu | What it is |
|---|---|---|
| `Scenes/PlantShop3D.unity` (first in Build Settings) | **Build 3D Shop Scene** / **Open 3D Shop Scene** | The game: 3D plant shop, empty pots, seeds, selling, weather, events |
| `Scenes/PlantDashboard.unity` | **Build Dashboard Scene** / **Open Dashboard Scene** | The original 2D monitoring dashboard (the 4 cards below) |

Press **Play** and try 1920×1080 and the *Phone Portrait (1080×1920)* Game-view size.

Shortcuts: `1` `2` `3` select a pot · `W` water · `P` prune · `F` fertilize · `B` seed shop · `O` settings & Mock · `N` notifications · `Esc` close.

## The 4 Trello cards → where they are

| Card | What it does | Main scripts |
|---|---|---|
| [UX][UI] จัดเรียงรูป 2D ให้เข้ากับหน้าจอ | Canvas switches between 1920×1080 / 1080×1920 reference, plants are sized to fit one shelf without stretching, pots share a baseline on a wooden plank, mascot / toasts / notification panel move for portrait, safe-area for notches | `ResponsiveLayout`, `SafeAreaFitter`, `PlantSlotView` |
| [Front End] กดที่ต้นไม้เพื่อแสดงค่า ความชื้น แสง อุณหภูมิ | Tap a plant → info card with live moisture %, °C, lux, Low/Normal/High chip, a bar with the ideal range, overall advice ("ควรรดน้ำ"). Pops up beside the plant (landscape) or as a bottom sheet (portrait). Tap again / background / ✕ / Esc to close | `PlantSelectable`, `PlantInfoCard`, `MetricRowView`, `PlantSelection` |
| [UX][UI] Interaction กับกระถางต้นไม้ | Hover grows, press squashes, release springs back; idle sway; thirsty plants droop and lean. **Water**: drops fall into the pot, splash, sparkles, moisture rises. **Prune**: scissors snip, leaves fall. Watering wet soil is refused with a head-shake + message | `PotInteraction`, `EffectsLayer` |
| [Front End] แจ้งเตือนแต่ละค่าถ้ามากหรือน้อยไป | Each species has min/max for moisture, temperature, light. Leaving the range → toast, bell badge, "!" badge on the pot, mascot speech bubble and history list. Hysteresis stops flicker at the limit, reminders repeat after 2 min, "back to normal" message when fixed | `NotificationCenter`, `ToastPresenter`, `ToastView`, `NotificationBell`, `NotificationHistoryPanel`, `MascotHelper`, `PlantHealth` |

## Tuning (no code needed)
* **Thresholds / names / art per plant** – `Data/Species_*.asset`. Ranges follow the team's 2D design (snake plant at 35 % = "ควรรดน้ำ"); adjust to the real sensor calibration.
* **Colours, fonts, icons** – `Data/UISkin.asset`, then rebuild the scene.
* **Alert behaviour** – `SmartPlantPotApp` object ▸ `NotificationCenter` (reminder seconds, back-to-normal messages, optional sound).
* **Language** – `SmartPlantPotApp ▸ Thai Language` (off = English).
* **Test alerts** – in Play mode select `SmartPlantPotApp` ▸ `Simulated Plant Data Source`, tick **manual** on a pot and drag the sliders, or right-click the component for *Dry out every pot / Heat wave / Cloudy day*.

## Connecting the real pots (Backend / ESP32)
Disable `SimulatedPlantDataSource`, enable `HttpPlantDataSource`, set `readingsUrl` (in the 3D scene just use the
settings panel ▸ **เซนเซอร์จำลอง**). Accepted JSON:
```json
{"pots":[{"id":"pot-01","moisture":35,"temperature":29,"light":520}]}
```
also a bare array `[...]`, or a single object (ESP32 `/data`) which goes to `singlePotTarget`.
`id` must match each plant's **Pot Id** (`pot-01`, `pot-02`, `pot-03`).
Optional `commandUrl` receives `POST {"id":"pot-01","action":"water"}` (or `"prune"`, `"fertilize"`) when the user taps
a care button, and `POST {"id":"pot-01","action":"servo","angle":90,"seconds":3}` whenever the servo moves.
On Android/iOS allow plain HTTP in *Player Settings ▸ Allow downloads over HTTP* if you don't use HTTPS.

## Debugging helpers (editor only)
* `Logs/SmartPlantPot_compile.log` and `Logs/SmartPlantPot_console.log` mirror compile errors and Console errors /
  warnings as text (`DevLogWriter`).
* Write `on` into `Logs/clickprobe.on` to log what every click hits (`ClickProbe`); `off` disables it.

## 3D plant shop (PlantShop3D)

**Flow:** every pot starts empty → buy seed packets with points (🛒 / `B`) → tap an empty pot and pick a seed →
the 3D plant grows from a sprout (0.12 scale) to full size → sell.

| Feature | How it works | Scripts |
|---|---|---|
| 3D models | `Art/3D/Props/*.fbx` from the 3D team (their materials in `Art/3D/Materials`; the builder doesn't change their import settings). The builder makes `Prefabs/Pot.prefab` (the shared terracotta pot) and `Prefabs/Plant_*.prefab` (plant only, pivot at the soil: `Root/Growth/Wobble/model`). Each pot slot spawns the species' prefab. | `PlantGrowth`, `SmartPlantPotSceneBuilder3D` |
| Species / thresholds / Mock | `Data/PlantCatalog.asset` lists the 5 built-in species (tomato, ว่านงาช้าง, กะเพรา, โฮย่า, ลิ้นมังกร). In Play mode, ⚙ / `O` ▸ **ค่าแจ้งเตือน** edits each species' moisture / temperature / light range, prices and grow time, and **+ เพิ่มชนิดใหม่** adds your own species (copies a model, saved in the save file). **เซนเซอร์จำลอง** switches Mock ↔ real HTTP sensors and has per-pot manual sliders. | `PlantCatalog`, `SettingsPanel`, `SimulatedPlantDataSource` |
| Servo (valve) | 0° closed · 90° water · 180° fertilizer. Water / Fertilize buttons pulse the servo; optional auto-water when soil is dry (90 s cooldown). Every move is sent to the ESP32 (see below). **Servo** tab: manual angle, durations, auto-water per pot. | `PotServo` |
| Two ways to sell | **ขายผลผลิตวันนี้** – keep the plant, earn points once per plant per day (from the young stage). **ขายทั้งต้น** – the plant leaves, the pot is empty again; price = base × growth × quality × streak × mutation × today's event. | `GameManager`, `PlantGameSection` |
| Quality & streak 🔥 | Quality follows the plant's health over time. A day with average care ≥ 75 % adds a streak day (+5 % price per day, max +50 %); a bad day resets it. | `PlantGrowth` |
| Mutations ✨ | After a 3-day streak a fully grown plant may mutate: Golden ×2, Variegated ×1.5, Giant ×1.3, Rainbow ×3 (tinted model). | `PlantGrowth.MutationInfo` |
| Real time & weather | Sun, sky, lamps and ambient light follow the computer's clock (sunrise 06:00, sunset 18:15); light alerts are paused at night. Live weather for Bangkok from Open-Meteo (no API key) every 15 min drives sky, rain, storm flashes and the Mock sensors. Override time / weather / event in **เวลา & อากาศ**. | `GameClock`, `DayNightCycle`, `WeatherService`, `WeatherEffects`, `WeatherOverlay` |
| Daily events | One per date: ตลาดนัดต้นไม้ (sell ×1.5), เมล็ดลดราคา (−30 %), ลูกค้าตามหา (one species ×2), เทศกาลสวนสวย, แดดดี, ฝนชุ่มฉ่ำ (faster growth). Tap the purple chip for details. | `DailyEvents` |
| Save | Points, seeds, pots, settings and custom species are saved to `smartplantpot_save.json` (path shown in **ทดสอบเกม**, which also has +100 points, skip day, grow fully, streak +3, mutate and reset). | `SaveData`, `GameManager` |

**Linking the real pot:** turn on *ใช้เซนเซอร์จริง* in **เซนเซอร์จำลอง** and the game uses the ESP32 readings. Good real-life
care then shows up in the game as quality, streak and higher prices, while bad care shows as alerts and a lost streak.

## Moving more plants into 3D
Add a species: drop the FBX in `Art/3D/Props/` (same pot `Material.010` + soil `Material.011` as the others), add a line in
`SmartPlantPotSceneBuilder3D.BuildCatalog()` and re-run **Build 3D Shop Scene**. Or, without code, create a custom
species in the settings panel and pick an existing model.

## Art & credits
* Plants and mascots: the team's 2D art (คุณเกวลิน, คุณชลธิชา), extracted from the design deck at deck resolution
  → **replace `Art/Plants/*.png` / `Art/Mascots/*.png` with the original exports** (keep the file names) for sharper images.
* Font: Prompt (SIL Open Font License, `Fonts/Prompt-OFL.txt`) – same as the design deck. The bundled copies are
  patched with private-use mappings to Prompt's own raised / left-shifted tone-mark glyphs; `ThaiShaper` uses them
  so stacked marks (กิ่ง, ที่, น้ำ) don't overlap in uGUI Text. If you swap the font, untick
  `SmartPlantPotApp ▸ Fix Thai Tone Marks`.
* Icons: Lucide (ISC License, `Art/Icons/Lucide-LICENSE.txt`).
* 3D models (`Art/3D/Props/`): the team's 3D artist. Seed packets (`Art/Seeds/`): cut from the team's seed-packet sheet.
