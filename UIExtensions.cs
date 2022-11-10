using System;
using SFS.UI;
using SFS.UI.ModGUI;

namespace UITools
{
    /// <summary>
    /// Class that provides some extra functionality for default 
    /// </summary>
    public static class UIExtensions
    {
        /// <summary>
        /// Allow you to register some actions on window drop after dragging.
        /// </summary>
        /// <param name="window">The window for which the action will be subscribed</param>
        /// <param name="onDrop">Action that will be called every time the window is dropped</param>
        /// <example>
        /// The following code register writing message in the console every time window dropped
        /// <code>
        /// Window window = Builder.CreateWindow(...);
        /// window.RegisterOnDropListener(() => Debug.Log("Window is dropped!"));
        /// </code>
        /// </example>
        public static void RegisterOnDropListener(this Window window, Action onDrop) => window.gameObject.GetComponent<DraggableWindowModule>().OnDropAction += onDrop;
    }
}