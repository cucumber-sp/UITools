using System;
using System.Collections.Generic;
using SFS.UI;
using SFS.UI.ModGUI;
using UnityEngine;
using UnityEngine.UI;
using Button = SFS.UI.ModGUI.Button;
using Object = UnityEngine.Object;

namespace UITools
{
    /// <summary>
    ///     Abstract class for radio buttons
    /// </summary>
    public abstract class RadioButtons : GUIElement
    {
        // Inner variables
        protected List<Button> buttons = new();
        Transform buttonsHolder;
        float height;
        int selected;
        float width;

        /// <summary>
        ///     Read-only array of buttons
        /// </summary>
        public Button[] Buttons => buttons.ToArray();

        /// <summary>
        ///     Determines what button is selected
        ///     Can be -1 if no button is selected
        /// </summary>
        public int Selected
        {
            get => selected;
            set
            {
                if (value < -1 || value >= buttons.Count)
                    throw new IndexOutOfRangeException("Selected index must be -1 or in range of buttons count");
                if (selected != -1 && selected == value && CanDeselect)
                    value = -1;
                selected = value;
                for (int i = 0; i < buttons.Count; i++)
                    buttons[i].SetSelected(i == selected);
                OnSelectedChanged?.Invoke(selected);
            }
        }

        /// <summary>
        ///     Determines if button can be deselected by clicking on it
        /// </summary>
        public bool CanDeselect { get; set; } = false;

        /// <summary>
        ///     Determines width of buttons
        /// </summary>
        public virtual float ButtonWidth
        {
            get => width;
            set
            {
                width = value;
                foreach (Button button in buttons)
                    button.Size = new Vector2(value, button.Size.y);
            }
        }

        /// <summary>
        ///     Determines height of buttons
        /// </summary>
        public virtual float ButtonHeight
        {
            get => height;
            set
            {
                height = value;
                foreach (Button button in buttons)
                    button.Size = new Vector2(button.Size.x, value);
            }
        }

        /// <summary>
        ///     Determines space between buttons
        /// </summary>
        public abstract float Spacing { get; set; }

        /// <summary>
        ///     Event that will be called when selected button is changed
        /// </summary>
        public event Action<int> OnSelectedChanged;

        /// <summary>
        ///     Add new option to radio buttons
        /// </summary>
        /// <param name="text">Text that will be displayed on button</param>
        public void AddOption(string text)
        {
            int index = buttons.Count;
            Button button = Builder.CreateButton(buttonsHolder, (int)ButtonWidth, (int)ButtonHeight, 0, 0,
                () => Selected = index, text);
            buttons.Add(button);
        }

        /// <summary>
        ///     Set array of options to radio buttons
        /// </summary>
        /// <param name="options">Array of options</param>
        public void SetOptions(params string[] options)
        {
            for (int i = buttonsHolder.childCount - 1; i >= 0; i--)
                Object.Destroy(buttonsHolder.GetChild(i).gameObject);
            buttons.Clear();
            foreach (string option in options)
                AddOption(option);
        }

        /// <inheritdoc />
        public override void Init(GameObject self, Transform parent)
        {
            gameObject = self;
            gameObject.transform.SetParent(parent, false);
            rectTransform = gameObject.Rect();
            buttonsHolder = CreateLayout(self);
        }

        protected abstract Transform CreateLayout(GameObject self);
    }

    public class VerticalRadioButtons : RadioButtons
    {
        // Inner variables
        VerticalLayoutGroup layout;

        /// <inheritdoc />
        public override float Spacing
        {
            get => layout.spacing;
            set => layout.spacing = value;
        }

        /// <inheritdoc />
        protected override Transform CreateLayout(GameObject self)
        {
            layout = self.AddComponent<VerticalLayoutGroup>();
            layout.DisableChildControl();
            layout.childAlignment = TextAnchor.MiddleCenter;
            return self.transform;
        }
    }

    public class HorizontalRadioButtons : RadioButtons
    {
        // Inner variables
        HorizontalLayoutGroup layout;

        /// <inheritdoc />
        public override float Spacing
        {
            get => layout.spacing;
            set => layout.spacing = value;
        }

        /// <inheritdoc />
        protected override Transform CreateLayout(GameObject self)
        {
            layout = self.AddComponent<HorizontalLayoutGroup>();
            layout.DisableChildControl();
            layout.childAlignment = TextAnchor.MiddleCenter;
            return self.transform;
        }
    }

    public class GridRadioButtons : RadioButtons
    {
        // Inner variables
        GridLayoutGroup layout;

        /// <inheritdoc />
        public override float Spacing
        {
            get => layout.spacing.x;
            set => layout.spacing = new Vector2(value, value);
        }

        /// <inheritdoc />
        public override float ButtonWidth
        {
            get => layout.cellSize.x;
            set => layout.cellSize = new Vector2(value, layout.cellSize.y);
        }

        /// <inheritdoc />
        public override float ButtonHeight
        {
            get => layout.cellSize.y;
            set => layout.cellSize = new Vector2(layout.cellSize.x, value);
        }

        /// <summary>
        ///     Determines constraint size of grid
        /// </summary>
        public int ConstraintCount
        {
            get => layout.constraintCount;
            set => layout.constraintCount = value;
        }

        /// <summary>
        ///     Determines how grid will be filled
        /// </summary>
        public GridLayoutGroup.Constraint Constraint
        {
            get => layout.constraint;
            set => layout.constraint = value;
        }

        /// <summary>
        ///     Determines if grid will fill columns or rows first
        /// </summary>
        public GridLayoutGroup.Axis StartAxis
        {
            get => layout.startAxis;
            set => layout.startAxis = value;
        }

        protected override Transform CreateLayout(GameObject self)
        {
            layout = self.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(ButtonWidth, ButtonHeight);
            layout.childAlignment = TextAnchor.MiddleCenter;
            return self.transform;
        }
    }
}