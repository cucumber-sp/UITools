using JetBrains.Annotations;
using SFS.UI.ModGUI;
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

            NumberInput numberInput = new(input, buttonLeft, buttonRight);
            numberInput.Init(container.gameObject, parent);
            numberInput.Size = new Vector2(width, height);
            numberInput.Value = value;
            numberInput.Step = buttonsStep;
            numberInput.Position = new Vector2(posX, posY);

            return numberInput;
        }

        /// <summary>
        ///     Creates closable window
        /// </summary>
        public static ClosableWindow CreateClosableWindow(Transform parent, int ID, int width, int height, int posX = 0,
            int posY = 0, bool draggable = false, bool savePosition = true, float opacity = 1f, string titleText = "",
            bool minimized = false)
        {
            GameObject self = Builder.CreateWindow(parent, ID, width, height, posX, posY, draggable, savePosition,
                opacity, titleText).gameObject;
            ClosableWindow window = new();
            window.Init(self, parent);
            window.Minimized = minimized;
            window.Size = new Vector2(width, height);
            return window;
        }

        /// <summary>
        ///     Creates RadioButtons with given layout type
        /// </summary>
        public static T CreateRadioButtons<T>(Transform parent, int buttonWidth, int buttonHeight, int positionX = 0,
            int positionY = 0, params string[] buttons) where T : RadioButtons, new()
        {
            Container container = Builder.CreateContainer(parent);
            GameObject self = container.gameObject;
            T radioButtons = new();
            radioButtons.Init(self, parent);
            radioButtons.Position = new Vector2(positionX, positionY);
            radioButtons.ButtonHeight = buttonHeight;
            radioButtons.ButtonWidth = buttonWidth;
            radioButtons.SetOptions(buttons);
            return radioButtons;
        }
    }
}