# Smart Plant Pot – Front End / UX-UI (CFC00002)

Unity 6.5 · URP · new Input System · uGUI. Everything lives in `Assets/SmartPlantPot/`.

## Run it
1. Open the project. On first load Unity asks **"Build the PlantDashboard scene now?"** → **Build**
   (or use the menu **Smart Plant Pot ▸ Build Dashboard Scene** at any time).
2. Press **Play**. Try the Game view at 1920×1080, 1280×720, 4:3 and a portrait phone size.

Keyboard shortcuts for demos: `1` `2` `3` select a plant · `W` water · `P` prune · `N` notifications · `Esc` close.

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
Disable `SimulatedPlantDataSource`, enable `HttpPlantDataSource`, set `readingsUrl`. Accepted JSON:
```json
{"pots":[{"id":"pot-01","moisture":35,"temperature":29,"light":520}]}
```
also a bare array `[...]`, or a single object (ESP32 `/data`) which goes to `singlePotTarget`.
`id` must match each plant's **Pot Id** (`pot-01`, `pot-02`, `pot-03`).
Optional `commandUrl` receives `POST {"id":"pot-01","action":"water"}` when the user taps Water/Prune.
On Android/iOS allow plain HTTP in *Player Settings ▸ Allow downloads over HTTP* if you don't use HTTPS.

## Moving to the 3D digital twin
Put `PlantUnit`, `PlantSelectable` and `PotInteraction` on the 3D plant (with a Collider), set
`PotInteraction.art` to the model (pivot at the pot's base). The camera already has a `PhysicsRaycaster`,
so tap-to-open, the info card, effects and alerts work unchanged.

## Art & credits
* Plants and mascots: the team's 2D art (คุณเกวลิน, คุณชลธิชา), extracted from the design deck at deck resolution
  → **replace `Art/Plants/*.png` / `Art/Mascots/*.png` with the original exports** (keep the file names) for sharper images.
* Font: Prompt (SIL Open Font License, `Fonts/Prompt-OFL.txt`) – same as the design deck. The bundled copies are
  patched with private-use mappings to Prompt's own raised / left-shifted tone-mark glyphs; `ThaiShaper` uses them
  so stacked marks (กิ่ง, ที่, น้ำ) don't overlap in uGUI Text. If you swap the font, untick
  `SmartPlantPotApp ▸ Fix Thai Tone Marks`.
* Icons: Lucide (ISC License, `Art/Icons/Lucide-LICENSE.txt`).
