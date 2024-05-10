using System;
using System.Globalization;
using JetBrains.Annotations;
using SFS.UI;
using SFS.UI.ModGUI;
using UnityEngine;
using Button = SFS.UI.ModGUI.Button;

namespace UITools
{
    /// <summary>
    ///     Default input styled input for decimal number with arrow buttons
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class NumberInput : GUIElement
    {
        readonly Color baseColor;
        readonly Button buttonLeft;
        readonly Button buttonRight;
        readonly TextInput input;

        float numberValue;

        internal NumberInput(TextInput input, Button buttonLeft, Button buttonRight)
        {
            this.input = input;
            this.buttonLeft = buttonLeft;
            this.buttonRight = buttonRight;
            baseColor = this.input.FieldColor;
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
            input.FieldColor = baseColor;
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
            else
                input.FieldColor = new Color(0.388f, 0.129f, 0.172f, 0.941f);
        }
    }
}