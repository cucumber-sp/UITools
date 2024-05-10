using System;
using JetBrains.Annotations;
using SFS.Tween;
using SFS.UI.ModGUI;
using SFS.Variables;
using UnityEngine;
using UnityEngine.UI;
using Button = SFS.UI.ModGUI.Button;

namespace UITools
{
    /// <summary>
    ///     Window with minimize button
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ClosableWindow : Window
    {
        Button minimizeButton;
        Transform minimizeButtonText;
        Bool_Local minimized = new() { Value = true };
        Vector2_Local openedSize = new();

        /// <inheritdoc />
        public override Vector2 Size
        {
            get => base.Size;
            set => openedSize.Value = value;
        }

        /// <summary>
        ///     Is window minimized?
        /// </summary>
        public bool Minimized
        {
            get => minimized.Value;
            set => minimized.Value = value;
        }

        /// <summary>
        ///     Fired every time minimized state changed
        /// </summary>
        public event Action OnMinimizedChangedEvent;

        void OnSizeChanged(Vector2 value)
        {
            if (!minimized.Value)
                base.Size = value;
            minimizeButton.Position = new Vector2(-(openedSize.Value.x / 2 - 25), -25);
            minimizeButtonText.TweenLocalRotate(new Vector3(0, 0, minimized.Value ? 90 : 0), 0.15f, true);
        }

        void OnMinimizedChanged(bool value)
        {
            base.Size = value ? new Vector2(openedSize.Value.x, 50) : openedSize.Value;
            LayoutRebuilder.ForceRebuildLayoutImmediate(ChildrenHolder as RectTransform);
            minimizeButton.Position = new Vector2(-(openedSize.Value.x / 2 - 25), -25);
            minimizeButtonText.TweenLocalRotate(new Vector3(0, 0, value ? 90 : 0), 0.15f, true);
            OnMinimizedChangedEvent?.Invoke();
        }

        /// <inheritdoc />
        public override void Init(GameObject self, Transform parent)
        {
            base.Init(self, parent);

            minimizeButton = Builder.CreateButton(rectTransform, 25, 25, (int)-(openedSize.Value.x / 2 - 15), -25,
                text: "▼", onClick: () => Minimized ^= true);
            minimizeButtonText = minimizeButton.rectTransform.Find("Text");

            openedSize.OnChange += OnSizeChanged;
            minimized.OnChange += OnMinimizedChanged;
        }
    }
}