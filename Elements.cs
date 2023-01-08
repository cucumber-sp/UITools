using System;
using System.Globalization;
using JetBrains.Annotations;
using SFS.Tween;
using SFS.UI;
using SFS.UI.ModGUI;
using SFS.Variables;
using UnityEngine;
using UnityEngine.UI;
using Button = SFS.UI.ModGUI.Button;
using Object = UnityEngine.Object;
using Type = SFS.UI.ModGUI.Type;

// ReSharper disable InconsistentNaming

namespace UITools
{
    /// <summary>
    ///     Builder for advanced UITools elements
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public static class UIToolsBuilder
    {
        /// <summary>
        ///     Creates a number input with given parameters
        /// </summary>
        public static NumberInput CreateNumberInput(Transform parent, int width, int height, float value,
            float buttonsStep, int posX = 0, int posY = 0)
        {
            Container container = Builder.CreateContainer(parent);
            Object.Destroy(container.gameObject.GetComponent<ContentSizeFitter>());
            container.CreateLayoutGroup(Type.Horizontal, spacing: 5f);
            Button buttonLeft = Builder.CreateButton(container, 30, 30, text: "<");
            TextInput input = Builder.CreateTextInput(container, width - 70, height);
            Button buttonRight = Builder.CreateButton(container, 30, 30, text: ">");

            NumberInput numberInput = new NumberInput(input, buttonLeft, buttonRight);
            numberInput.Init(container.gameObject, parent);
            numberInput.Size = new Vector2(width, height);
            numberInput.Value = value;
            numberInput.Step = buttonsStep;
            numberInput.Position = new Vector2(posX, posY);

            return numberInput;
        }

        /// <summary>
        /// Creates closable window
        /// </summary>
        public static ClosableWindow CreateClosableWindow(Transform parent, int ID, int width, int height, int posX = 0,
            int posY = 0, bool draggable = false, bool savePosition = true, float opacity = 1f, string titleText = "",
            bool minimized = false)
        {
            GameObject self = Builder.CreateWindow(parent, ID, width, height, posX, posY, draggable, savePosition,
                opacity, titleText).gameObject;
            ClosableWindow window = new ClosableWindow();
            window.Init(self, parent);
            window.Minimized = minimized;
            window.Size = new Vector2(width, height);
            return window;
        }
    }

    /// <summary>
    ///     Default input styled input for decimal number with arrow buttons
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class NumberInput : GUIElement
    {
        readonly Button buttonLeft;
        readonly Button buttonRight;
        readonly TextInput input;

        float numberValue;

        internal NumberInput(TextInput input, Button buttonLeft, Button buttonRight)
        {
            this.input = input;
            this.buttonLeft = buttonLeft;
            this.buttonRight = buttonRight;
        }

        /// <summary>
        ///     Value of number input
        /// </summary>
        public float Value
        {
            get => numberValue;
            set
            {
                numberValue = value;
                OnValueChanged();
            }
        }

        /// <summary>
        ///     Step that will be used for change buttons
        /// </summary>
        public float Step { get; set; }


        /// <inheritdoc />
        public override Vector2 Size
        {
            get => base.Size;
            set
            {
                base.Size = value;
                RecalculateSize();
            }
        }

        /// <inheritdoc />
        public override void Init(GameObject self, Transform parent)
        {
            gameObject = self;
            gameObject.transform.SetParent(parent, false);
            rectTransform = gameObject.Rect();
            buttonLeft.OnClick += () => OnChangeButtonClicked(-1);
            buttonRight.OnClick += () => OnChangeButtonClicked(1);
            input.OnChange += OnInputFieldChanged;
        }

        /// <summary>
        ///     Event that will be called every time float value changed
        /// </summary>
        public event Action<float> OnValueChangedEvent;

        void RecalculateSize()
        {
            Vector2 size = Size;
            buttonLeft.Size = new Vector2(30, 30);
            buttonRight.Size = new Vector2(30, 30);
            input.Size = new Vector2(size.x - 70, size.y);
        }

        void OnValueChanged()
        {
            input.Text = numberValue.ToString(CultureInfo.InvariantCulture);
            OnValueChangedEvent?.Invoke(Value);
        }

        void OnChangeButtonClicked(int multiplier)
        {
            Value += Step * multiplier;
        }

        void OnInputFieldChanged(string value)
        {
            if (value.EndsWith(".") || value == "-0" || (value.Contains(".") && value.EndsWith("0")))
                return;
            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                Value = result;
        }
    }

    /// <summary>
    /// Window with minimize button
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ClosableWindow : Window
    {
        Vector2_Local openedSize = new ();
        Bool_Local minimized = new () {Value = true};
        
        Button minimizeButton;
        Transform minimizeButtonText;

        /// <summary>
        /// Fired every time minimized state changed
        /// </summary>
        public event Action OnMinimizedChangedEvent;

        /// <inheritdoc />
        public override Vector2 Size
        {
            get => base.Size;
            set => openedSize.Value = value;
        }

        /// <summary>
        ///    Is window minimized?
        /// </summary>
        public bool Minimized
        {
            get => minimized.Value;
            set => minimized.Value = value;
        }
        
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
            
            minimizeButton = Builder.CreateButton(rectTransform, 25, 25, posX:(int)-(openedSize.Value.x / 2 - 15), posY:-25, text: "▼", onClick: () => Minimized ^= true);
            minimizeButtonText = minimizeButton.rectTransform.Find("Text");
            
            openedSize.OnChange += OnSizeChanged;
            minimized.OnChange += OnMinimizedChanged;
        }
    }
}