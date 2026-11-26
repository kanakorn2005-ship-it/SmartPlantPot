using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// In-game "Settings &amp; Mock" panel:
    ///  1. ค่าแจ้งเตือน - alert thresholds (moisture / temperature / light) and prices per species,
    ///     plus creating new species for the future (they reuse an existing 3D model)
    ///  2. เซนเซอร์จำลอง - switch Mock ↔ real HTTP sensors, drag each pot's sensor values
    ///  3. Servo - move each pot's valve, test water / fertilizer, auto-water
    ///  4. เวลา &amp; อากาศ - real clock or a fixed hour, real weather or a forced one, daily event
    ///  5. ทดสอบเกม - points, skip a day, grow instantly, force mutations, reset the save
    /// Everything applies immediately and is saved with the game.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Settings Panel")]
    public class SettingsPanel : ModalPanel
    {
        public RectTransform tabBar;
        public RectTransform content;
        public ScrollRect scroll;

        int tab;
        string selectedSpeciesId = "";
        int forcedMutationIndex;
        readonly List<Action> live = new List<Action>();
        float liveTimer;

        static string[] TabNames
        {
            get
            {
                return new[]
                {
                    L.T("ค่าแจ้งเตือน", "Alert thresholds"),
                    L.T("เซนเซอร์จำลอง", "Mock sensors"),
                    "Servo",
                    L.T("เวลา & อากาศ", "Time & weather"),
                    L.T("ทดสอบเกม", "Game test")
                };
            }
        }

        static UISkin Skin
        {
            get { return UISkin.Current; }
        }

        protected override void OnOpened()
        {
            BuildTabs();
            Build();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!IsOpen) return;
            liveTimer += Time.unscaledDeltaTime;
            if (liveTimer < 0.25f) return;
            liveTimer = 0f;
            foreach (var a in live) a();
        }

        void BuildTabs()
        {
            if (tabBar == null) return;
            UIKit.Clear(tabBar);
            var names = TabNames;
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                bool on = i == tab;
                var b = UIFactory.Button(tabBar, "Tab" + i, names[i], null, on ? Skin.accent : UISkin.WithAlpha(Skin.accent, 0.1f),
                    on ? Color.white : Skin.accentDark, 20, 20f, 48f);
                UIFactory.Layout(b, -1f, 48f, 0f, -1f);
                b.onClick.AddListener(() =>
                {
                    tab = index;
                    BuildTabs();
                    Build();
                });
            }
        }

        void Build()
        {
            if (content == null) return;
            UIKit.Clear(content);
            live.Clear();
            switch (tab)
            {
                case 0: BuildSpecies(); break;
                case 1: BuildSensors(); break;
                case 2: BuildServo(); break;
                case 3: BuildTimeWeather(); break;
                default: BuildDebug(); break;
            }
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        static string Pct(float v)
        {
            return Mathf.RoundToInt(v) + "%";
        }

        static string Deg(float v)
        {
            return v.ToString("0.0", CultureInfo.InvariantCulture) + "°C";
        }

        static string Lux(float t)
        {
            return UIKit.LuxFromSlider(t).ToString("N0", CultureInfo.InvariantCulture) + " lux";
        }

        static void Post(string title, string msg, AlertKind kind)
        {
            if (NotificationCenter.Instance != null) NotificationCenter.Instance.PostInfo(null, title, msg, kind);
        }

        static List<PlantGrowth> SortedPots()
        {
            var list = new List<PlantGrowth>(PlantGrowth.All);
            list.Sort((a, b) => string.CompareOrdinal(a.plant.potId, b.plant.potId));
            return list;
        }

        // ================================================================== 1. species thresholds

        void BuildSpecies()
        {
            var catalog = PlantCatalog.Current;
            var gm = GameManager.Instance;
            if (catalog == null) return;

            UIKit.Heading(content, L.T("ค่าแจ้งเตือนตามชนิดพืช", "Alert thresholds per species"));
            UIKit.Note(content, L.T("ค่าที่อยู่นอกช่วงจะแจ้งเตือนทันที ปรับแล้วมีผลเลยและบันทึกอัตโนมัติ เพิ่มชนิดใหม่ได้ (ใช้โมเดล 3D ของชนิดที่เลือกอยู่)",
                "Values outside a range raise an alert. Changes apply at once and are saved. Add new species for the future (they reuse the selected species' 3D model)."));

            var chips = UIFactory.Rect(content, "SpeciesChips");
            var grid = chips.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(230f, 48f);
            grid.spacing = new Vector2(10f, 10f);
            UIFactory.Fit(chips.gameObject, false, true);

            PlantSpecies selected = catalog.Find(selectedSpeciesId);
            if (selected == null && catalog.All.Count > 0) selected = catalog.All[0];
            if (selected != null) selectedSpeciesId = selected.Key;

            foreach (var s in catalog.All)
            {
                var species = s;
                bool on = s == selected;
                var b = UIFactory.Button(chips, "Chip_" + s.Key, s.DisplayName, s.isCustom ? Skin.iconSparkles : null,
                    on ? Skin.accent : Color.white, on ? Color.white : Skin.textPrimary, 18, 20f, 48f);
                b.onClick.AddListener(() =>
                {
                    selectedSpeciesId = species.Key;
                    Build();
                });
            }
            var add = UIFactory.Button(chips, "Chip_Add", L.T("เพิ่มชนิดใหม่", "New species"), Skin.iconPlus,
                UISkin.WithAlpha(Skin.eventColor, 0.15f), Skin.eventColor, 18, 20f, 48f);
            add.onClick.AddListener(() =>
            {
                var n = catalog.AddCustom(selected, "", "");
                if (n != null)
                {
                    selectedSpeciesId = n.Key;
                    if (gm != null) gm.MarkDirty();
                    Post(L.T("เพิ่มชนิดพืชใหม่", "New species added"), L.T("ตั้งชื่อและเกณฑ์ได้เลย เมล็ดจะขายในร้านด้วย", "Name it and set its thresholds - its seeds are now in the shop"), AlertKind.GameEvent);
                }
                Build();
            });

            if (selected == null) return;
            var s0 = selected;
            var card = UIKit.Card(content, "Editor", 12f);

            if (s0.isCustom)
            {
                UIKit.TextField(card.transform, L.T("ชื่อภาษาไทย", "Thai name"), s0.nameThai, "ต้น...", InputField.ContentType.Standard, v =>
                {
                    if (!string.IsNullOrEmpty(v)) s0.nameThai = v;
                    Changed(s0, true);
                });
                UIKit.TextField(card.transform, L.T("ชื่อภาษาอังกฤษ", "English name"), s0.nameEnglish, "Plant", InputField.ContentType.Standard, v =>
                {
                    if (!string.IsNullOrEmpty(v)) s0.nameEnglish = v;
                    Changed(s0, true);
                });
                var template = catalog.Find(s0.modelId);
                UIKit.Note(card.transform, L.T("ใช้โมเดล 3D ของ: ", "Uses the 3D model of: ") + (template != null ? template.DisplayName : s0.modelId));
            }
            else
            {
                UIKit.Heading(card.transform, s0.DisplayName, 24);
                UIKit.Note(card.transform, L.T("ชนิดพื้นฐาน (มีโมเดล 3D และรูปเมล็ดของตัวเอง)", "Built-in species (own 3D model and seed picture)"));
            }

            UIKit.Heading(card.transform, L.MetricName(PlantMetric.Moisture) + " (%)", 21);
            UIKit.LabeledSlider(card.transform, L.T("ต่ำสุด (ต่ำกว่านี้ = ควรรดน้ำ)", "Minimum (below = needs water)"), 0f, 100f, s0.moisture.min, true, Pct, v =>
            {
                s0.moisture = new MetricRange(Mathf.Min(v, s0.moisture.max - 1f), s0.moisture.max);
                Changed(s0, false);
            }, Skin.water);
            UIKit.LabeledSlider(card.transform, L.T("สูงสุด (เกินนี้ = ดินแฉะ)", "Maximum (above = too wet)"), 0f, 100f, s0.moisture.max, true, Pct, v =>
            {
                s0.moisture = new MetricRange(s0.moisture.min, Mathf.Max(v, s0.moisture.min + 1f));
                Changed(s0, false);
            }, Skin.water);

            UIKit.Heading(card.transform, L.MetricName(PlantMetric.Temperature) + " (°C)", 21);
            UIKit.LabeledSlider(card.transform, L.T("ต่ำสุด", "Minimum"), 0f, 50f, s0.temperature.min, false, Deg, v =>
            {
                v = Mathf.Round(v * 2f) / 2f;
                s0.temperature = new MetricRange(Mathf.Min(v, s0.temperature.max - 0.5f), s0.temperature.max);
                Changed(s0, false);
            }, Skin.statusHigh);
            UIKit.LabeledSlider(card.transform, L.T("สูงสุด", "Maximum"), 0f, 50f, s0.temperature.max, false, Deg, v =>
            {
                v = Mathf.Round(v * 2f) / 2f;
                s0.temperature = new MetricRange(s0.temperature.min, Mathf.Max(v, s0.temperature.min + 0.5f));
                Changed(s0, false);
            }, Skin.statusHigh);

            UIKit.Heading(card.transform, L.MetricName(PlantMetric.Light) + " (lux)", 21);
            UIKit.LabeledSlider(card.transform, L.T("ต่ำสุด (กลางวัน)", "Minimum (daytime)"), 0f, 1f, UIKit.SliderFromLux(s0.light.min), false, Lux, v =>
            {
                float lux = UIKit.LuxFromSlider(v);
                s0.light = new MetricRange(Mathf.Min(lux, s0.light.max - 10f), s0.light.max);
                Changed(s0, false);
            }, Skin.gold);
            UIKit.LabeledSlider(card.transform, L.T("สูงสุด", "Maximum"), 0f, 1f, UIKit.SliderFromLux(s0.light.max), false, Lux, v =>
            {
                float lux = UIKit.LuxFromSlider(v);
                s0.light = new MetricRange(s0.light.min, Mathf.Max(lux, s0.light.min + 10f));
                Changed(s0, false);
            }, Skin.gold);

            UIKit.Heading(card.transform, L.T("ในเกม (แต้ม / เวลาโต)", "In game (points / growth)"), 21);
            UIKit.LabeledSlider(card.transform, L.T("ราคาเมล็ด", "Seed price"), 5f, 300f, s0.seedPrice, true, v => Mathf.RoundToInt(v) + " " + L.Points,
                v => { s0.seedPrice = Mathf.RoundToInt(v); Changed(s0, false); }, Skin.gold);
            UIKit.LabeledSlider(card.transform, L.T("ราคาขายทั้งต้น (โตเต็มที่ คุณภาพ 100%)", "Plant price (grown, 100% quality)"), 10f, 1000f, s0.sellPrice, true,
                v => Mathf.RoundToInt(v) + " " + L.Points, v => { s0.sellPrice = Mathf.RoundToInt(v); Changed(s0, false); }, Skin.gold);
            UIKit.LabeledSlider(card.transform, L.T("แต้มขายผลผลิตต่อวัน", "Daily harvest points"), 1f, 200f, s0.dailyPoints, true,
                v => Mathf.RoundToInt(v) + " " + L.Points, v => { s0.dailyPoints = Mathf.RoundToInt(v); Changed(s0, false); }, Skin.gold);
            UIKit.LabeledSlider(card.transform, L.T("เวลาโตเต็มที่ (นาทีที่ดูแลดี)", "Minutes to fully grown"), 1f, 240f, s0.growMinutes, true,
                v => Mathf.RoundToInt(v) + L.T(" นาที", " min"), v => { s0.growMinutes = Mathf.Round(v); Changed(s0, false); }, Skin.leafColor);

            var row = UIKit.Row(card.transform, "Actions", 48f);
            if (s0.isCustom)
            {
                UIKit.SmallButton(row, L.T("ลบชนิดนี้", "Delete species"), Skin.iconTrash, UISkin.WithAlpha(Skin.statusHigh, 0.15f), Skin.statusHigh, () =>
                {
                    foreach (var p in PlantUnit.All)
                        if (p.species == s0)
                        {
                            Post(L.T("ลบไม่ได้", "Can't delete"), L.T("มีกระถางที่ปลูกชนิดนี้อยู่ ขายต้นก่อน", "A pot still grows this species - sell it first"), AlertKind.Refused);
                            return;
                        }
                    catalog.Remove(s0);
                    selectedSpeciesId = "";
                    if (gm != null) gm.MarkDirty();
                    Build();
                });
            }
            else
            {
                UIKit.SmallButton(row, L.T("คืนค่าเริ่มต้น", "Reset to default"), Skin.iconRefresh, UISkin.WithAlpha(Skin.accent, 0.12f), Skin.accentDark, () =>
                {
                    catalog.ResetToDefault(s0);
                    Changed(s0, false);
                    Build();
                });
            }
        }

        static void Changed(PlantSpecies s, bool names)
        {
            foreach (var p in PlantUnit.All)
                if (p.species == s && p.HasReading) p.ApplyReading(p.Reading);
            if (GameManager.Instance != null) GameManager.Instance.MarkDirty();
            if (names && PlantCatalog.Current != null) PlantCatalog.Current.RaiseChanged();
        }

        // ================================================================== 2. mock sensors

        void BuildSensors()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var settings = gm.Settings;

            UIKit.Heading(content, L.T("แหล่งข้อมูลเซนเซอร์", "Sensor source"));
            UIKit.Switch(content, L.T("ใช้เซนเซอร์จริง (ESP32 / Backend ผ่าน HTTP)", "Use real sensors (ESP32 / backend over HTTP)"), settings.useRealSensors, v =>
            {
                settings.useRealSensors = v;
                gm.ApplySettings();
                Build();
            });
            UIKit.TextField(content, L.T("URL อ่านค่า (GET)", "Readings URL (GET)"), settings.readingsUrl, "http://...", InputField.ContentType.Standard, v =>
            {
                settings.readingsUrl = v.Trim();
                gm.ApplySettings();
            });
            UIKit.TextField(content, L.T("URL สั่งงาน + Servo (POST)", "Command + servo URL (POST)"), settings.commandUrl, "http://...", InputField.ContentType.Standard, v =>
            {
                settings.commandUrl = v.Trim();
                gm.ApplySettings();
            });
            var status = UIKit.Note(content, "");
            live.Add(() =>
            {
                var src = PlantDataSource.Active;
                string t = L.T("กำลังใช้: ", "Active: ") + (src != null ? src.ConnectionLabel : "-");
                if (gm.http != null && settings.useRealSensors && !string.IsNullOrEmpty(gm.http.LastError)) t += "  ·  " + gm.http.LastError;
                status.text = ThaiShaper.Fix(t);
            });
            if (settings.useRealSensors)
            {
                UIKit.SmallButton(content, L.T("ทดสอบเชื่อมต่อตอนนี้", "Test connection now"), Skin.iconWifi, UISkin.WithAlpha(Skin.accent, 0.12f), Skin.accentDark, () =>
                {
                    if (gm.http != null) gm.http.PollNow();
                });
                var room = UIKit.Note(content, "", 19);
                live.Add(() =>
                {
                    string t;
                    if (EnvironmentState.SensorLive)
                    {
                        t = L.T("สภาพห้องจากเซนเซอร์: ", "Room from the sensors: ");
                        if (!float.IsNaN(EnvironmentState.SensorTemperature)) t += EnvironmentState.SensorTemperature.ToString("0.0") + "°C  ";
                        if (!float.IsNaN(EnvironmentState.SensorLux)) t += Mathf.RoundToInt(EnvironmentState.SensorLux) + " lux  ";
                        if (!float.IsNaN(EnvironmentState.SensorHumidity)) t += L.T("อากาศชื้น ", "air ") + Mathf.RoundToInt(EnvironmentState.SensorHumidity) + "%";
                        t += L.T("\nใช้แทนอากาศกรุงเทพ: ความสว่างในฉาก อุณหภูมิบนแถบด้านบน และกระถางที่ไม่มีเซนเซอร์จะตามค่านี้",
                            "\nUsed instead of the weather API: scene brightness, the header temperature and pots without sensors follow it");
                    }
                    else t = L.T("ยังไม่ได้รับค่าจากเซนเซอร์ - ตอนนี้ใช้อากาศกรุงเทพจริงไปก่อน", "No sensor data yet - using the real Bangkok weather for now");
                    room.text = ThaiShaper.Fix(t);
                });
                UIKit.Note(content, L.T("JSON ที่รับ: {\"pots\":[{\"id\":\"pot-01\",\"moisture\":35,\"temperature\":29,\"light\":520}]}",
                    "Expected JSON: {\"pots\":[{\"id\":\"pot-01\",\"moisture\":35,\"temperature\":29,\"light\":520}]}"));
                return;
            }

            var sim = gm.simulator;
            if (sim == null) return;
            UIKit.Heading(content, L.T("ค่าเซนเซอร์จำลองรายกระถาง", "Mock values per pot"));
            UIKit.Note(content, L.T("ลากแถบเพื่อตั้งค่าเอง (จะหยุดจำลองอัตโนมัติของกระถางนั้น) ปิดสวิตช์เพื่อให้กลับไปจำลองเอง",
                "Drag a slider to set a value by hand (stops auto-simulation for that pot). Switch off to simulate again."));

            foreach (var pot in SortedPots())
            {
                var unit = pot.plant;
                var ps = sim.GetSim(unit);
                var card = UIKit.Card(content, "Pot_" + unit.potId, 8f);
                UIKit.Heading(card.transform, unit.potId + "  ·  " + unit.DisplayName, 22);

                Toggle manual = null;
                manual = UIKit.Switch(card.transform, L.T("ตั้งค่าเอง (Manual)", "Manual values"), ps.manual, v => { ps.manual = v; gm.MarkDirty(); });

                Text mText, tText, lText;
                var mS = UIKit.LabeledSlider(card.transform, L.MetricName(PlantMetric.Moisture), 0f, 100f, ps.moisture, true, Pct, v =>
                {
                    ps.manual = true;
                    manual.SetIsOnWithoutNotify(true);
                    manual.GetComponent<SwitchKnob>().Set(true);
                    ps.moisture = v;
                    sim.SendNow(unit);
                    gm.MarkDirty();
                }, Skin.water, out mText);
                var tS = UIKit.LabeledSlider(card.transform, L.MetricName(PlantMetric.Temperature), 0f, 50f, ps.temperature, false, Deg, v =>
                {
                    ps.manual = true;
                    manual.SetIsOnWithoutNotify(true);
                    manual.GetComponent<SwitchKnob>().Set(true);
                    ps.temperature = v;
                    sim.SendNow(unit);
                    gm.MarkDirty();
                }, Skin.statusHigh, out tText);
                var lS = UIKit.LabeledSlider(card.transform, L.MetricName(PlantMetric.Light), 0f, 1f, UIKit.SliderFromLux(ps.light), false, Lux, v =>
                {
                    ps.manual = true;
                    manual.SetIsOnWithoutNotify(true);
                    manual.GetComponent<SwitchKnob>().Set(true);
                    ps.light = UIKit.LuxFromSlider(v);
                    sim.SendNow(unit);
                    gm.MarkDirty();
                }, Skin.gold, out lText);

                live.Add(() =>
                {
                    if (ps.manual) return;
                    mS.SetValueWithoutNotify(ps.moisture);
                    mText.text = Pct(ps.moisture);
                    tS.SetValueWithoutNotify(ps.temperature);
                    tText.text = Deg(ps.temperature);
                    lS.SetValueWithoutNotify(UIKit.SliderFromLux(ps.light));
                    lText.text = Mathf.RoundToInt(ps.light).ToString("N0", CultureInfo.InvariantCulture) + " lux";
                });

                var presets = UIKit.Row(card.transform, "Presets", 46f, 8f);
                Preset(presets, L.T("ดินแห้ง", "Dry soil"), () => { ps.moisture = 12f; }, ps, sim, unit);
                Preset(presets, L.T("ดินแฉะ", "Soaked"), () => { ps.moisture = 92f; }, ps, sim, unit);
                Preset(presets, L.T("ร้อนจัด", "Heat"), () => { ps.temperature = 38f; }, ps, sim, unit);
                Preset(presets, L.T("มืด", "Dark"), () => { ps.light = 40f; }, ps, sim, unit);
                UIKit.SmallButton(presets, L.T("ปกติ", "Normal"), null, UISkin.WithAlpha(Skin.statusNormal, 0.15f), Skin.statusNormal, () =>
                {
                    ps.manual = false;
                    var sp = unit.species;
                    if (sp != null)
                    {
                        ps.moisture = (sp.moisture.min + sp.moisture.max) * 0.5f;
                        ps.temperature = ps.baseTemperature = (sp.temperature.min + sp.temperature.max) * 0.5f;
                        ps.light = ps.baseLight = Mathf.Max(sp.light.min * 1.5f, 400f);
                    }
                    sim.SendNow(unit);
                    gm.MarkDirty();
                    Build();
                }, 44f);
            }
        }

        void Preset(Transform row, string label, Action apply, SimulatedPlantDataSource.PotSim ps, SimulatedPlantDataSource sim, PlantUnit unit)
        {
            UIKit.SmallButton(row, label, null, UISkin.WithAlpha(Skin.statusLow, 0.15f), Skin.textPrimary, () =>
            {
                ps.manual = true;
                apply();
                sim.SendNow(unit);
                if (GameManager.Instance != null) GameManager.Instance.MarkDirty();
                Build();
            }, 44f);
        }

        // ================================================================== 3. servo

        void BuildServo()
        {
            var gm = GameManager.Instance;
            UIKit.Heading(content, L.T("Servo วาล์วน้ำ / ปุ๋ย", "Servo valve (water / fertilizer)"));
            UIKit.Note(content, L.T("0° = ปิด · 90° = เปิดน้ำ · 180° = เปิดปุ๋ย. ทุกการขยับส่งไปที่ ESP32: POST {\"id\":\"pot-01\",\"action\":\"servo\",\"angle\":90,\"seconds\":3} (ตั้ง URL ในแท็บเซนเซอร์). ในโหมดจำลอง ความชื้นจะเพิ่มขึ้นตลอดที่วาล์วน้ำเปิด",
                "0° = closed · 90° = water open · 180° = fertilizer open. Every move is sent to the ESP32: POST {\"id\":\"pot-01\",\"action\":\"servo\",\"angle\":90,\"seconds\":3} (set the URL in the sensor tab). In Mock mode moisture rises while the water line is open."));

            foreach (var pot in SortedPots())
            {
                var servo = pot.GetComponent<PotServo>();
                if (servo == null) continue;
                var unit = pot.plant;
                var card = UIKit.Card(content, "Servo_" + unit.potId, 8f);
                UIKit.Heading(card.transform, unit.potId + "  ·  " + unit.DisplayName, 22);
                var state = UIKit.Note(card.transform, "", 20);
                live.Add(() =>
                {
                    string t = L.T("มุมตอนนี้ ", "Now at ") + Mathf.RoundToInt(servo.Angle) + "°  ·  " + PotServo.StateName(servo.Current);
                    if (servo.IsOpen && servo.SecondsLeft > 0f) t += "  (" + servo.SecondsLeft.ToString("0.0") + "s)";
                    if (unit.HasReading) t += "  ·  " + L.MetricName(PlantMetric.Moisture) + " " + PlantHealth.Format(PlantMetric.Moisture, unit.Reading.moisture);
                    state.text = ThaiShaper.Fix(t);
                });

                UIKit.LabeledSlider(card.transform, L.T("หมุน Servo เอง (ค้างไว้)", "Move servo by hand (holds)"), 0f, 180f, servo.TargetAngle, true,
                    v => Mathf.RoundToInt(v) + "°", v => servo.SetManualAngle(v), Skin.accent);

                var row = UIKit.Row(card.transform, "Buttons", 46f, 8f);
                UIKit.SmallButton(row, L.T("รดน้ำ 90°", "Water 90°"), Skin.iconMoisture, Skin.water, Color.white, () =>
                {
                    servo.Pulse(ServoAction.Water);
                    var fx = pot.GetComponent<PotInteraction>();
                    if (EffectsLayer.Instance != null && fx != null && fx.art != null) EffectsLayer.Instance.PlayWater(fx.art);
                }, 44f);
                UIKit.SmallButton(row, L.T("ให้ปุ๋ย 180°", "Fertilizer 180°"), Skin.iconFertilizer, Skin.leafColor, Color.white, () => servo.Pulse(ServoAction.Fertilize), 44f);
                UIKit.SmallButton(row, L.T("ปิด 0°", "Close 0°"), Skin.iconClose, UISkin.WithAlpha(Skin.textPrimary, 0.1f), Skin.textPrimary, servo.Close, 44f);

                UIKit.Switch(card.transform, L.T("รดน้ำอัตโนมัติเมื่อดินแห้ง", "Water automatically when soil is dry"), servo.autoWater, v =>
                {
                    servo.autoWater = v;
                    if (gm != null) gm.MarkDirty();
                });
                UIKit.LabeledSlider(card.transform, L.T("เปิดน้ำครั้งละ", "Water pulse"), 1f, 10f, servo.waterSeconds, false, v => v.ToString("0.0") + L.T(" วินาที", " s"), v =>
                {
                    servo.waterSeconds = Mathf.Round(v * 2f) / 2f;
                    if (gm != null) gm.MarkDirty();
                }, Skin.water);
                UIKit.LabeledSlider(card.transform, L.T("เปิดปุ๋ยครั้งละ", "Fertilizer pulse"), 1f, 10f, servo.fertilizerSeconds, false, v => v.ToString("0.0") + L.T(" วินาที", " s"), v =>
                {
                    servo.fertilizerSeconds = Mathf.Round(v * 2f) / 2f;
                    if (gm != null) gm.MarkDirty();
                }, Skin.leafColor);
            }
        }

        // ================================================================== 4. time & weather

        void BuildTimeWeather()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var s = gm.Settings;

            UIKit.Heading(content, L.T("เวลา (เช้า - ค่ำ)", "Time (day - night)"));
            bool real = s.manualHour < 0f;
            UIKit.Switch(content, L.T("ใช้เวลาจริงของเครื่อง", "Use the real clock"), real, v =>
            {
                s.manualHour = v ? -1f : Mathf.Floor(GameClock.HourOfDay * 4f) / 4f;
                gm.ApplySettings();
                Build();
            });
            if (!real)
            {
                UIKit.LabeledSlider(content, L.T("ชั่วโมงในเกม", "Game hour"), 0f, 23.75f, s.manualHour, false, HourText, v =>
                {
                    s.manualHour = Mathf.Round(v * 4f) / 4f;
                    gm.ApplySettings();
                }, Skin.gold);
            }
            var clock = UIKit.Note(content, "");
            live.Add(() =>
            {
                var n = GameClock.Now;
                clock.text = ThaiShaper.Fix(L.T("เวลาในเกม ", "Game time ") + n.ToString("HH:mm", CultureInfo.InvariantCulture) + "  ·  " +
                                             DayNightCycle.PartOfDay(GameClock.HourOfDay) + "  ·  " + GameClock.TodayKey +
                                             (GameClock.DayOffset != 0 ? "  (+" + GameClock.DayOffset + L.T(" วัน", " d") + ")" : ""));
            });

            UIKit.Heading(content, L.T("สภาพอากาศ", "Weather"));
            UIKit.Switch(content, L.T("ดึงสภาพอากาศจริง (Open-Meteo)", "Live weather (Open-Meteo)"), s.useRealWeather, v =>
            {
                s.useRealWeather = v;
                gm.ApplySettings();
                if (gm.weather != null) gm.weather.RefreshNow();
            });
            var pos = UIKit.Row(content, "LatLon", 80f, 12f);
            var lat = UIKit.TextField(pos, L.T("ละติจูด", "Latitude"), s.latitude.ToString("0.####", CultureInfo.InvariantCulture), "13.75",
                InputField.ContentType.DecimalNumber, v =>
                {
                    double d;
                    if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) s.latitude = d;
                    gm.ApplySettings();
                    if (gm.weather != null) gm.weather.RefreshNow();
                });
            UIFactory.Layout(lat.transform.parent.GetComponent<RectTransform>(), -1f, -1f, 1f, -1f);
            var lon = UIKit.TextField(pos, L.T("ลองจิจูด", "Longitude"), s.longitude.ToString("0.####", CultureInfo.InvariantCulture), "100.5",
                InputField.ContentType.DecimalNumber, v =>
                {
                    double d;
                    if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) s.longitude = d;
                    gm.ApplySettings();
                    if (gm.weather != null) gm.weather.RefreshNow();
                });
            UIFactory.Layout(lon.transform.parent.GetComponent<RectTransform>(), -1f, -1f, 1f, -1f);

            var kinds = new List<string> { L.T("ตามจริง", "Real") };
            for (int i = 0; i <= (int)WeatherKind.Storm; i++) kinds.Add(WeatherService.Name((WeatherKind)i));
            UIKit.Cycle(content, L.T("บังคับสภาพอากาศ", "Force weather"), kinds.ToArray(), s.weatherOverride + 1, i =>
            {
                s.weatherOverride = i - 1;
                gm.ApplySettings();
            });
            var wx = UIKit.Note(content, "");
            live.Add(() =>
            {
                var w = gm.weather;
                string t = L.T("ตอนนี้: ", "Now: ") + WeatherService.Name(EnvironmentState.Weather);
                if (w != null && w.HasRealData)
                    t += L.T("  ·  ข้อมูลจริง ", "  ·  live ") + Mathf.RoundToInt(w.RealTemperature) + "°C, " + L.T("ความชื้นอากาศ ", "humidity ") +
                         Mathf.RoundToInt(w.RealHumidity) + "%  ·  " + WeatherService.Name(w.RealKind) + L.T("  · อัปเดต ", "  · updated ") +
                         w.LastUpdate.ToString("HH:mm", CultureInfo.InvariantCulture);
                else if (w != null && !string.IsNullOrEmpty(w.LastError)) t += "  ·  " + w.LastError;
                wx.text = ThaiShaper.Fix(t);
            });
            UIKit.SmallButton(content, L.T("ดึงข้อมูลอากาศตอนนี้", "Refresh weather now"), Skin.iconRefresh, UISkin.WithAlpha(Skin.accent, 0.12f), Skin.accentDark, () =>
            {
                if (gm.weather != null) gm.weather.RefreshNow();
            });

            UIKit.Heading(content, L.T("อีเวนต์ประจำวัน", "Daily event"));
            var evNames = new List<string> { L.T("ตามวันที่", "By date") };
            var savedForce = DailyEvents.Instance != null ? DailyEvents.Instance.forceEvent : -1;
            for (int i = 0; i <= (int)DayEventKind.RainyDay; i++)
            {
                if (DailyEvents.Instance != null)
                {
                    DailyEvents.Instance.forceEvent = i;
                    DailyEvents.Instance.Refresh(false);
                    evNames.Add(DailyEvents.Instance.Title);
                }
            }
            if (DailyEvents.Instance != null)
            {
                DailyEvents.Instance.forceEvent = savedForce;
                DailyEvents.Instance.Refresh(false);
            }
            UIKit.Cycle(content, L.T("บังคับอีเวนต์", "Force event"), evNames.ToArray(), s.eventOverride + 1, i =>
            {
                s.eventOverride = i - 1;
                gm.ApplySettings();
            });
            var ev = UIKit.Note(content, "");
            live.Add(() =>
            {
                var d = DailyEvents.Instance;
                if (d != null) ev.text = ThaiShaper.Fix(d.Title + ": " + d.Description);
            });
        }

        static string HourText(float h)
        {
            int hh = Mathf.FloorToInt(h);
            int mm = Mathf.RoundToInt((h - hh) * 60f);
            return hh.ToString("00") + ":" + mm.ToString("00");
        }

        // ================================================================== 5. game test

        void AnimButton(Transform row, string label, PlantMetric metric, MetricStatus status, Color color)
        {
            UIKit.SmallButton(row, label, Skin.iconSparkles, UISkin.WithAlpha(color, 0.15f), Skin.textPrimary, () =>
            {
                var layer = AlertAnimationLayer.Instance;
                var plant = PlantSelection.Current != null ? PlantSelection.Current : (PlantUnit.All.Count > 0 ? PlantUnit.All[0] : null);
                if (layer == null || plant == null) return;
                Close();   // so the pot is visible
                layer.Play(plant, metric, status);
            });
        }

        void BuildDebug()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var s = gm.Settings;

            UIKit.Heading(content, L.T("ทดสอบระบบเกม", "Game test tools"));
            var info = UIKit.Note(content, "", 20);
            live.Add(() =>
            {
                var sel = PlantSelection.Current;
                info.text = ThaiShaper.Fix(L.T("แต้ม ", "Points ") + gm.Points + L.T("  ·  เมล็ดในคลัง ", "  ·  seeds ") + gm.TotalSeeds +
                                           L.T("  ·  วันที่ในเกม ", "  ·  game date ") + GameClock.TodayKey +
                                           L.T("  ·  ต้นที่เลือก: ", "  ·  selected: ") + (sel != null ? sel.potId + " " + sel.DisplayName : "-"));
            });

            var row1 = UIKit.Row(content, "Row1", 48f, 8f);
            UIKit.SmallButton(row1, "+100 " + L.Points, Skin.iconCoins, UISkin.WithAlpha(Skin.gold, 0.2f), Skin.textPrimary, () => gm.AddPoints(100));
            UIKit.SmallButton(row1, L.T("ข้ามไป 1 วัน", "Skip 1 day"), Skin.iconFastForward, UISkin.WithAlpha(Skin.eventColor, 0.15f), Skin.eventColor, () =>
            {
                gm.SkipDay();
                Post(L.T("ข้ามวัน", "Day skipped"), L.T("วันที่ในเกม: ", "Game date: ") + GameClock.TodayKey, AlertKind.GameEvent);
            });

            var row2 = UIKit.Row(content, "Row2", 48f, 8f);
            UIKit.SmallButton(row2, L.T("ต้นที่เลือก: โตเต็มที่", "Selected: grow fully"), Skin.iconSprout, UISkin.WithAlpha(Skin.leafColor, 0.15f), Skin.textPrimary, () =>
            {
                var g = SelectedGrowth();
                if (g != null && !g.IsEmpty) g.growth = 1f;
            });
            UIKit.SmallButton(row2, L.T("ต้นที่เลือก: Streak +3", "Selected: streak +3"), Skin.iconFlame, UISkin.WithAlpha(Skin.flame, 0.15f), Skin.textPrimary, () =>
            {
                var g = SelectedGrowth();
                if (g != null && !g.IsEmpty)
                {
                    g.streak += 3;
                    gm.MarkDirty();
                }
            });
            UIKit.SmallButton(row2, L.T("ต้นที่เลือก: กลายพันธุ์", "Selected: mutate"), Skin.iconSparkles, UISkin.WithAlpha(Skin.gold, 0.2f), Skin.textPrimary, () =>
            {
                var g = SelectedGrowth();
                if (g == null || g.IsEmpty) return;
                forcedMutationIndex = forcedMutationIndex % 4 + 1;
                g.ForceMutation((Mutation)forcedMutationIndex);
                gm.MarkDirty();
                Post(g.plant.DisplayName, L.T("กลายพันธุ์เป็น ", "Mutated to ") + MutationInfo.Name(g.mutation), AlertKind.GameEvent);
            });

            UIKit.Note(content, L.T("ดูแอนิเมชันแจ้งเตือนบนกระถางที่เลือก (หรือกระถางแรก)", "Preview the alert animations on the selected pot (or the first pot)"), 17);
            var row3 = UIKit.Row(content, "Row3", 48f, 8f);
            AnimButton(row3, L.T("ร้อนไป", "Too hot"), PlantMetric.Temperature, MetricStatus.High, Skin.statusHigh);
            AnimButton(row3, L.T("หนาวไป", "Too cold"), PlantMetric.Temperature, MetricStatus.Low, Skin.water);
            AnimButton(row3, L.T("น้ำเยอะไป", "Too wet"), PlantMetric.Moisture, MetricStatus.High, Skin.water);
            AnimButton(row3, L.T("ดินแห้งไป", "Too dry"), PlantMetric.Moisture, MetricStatus.Low, Skin.statusLow);

            UIKit.LabeledSlider(content, L.T("ความเร็วการโต (สำหรับเดโม)", "Growth speed (for demos)"), 1f, 30f, s.growthSpeed, true, v => "x" + Mathf.RoundToInt(v), v =>
            {
                s.growthSpeed = Mathf.Round(v);
                gm.ApplySettings();
            }, Skin.leafColor);
            UIKit.LabeledSlider(content, L.T("แจ้งเตือนซ้ำถ้ายังไม่แก้ทุก", "Repeat unresolved alerts every"), 0f, 600f, s.remindAfterSeconds, true,
                v => v <= 0f ? L.T("ไม่เตือนซ้ำ", "never") : Mathf.RoundToInt(v) + L.T(" วินาที", " s"), v =>
                {
                    s.remindAfterSeconds = Mathf.Round(v);
                    gm.ApplySettings();
                }, Skin.statusLow);

            UIKit.Heading(content, L.T("ไฟล์บันทึก", "Save file"), 21);
            UIKit.Note(content, gm.SavePath);
            UIKit.SmallButton(content, L.T("ล้างเซฟ เริ่มใหม่ (กระถางว่าง + แต้มเริ่มต้น)", "Reset save (empty pots + starting points)"), Skin.iconTrash,
                UISkin.WithAlpha(Skin.statusHigh, 0.15f), Skin.statusHigh, () =>
                {
                    ConfirmDialog.Show(L.T("ล้างข้อมูลเกมทั้งหมด?", "Wipe all game data?"),
                        L.T("แต้ม เมล็ด ต้นไม้ และค่าที่ตั้งไว้จะหายทั้งหมด", "Points, seeds, plants and settings will be lost."),
                        L.T("ล้างเลย", "Wipe"), () =>
                        {
                            gm.ResetSave();
                            Build();
                        });
                });
        }

        static PlantGrowth SelectedGrowth()
        {
            var sel = PlantSelection.Current;
            if (sel == null)
            {
                if (NotificationCenter.Instance != null)
                    NotificationCenter.Instance.PostInfo(null, L.T("เลือกกระถางก่อน", "Select a pot first"),
                        L.T("แตะกระถางในฉากแล้วเปิดหน้านี้อีกครั้ง", "Tap a pot in the scene, then come back"), AlertKind.Refused);
                return null;
            }
            return sel.GetComponent<PlantGrowth>();
        }
    }
}
