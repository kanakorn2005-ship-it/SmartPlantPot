using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>Simple yes/no dialog (used before selling a plant or wiping the save).</summary>
    [AddComponentMenu("Smart Plant Pot/Confirm Dialog")]
    public class ConfirmDialog : ModalPanel
    {
        public static ConfirmDialog Instance { get; private set; }

        public Text titleText;
        public Text messageText;
        public Button confirmButton;
        public Button cancelButton;

        Action onConfirm;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }

        public static void Show(string title, string message, string confirmLabel, Action confirm)
        {
            if (Instance == null)
            {
                if (confirm != null) confirm();
                return;
            }
            Instance.titleText.text = ThaiShaper.Fix(title);
            Instance.messageText.text = ThaiShaper.Fix(message);
            UIKit.SetButtonLabel(Instance.confirmButton, confirmLabel);
            Instance.onConfirm = confirm;
            Instance.Open();
        }

        void OnConfirm()
        {
            var a = onConfirm;
            onConfirm = null;
            Close();
            if (a != null) a();
        }
    }
}
