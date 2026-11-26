using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SmartPlantPot.EditorTools
{
    /// <summary>
    /// Menu: Smart Plant Pot > Build 3D Shop Scene.
    /// Imports the 3D team's models, makes pot-centred plant prefabs (Root > Growth > Wobble > model,
    /// growth pivot at the soil), and builds Scenes/PlantShop3D.unity: the shop, 3 empty pots on the
    /// table, camera, sun + lamps, sky, post-processing, rain, game systems and all UI.
    /// Safe to re-run: species/skin/catalog assets you edited are kept.
    /// </summary>
    public static partial class SmartPlantPotSceneBuilder
    {
        // the 3D team's models (with their import settings / materials in Art/3D/Materials)
        const string ModelsDir = Root + "/Art/3D/Props";
        const string PrefabDir = Root + "/Prefabs";
        const string MaterialsDir = Root + "/Materials";
        const string Scene3DPath = SceneDir + "/PlantShop3D.unity";
        const int PotCount = 3;

        [MenuItem("Smart Plant Pot/Build 3D Shop Scene", false, 2)]
        public static void Build3D()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Materials");
            EnsureImportSettings(false);

            var skin = LoadOrCreateSkin();
            FillGameIcons(skin);
            UISkin.Current = skin;
            L.Thai = true;
            var catalog = BuildCatalog();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- environment ----------
            var shopAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsDir + "/PlantShop.fbx");
            GameObject shop = null;
            if (shopAsset != null)
            {
                shop = (GameObject)PrefabUtility.InstantiatePrefab(shopAsset);
                shop.name = "PlantShop";
                foreach (var c in shop.GetComponentsInChildren<Camera>(true)) c.gameObject.SetActive(false);
                foreach (var l in shop.GetComponentsInChildren<Light>(true)) l.gameObject.SetActive(false);
            }
            else Debug.LogWarning("[SmartPlantPot] " + ModelsDir + "/PlantShop.fbx not found - building pots on a plain floor.");

            Bounds room = shop != null ? BoundsOf(shop.transform) : new Bounds(Vector3.zero, new Vector3(6f, 3f, 6f));
            Bounds table = new Bounds();
            if (shop == null || !TryBoundsOfNamed(shop.transform, "_Test.008", out table))
            {
                table = new Bounds(new Vector3(room.center.x, room.min.y + 0.9f, room.center.z), new Vector3(1f, 0.02f, 2.3f));
                if (shop != null) Debug.LogWarning("[SmartPlantPot] Table '_Test.008' not found - placing pots in the middle of the room.");
            }
            Bounds cash = new Bounds();
            bool hasCash = shop != null && TryBoundsOfNamed(shop.transform, "Cash", out cash);
            Bounds window = new Bounds();
            bool hasWindow = shop != null && TryBoundsOfNamed(shop.transform, "Window", out window);
            if (!hasWindow) window = new Bounds(room.center + Vector3.forward * room.extents.z, Vector3.one);

            // ---------- pot layout on the table ----------
            bool longIsX = table.size.x >= table.size.z;
            Vector3 longAxis = longIsX ? Vector3.right : Vector3.forward;
            Vector3 shortAxis = longIsX ? Vector3.forward : Vector3.right;
            float lo = Dot(table.min, longAxis) + 0.14f;
            float hi = Dot(table.max, longAxis) - 0.14f;
            if (hasCash && cash.Intersects(new Bounds(table.center, table.size + Vector3.up)))
            {
                float cLo = Dot(cash.min, longAxis) - 0.1f;
                float cHi = Dot(cash.max, longAxis) + 0.1f;
                if (cLo - lo >= hi - cHi) hi = Mathf.Min(hi, cLo);
                else lo = Mathf.Max(lo, cHi);
            }
            float segment = Mathf.Max(0.3f, hi - lo);
            float spacing = Mathf.Min(0.36f, segment / PotCount);
            float mid = (lo + hi) * 0.5f;
            float towardRoom = Mathf.Sign(Dot(room.center - table.center, shortAxis));
            if (towardRoom == 0f) towardRoom = 1f;
            float shortPos = Dot(table.center, shortAxis) + towardRoom * table.size.y * 0f + towardRoom * Mathf.Min(0.12f, Dot(table.size, shortAxis) * 0.18f);
            float topY = table.max.y;

            Vector3 viewHorizontal = shortAxis * towardRoom;
            Vector3 viewDirection = (viewHorizontal + Vector3.up * 0.62f).normalized;
            Quaternion potRotation = Quaternion.LookRotation(viewHorizontal, Vector3.up);

            // ---------- camera ----------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 34f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PhysicsRaycaster>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var rig = camGo.AddComponent<CameraRig>();
            rig.cam = cam;
            rig.viewDirection = viewDirection;
            Vector3 potsCenter = longAxis * mid + shortAxis * shortPos + Vector3.up * topY;
            camGo.transform.position = potsCenter + Vector3.up * 0.18f + viewDirection * 1.7f;
            camGo.transform.LookAt(potsCenter + Vector3.up * 0.15f);

            // ---------- lights ----------
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.intensity = 1.4f;
            Vector3 toWindow = window.center - potsCenter;
            toWindow.y = 0f;
            float noonYaw = toWindow.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-toWindow.normalized).eulerAngles.y : 180f;
            sunGo.transform.rotation = Quaternion.Euler(50f, noonYaw, 0f);

            var lamps = new List<Light>();
            for (int i = 0; i < 2; i++)
            {
                var lg = new GameObject("ShopLamp_" + (i + 1));
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 2.6f;
                l.color = new Color(1f, 0.84f, 0.62f);
                l.intensity = 1.4f;
                l.shadows = LightShadows.None;
                lg.transform.position = potsCenter + Vector3.up * 1.05f + longAxis * (i == 0 ? -0.45f : 0.45f) + viewHorizontal * 0.25f;
                lamps.Add(l);
            }
            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 3f;
            fill.intensity = 0.6f;
            fill.color = new Color(0.9f, 0.95f, 1f);
            fill.shadows = LightShadows.None;
            fillGo.transform.position = potsCenter + viewDirection * 1.2f + Vector3.up * 0.4f;

            SetupSky();

            var volGo = new GameObject("Global Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            // ---------- pots ----------
            var potsRoot = new GameObject("Pots").transform;
            var units = new List<PlantUnit>();
            var growths = new List<PlantGrowth>();
            Vector3 screenRight = Vector3.Cross(Vector3.up, -viewHorizontal);
            float order = Dot(longAxis, screenRight) >= 0f ? 1f : -1f;
            for (int i = 0; i < PotCount; i++)
            {
                Vector3 p = longAxis * (mid + order * (i - (PotCount - 1) * 0.5f) * spacing) + shortAxis * shortPos + Vector3.up * topY;
                var g = BuildPotSlot(potsRoot, "pot-0" + (i + 1), p, potRotation, catalog, skin);
                units.Add(g.plant);
                growths.Add(g);
            }

            // ---------- rain outside the window ----------
            var rain = BuildRain(window, potsCenter);

            // ---------- app / game systems ----------
            var appGo = new GameObject("SmartPlantPotApp");
            var app = appGo.AddComponent<SmartPlantPotApp>();
            app.skin = skin;
            app.deselectOnEmptyClick = true;
            var sim = appGo.AddComponent<SimulatedPlantDataSource>();
            var http = appGo.AddComponent<HttpPlantDataSource>();
            http.enabled = false;
            appGo.AddComponent<NotificationCenter>();
            var weather = appGo.AddComponent<WeatherService>();
            var dayNight = appGo.AddComponent<DayNightCycle>();
            dayNight.sun = sun;
            dayNight.lamps = lamps.ToArray();
            dayNight.noonYaw = noonYaw;
            var events = appGo.AddComponent<DailyEvents>();
            var fx = appGo.AddComponent<WeatherEffects>();
            fx.rain = rain != null ? new[] { rain } : new ParticleSystem[0];
            var gm = appGo.AddComponent<GameManager>();
            gm.catalog = catalog;
            gm.simulator = sim;
            gm.http = http;
            gm.weather = weather;
            gm.events = events;
            gm.skins = BuildSkinCatalog();
            foreach (var u in units)
                sim.pots.Add(new SimulatedPlantDataSource.PotSim { plant = u, moisture = 55f, temperature = 28f, light = 900f });
            http.singlePotTarget = units[0];

            rig.roomBounds = room;

            // ---------- UI ----------
            BuildUI3D(skin, cam, app, catalog);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Scene3DPath);
            AddSceneToBuildSettings(Scene3DPath, true);
            Selection.activeGameObject = camGo;
            Debug.Log("[SmartPlantPot] Built " + Scene3DPath + " - press Play. Shop (B), Settings & Mock (O), tap a pot to plant / care / sell.");
        }

        [MenuItem("Smart Plant Pot/Open 3D Shop Scene", false, 3)]
        public static void Open3D()
        {
            if (!File.Exists(Scene3DPath))
            {
                Build3D();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Scene3DPath);
        }

        // =====================================================================================
        // Models, prefabs, species
        // =====================================================================================

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        static float Dot(Vector3 a, Vector3 b)
        {
            return Vector3.Dot(a, b);
        }

        static bool IsMaterial(Renderer r, string prefix)
        {
            foreach (var m in r.sharedMaterials)
                if (m != null && m.name.StartsWith(prefix)) return true;
            return false;
        }

        /// <summary>
        /// Every model from the 3D team has the same pot (Material.010) and soil disc (Material.011).
        /// The pot prefab keeps them; plant prefabs hide them and put the plant under a pivot at the soil.
        /// </summary>
        static GameObject BuildModelPrefab(string modelFile, string prefabName, bool isPot)
        {
            string path = ModelsDir + "/" + modelFile;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("[SmartPlantPot] Missing model " + path);
                return null;
            }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;
            // the FBX import settings belong to the 3D team: strip what a static plant doesn't need
            foreach (var c in inst.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(c.gameObject);
            foreach (var l in inst.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            foreach (var a in inst.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);

            Renderer pot = null;
            Renderer soil = null;
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                if (pot == null && IsMaterial(r, "Material.010")) pot = r;
                else if (soil == null && IsMaterial(r, "Material.011")) soil = r;
            }
            Bounds pb = pot != null ? pot.bounds : BoundsOf(inst.transform);
            Vector3 potBase = new Vector3(pb.center.x, pb.min.y, pb.center.z);
            float soilY = soil != null ? soil.bounds.max.y - pb.min.y : pb.size.y * 0.87f;
            if (pot == null) Debug.LogWarning("[SmartPlantPot] " + modelFile + ": pot (Material.010) not found, using model bounds.");

            var root = new GameObject(prefabName);
            if (isPot)
            {
                inst.transform.SetParent(root.transform, true);
                inst.transform.position -= potBase;
            }
            else
            {
                var growth = new GameObject("Growth").transform;
                growth.SetParent(root.transform, false);
                growth.localPosition = new Vector3(0f, soilY, 0f);
                var wobble = new GameObject("Wobble").transform;
                wobble.SetParent(growth, false);
                inst.transform.SetParent(wobble, true);
                inst.transform.position -= potBase;
                if (pot != null) pot.gameObject.SetActive(false);
                if (soil != null) soil.gameObject.SetActive(false);
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            string prefabPath = PrefabDir + "/" + prefabName + ".prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        static PlantCatalog BuildCatalog()
        {
            var potPrefab = BuildModelPrefab("Plantpot.fbx", "Pot", true);
            var tomatoModel = BuildModelPrefab("Tomato.fbx", "Plant_Tomato", false);
            var cylModel = BuildModelPrefab("Cylindricalsnake.fbx", "Plant_CylindricalSnake", false);
            var basilModel = BuildModelPrefab("Holybasil.fbx", "Plant_HolyBasil", false);
            var hoyaModel = BuildModelPrefab("WaxPlant.fbx", "Plant_Hoya", false);
            var snakeModel = BuildModelPrefab("Snakeplant.fbx", "Plant_SnakePlant", false);

            var two = LoadOrCreateSpecies(); // snake, basil, tomato (2D dashboard species)
            var snake = two[0];
            var basil = two[1];
            var tomato = two[2];
            var cyl = Species("Species_CylindricalSnake", "ต้นว่านงาช้าง", "Cylindrical Snake Plant", "none",
                new MetricRange(25f, 60f), new MetricRange(18f, 35f), new MetricRange(150f, 15000f), new SensorReading(42f, 28f, 900f));
            var hoya = Species("Species_Hoya", "ต้นโฮย่า", "Hoya (Wax Plant)", "none",
                new MetricRange(35f, 65f), new MetricRange(18f, 30f), new MetricRange(800f, 20000f), new SensorReading(50f, 26f, 1400f));

            SetGame(tomato, "tomato", tomatoModel, "seed_tomato", 30, 120, 20, 20f);
            SetGame(cyl, "cylindrical_snake", cylModel, "seed_cylindrical", 50, 160, 16, 25f);
            SetGame(basil, "holy_basil", basilModel, "seed_basil", 25, 90, 14, 15f);
            SetGame(hoya, "hoya", hoyaModel, "seed_hoya", 60, 200, 22, 30f);
            SetGame(snake, "snake_plant", snakeModel, "seed_snake", 40, 150, 12, 25f);

            string path = DataDir + "/PlantCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<PlantCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlantCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            catalog.builtIn = new List<PlantSpecies> { tomato, cyl, basil, hoya, snake };
            catalog.potPrefab = potPrefab;
            catalog.customSeedSprite = LoadSprite(ArtDir + "/Seeds/seed_bag_empty.png");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        /// <summary>Mascot skins sold in the shop (the 2D team's Katuii characters). Keeps edited names / prices.</summary>
        static MascotSkinCatalog BuildSkinCatalog()
        {
            string path = DataDir + "/MascotSkins.asset";
            var cat = AssetDatabase.LoadAssetAtPath<MascotSkinCatalog>(path);
            if (cat == null)
            {
                cat = ScriptableObject.CreateInstance<MascotSkinCatalog>();
                cat.skins.Add(new MascotSkin { id = "bunny_hat", nameTh = "กระต่ายคนสวน", nameEn = "Gardener bunny", price = 0 });
                cat.skins.Add(new MascotSkin { id = "bunny_glasses", nameTh = "กระต่ายแว่นเท่", nameEn = "Cool bunny", price = 300 });
                cat.skins.Add(new MascotSkin { id = "boy", nameTh = "น้องหมวกแดง", nameEn = "Red-cap kid", price = 200 });
                cat.defaultSkinId = "bunny_hat";
                AssetDatabase.CreateAsset(cat, path);
            }
            string m = ArtDir + "/Mascots/";
            foreach (var s in cat.skins)
            {
                string file = s.id == "bunny_hat" ? "mascot_bunny_hat" : s.id == "bunny_glasses" ? "mascot_bunny_glasses" : s.id == "boy" ? "mascot_boy" : null;
                if (file == null) continue;
                if (s.front == null) s.front = LoadSprite(m + file + ".png");
                if (s.back == null) s.back = LoadSprite(m + file + "_back.png");
            }
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            return cat;
        }

        static Sprite[] AlertFrames(string kind)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 12; i++)
            {
                var sp = LoadSprite(ArtDir + "/Alerts/" + kind + "/" + kind + "_" + i + ".png");
                if (sp == null) break;
                list.Add(sp);
            }
            if (list.Count == 0) Debug.LogWarning("[SmartPlantPot] No alert frames found for " + kind);
            return list.ToArray();
        }

        static void SetGame(PlantSpecies s, string id, GameObject model, string seed, int seedPrice, int sellPrice, int daily, float minutes)
        {
            if (string.IsNullOrEmpty(s.id)) s.id = id;
            if (model != null) s.modelPrefab = model;
            if (s.seedSprite == null) s.seedSprite = LoadSprite(ArtDir + "/Seeds/" + seed + ".png");
            // only fill economy values the first time (keep edits made in the Inspector)
            if (s.seedPrice == 40 && s.sellPrice == 120 && s.dailyPoints == 15 && Mathf.Approximately(s.growMinutes, 20f))
            {
                s.seedPrice = seedPrice;
                s.sellPrice = sellPrice;
                s.dailyPoints = daily;
                s.growMinutes = minutes;
            }
            EditorUtility.SetDirty(s);
        }

        static void FillGameIcons(UISkin skin)
        {
            string ic = ArtDir + "/Icons/";
            skin.iconCoins = Keep(skin.iconCoins, ic + "icon_coins.png");
            skin.iconStore = Keep(skin.iconStore, ic + "icon_store.png");
            skin.iconSettings = Keep(skin.iconSettings, ic + "icon_settings.png");
            skin.iconFlame = Keep(skin.iconFlame, ic + "icon_flame_filled.png");
            skin.iconSparkles = Keep(skin.iconSparkles, ic + "icon_sparkles.png");
            skin.iconFertilizer = Keep(skin.iconFertilizer, ic + "icon_flask_conical.png");
            skin.iconGauge = Keep(skin.iconGauge, ic + "icon_gauge.png");
            skin.iconPlus = Keep(skin.iconPlus, ic + "icon_plus.png");
            skin.iconTrash = Keep(skin.iconTrash, ic + "icon_trash_2.png");
            skin.iconCalendar = Keep(skin.iconCalendar, ic + "icon_calendar_days.png");
            skin.iconClock = Keep(skin.iconClock, ic + "icon_clock.png");
            skin.iconWifi = Keep(skin.iconWifi, ic + "icon_wifi.png");
            skin.iconFastForward = Keep(skin.iconFastForward, ic + "icon_fast_forward.png");
            skin.iconMoon = Keep(skin.iconMoon, ic + "icon_moon.png");
            skin.iconCloud = Keep(skin.iconCloud, ic + "icon_cloud.png");
            skin.iconCloudSun = Keep(skin.iconCloudSun, ic + "icon_cloud_sun.png");
            skin.iconCloudRain = Keep(skin.iconCloudRain, ic + "icon_cloud_rain.png");
            skin.iconCloudStorm = Keep(skin.iconCloudStorm, ic + "icon_cloud_lightning.png");
            skin.iconCloudFog = Keep(skin.iconCloudFog, ic + "icon_cloud_fog.png");
            skin.iconStar = Keep(skin.iconStar, ic + "icon_star.png");
            skin.iconPackage = Keep(skin.iconPackage, ic + "icon_package.png");
            skin.iconBag = Keep(skin.iconBag, ic + "icon_shopping_bag.png");
            skin.iconRefresh = Keep(skin.iconRefresh, ic + "icon_refresh_cw.png");
            skin.iconSliders = Keep(skin.iconSliders, ic + "icon_sliders_horizontal.png");
            skin.iconWrench = Keep(skin.iconWrench, ic + "icon_wrench.png");
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
        }

        static Bounds BoundsOf(Transform t)
        {
            Bounds b;
            if (!ScreenSpace.WorldBounds(t, out b)) b = new Bounds(t.position, Vector3.one * 0.1f);
            return b;
        }

        static bool TryBoundsOfNamed(Transform root, string name, out Bounds b)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return ScreenSpace.WorldBounds(t, out b);
            }
            b = new Bounds();
            return false;
        }

        // =====================================================================================
        // Scene pieces
        // =====================================================================================

        static PlantGrowth BuildPotSlot(Transform parent, string potId, Vector3 position, Quaternion rotation, PlantCatalog catalog, UISkin skin)
        {
            var slot = new GameObject("Pot_" + potId);
            slot.transform.SetParent(parent, false);
            slot.transform.position = position;
            slot.transform.rotation = rotation;

            GameObject potVisual = null;
            if (catalog.potPrefab != null)
            {
                potVisual = (GameObject)PrefabUtility.InstantiatePrefab(catalog.potPrefab);
                potVisual.transform.SetParent(slot.transform, false);
                potVisual.transform.localPosition = Vector3.zero;
                potVisual.transform.localRotation = Quaternion.identity;
            }

            var glowGo = new GameObject("StatusGlow");
            glowGo.transform.SetParent(slot.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.003f, 0f);
            glowGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var glow = glowGo.AddComponent<SpriteRenderer>();
            glow.sprite = skin.glow;
            glow.color = new Color(1f, 1f, 1f, 0.2f);
            float spriteWorld = skin.glow != null ? skin.glow.bounds.size.x : 2.56f;
            float s = 0.46f / Mathf.Max(0.01f, spriteWorld);
            glowGo.transform.localScale = new Vector3(s, s, s);

            var col = slot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.2f, 0f);
            col.size = new Vector3(0.28f, 0.42f, 0.28f);

            var unit = slot.AddComponent<PlantUnit>();
            unit.potId = potId;
            unit.species = null;
            var interaction = slot.AddComponent<PotInteraction>();
            interaction.plant = unit;
            interaction.art = potVisual != null ? potVisual.transform : slot.transform;
            interaction.swayDegrees = 1.6f;
            var servo = slot.AddComponent<PotServo>();
            servo.plant = unit;
            var growth = slot.AddComponent<PlantGrowth>();
            growth.plant = unit;
            growth.interaction = interaction;
            growth.modelAnchor = slot.transform;
            growth.statusGlow = glow;
            var selectable = slot.AddComponent<PlantSelectable>();
            selectable.plant = unit;
            selectable.interaction = interaction;
            return growth;
        }

        static void SetupSky()
        {
            Material sky = null;
            try
            {
                sky = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            }
            catch
            {
                sky = null;
            }
            if (sky == null)
            {
                string p = MaterialsDir + "/ShopSky.mat";
                sky = AssetDatabase.LoadAssetAtPath<Material>(p);
                var sh = Shader.Find("Skybox/Procedural");
                if (sky == null && sh != null)
                {
                    sky = new Material(sh);
                    AssetDatabase.CreateAsset(sky, p);
                }
            }
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
        }

        static ParticleSystem BuildRain(Bounds window, Vector3 potsCenter)
        {
            var go = new GameObject("Rain");
            Vector3 outward = window.center - potsCenter;
            outward.y = 0f;
            outward = outward.sqrMagnitude > 0.01f ? outward.normalized : Vector3.forward;
            go.transform.position = window.center + outward * 0.8f + Vector3.up * 1.6f;
            go.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.9f;
            main.startSpeed = 7f;
            main.startSize = 0.015f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.6f;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(Mathf.Max(2f, window.size.x + window.size.z) + 1.5f, 0.05f, 1.6f);
            shape.rotation = new Vector3(90f, 0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.03f;
            r.lengthScale = 2f;
            r.sharedMaterial = RainMaterial();
            return ps;
        }

        static Material RainMaterial()
        {
            string p = MaterialsDir + "/Rain.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null) return m;
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) return null;
            m = new Material(sh);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetColor("_BaseColor", new Color(0.75f, 0.85f, 1f, 0.45f));
            AssetDatabase.CreateAsset(m, p);
            return m;
        }

        static void AddSceneToBuildSettings(string path, bool first)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(x => x.path == path);
            if (first) list.Insert(0, new EditorBuildSettingsScene(path, true));
            else list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // =====================================================================================
        // UI
        // =====================================================================================

        static void BuildUI3D(UISkin skin, Camera cam, SmartPlantPotApp app, PlantCatalog catalog)
        {
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

            // rain on the shop window, behind every other piece of UI
            var weatherFx = UIFactory.Rect(canvasRt, "WeatherOverlay");
            UIFactory.Stretch(weatherFx);
            weatherFx.gameObject.AddComponent<WeatherOverlay>();

            var safe = UIFactory.Rect(canvasRt, "SafeArea");
            UIFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // floating pot labels (behind everything else)
            var labels = UIFactory.Rect(safe, "PotLabels");
            UIFactory.Stretch(labels);
            labels.gameObject.AddComponent<PotLabelLayer>().worldCamera = cam;

            // header with a soft backdrop so it reads over the 3D scene
            Text subtitle;
            var bell = BuildHeader(safe, skin, out subtitle);
            var header = (RectTransform)safe.Find("Header");
            var headerBg = UIFactory.Image(header, "Backdrop", skin.glow, UISkin.WithAlpha(skin.background, 0.85f));
            headerBg.rectTransform.anchorMin = new Vector2(-0.05f, -0.6f);
            headerBg.rectTransform.anchorMax = new Vector2(0.55f, 1.4f);
            headerBg.rectTransform.offsetMin = Vector2.zero;
            headerBg.rectTransform.offsetMax = Vector2.zero;
            headerBg.transform.SetAsFirstSibling();
            var chips = AddHeaderChips(header, skin);

            // the 2D team's alert animations, above the pot that has the problem
            var alerts = UIFactory.Rect(safe, "AlertAnimations");
            UIFactory.Stretch(alerts);
            var alertLayer = alerts.gameObject.AddComponent<AlertAnimationLayer>();
            alertLayer.worldCamera = cam;
            alertLayer.tooHot = AlertFrames("too_hot");
            alertLayer.tooCold = AlertFrames("too_cold");
            alertLayer.tooWet = AlertFrames("too_wet");
            alertLayer.tooDry = AlertFrames("too_dry");

            BuildFreeLookHud(safe, skin, cam.GetComponent<CameraRig>());

            var mascot = BuildMascot(safe, skin);
            var card = BuildInfoCard(safe, skin, catalog.builtIn.Count > 0 ? catalog.builtIn[0] : null);
            card.landscapeWidth = 620f;
            AddGameSection(card, skin);
            var emptyCard = BuildEmptyPotCard(safe, skin);
            var panel = BuildHistoryPanel(safe, skin);

            var toasts = UIFactory.Rect(safe, "Toasts");
            UIFactory.Place(toasts, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -150f), new Vector2(540f, 10f));
            UIFactory.VColumn(toasts.gameObject, 12f, UIFactory.Pad(0), TextAnchor.UpperRight);
            UIFactory.Fit(toasts.gameObject, false, true);
            toasts.gameObject.AddComponent<ToastPresenter>().stack = toasts;

            var shop = BuildShopPanel(safe, skin);
            var settings = BuildSettingsPanel(safe, skin);
            BuildConfirmDialog(safe, skin);

            var fx = UIFactory.Rect(canvasRt, "Effects");
            UIFactory.Stretch(fx);
            fx.gameObject.AddComponent<EffectsLayer>();

            bell.panel = panel;
            app.historyPanel = panel;
            app.shopPanel = shop;
            app.settingsPanel = settings;
            mascot.infoCard = card;
            emptyCard.shop = shop;
            chips.shop = shop;
            chips.settings = settings;

            layout.headerSubtitle = subtitle.gameObject;
            layout.mascotArea = (RectTransform)mascot.transform;
            layout.toastStack = toasts;
            layout.historyPanel = (RectTransform)panel.transform;
            layout.infoCard = card;
        }

        static void BuildFreeLookHud(RectTransform safe, UISkin skin, CameraRig rig)
        {
            var root = UIFactory.Rect(safe, "FreeLookHud");
            UIFactory.Place(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(820f, 116f));
            root.gameObject.AddComponent<CanvasGroup>();

            var btn = UIFactory.Button(root, "Reset", L.T("กลับไปดูต้นไม้", "Back to the plants"), skin.iconSprout, skin.accent, Color.white, 22, 30f, 60f);
            LocalizedText.Attach(btn.transform.Find("Label").GetComponent<Text>(), "กลับไปดูต้นไม้", "Back to the plants");
            UIFactory.Place((RectTransform)btn.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(330f, 60f));
            UIFactory.Shadow((RectTransform)btn.transform, 18f, 0.2f, -5f);

            var hintBg = UIFactory.Rounded(root, "Hint", UISkin.WithAlpha(skin.textPrimary, 0.55f), 16f);
            UIFactory.Place(hintBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(820f, 38f));
            var hint = UIFactory.Text(hintBg.transform, "Text", "", 17, Color.white, TextAnchor.MiddleCenter);
            UIFactory.Stretch(hint.rectTransform);
            hint.raycastTarget = false;

            var hud = root.gameObject.AddComponent<FreeLookHud>();
            hud.rig = rig;
            hud.resetButton = btn;
            hud.hintText = hint;
        }

        static HeaderGameChips AddHeaderChips(RectTransform header, UISkin skin)
        {
            var right = header.Find("Right");
            var chips = header.gameObject.AddComponent<HeaderGameChips>();

            // weather
            var wx = UIFactory.Rounded(right, "Weather", skin.surface, 24f);
            UIFactory.HRow(wx.gameObject, 8f, UIFactory.Pad(16, 8), TextAnchor.MiddleCenter);
            UIFactory.Layout(wx, -1f, 48f);
            var wIcon = UIFactory.Icon(wx.transform, "Icon", skin.iconLight, skin.gold, 28f);
            UIFactory.Layout(wIcon, 28f, 28f);
            var wText = UIFactory.Text(wx.transform, "Text", "--", 20, skin.textPrimary, TextAnchor.MiddleLeft);
            wText.horizontalOverflow = HorizontalWrapMode.Overflow;
            wx.transform.SetSiblingIndex(0);

            // points
            var pts = UIFactory.Rounded(right, "Points", skin.surface, 24f);
            UIFactory.HRow(pts.gameObject, 8f, UIFactory.Pad(16, 8), TextAnchor.MiddleCenter);
            UIFactory.Layout(pts, -1f, 48f);
            var coin = UIFactory.Icon(pts.transform, "Coin", skin.iconCoins, skin.gold, 28f);
            UIFactory.Layout(coin, 28f, 28f);
            var pText = UIFactory.Text(pts.transform, "Value", "0", 24, skin.textPrimary, TextAnchor.MiddleLeft, true);
            pText.horizontalOverflow = HorizontalWrapMode.Overflow;
            pts.transform.SetSiblingIndex(1);

            // shop + settings buttons (before the bell)
            var shopBtn = UIFactory.IconButton(right, "ShopButton", skin.iconStore, skin.accent, Color.white, 72f);
            UIFactory.Layout(shopBtn, 72f, 72f);
            var setBtn = UIFactory.IconButton(right, "SettingsButton", skin.iconSettings, skin.surface, skin.accentDark, 72f);
            UIFactory.Layout(setBtn, 72f, 72f);
            var bell = right.Find("Bell");
            if (bell != null) bell.SetAsLastSibling();

            // today's event chip under the header
            var ev = UIFactory.Rounded(header, "EventChip", UISkin.WithAlpha(skin.eventColor, 0.92f), 22f, true);
            UIFactory.Place(ev.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(10f, 44f));
            UIFactory.HRow(ev.gameObject, 8f, UIFactory.Pad(18, 6), TextAnchor.MiddleCenter);
            UIFactory.Fit(ev.gameObject, true, false);
            var evIcon = UIFactory.Icon(ev.transform, "Icon", skin.iconCalendar, Color.white, 24f);
            UIFactory.Layout(evIcon, 24f, 24f);
            var evText = UIFactory.Text(ev.transform, "Text", "", 20, Color.white, TextAnchor.MiddleLeft, true);
            evText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var evBtn = ev.gameObject.AddComponent<Button>();
            evBtn.targetGraphic = ev;

            chips.pointsText = pText;
            chips.pointsChip = pts.rectTransform;
            chips.weatherIcon = wIcon;
            chips.weatherText = wText;
            chips.eventChip = evBtn;
            chips.eventText = evText;
            chips.shopButton = shopBtn;
            chips.settingsButton = setBtn;
            var hide = new List<GameObject> { wText.gameObject };
            var conn = right.Find("Connection");
            if (conn != null) hide.Add(conn.gameObject);
            var clock = right.Find("Clock");
            if (clock != null) hide.Add(clock.gameObject);
            chips.hideInPortrait = hide.ToArray();
            return chips;
        }

        static RectTransform Bar(Transform parent, string name, Color fill, out RectTransform fillRect, float height)
        {
            var track = UIFactory.Rounded(parent, name, UISkin.WithAlpha(UISkin.Current.textPrimary, 0.08f), height * 0.5f);
            UIFactory.Layout(track, -1f, height, 1f, -1f);
            var f = UIFactory.Rounded(track.transform, "Fill", fill, height * 0.5f);
            f.rectTransform.anchorMin = Vector2.zero;
            f.rectTransform.anchorMax = new Vector2(0.3f, 1f);
            f.rectTransform.offsetMin = Vector2.zero;
            f.rectTransform.offsetMax = Vector2.zero;
            fillRect = f.rectTransform;
            return track.rectTransform;
        }

        static void AddGameSection(PlantInfoCard card, UISkin skin)
        {
            var cardRt = (RectTransform)card.transform;
            // the 3D card carries more, so keep it compact: tighter spacing, the ideal-range band on
            // each bar replaces the range text line
            var column = card.GetComponent<VerticalLayoutGroup>();
            if (column != null) column.spacing = 10f;
            if (card.rows != null)
                foreach (var row in card.rows)
                    if (row != null && row.rangeText != null) row.rangeText.gameObject.SetActive(false);

            var block = UIFactory.Rounded(cardRt, "Game", new Color(1f, 1f, 1f, 0.75f), 20f);
            UIFactory.VColumn(block.gameObject, 8f, new RectOffset(18, 18, 12, 14));
            var subtitle = cardRt.Find("Subtitle");
            block.transform.SetSiblingIndex(subtitle != null ? subtitle.GetSiblingIndex() + 1 : 2);

            var r1 = UIFactory.Rect(block.transform, "Stage");
            UIFactory.HRow(r1.gameObject, 10f, new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.Layout(r1, -1f, 30f);
            var sprout = UIFactory.Icon(r1, "Icon", skin.iconSprout, skin.leafColor, 26f);
            UIFactory.Layout(sprout, 26f, 26f);
            var stage = UIFactory.Text(r1, "Text", "", 21, skin.textPrimary, TextAnchor.MiddleLeft, true);
            UIFactory.Layout(stage, -1f, -1f, 1f, -1f);
            var gPct = UIFactory.Text(r1, "Pct", "0%", 20, skin.textMuted, TextAnchor.MiddleRight, true);
            gPct.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform gFill;
            Bar(block.transform, "GrowthBar", skin.leafColor, out gFill, 12f);

            var r2 = UIFactory.Rect(block.transform, "Quality");
            UIFactory.HRow(r2.gameObject, 10f, new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.Layout(r2, -1f, 28f);
            var star = UIFactory.Icon(r2, "Icon", skin.iconStar, skin.gold, 24f);
            UIFactory.Layout(star, 24f, 24f);
            var qLbl = UIFactory.Text(r2, "Label", L.Quality, 19, skin.textMuted, TextAnchor.MiddleLeft);
            UIFactory.Layout(qLbl, 90f, -1f);
            RectTransform qFill;
            Bar(r2, "QualityBar", skin.statusNormal, out qFill, 10f);
            var qPct = UIFactory.Text(r2, "Pct", "0%", 20, skin.textPrimary, TextAnchor.MiddleRight, true);
            UIFactory.Layout(qPct, 64f, -1f);

            var streak = UIFactory.Rounded(block.transform, "Streak", UISkin.WithAlpha(skin.flame, 0.12f), 14f);
            UIFactory.HRow(streak.gameObject, 8f, new RectOffset(12, 12, 4, 4), TextAnchor.MiddleLeft);
            UIFactory.Layout(streak, -1f, 36f);
            var flame = UIFactory.Icon(streak.transform, "Icon", skin.iconFlame, skin.flame, 24f);
            UIFactory.Layout(flame, 24f, 24f);
            var sText = UIFactory.Text(streak.transform, "Text", "", 19, skin.textPrimary, TextAnchor.MiddleLeft, true);
            UIFactory.Layout(sText, -1f, -1f, 1f, -1f);

            var mut = UIFactory.Rounded(block.transform, "Mutation", UISkin.WithAlpha(skin.gold, 0.18f), 14f);
            UIFactory.HRow(mut.gameObject, 8f, new RectOffset(12, 12, 4, 4), TextAnchor.MiddleLeft);
            UIFactory.Layout(mut, -1f, 36f);
            var spark = UIFactory.Icon(mut.transform, "Icon", skin.iconSparkles, skin.gold, 24f);
            UIFactory.Layout(spark, 24f, 24f);
            var mText = UIFactory.Text(mut.transform, "Text", "", 19, skin.textPrimary, TextAnchor.MiddleLeft, true);
            UIFactory.Layout(mText, -1f, -1f, 1f, -1f);
            mut.gameObject.SetActive(false);

            var servo = UIFactory.Text(block.transform, "Servo", "", 17, skin.textMuted, TextAnchor.MiddleLeft);
            UIFactory.Layout(servo, -1f, 24f);

            // fertilize joins Water / Prune
            var actions = cardRt.Find("Actions");
            var fert = UIFactory.Button(actions, "Fertilize", L.Fertilize, skin.iconFertilizer, skin.gold, Color.white, 24, 32f, 64f);
            LocalizedText.Attach(fert.transform.Find("Label").GetComponent<Text>(), "ให้ปุ๋ย", "Fertilize");
            card.fertilizeButton = fert;

            // the two kinds of selling
            var sell = UIFactory.Rect(cardRt, "Sell");
            var sellRow = UIFactory.HRow(sell.gameObject, 12f, UIFactory.Pad(0), TextAnchor.MiddleCenter);
            sellRow.childForceExpandWidth = true;
            UIFactory.Layout(sell, -1f, 56f);
            var harvest = UIFactory.Button(sell, "Harvest", L.SellHarvest(0), skin.iconCoins, UISkin.WithAlpha(skin.gold, 0.22f), skin.textPrimary, 19, 22f, 56f);
            var sellAll = UIFactory.Button(sell, "SellPlant", L.SellPlant(0), skin.iconStore, skin.accentDark, Color.white, 19, 22f, 56f);

            var section = block.gameObject.AddComponent<PlantGameSection>();
            section.stageText = stage;
            section.growthFill = gFill;
            section.growthText = gPct;
            section.qualityFill = qFill;
            section.qualityText = qPct;
            section.streakChip = streak.gameObject;
            section.streakText = sText;
            section.mutationChip = mut.gameObject;
            section.mutationText = mText;
            section.servoText = servo;
            section.harvestButton = harvest;
            section.sellButton = sellAll;
        }

        static EmptyPotCard BuildEmptyPotCard(RectTransform safe, UISkin skin)
        {
            var bg = UIFactory.Rounded(safe, "EmptyPotCard", skin.infoCard, 30f, true);
            var rt = bg.rectTransform;
            UIFactory.Place(rt, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(700f, 470f), new Vector2(560f, 10f));
            bg.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            UIFactory.Shadow(rt, 30f, 0.18f, -10f);
            UIFactory.VColumn(bg.gameObject, 14f, new RectOffset(30, 30, 24, 28));
            UIFactory.Fit(bg.gameObject, false, true);

            var head = UIFactory.Rect(rt, "Header");
            UIFactory.HRow(head.gameObject, 12f, UIFactory.Pad(0), TextAnchor.MiddleLeft);
            UIFactory.Layout(head, -1f, 52f);
            var icon = UIFactory.Icon(head, "Icon", skin.iconSprout, skin.leafColor, 34f);
            UIFactory.Layout(icon, 34f, 34f);
            var title = UIFactory.Text(head, "Title", L.EmptyPot, 32, skin.textPrimary, TextAnchor.MiddleLeft, true);
            UIFactory.Layout(title, -1f, -1f, 1f, -1f);
            var close = UIFactory.IconButton(head, "Close", skin.iconClose, UISkin.WithAlpha(skin.textPrimary, 0.08f), skin.textPrimary, 48f);
            UIFactory.Layout(close, 48f, 48f);

            var subtitle = UIFactory.Text(rt, "Subtitle", "", 19, skin.textMuted, TextAnchor.UpperLeft);
            var choose = UIFactory.Text(rt, "Choose", L.ChooseSeed, 22, skin.textPrimary, TextAnchor.MiddleLeft, true);
            LocalizedText.Attach(choose, "เลือกเมล็ดที่จะปลูก", "Choose a seed to plant");

            var grid = UIFactory.Rect(rt, "Seeds");
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(152f, 150f);
            g.spacing = new Vector2(10f, 10f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;

            var empty = UIFactory.Rounded(rt, "NoSeeds", new Color(1f, 1f, 1f, 0.7f), 18f);
            UIFactory.VColumn(empty.gameObject, 6f, new RectOffset(16, 16, 12, 12), TextAnchor.MiddleCenter);
            var bag = UIFactory.Image(empty.transform, "Bag", LoadSprite(ArtDir + "/Seeds/seed_bag_empty.png"), Color.white);
            bag.preserveAspect = true;
            UIFactory.Layout(bag, -1f, 96f);
            var none = UIFactory.Text(empty.transform, "Text", L.NoSeedsYet, 20, skin.textMuted, TextAnchor.MiddleCenter);
            LocalizedText.Attach(none, "ยังไม่มีเมล็ด ไปซื้อที่ร้านก่อนนะ", "No seeds yet - buy some in the shop");

            var shopBtn = UIFactory.Button(rt, "Shop", L.GoToShop, skin.iconStore, skin.accent, Color.white, 22, 28f, 58f);
            LocalizedText.Attach(shopBtn.transform.Find("Label").GetComponent<Text>(), "ไปร้านเมล็ด", "Open seed shop");

            var c = bg.gameObject.AddComponent<EmptyPotCard>();
            c.title = title;
            c.subtitle = subtitle;
            c.seedGrid = grid;
            c.emptyState = empty.gameObject;
            c.shopButton = shopBtn;
            c.closeButton = close;
            c.landscapeWidth = 560f;
            return c;
        }

        /// <summary>Full-screen dim + centred window; returns the window.</summary>
        static RectTransform ModalShell(RectTransform safe, string name, UISkin skin, Vector2 anchorMin, Vector2 anchorMax, out GameObject root)
        {
            var dim = UIFactory.Image(safe, name, null, skin.panelDim, true);
            UIFactory.Stretch(dim.rectTransform, -200f, -200f, -200f, -200f);
            dim.gameObject.AddComponent<CanvasGroup>();
            root = dim.gameObject;

            // the dim reaches past the safe area; the frame brings the window back to the screen rect
            var frame = UIFactory.Rect(dim.transform, "Frame");
            UIFactory.Stretch(frame, 200f, 200f, 200f, 200f);
            var win = UIFactory.Rounded(frame, "Window", skin.background, 32f, true);
            win.rectTransform.anchorMin = anchorMin;
            win.rectTransform.anchorMax = anchorMax;
            win.rectTransform.offsetMin = Vector2.zero;
            win.rectTransform.offsetMax = Vector2.zero;
            UIFactory.Shadow(win.rectTransform, 40f, 0.25f, -14f);
            return win.rectTransform;
        }

        static Button ModalHeader(RectTransform win, string titleThai, string titleEn, Sprite icon, UISkin skin, out RectTransform headRow)
        {
            var head = UIFactory.Rect(win, "Header");
            head.anchorMin = new Vector2(0f, 1f);
            head.anchorMax = new Vector2(1f, 1f);
            head.pivot = new Vector2(0.5f, 1f);
            head.anchoredPosition = Vector2.zero;
            head.sizeDelta = new Vector2(0f, 96f);
            UIFactory.HRow(head.gameObject, 14f, new RectOffset(32, 24, 0, 0), TextAnchor.MiddleLeft);
            var ic = UIFactory.Icon(head, "Icon", icon, skin.accent, 40f);
            UIFactory.Layout(ic, 40f, 40f);
            var title = UIFactory.Text(head, "Title", "", 34, skin.textPrimary, TextAnchor.MiddleLeft, true);
            LocalizedText.Attach(title, titleThai, titleEn);
            UIFactory.Layout(title, -1f, -1f, 1f, -1f);
            var close = UIFactory.IconButton(head, "Close", skin.iconClose, UISkin.WithAlpha(skin.textPrimary, 0.08f), skin.textPrimary, 56f);
            UIFactory.Layout(close, 56f, 56f);
            headRow = head;
            return close;
        }

        static ScrollRect ScrollArea(RectTransform win, float top, float bottom, out RectTransform content, bool grid)
        {
            var area = UIFactory.Rect(win, "Scroll");
            UIFactory.Stretch(area, 28f, 28f, top, bottom);
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            var viewport = UIFactory.Rect(area, "Viewport");
            UIFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0f);
            content = UIFactory.Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            if (grid)
            {
                var g = content.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(236f, 370f);
                g.spacing = new Vector2(18f, 18f);
                g.childAlignment = TextAnchor.UpperCenter;
                g.padding = new RectOffset(4, 4, 8, 16);
            }
            else
            {
                UIFactory.VColumn(content.gameObject, 14f, new RectOffset(4, 4, 8, 24));
            }
            UIFactory.Fit(content.gameObject, false, true);
            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        static SeedShopPanel BuildShopPanel(RectTransform safe, UISkin skin)
        {
            GameObject root;
            var win = ModalShell(safe, "SeedShop", skin, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.9f), out root);
            RectTransform head;
            var close = ModalHeader(win, "ร้านค้า", "Shop", skin.iconStore, skin, out head);

            var pts = UIFactory.Rounded(head, "Points", skin.surface, 24f);
            UIFactory.HRow(pts.gameObject, 8f, UIFactory.Pad(16, 8), TextAnchor.MiddleCenter);
            UIFactory.Layout(pts, -1f, 52f);
            var coin = UIFactory.Icon(pts.transform, "Coin", skin.iconCoins, skin.gold, 30f);
            UIFactory.Layout(coin, 30f, 30f);
            var pText = UIFactory.Text(pts.transform, "Value", "0", 24, skin.textPrimary, TextAnchor.MiddleLeft, true);
            pText.horizontalOverflow = HorizontalWrapMode.Overflow;
            pts.transform.SetSiblingIndex(head.childCount - 2);

            var note = UIFactory.Text(win, "Note", "", 19, skin.textMuted, TextAnchor.MiddleLeft);
            note.rectTransform.anchorMin = new Vector2(0f, 1f);
            note.rectTransform.anchorMax = new Vector2(1f, 1f);
            note.rectTransform.pivot = new Vector2(0.5f, 1f);
            note.rectTransform.anchoredPosition = new Vector2(0f, -150f);
            note.rectTransform.sizeDelta = new Vector2(-64f, 40f);

            var tabs = UIFactory.Rect(win, "Tabs");
            tabs.anchorMin = new Vector2(0f, 1f);
            tabs.anchorMax = new Vector2(1f, 1f);
            tabs.pivot = new Vector2(0.5f, 1f);
            tabs.anchoredPosition = new Vector2(0f, -94f);
            tabs.sizeDelta = new Vector2(-56f, 52f);
            var tabRow = UIFactory.HRow(tabs.gameObject, 10f, UIFactory.Pad(0), TextAnchor.MiddleLeft);
            tabRow.childForceExpandWidth = false;

            RectTransform content;
            ScrollArea(win, 196f, 24f, out content, true);

            var p = root.AddComponent<SeedShopPanel>();
            p.window = win;
            p.closeButton = close;
            p.grid = content;
            p.pointsText = pText;
            p.noteText = note;
            p.tabBar = tabs;
            root.AddComponent<ModalBackdrop>().panel = p;
            root.GetComponent<CanvasGroup>().alpha = 0f;
            return p;
        }

        static SettingsPanel BuildSettingsPanel(RectTransform safe, UISkin skin)
        {
            GameObject root;
            var win = ModalShell(safe, "Settings", skin, new Vector2(0.14f, 0.05f), new Vector2(0.86f, 0.92f), out root);
            RectTransform head;
            var close = ModalHeader(win, "ตั้งค่า & Mock", "Settings & Mock", skin.iconSliders, skin, out head);

            var tabs = UIFactory.Rect(win, "Tabs");
            tabs.anchorMin = new Vector2(0f, 1f);
            tabs.anchorMax = new Vector2(1f, 1f);
            tabs.pivot = new Vector2(0.5f, 1f);
            tabs.anchoredPosition = new Vector2(0f, -96f);
            tabs.sizeDelta = new Vector2(-56f, 52f);
            UIFactory.HRow(tabs.gameObject, 10f, UIFactory.Pad(0), TextAnchor.MiddleLeft);

            RectTransform content;
            var scroll = ScrollArea(win, 162f, 24f, out content, false);

            var p = root.AddComponent<SettingsPanel>();
            p.window = win;
            p.closeButton = close;
            p.tabBar = tabs;
            p.content = content;
            p.scroll = scroll;
            root.AddComponent<ModalBackdrop>().panel = p;
            root.GetComponent<CanvasGroup>().alpha = 0f;
            return p;
        }

        static void BuildConfirmDialog(RectTransform safe, UISkin skin)
        {
            GameObject root;
            var win = ModalShell(safe, "ConfirmDialog", skin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), out root);
            win.sizeDelta = new Vector2(680f, 10f);
            UIFactory.VColumn(win.gameObject, 16f, new RectOffset(36, 36, 30, 30));
            UIFactory.Fit(win.gameObject, false, true);
            var title = UIFactory.Text(win, "Title", "", 30, skin.textPrimary, TextAnchor.UpperLeft, true);
            var msg = UIFactory.Text(win, "Message", "", 21, skin.textMuted, TextAnchor.UpperLeft);
            var row = UIFactory.Rect(win, "Buttons");
            var h = UIFactory.HRow(row.gameObject, 12f, UIFactory.Pad(0), TextAnchor.MiddleRight);
            h.childForceExpandWidth = true;
            UIFactory.Layout(row, -1f, 60f);
            var cancel = UIFactory.Button(row, "Cancel", L.Cancel, null, UISkin.WithAlpha(skin.textPrimary, 0.08f), skin.textPrimary, 22, 26f, 58f);
            LocalizedText.Attach(cancel.transform.Find("Label").GetComponent<Text>(), "ยกเลิก", "Cancel");
            var ok = UIFactory.Button(row, "Confirm", L.Confirm, null, skin.accent, Color.white, 22, 26f, 58f);

            var d = root.AddComponent<ConfirmDialog>();
            d.window = win;
            d.titleText = title;
            d.messageText = msg;
            d.confirmButton = ok;
            d.cancelButton = cancel;
            root.AddComponent<ModalBackdrop>().panel = d;
            root.GetComponent<CanvasGroup>().alpha = 0f;
        }
    }
}
