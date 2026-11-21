using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

        [Header("3D shop")]
        public ModalPanel shopPanel;
        public ModalPanel settingsPanel;
        [Tooltip("Clicking empty space in the 3D view deselects the pot (3D scene).")]
        public bool deselectOnEmptyClick;

        Vector2 pressPosition;
        static readonly List<RaycastResult> probeHits = new List<RaycastResult>();

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
            HandleEmptyClick();

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (ModalPanel.CloseTop()) return;
                if (historyPanel != null && historyPanel.IsOpen) historyPanel.Close();
                else PlantSelection.Clear();
            }

            // Don't steal letters while typing in a text field (settings panel).
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null &&
                es.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() != null) return;

            if (kb.bKey.wasPressedThisFrame && shopPanel != null) shopPanel.Toggle();
            if (kb.oKey.wasPressedThisFrame && settingsPanel != null) settingsPanel.Toggle();
            if (kb.fKey.wasPressedThisFrame) PotInteraction.PerformAndNotify(PlantSelection.Current, CareAction.Fertilize);

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

        void HandleEmptyClick()
        {
            if (!deselectOnEmptyClick || ModalPanel.AnyOpen) return;
            var p = Pointer.current;
            if (p == null) return;
            if (p.press.wasPressedThisFrame) pressPosition = p.position.ReadValue();
            if (!p.press.wasReleasedThisFrame) return;
            if ((p.position.ReadValue() - pressPosition).sqrMagnitude > 400f) return; // a drag, not a tap
            var es = EventSystem.current;
            if (es != null)
            {
                // Anything under the pointer (UI or a 3D pot via the PhysicsRaycaster) handles the tap itself.
                var ped = new PointerEventData(es);
                ped.position = p.position.ReadValue();
                probeHits.Clear();
                es.RaycastAll(ped, probeHits);
                if (probeHits.Count > 0) return;
            }
            PlantSelection.Clear();
            if (historyPanel != null && historyPanel.IsOpen) historyPanel.Close();
        }
    }
}
