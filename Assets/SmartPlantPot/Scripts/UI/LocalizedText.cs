using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>Static label that shows Thai or English depending on SmartPlantPotApp.thaiLanguage.</summary>
    [RequireComponent(typeof(Text))]
    [AddComponentMenu("Smart Plant Pot/Localized Text")]
    public class LocalizedText : MonoBehaviour
    {
        [TextArea] public string thai;
        [TextArea] public string english;

        void Start()
        {
            Apply();
        }

        public void Apply()
        {
            var t = GetComponent<Text>();
            if (t != null) t.text = L.Thai ? ThaiShaper.Fix(thai) : english;
        }

        public static void Attach(Text text, string thai, string english)
        {
            var lt = text.GetComponent<LocalizedText>();
            if (lt == null) lt = text.gameObject.AddComponent<LocalizedText>();
            lt.thai = thai;
            lt.english = english;
            text.text = L.Thai ? ThaiShaper.Fix(thai) : english;
        }
    }
}
