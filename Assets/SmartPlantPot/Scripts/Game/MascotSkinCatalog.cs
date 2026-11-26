using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartPlantPot
{
    [Serializable]
    public class MascotSkin
    {
        public string id = "bunny_hat";
        public string nameTh = "กระต่ายคนสวน";
        public string nameEn = "Gardener bunny";
        public Sprite front;
        [Tooltip("Shown while the camera looks around the shop (the mascot watches with you).")]
        public Sprite back;
        [Tooltip("0 = free / owned from the start.")]
        public int price;

        public string DisplayName
        {
            get { return L.T(nameTh, nameEn); }
        }
    }

    /// <summary>
    /// The mascot skins sold in the shop (the 2D team's Katuii characters). The equipped one is the
    /// helper in the bottom-left corner. Alert animations are separate and don't follow the skin.
    /// </summary>
    [CreateAssetMenu(menuName = "Smart Plant Pot/Mascot Skin Catalog", fileName = "MascotSkins")]
    public class MascotSkinCatalog : ScriptableObject
    {
        public List<MascotSkin> skins = new List<MascotSkin>();
        public string defaultSkinId = "bunny_hat";

        public MascotSkin Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in skins)
                if (s != null && s.id == id) return s;
            return null;
        }

        public MascotSkin Default
        {
            get
            {
                var d = Find(defaultSkinId);
                if (d != null) return d;
                return skins.Count > 0 ? skins[0] : null;
            }
        }
    }
}
