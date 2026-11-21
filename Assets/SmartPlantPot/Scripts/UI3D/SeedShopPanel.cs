using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Seed shop: every species as a 2D seed packet with its price in points. Buying adds the seed
    /// to your inventory; tap an empty pot to plant it. Prices follow today's event (seed sale).
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Seed Shop Panel")]
    public class SeedShopPanel : ModalPanel
    {
        public RectTransform grid;
        public Text pointsText;
        public Text noteText;

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.Changed += OnGameChanged;
            if (PlantCatalog.Current != null) PlantCatalog.Current.Changed += OnGameChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (GameManager.Instance != null) GameManager.Instance.Changed -= OnGameChanged;
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
    }
}
