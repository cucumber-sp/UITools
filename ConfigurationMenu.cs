using System;
using System.Collections.Generic;
using System.Linq;
using SFS.UI;
using SFS.UI.ModGUI;
using UnityEngine;
using UnityEngine.UI;
using Button = SFS.UI.ModGUI.Button;
using Object = UnityEngine.Object;
using Type = SFS.UI.ModGUI.Type;

namespace UITools
{
    /// <summary>
    /// All-in-One configuration menu for mods with categories
    /// </summary>
    public static class ConfigurationMenu
    {
        static ConfigurationWindow window;

        internal static void Initialize()
        {
            CreateMenu();
            GameHook.settingsMenu.OnChange += OnGettingSettingsMenu;
            GameHook.OnSettingsMenuOpened += () => window.Enable();
            GameHook.OnSettingsMenuClosed += () => window.Disable();
        }

        // Moving Settings menu right
        static void OnGettingSettingsMenu(GameObject settingsMenu)
        {
            if (settingsMenu is null)
                return;
            RectTransform menu = settingsMenu.transform.GetChild(0).GetChild(0) as RectTransform;
            if (menu is null || !window.ElementsInside)
                return;
            menu.pivot = new Vector2(0, 0.5f);
            menu.localPosition = new Vector3(15, 0, 0);
            window.RectTransform.pivot =  new Vector2(1, 0.5f);
            window.RectTransform.localPosition = new Vector3(-15, 0, 0);
        }
        static void CreateMenu()
        {
            window = new ConfigurationWindow();
            window.Initialize(800, 1200, 0, 0, "Mods Settings", Builder.SceneToAttach.BaseScene);
        }

        /// <summary>
        /// Size of content that you should use while generating content
        /// </summary>
        public static Vector2Int ContentSize => window.RecommendedContentSize;

        /// <summary>
        /// Function that let you add submenu with categories in configuration window
        /// </summary>
        /// <param name="title">Title of your submenu (it shouldn't be too big)</param>
        /// <param name="buttons">
        /// Array of tuples of menu button name and function that generates content of menu and returns your content object (usually box).
        /// Transform is parent that you can use for content. You should use ContentSize as your content size.
        /// </param>
        public static void Add(string title, (string name, Func<Transform, GameObject> createWindowFunc)[] buttons) => window.AddModeButtons(title, buttons);
    }
    
    class ConfigurationWindow
    {
        GameObject holder;
        Window mainWindow;
        Box categoriesButtonsBox;
        GameObject currentDataScreen;

        readonly List<(string name, Func<Transform, GameObject> createWindowFunc)> elements = new List<(string name, Func<Transform, GameObject> createWindowFunc)>();
        readonly List<Button> categoriesButtons = new List<Button>();

        public Vector2Int RecommendedContentSize { get; private set; }

        bool initialized;
        public void Initialize(int width, int height, int posX, int posY, string title, Builder.SceneToAttach attachMode)
        {
            if (initialized)
                return;
            initialized = true;
            
            holder = Builder.CreateHolder(attachMode, "CategoriesWindow Holder");
            holder.SetActive(false);
            
            BuildBase(width, height, posX, posY, title);
        }

        void BuildBase(int width, int height, int posX, int posY, string title)
        {
            // Window
            mainWindow = Builder.CreateWindow(holder.transform, 0, width, height, posX, posY, false, false, 1, title);
            mainWindow.CreateLayoutGroup(Type.Horizontal, TextAnchor.UpperLeft, padding: new RectOffset(20, 20, 20, 20));
            
            // Categories Buttons
            categoriesButtonsBox = Builder.CreateBox(mainWindow, 200, 10, opacity: 0.15f);
            categoriesButtonsBox.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter, spacing: 10, padding: new RectOffset(10, 10, 10, 10));
            categoriesButtonsBox.gameObject.GetOrAddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Vertical Auto-resizing

            RecommendedContentSize = new Vector2Int(width - 260, height - 100);
            
            // Placeholder
            Box dataBox = Builder.CreateBox(mainWindow, RecommendedContentSize.x, RecommendedContentSize.y);
            currentDataScreen = dataBox.gameObject;
        }

        public void Disable()
        {
            holder.SetActive(false);
        }

        public void Enable()
        {
            if (elements.Count == 0)
                return;
            holder.SetActive(true);
            SetScreen(elements[0].name, elements[0].createWindowFunc, 0);
        }

        void SetScreen(string name, Func<Transform,GameObject> createWindowFunc, int index)
        {
            if (name == string.Empty)
                SetPlaceholder();
            else
                SetByFunc(name, createWindowFunc, index);
        }

        void SetPlaceholder()
        {
            Object.Destroy(currentDataScreen);
            Box dataBox = Builder.CreateBox(mainWindow, RecommendedContentSize.x, RecommendedContentSize.y);
            currentDataScreen = dataBox.gameObject;
        }

        void SetByFunc(string name, Func<Transform,GameObject> createWindowFunc, int index)
        {
            Object.Destroy(currentDataScreen);
            currentDataScreen = createWindowFunc.Invoke(mainWindow) ?? Builder.CreateBox(mainWindow, RecommendedContentSize.x, RecommendedContentSize.y).gameObject;

            for (int i = 0; i < categoriesButtons.Count; i++)
                categoriesButtons[i].gameObject.GetComponent<ButtonPC>().SetSelected(i == index);

            LayoutRebuilder.ForceRebuildLayoutImmediate(currentDataScreen.transform as RectTransform);
        }
        
        void AddMenu(string name, Func<Transform, GameObject> createWindowFunc)
        {
            elements.Add((name, createWindowFunc));
            int index = elements.Count - 1;
            Button selectButton = Builder.CreateButton(categoriesButtonsBox, 180, 60, 0, 0, () => SetScreen(name, createWindowFunc, index), name);
            categoriesButtons.Add(selectButton);
        }

        void AddTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return;
            Builder.CreateLabel(categoriesButtonsBox, 180, 40, 0, 0, title);
        }

        void AddSeparator()
        {
            Builder.CreateSeparator(categoriesButtonsBox, 180);
        }

        public void AddModeButtons(string title, (string name, Func<Transform, GameObject> createWindowFunc)[] buttons)
        {
            if (categoriesButtons.Count > 0)
                AddSeparator();
            AddTitle(title);
            foreach (var element in buttons)
                AddMenu(element.name, element.createWindowFunc);
        }

        internal bool ElementsInside => categoriesButtons.Count > 0;

        internal RectTransform RectTransform => mainWindow.rectTransform;
    }
}