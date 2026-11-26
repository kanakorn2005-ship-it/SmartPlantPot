using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// The shop. Tab 1 - seeds: every species as a 2D seed packet with its price in points; buying
    /// adds the seed to your inventory, tap an empty pot to plant it (prices follow today's event).
    /// Tab 2 - mascot skins: buy once with points, then wear any skin you own.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Seed Shop Panel")]
    public class SeedShopPanel : ModalPanel
    {
        public RectTransform grid;
        public Text pointsText;
        public Text noteText;
        [Tooltip("Optional: tab buttons (seeds / mascot skins) are built here.")]
        public RectTransform tabBar;

        int tab;

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Changed += OnGameChanged;
                GameManager.Instance.SkinChanged += OnGameChanged;
            }
            if (PlantCatalog.Current != null) PlantCatalog.Current.Changed += OnGameChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Changed -= OnGameChanged;
                GameManager.Instance.SkinChanged -= OnGameChanged;
            }
            if (PlantCatalog.Current != null) PlantCatalog.Current.Changed -= OnGameChanged;
        }

        void OnGameChanged()
        {
            if (IsOpen) Rebuild();
        }

        protected override void OnOpened()
        {
            Rebuild();
        }

        void Rebuild()
        {
            var skin = UISkin.Current;
            var gm = GameManager.Instance;
            var catalog = PlantCatalog.Current;
            if (grid == null || gm == null || catalog == null) return;

            if (pointsText != null) pointsText.text = gm.Points + " " + L.Points;
            BuildTabs(skin, gm);
            if (tab == 1)
            {
                BuildSkins(skin, gm);
                return;
            }
            if (noteText != null)
            {
                var ev = DailyEvents.Instance;
                bool sale = ev != null && ev.SeedPriceMultiplier < 1f;
                noteText.text = ThaiShaper.Fix(sale
                    ? L.T("วันนี้ " + ev.Title + ": " + ev.Description, "Today " + ev.Title + ": " + ev.Description)
                    : L.T("ซื้อเมล็ดแล้วแตะกระถางว่างเพื่อปลูก · ขายผลผลิตหรือขายต้นไม้เพื่อรับแต้ม",
                        "Buy seeds, then tap an empty pot to plant · sell harvests or plants to earn points"));
            }

            UIKit.Clear(grid);
            foreach (var s in catalog.All)
            {
                var species = s;
                int price = gm.SeedPrice(s);
                bool afford = gm.Points >= price;

                var card = UIFactory.Rounded(grid, "Seed_" + s.Key, Color.white, 20f);
                UIFactory.VColumn(card.gameObject, 4f, new RectOffset(12, 12, 12, 12), TextAnchor.UpperCenter);
                UIFactory.Shadow(card.rectTransform, 16f, 0.1f, -4f);

                var img = UIFactory.Image(card.transform, "Packet", catalog.SeedSpriteFor(s), Color.white);
                img.preserveAspect = true;
                UIFactory.Layout(img, -1f, 150f);

                var name = UIFactory.Text(card.transform, "Name", s.DisplayName, 21, skin.textPrimary, TextAnchor.MiddleCenter, true);
                UIFactory.Layout(name, -1f, 32f);
                if (s.isCustom)
                {
                    var tag = UIFactory.Text(card.transform, "Custom", L.T("ชนิดที่สร้างเอง", "Custom species"), 15, skin.eventColor, TextAnchor.MiddleCenter);
                    UIFactory.Layout(tag, -1f, 20f);
                }

                var priceRow = UIFactory.Rect(card.transform, "Price");
                UIFactory.HRow(priceRow.gameObject, 6f, new RectOffset(), TextAnchor.MiddleCenter);
                UIFactory.Layout(priceRow, -1f, 30f);
                var coin = UIFactory.Icon(priceRow, "Coin", skin.iconCoins, skin.gold, 24f);
                UIFactory.Layout(coin, 24f, 24f);
                var pt = UIFactory.Text(priceRow, "Value", price.ToString(), 22, skin.textPrimary, TextAnchor.MiddleLeft, true);
                pt.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (price < s.seedPrice)
                {
                    var old = UIFactory.Text(priceRow, "Old", "(" + s.seedPrice + ")", 16, skin.textMuted, TextAnchor.MiddleLeft);
                    old.horizontalOverflow = HorizontalWrapMode.Overflow;
                }

                var owned = UIFactory.Text(card.transform, "Owned", L.Owned(gm.SeedCount(s)), 16, skin.textMuted, TextAnchor.MiddleCenter);
                UIFactory.Layout(owned, -1f, 22f);

                var buy = UIFactory.Button(card.transform, "Buy", L.Buy, skin.iconBag, afford ? skin.accent : UISkin.WithAlpha(skin.textMuted, 0.5f), Color.white, 20, 18f, 46f);
                buy.interactable = afford;
                buy.onClick.AddListener(() =>
                {
                    string msg;
                    bool ok = gm.BuySeed(species, out msg);
                    if (NotificationCenter.Instance != null)
                        NotificationCenter.Instance.PostInfo(null, L.SeedShop, msg, ok ? AlertKind.Care : AlertKind.Refused);
                });
            }
        }

        void BuildTabs(UISkin skin, GameManager gm)
        {
            if (tabBar == null) return;
            UIKit.Clear(tabBar);
            if (gm.skins == null || gm.skins.skins.Count == 0)
            {
                tab = 0;
                return;
            }
            Tab(skin, 0, L.T("เมล็ดพันธุ์", "Seeds"), skin.iconSprout);
            Tab(skin, 1, L.T("สกินน้อง", "Mascot skins"), skin.iconSparkles);
        }

        void Tab(UISkin skin, int index, string label, Sprite icon)
        {
            bool on = tab == index;
            var b = UIFactory.Button(tabBar, "Tab" + index, label, icon, on ? skin.accent : UISkin.WithAlpha(skin.accent, 0.12f),
                on ? Color.white : skin.accentDark, 20, 22f, 48f);
            var le = b.GetComponent<LayoutElement>();
            if (le == null) le = b.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 190f;
            le.flexibleWidth = 0f;
            b.onClick.AddListener(() =>
            {
                if (tab == index) return;
                tab = index;
                Rebuild();
            });
        }

        void BuildSkins(UISkin skin, GameManager gm)
        {
            if (noteText != null)
                noteText.text = ThaiShaper.Fix(L.T("ซื้อครั้งเดียว ใส่ได้ตลอด · น้องจะเปลี่ยนชุดที่มุมล่างซ้ายของจอ",
                    "Buy once, wear any time · the mascot in the bottom-left corner changes outfit"));
            UIKit.Clear(grid);
            foreach (var s in gm.skins.skins)
            {
                if (s == null) continue;
                var item = s;
                bool owned = gm.OwnsSkin(s);
                bool worn = gm.IsEquipped(s);
                bool afford = gm.Points >= s.price;

                var card = UIFactory.Rounded(grid, "Skin_" + s.id, worn ? UISkin.WithAlpha(skin.accent, 0.12f) : Color.white, 20f);
                UIFactory.VColumn(card.gameObject, 4f, new RectOffset(12, 12, 12, 12), TextAnchor.UpperCenter);
                UIFactory.Shadow(card.rectTransform, 16f, 0.1f, -4f);

                var img = UIFactory.Image(card.transform, "Preview", s.front, Color.white);
                img.preserveAspect = true;
                UIFactory.Layout(img, -1f, 170f);

                var name = UIFactory.Text(card.transform, "Name", s.DisplayName, 21, skin.textPrimary, TextAnchor.MiddleCenter, true);
                UIFactory.Layout(name, -1f, 32f);

                var info = UIFactory.Rect(card.transform, "Info");
                UIFactory.HRow(info.gameObject, 6f, new RectOffset(), TextAnchor.MiddleCenter);
                UIFactory.Layout(info, -1f, 30f);
                if (owned)
                {
                    var t = UIFactory.Text(info, "Owned", worn ? L.T("กำลังใส่อยู่", "Wearing") : L.T("เป็นของคุณแล้ว", "Owned"),
                        18, worn ? skin.accentDark : skin.textMuted, TextAnchor.MiddleCenter, worn);
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
                else
                {
                    var coin = UIFactory.Icon(info, "Coin", skin.iconCoins, skin.gold, 24f);
                    UIFactory.Layout(coin, 24f, 24f);
                    var pt = UIFactory.Text(info, "Value", s.price.ToString(), 22, skin.textPrimary, TextAnchor.MiddleLeft, true);
                    pt.horizontalOverflow = HorizontalWrapMode.Overflow;
                }

                string label = worn ? L.T("ใส่อยู่", "Worn") : owned ? L.T("ใส่ชุดนี้", "Wear") : L.Buy;
                Color bg = worn ? UISkin.WithAlpha(skin.textMuted, 0.35f)
                    : owned ? skin.accentDark
                    : afford ? skin.accent : UISkin.WithAlpha(skin.textMuted, 0.5f);
                var btn = UIFactory.Button(card.transform, "Action", label, owned ? skin.iconSparkles : skin.iconBag, bg, Color.white, 20, 18f, 46f);
                btn.interactable = !worn && (owned || afford);
                btn.onClick.AddListener(() =>
                {
                    if (gm.OwnsSkin(item))
                    {
                        gm.EquipSkin(item);
                        return;
                    }
                    string msg;
                    bool ok = gm.BuySkin(item, out msg);
                    if (NotificationCenter.Instance != null && !string.IsNullOrEmpty(msg))
                        NotificationCenter.Instance.PostInfo(null, L.T("ร้านค้า", "Shop"), msg, ok ? AlertKind.Reward : AlertKind.Refused);
                });
            }
        }
    }
}
