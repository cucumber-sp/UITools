using SFS.UI.ModGUI;
using SFS.Variables;
using UnityEngine;

namespace UITools
{
    /// <summary>
    /// Observable GameObject variable
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class GameObject_Local : Obs<GameObject>
    {
        /// <summary>Comparison</summary>
        protected override bool IsEqual(GameObject a, GameObject b)
        {
            return a == b;
        }
    }

    /// <summary>
    /// Utility for UI
    /// </summary>
    public static class UIUtility
    {
        static RectTransform canvas;

        /// <summary>
        /// Rect Transform of the canvas
        /// </summary>
        public static RectTransform CanvasRectTransform  => canvas ??= GetCanvasRect();

        /// <summary>
        /// Get the canvas pixel size
        /// </summary>
        public static Vector2 CanvasPixelSize
        {
            get
            {
                canvas ??= GetCanvasRect();
                return canvas.sizeDelta;
            }
        }
        static RectTransform GetCanvasRect()
        {
            GameObject temp = Builder.CreateHolder(Builder.SceneToAttach.BaseScene, "TEMP");
            RectTransform result = temp.transform.parent as RectTransform;
            Object.Destroy(temp);
            return result;
        }

    }
}