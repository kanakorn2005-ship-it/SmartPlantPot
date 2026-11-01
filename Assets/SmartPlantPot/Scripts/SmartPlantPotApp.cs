using UnityEngine;
using UnityEngine.InputSystem;

namespace SmartPlantPot
{
    /// <summary>
    /// Scene root: sets the skin + language before anything else runs, and adds keyboard shortcuts
    /// for demos (1-3 select a plant, W water, P prune, N notifications, Esc close).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Smart Plant Pot/Smart Plant Pot App")]
    public class SmartPlantPotApp : MonoBehaviour
    {
        public UISkin skin;

        [Tooltip("Off = English labels.")]
        public bool thaiLanguage = true;

        [Tooltip("Fix Thai tone marks that overlap upper vowels (needs the bundled patched Prompt fonts).")]
        public bool fixThaiToneMarks = true;

        public int targetFrameRate = 60;
        public NotificationHistoryPanel historyPanel;

        static readonly Key[] DigitKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };

        void Awake()
        {
            if (skin != null) UISkin.Current = skin;
            L.Thai = thaiLanguage;
            ThaiShaper.Enabled = fixThaiToneMarks;
            Application.targetFrameRate = targetFrameRate;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (historyPanel != null && historyPanel.IsOpen) historyPanel.Close();
                else PlantSelection.Clear();
            }

            for (int i = 0; i < DigitKeys.Length && i < PlantUnit.All.Count; i++)
            {
                if (!kb[DigitKeys[i]].wasPressedThisFrame) continue;
                var plant = PlantUnit.All[i];
                PlantSelection.Toggle(plant);
                var interaction = plant.GetComponent<PotInteraction>();
                if (interaction != null) interaction.Poke();
            }

            if (kb.wKey.wasPressedThisFrame) PotInteraction.PerformAndNotify(PlantSelection.Current, CareAction.Water);
            if (kb.pKey.wasPressedThisFrame) PotInteraction.PerformAndNotify(PlantSelection.Current, CareAction.Prune);
            if (kb.nKey.wasPressedThisFrame && historyPanel != null) historyPanel.Toggle();
        }
    }
}
