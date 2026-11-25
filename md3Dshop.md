# Smart Plant Pot – Front End / UX-UI handoff (CFC00002)

Unity project (team GitHub clone): `D:\SmartPlantPot` · Unity 6.5 (6000.5.2f1) · URP · new Input System · uGUI
Everything lives in `Assets/SmartPlantPot/`. Full details: `Assets/SmartPlantPot/README.md`.

| Scene | Menu (Smart Plant Pot ▸ …) | Status |
|---|---|---|
| `Scenes/PlantShop3D.unity` (first in Build Settings) | Build 3D Shop Scene / Open 3D Shop Scene | 21 Nov 2026: compiles clean, play-tested at 1920×1080 and 1080×1920 |
| `Scenes/PlantDashboard.unity` | Build Dashboard Scene / Open Dashboard Scene | 2D dashboard from 1 Nov, still works with the shared code |

## 1 Nov – the 4 Trello cards (2D dashboard)
| Card | Done | Scripts |
|---|---|---|
| [UX][UI] จัดเรียงรูป 2D ให้เข้ากับหน้าจอ | 1920×1080 ↔ 1080×1920 reference, plants sized to one shelf, portrait layout, safe area | ResponsiveLayout, SafeAreaFitter, PlantSlotView |
| [Front End] กดที่ต้นไม้เพื่อแสดงค่า ความชื้น แสง อุณหภูมิ | Tap plant → card with live %, °C, lux, status chip, ideal-range bar, advice | PlantSelectable, PlantInfoCard, MetricRowView |
| [UX][UI] Interaction กับกระถางต้นไม้ | Hover/press squash, sway, droop; water / prune effects | PotInteraction, EffectsLayer |
| [Front End] แจ้งเตือนแต่ละค่าถ้ามากหรือน้อยไป | Per-species min/max; toast, bell, badge, mascot, history, hysteresis, reminders | NotificationCenter + UI |

## 21 Nov – 3D plant shop game (PlantShop3D)
* **3D models** (pot, tomato, ว่านงาช้าง, กะเพรา, โฮย่า, ลิ้นมังกร, PlantShop) in `Models/`. Builder makes `Prefabs/Pot.prefab` + `Prefabs/Plant_*.prefab` (pivot at the soil). Plant.fbx / plantrig.fbx not imported (all-in-one / rig without animation).
* **Game loop:** 3 empty pots on the shop table → buy 2D seed packets with points → plant → sprout grows to full size (speed = health × event × fertilizer).
* **Mock / thresholds:** settings panel (⚙ or `O`): per-species moisture / temperature / light ranges, prices, grow time, add custom species; Mock ↔ real HTTP sensors; per-pot manual sensor sliders.
* **Servo valve:** 0° closed, 90° water, 180° fertilizer; auto-water option; every move POSTs `{"id","action":"servo","angle","seconds"}` to `commandUrl`.
* **Selling:** daily harvest (once per plant per day, plant stays) and sell whole plant (pot empties; price = base × growth × quality × streak × mutation × event).
* **Streak 🔥 / mutations ✨:** good-care days (≥ 75 %) build a streak (+5 %/day, max +50 %); after 3 days a grown plant may mutate (Golden ×2, Variegated ×1.5, Giant ×1.3, Rainbow ×3).
* **Real time & weather:** sun / lamps follow the PC clock; Bangkok weather from Open-Meteo (no key) drives sky, rain, storm flashes and the Mock sensors; daily events (plant market, seed sale, customer request, festival, sunny / rainy day).
* **Real pot link:** switch on real sensors → real-life care becomes game quality, streak and price.
* **Save:** `smartplantpot_save.json` in persistentDataPath; test tools (+points, skip day, grow, streak, mutate, reset) in the ทดสอบเกม tab.

## Notes for the team
* No `.gitignore` in the repo yet – Unity's `Library/`, `Temp/`, `Logs/`, `UserSettings/` should not be committed.
* Editor-only helpers: `Logs/SmartPlantPot_compile.log` / `SmartPlantPot_console.log` (DevLogWriter); write `on` into `Logs/clickprobe.on` to log what clicks hit (ClickProbe).
* Backend JSON for readings: `{"pots":[{"id":"pot-01","moisture":35,"temperature":29,"light":520}]}`; care commands `{"id":"pot-01","action":"water|prune|fertilize"}`.

Demo keys: 1–3 select pot · W water · P prune · F fertilize · B seed shop · O settings · N notifications · Esc close.
