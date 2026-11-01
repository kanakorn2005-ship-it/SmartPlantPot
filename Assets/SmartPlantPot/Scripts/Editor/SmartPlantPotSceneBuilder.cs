using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SmartPlantPot.EditorTools
{
    /// <summary>
    /// Menu: Smart Plant Pot > Build Dashboard Scene.
    /// Creates Assets/SmartPlantPot/Scenes/PlantDashboard.unity with every object wired up.
    /// Safe to run again: species / skin assets you edited are kept, only the scene is rebuilt.
    /// </summary>
    public static class SmartPlantPotSceneBuilder
    {
        const string Root = "Assets/SmartPlantPot";
        const string ArtDir = Root + "/Art";
        const string DataDir = Root + "/Data";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/PlantDashboard.unity";

        const float LabelHeight = 64f;
        const float StartPlantHeight = 520f;
        const float PlantAspect = 364f / 532f;

        [MenuItem("Smart Plant Pot/Build Dashboard Scene", false, 0)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            EnsureImportSettings(false);
            var skin = LoadOrCreateSkin();
            var species = LoadOrCreateSpecies();
            UISkin.Current = skin;
            L.Thai = true;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- camera, light, input ----------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = skin.background;
            camGo.transform.position = new Vector3(0f, 1f, -10f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PhysicsRaycaster>(); // lets PlantSelectable work on 3D models later

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            // ---------- app logic ----------
            var appGo = new GameObject("SmartPlantPotApp");
            var app = appGo.AddComponent<SmartPlantPotApp>();
            app.skin = skin;
            var sim = appGo.AddComponent<SimulatedPlantDataSource>();
            var http = appGo.AddComponent<HttpPlantDataSource>();
            http.enabled = false;
            appGo.AddComponent<NotificationCenter>();

            // ---------- canvas ----------
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.layer = 5;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var layout = canvasGo.AddComponent<ResponsiveLayout>();
            layout.scaler = scaler;
            var canvasRt = (RectTransform)canvasGo.transform;

            // background "room": wall, soft light blobs, floor
            var bg = UIFactory.Image(canvasRt, "Background", null, skin.background, true);
            UIFactory.Stretch(bg.rectTransform);
            var catcher = bg.gameObject.AddComponent<BackgroundClickCatcher>();

            var blobA = UIFactory.Image(canvasRt, "Decor_LightA", skin.glow, UISkin.WithAlpha(skin.accent, 0.10f));
            UIFactory.Place(blobA.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(160f, -120f), new Vector2(1000f, 1000f));
            var blobB = UIFactory.Image(canvasRt, "Decor_LightB", skin.glow, UISkin.WithAlpha(skin.leafColor, 0.08f));
            UIFactory.Place(blobB.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-220f, -300f), new Vector2(820f, 820f));

            var floor = UIFactory.Image(canvasRt, "Floor", null, skin.floor);
            floor.rectTransform.anchorMin = Vector2.zero;
            floor.rectTransform.anchorMax = new Vector2(1f, 0.3f);
            floor.rectTransform.offsetMin = Vector2.zero;
            floor.rectTransform.offsetMax = Vector2.zero;
            var floorEdge = UIFactory.Image(floor.transform, "Edge", null, UISkin.WithAlpha(skin.shelfShadow, 0.18f));
            floorEdge.rectTransform.anchorMin = new Vector2(0f, 1f);
            floorEdge.rectTransform.anchorMax = new Vector2(1f, 1f);
            floorEdge.rectTransform.pivot = new Vector2(0.5f, 1f);
            floorEdge.rectTransform.anchoredPosition = Vector2.zero;
            floorEdge.rectTransform.sizeDelta = new Vector2(0f, 6f);

            var safe = UIFactory.Rect(canvasRt, "SafeArea");
            UIFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // ---------- header ----------
            Text subtitle;
            var bell = BuildHeader(safe, skin, out subtitle);

            // ---------- shelf with the plants (card 1) ----------
            var shelf = UIFactory.Rect(safe, "Shelf");
            UIFactory.Stretch(shelf, 250f, 250f, 150f, 40f);

            var plank = UIFactory.Rounded(shelf, "Plank", skin.shelf, 10f);
            UIFactory.Shadow(plank.rectTransform, 34f, 0.2f, -16f);
            var lip = UIFactory.Rounded(plank.transform, "Lip", skin.shelfShadow, 8f);
            float w0 = StartPlantHeight * PlantAspect;
            UIFactory.Place(plank.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, LabelHeight + StartPlantHeight * 0.035f), new Vector2(3f * w0 + 2f * 84f + w0 * 0.5f, 23f));

            var row = UIFactory.Rect(shelf, "PlantRow");
            UIFactory.Stretch(row);
            var hlg = UIFactory.HRow(row.gameObject, 84f, UIFactory.Pad(0), TextAnchor.LowerCenter);

            string[] potIds = { "pot-01", "pot-02", "pot-03" };
            var units = new List<PlantUnit>();
            for (int i = 0; i < species.Length; i++)
                units.Add(BuildPlantSlot(row, species[i], potIds[i], skin));

            // pre-fill the simulator so the sliders show up in the Inspector before Play
            foreach (var u in units)
            {
                sim.pots.Add(new SimulatedPlantDataSource.PotSim
                {
                    plant = u,
                    moisture = u.species.demoReading.moisture,
                    temperature = u.species.demoReading.temperature,
                    light = u.species.demoReading.light
                });
            }
            http.singlePotTarget = units[0];

            // ---------- mascot ----------
            var mascot = BuildMascot(safe, skin);

            // ---------- info card (card 2 + card 3 buttons) ----------
            var card = BuildInfoCard(safe, skin, species[0]);

            // ---------- notifications (card 4) ----------
            var panel = BuildHistoryPanel(safe, skin);
            var toasts = UIFactory.Rect(safe, "Toasts");
            UIFactory.Place(toasts, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -150f), new Vector2(540f, 10f));
            UIFactory.VColumn(toasts.gameObject, 12f, UIFactory.Pad(0), TextAnchor.UpperRight);
            UIFactory.Fit(toasts.gameObject, false, true);
            var presenter = toasts.gameObject.AddComponent<ToastPresenter>();
            presenter.stack = toasts;

            // ---------- effects on top of everything ----------
            var fx = UIFactory.Rect(canvasRt, "Effects");
            UIFactory.Stretch(fx);
            fx.gameObject.AddComponent<EffectsLayer>();

            // ---------- wiring ----------
            bell.panel = panel;
            catcher.historyPanel = panel;
            app.historyPanel = panel;
            mascot.infoCard = card;

            layout.shelfArea = shelf;
            layout.plantRow = hlg;
            layout.plank = plank.rectTransform;
            layout.plankLip = lip.rectTransform;
            layout.headerSubtitle = subtitle.gameObject;
            layout.mascotArea = (RectTransform)mascot.transform;
            layout.toastStack = toasts;
            layout.historyPanel = (RectTransform)panel.transform;
            layout.infoCard = card;
            layout.labelHeight = LabelHeight;
            layout.plantAspect = PlantAspect;
            layout.landscapeSideGutter = 250f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Selection.activeGameObject = canvasGo;
            Debug.Log("[SmartPlantPot] Built " + ScenePath + ". Press Play: tap a plant, use Water/Prune, watch the alerts.");
        }

        [MenuItem("Smart Plant Pot/Open Dashboard Scene", false, 1)]
        public static void Open()
        {
            if (!File.Exists(ScenePath))
            {
                Build();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Smart Plant Pot/Fix Sprite Import Settings", false, 20)]
        public static void FixImports()
        {
            EnsureImportSettings(true);
        }

        // =====================================================================================
        // Pieces
        // =====================================================================================

        static NotificationBell BuildHeader(RectTransform safe, UISkin skin, out Text subtitle)
        {
            var header = UIFactory.Rect(safe, "Header");
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 120f);
            var bar = header.gameObject.AddComponent<HeaderBar>();

            var logo = UIFactory.Circle(header, "Logo", skin.accent, 76f);
            UIFactory.Place(logo.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(76f, 76f));
            var logoIcon = UIFactory.Icon(logo.transform, "Icon", skin.iconSprout, Color.white, 44f);
            UIFactory.Place(logoIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));

            var title = UIFactory.Text(header, "Title", "Smart Plant Pot", 44, skin.textPrimary, TextAnchor.LowerLeft, true);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(128f, -6f), new Vector2(700f, 64f));

            subtitle = UIFactory.Text(header, "Subtitle", "", 22, skin.textMuted, TextAnchor.UpperLeft);
            subtitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(130f, -6f), new Vector2(800f, 36f));
            LocalizedText.Attach(subtitle, "Leafy Corner · ดิจิทัลทวินกระถางต้นไม้", "Leafy Corner · Smart pot digital twin");

            var right = UIFactory.Rect(header, "Right");
            UIFactory.Place(right, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(10f, 80f));
            UIFactory.HRow(right.gameObject, 20f, UIFactory.Pad(0), TextAnchor.MiddleRight);
            UIFactory.Fit(right.gameObject, true, false);

            var pill = UIFactory.Rounded(right, "Connection", skin.surface, 24f);
            UIFactory.HRow(pill.gameObject, 10f, UIFactory.Pad(18, 10), TextAnchor.MiddleCenter);
            UIFactory.Layout(pill, -1f, 48f);
            var dot = UIFactory.Circle(pill.transform, "Dot", skin.statusNormal, 14f);
            UIFactory.Layout(dot, 14f, 14f);
            var conn = UIFactory.Text(pill.transform, "Label", L.SimulatedData, 22, skin.textPrimary, TextAnchor.MiddleLeft);
            conn.horizontalOverflow = HorizontalWrapMode.Overflow;

            var clock = UIFactory.Text(right, "Clock", "00:00", 40, skin.textPrimary, TextAnchor.MiddleRight, true);
            clock.horizontalOverflow = HorizontalWrapMode.Overflow;

            var bellBtn = UIFactory.IconButton(right, "Bell", skin.iconBell, skin.surface, skin.accentDark, 72f);
            UIFactory.Layout(bellBtn, 72f, 72f);
            var badge = UIFactory.Circle(bellBtn.transform, "Badge", skin.statusHigh, 30f);
            UIFactory.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(30f, 30f));
            var badgeText = UIFactory.Text(badge.transform, "Count", "1", 18, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(badgeText.rectTransform);
            badgeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            badge.gameObject.SetActive(false);

            bar.clock = clock;
            bar.connectionDot = dot;
            bar.connectionLabel = conn;

            var bell = bellBtn.gameObject.AddComponent<NotificationBell>();
            bell.button = bellBtn;
            bell.icon = (RectTransform)bellBtn.transform.Find("Icon");
            bell.badge = badge.gameObject;
            bell.badgeText = badgeText;
            return bell;
        }

        static PlantUnit BuildPlantSlot(RectTransform row, PlantSpecies sp, string potId, UISkin skin)
        {
            var slot = UIFactory.Rect(row, "Plant_" + sp.nameEnglish.Replace(" ", ""));
            float w = StartPlantHeight * PlantAspect;
            var le = slot.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = w;
            le.minHeight = le.preferredHeight = StartPlantHeight + LabelHeight;

            var unit = slot.gameObject.AddComponent<PlantUnit>();
            unit.species = sp;
            unit.potId = potId;

            var artArea = UIFactory.Rect(slot, "ArtArea");
            UIFactory.Stretch(artArea, 0f, 0f, 0f, LabelHeight);

            var glow = UIFactory.Image(artArea, "StatusGlow", skin.glow, UISkin.WithAlpha(skin.statusNormal, 0.22f));
            glow.rectTransform.anchorMin = new Vector2(-0.2f, -0.07f);
            glow.rectTransform.anchorMax = new Vector2(1.2f, 0.13f);
            glow.rectTransform.offsetMin = Vector2.zero;
            glow.rectTransform.offsetMax = Vector2.zero;

            var art = UIFactory.Image(artArea, "Art", sp.sprite, Color.white, true);
            art.preserveAspect = true;
            UIFactory.Stretch(art.rectTransform);
            art.rectTransform.pivot = new Vector2(0.5f, 0f); // squash & sway from the pot's base
            art.rectTransform.offsetMin = Vector2.zero;
            art.rectTransform.offsetMax = Vector2.zero;

            var badge = UIFactory.Circle(artArea, "AlertBadge", skin.statusLow, 60f);
            UIFactory.Place(badge.rectTransform, new Vector2(0.86f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            var badgeIcon = UIFactory.Icon(badge.transform, "Icon", skin.iconAlert, Color.white, 34f);
            UIFactory.Place(badgeIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(34f, 34f));
            badge.gameObject.SetActive(false);

            var pill = UIFactory.Rounded(slot, "NamePill", skin.surface, 24f, true);
            UIFactory.Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(160f, 48f));
            UIFactory.Shadow(pill.rectTransform, 14f, 0.10f, -3f);
            UIFactory.HRow(pill.gameObject, 0f, UIFactory.Pad(22, 0), TextAnchor.MiddleCenter);
            UIFactory.Fit(pill.gameObject, true, false);
            var name = UIFactory.Text(pill.transform, "Name", sp.DisplayName, 24, skin.textPrimary, TextAnchor.MiddleCenter, true);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            var view = slot.gameObject.AddComponent<PlantSlotView>();
            view.plant = unit;
            view.art = art;
            view.statusGlow = glow;
            view.namePill = pill;
            view.nameLabel = name;
            view.alertBadge = badge.rectTransform;
            view.alertBadgeBackground = badge;

            var interaction = slot.gameObject.AddComponent<PotInteraction>();
            interaction.plant = unit;
            interaction.art = art.rectTransform;

            var selectable = slot.gameObject.AddComponent<PlantSelectable>();
            selectable.plant = unit;
            selectable.interaction = interaction;
            return unit;
        }

        static MascotHelper BuildMascot(RectTransform safe, UISkin skin)
        {
            var area = UIFactory.Rect(safe, "Mascot");
            UIFactory.Place(area, Vector2.zero, Vector2.zero, new Vector2(24f, 16f), new Vector2(200f, 200f));

            var body = UIFactory.Image(area, "Body", LoadSprite(ArtDir + "/Mascots/mascot_bunny_hat.png"), Color.white, true);
            body.preserveAspect = true;
            UIFactory.Stretch(body.rectTransform);
            body.rectTransform.pivot = new Vector2(0.5f, 0f);
            body.rectTransform.offsetMin = Vector2.zero;
            body.rectTransform.offsetMax = Vector2.zero;

            var bubble = UIFactory.Rounded(area, "Bubble", skin.surface, 22f);
            UIFactory.Place(bubble.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(8f, 10f), new Vector2(300f, 60f));
            UIFactory.Shadow(bubble.rectTransform, 18f, 0.12f, -4f);
            var tail = UIFactory.Rounded(bubble.transform, "Tail", skin.surface, 4f);
            UIFactory.Place(tail.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(56f, 2f), new Vector2(26f, 26f));
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIFactory.VColumn(bubble.gameObject, 0f, new RectOffset(20, 20, 14, 16));
            UIFactory.Fit(bubble.gameObject, false, true);
            var text = UIFactory.Text(bubble.transform, "Text", L.HintTapPlant, 21, skin.textPrimary, TextAnchor.UpperLeft);
            var group = bubble.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var helper = area.gameObject.AddComponent<MascotHelper>();
            helper.body = body.rectTransform;
            helper.bubble = bubble.rectTransform;
            helper.bubbleText = text;
            helper.bubbleGroup = group;
            return helper;
        }

        static PlantInfoCard BuildInfoCard(RectTransform safe, UISkin skin, PlantSpecies preview)
        {
            var bg = UIFactory.Rounded(safe, "InfoCard", skin.infoCard, 30f, true);
            var rt = bg.rectTransform;
            UIFactory.Place(rt, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(760f, 470f), new Vector2(600f, 10f));
            // Hidden until a plant is tapped. Set this alpha to 1 temporarily to style the card in the editor.
            bg.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            UIFactory.Shadow(rt, 30f, 0.18f, -10f);
            UIFactory.VColumn(bg.gameObject, 14f, new RectOffset(30, 30, 24, 28));
            UIFactory.Fit(bg.gameObject, false, true);

            var head = UIFactory.Rect(rt, "Header");
            UIFactory.HRow(head.gameObject, 12f, UIFactory.Pad(0), TextAnchor.MiddleLeft);
            UIFactory.Layout(head, -1f, 52f);
            var dot = UIFactory.Circle(head, "Dot", skin.statusNormal, 18f);
            UIFactory.Layout(dot, 18f, 18f);
            var title = UIFactory.Text(head, "Title", preview.DisplayName, 34, skin.textPrimary, TextAnchor.MiddleLeft, true);
            UIFactory.Layout(title, -1f, -1f, 1f, -1f);
            var close = UIFactory.IconButton(head, "Close", skin.iconClose, UISkin.WithAlpha(skin.textPrimary, 0.08f), skin.textPrimary, 48f);
            UIFactory.Layout(close, 48f, 48f);

            var subtitle = UIFactory.Text(rt, "Subtitle", "pot-01  ·  " + L.Updated("--:--:--"), 19, skin.textMuted, TextAnchor.UpperLeft);

            var rows = new MetricRowView[3];
            for (int i = 0; i < 3; i++) rows[i] = BuildMetricRow(rt, PlantMetrics.All[i], skin);

            var banner = UIFactory.Rounded(rt, "Status", UISkin.WithAlpha(skin.statusNormal, 0.16f), 18f);
            UIFactory.HRow(banner.gameObject, 12f, new RectOffset(16, 16, 12, 12), TextAnchor.MiddleLeft);
            var sIcon = UIFactory.Icon(banner.transform, "Icon", skin.iconCheck, skin.statusNormal, 32f);
            UIFactory.Layout(sIcon, 32f, 32f);
            var sText = UIFactory.Text(banner.transform, "Text", L.StatusName(MetricStatus.Normal), 24, skin.textPrimary, TextAnchor.MiddleLeft, true);
            UIFactory.Layout(sText, -1f, -1f, 1f, -1f);

            var actions = UIFactory.Rect(rt, "Actions");
            var actionsRow = UIFactory.HRow(actions.gameObject, 14f, UIFactory.Pad(0), TextAnchor.MiddleCenter);
            actionsRow.childForceExpandWidth = true;
            UIFactory.Layout(actions, -1f, 64f);
            var water = UIFactory.Button(actions, "Water", L.Water, skin.iconMoisture, skin.water, Color.white, 26, 32f, 64f);
            LocalizedText.Attach(water.transform.Find("Label").GetComponent<Text>(), "รดน้ำ", "Water");
            var prune = UIFactory.Button(actions, "Prune", L.Prune, skin.iconScissors, skin.leafColor, Color.white, 26, 32f, 64f);
            LocalizedText.Attach(prune.transform.Find("Label").GetComponent<Text>(), "แต่งกิ่ง", "Prune");

            var card = bg.gameObject.AddComponent<PlantInfoCard>();
            card.titleDot = dot;
            card.title = title;
            card.subtitle = subtitle;
            card.rows = rows;
            card.statusBanner = banner;
            card.statusIcon = sIcon;
            card.statusText = sText;
            card.waterButton = water;
            card.pruneButton = prune;
            card.closeButton = close;
            return card;
        }

        static MetricRowView BuildMetricRow(RectTransform parent, PlantMetric metric, UISkin skin)
        {
            var row = UIFactory.Rounded(parent, "Row_" + metric, new Color(1f, 1f, 1f, 0.75f), 20f);
            UIFactory.HRow(row.gameObject, 16f, new RectOffset(16, 18, 14, 14), TextAnchor.MiddleLeft);

            var iconBg = UIFactory.Circle(row.transform, "IconBg", skin.statusNormal, 56f);
            UIFactory.Layout(iconBg, 56f, 56f);
            var icon = UIFactory.Icon(iconBg.transform, "Icon", skin.MetricIcon(metric), Color.white, 32f);
            UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));

            var col = UIFactory.Rect(row.transform, "Column");
            UIFactory.VColumn(col.gameObject, 6f, UIFactory.Pad(0));
            UIFactory.Layout(col, -1f, -1f, 1f, -1f);

            var top = UIFactory.Rect(col, "Top");
            UIFactory.HRow(top.gameObject, 10f, UIFactory.Pad(0), TextAnchor.MiddleLeft);
            UIFactory.Layout(top, -1f, 36f);
            var label = UIFactory.Text(top, "Label", L.MetricName(metric), 22, skin.textMuted, TextAnchor.MiddleLeft);
            UIFactory.Layout(label, -1f, -1f, 1f, -1f);
            var chip = UIFactory.Rounded(top, "Chip", UISkin.WithAlpha(skin.statusNormal, 0.16f), 14f);
            UIFactory.HRow(chip.gameObject, 0f, UIFactory.Pad(12, 2), TextAnchor.MiddleCenter);
            UIFactory.Layout(chip, -1f, 30f);
            var chipText = UIFactory.Text(chip.transform, "Text", L.StatusName(MetricStatus.Normal), 18, skin.statusNormal, TextAnchor.MiddleCenter, true);
            chipText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var value = UIFactory.Text(top, "Value", "--", 30, skin.textPrimary, TextAnchor.MiddleRight, true);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;

            var bar = UIFactory.Rounded(col, "Bar", UISkin.WithAlpha(skin.textPrimary, 0.08f), 7f);
            UIFactory.Layout(bar, -1f, 14f);
            var band = UIFactory.Rounded(bar.transform, "IdealRange", UISkin.WithAlpha(skin.statusNormal, 0.35f), 7f);
            band.rectTransform.anchorMin = new Vector2(0.4f, 0f);
            band.rectTransform.anchorMax = new Vector2(0.75f, 1f);
            band.rectTransform.offsetMin = Vector2.zero;
            band.rectTransform.offsetMax = Vector2.zero;
            var marker = UIFactory.Circle(bar.transform, "Marker", Color.white, 28f);
            UIFactory.Place(marker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));
            var markerFill = UIFactory.Circle(marker.transform, "Fill", skin.statusNormal, 18f);
            UIFactory.Place(markerFill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));

            var range = UIFactory.Text(col, "Range", "", 17, skin.textMuted, TextAnchor.UpperLeft);

            var view = row.gameObject.AddComponent<MetricRowView>();
            view.metric = metric;
            view.iconBackground = iconBg;
            view.icon = icon;
            view.label = label;
            view.value = value;
            view.statusChip = chip;
            view.statusText = chipText;
            view.rangeBand = band.rectTransform;
            view.rangeBandImage = band;
            view.marker = marker.rectTransform;
            view.markerImage = markerFill;
            view.rangeText = range;
            return view;
        }

        static NotificationHistoryPanel BuildHistoryPanel(RectTransform safe, UISkin skin)
        {
            var bg = UIFactory.Rounded(safe, "NotificationPanel", skin.surface, 28f, true);
            var rt = bg.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(-624f, 24f);
            rt.offsetMax = new Vector2(-24f, -144f);
            // Hidden until the bell is tapped. Set this alpha to 1 temporarily to style the panel in the editor.
            bg.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            UIFactory.Shadow(rt, 30f, 0.2f, -10f);

            var head = UIFactory.Rect(rt, "Header");
            head.anchorMin = new Vector2(0f, 1f);
            head.anchorMax = new Vector2(1f, 1f);
            head.pivot = new Vector2(0.5f, 1f);
            head.anchoredPosition = Vector2.zero;
            head.sizeDelta = new Vector2(0f, 90f);
            UIFactory.HRow(head.gameObject, 12f, new RectOffset(28, 20, 0, 0), TextAnchor.MiddleLeft);
            var title = UIFactory.Text(head, "Title", L.Notifications, 30, skin.textPrimary, TextAnchor.MiddleLeft, true);
            UIFactory.Layout(title, -1f, -1f, 1f, -1f);
            LocalizedText.Attach(title, "การแจ้งเตือน", "Notifications");
            var clear = UIFactory.Button(head, "Clear", L.ClearAll, null, UISkin.WithAlpha(skin.accent, 0.12f), skin.accentDark, 20, 22f, 44f);
            UIFactory.Layout(clear, -1f, 44f, 0f, -1f);
            LocalizedText.Attach(clear.transform.Find("Label").GetComponent<Text>(), "ล้างทั้งหมด", "Clear all");
            var close = UIFactory.IconButton(head, "Close", skin.iconClose, UISkin.WithAlpha(skin.textPrimary, 0.08f), skin.textPrimary, 48f);
            UIFactory.Layout(close, 48f, 48f);

            var scrollRt = UIFactory.Rect(rt, "Scroll");
            UIFactory.Stretch(scrollRt, 16f, 16f, 96f, 16f);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = UIFactory.Rect(scrollRt, "Viewport");
            UIFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0f);

            var content = UIFactory.Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            UIFactory.VColumn(content.gameObject, 10f, UIFactory.Pad(0));
            UIFactory.Fit(content.gameObject, false, true);
            scroll.viewport = viewport;
            scroll.content = content;

            var empty = UIFactory.Text(rt, "Empty", L.NoNotifications, 22, skin.textMuted, TextAnchor.MiddleCenter);
            UIFactory.Stretch(empty.rectTransform, 20f, 20f, 100f, 20f);
            LocalizedText.Attach(empty, "ยังไม่มีการแจ้งเตือน", "No notifications yet");

            var panel = bg.gameObject.AddComponent<NotificationHistoryPanel>();
            panel.content = content;
            panel.scroll = scroll;
            panel.emptyState = empty.gameObject;
            panel.clearButton = clear;
            panel.closeButton = close;
            return panel;
        }

        // =====================================================================================
        // Assets
        // =====================================================================================

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(DataDir)) AssetDatabase.CreateFolder(Root, "Data");
            if (!AssetDatabase.IsValidFolder(SceneDir)) AssetDatabase.CreateFolder(Root, "Scenes");
        }

        static void EnsureImportSettings(bool force)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string file = Path.GetFileName(path);
                float b = file == "ui_rounded.png" ? 44f : file == "ui_shadow.png" ? 64f : 0f;
                var border = new Vector4(b, b, b, b);
                bool needs = force || ti.textureType != TextureImporterType.Sprite ||
                             ti.spriteImportMode != SpriteImportMode.Single || ti.spriteBorder != border || ti.mipmapEnabled;
                if (!needs) continue;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.spriteBorder = border;
                ti.spritePixelsPerUnit = 100f;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
            }
        }

        static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static T Keep<T>(T current, string path) where T : Object
        {
            return current != null ? current : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        static UISkin LoadOrCreateSkin()
        {
            string path = DataDir + "/UISkin.asset";
            var skin = AssetDatabase.LoadAssetAtPath<UISkin>(path);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<UISkin>();
                AssetDatabase.CreateAsset(skin, path);
            }
            string ui = ArtDir + "/UI/";
            string ic = ArtDir + "/Icons/";
            skin.font = Keep(skin.font, Root + "/Fonts/Prompt-Regular.ttf");
            skin.fontBold = Keep(skin.fontBold, Root + "/Fonts/Prompt-SemiBold.ttf");
            skin.rounded = Keep(skin.rounded, ui + "ui_rounded.png");
            skin.circle = Keep(skin.circle, ui + "ui_circle.png");
            skin.shadow = Keep(skin.shadow, ui + "ui_shadow.png");
            skin.glow = Keep(skin.glow, ui + "ui_glow.png");
            skin.sparkle = Keep(skin.sparkle, ui + "ui_sparkle.png");
            skin.drop = Keep(skin.drop, ui + "fx_drop.png");
            skin.leaf = Keep(skin.leaf, ui + "fx_leaf.png");
            skin.iconMoisture = Keep(skin.iconMoisture, ic + "icon_droplet.png");
            skin.iconTemperature = Keep(skin.iconTemperature, ic + "icon_thermometer.png");
            skin.iconLight = Keep(skin.iconLight, ic + "icon_sun.png");
            skin.iconBell = Keep(skin.iconBell, ic + "icon_bell.png");
            skin.iconScissors = Keep(skin.iconScissors, ic + "icon_scissors.png");
            skin.iconClose = Keep(skin.iconClose, ic + "icon_x.png");
            skin.iconAlert = Keep(skin.iconAlert, ic + "icon_triangle_alert.png");
            skin.iconCheck = Keep(skin.iconCheck, ic + "icon_circle_check.png");
            skin.iconSprout = Keep(skin.iconSprout, ic + "icon_sprout.png");
            skin.iconInfo = Keep(skin.iconInfo, ic + "icon_info.png");
            skin.iconCare = Keep(skin.iconCare, ic + "icon_hand_heart.png");
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            if (skin.font == null || skin.rounded == null)
                Debug.LogWarning("[SmartPlantPot] Some skin assets are missing. Run Smart Plant Pot > Fix Sprite Import Settings, then rebuild.");
            return skin;
        }

        static PlantSpecies[] LoadOrCreateSpecies()
        {
            // Ranges follow the team's 2D design (35% moisture on the snake plant = "ควรรดน้ำ").
            // Tune them per plant in Assets/SmartPlantPot/Data.
            return new[]
            {
                Species("Species_SnakePlant", "ต้นลิ้นมังกร", "Snake Plant", "plant_snake",
                    new MetricRange(40f, 75f), new MetricRange(18f, 32f), new MetricRange(200f, 10000f),
                    new SensorReading(35f, 29f, 520f)),
                Species("Species_HolyBasil", "ต้นกะเพรา", "Holy Basil", "plant_basil",
                    new MetricRange(50f, 80f), new MetricRange(20f, 35f), new MetricRange(300f, 40000f),
                    new SensorReading(72f, 26f, 450f)),
                Species("Species_Tomato", "ต้นมะเขือเทศ", "Tomato", "plant_tomato",
                    new MetricRange(50f, 80f), new MetricRange(18f, 30f), new MetricRange(500f, 50000f),
                    new SensorReading(58f, 27f, 680f)),
            };
        }

        static PlantSpecies Species(string file, string thai, string english, string sprite,
            MetricRange moisture, MetricRange temperature, MetricRange light, SensorReading demo)
        {
            string path = DataDir + "/" + file + ".asset";
            var s = AssetDatabase.LoadAssetAtPath<PlantSpecies>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<PlantSpecies>();
                s.nameThai = thai;
                s.nameEnglish = english;
                s.moisture = moisture;
                s.temperature = temperature;
                s.light = light;
                s.demoReading = demo;
                AssetDatabase.CreateAsset(s, path);
            }
            if (s.sprite == null)
            {
                s.sprite = LoadSprite(ArtDir + "/Plants/" + sprite + ".png");
                EditorUtility.SetDirty(s);
            }
            return s;
        }

        static void AddToBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(x => x.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }

    /// <summary>Offers to build the scene the first time the project opens with these scripts.</summary>
    [InitializeOnLoad]
    static class SmartPlantPotFirstRun
    {
        const string Key = "SmartPlantPot.FirstRunAsked";

        static SmartPlantPotFirstRun()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, true);
                if (File.Exists("Assets/SmartPlantPot/Scenes/PlantDashboard.unity")) return;
                if (EditorUtility.DisplayDialog("Smart Plant Pot",
                        "Build the PlantDashboard scene now?\n(You can also do it later from the menu: Smart Plant Pot > Build Dashboard Scene)",
                        "Build", "Later"))
                {
                    SmartPlantPotSceneBuilder.Build();
                }
            };
        }
    }
}
