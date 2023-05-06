using SFS.Variables;
using Object = UnityEngine.Object;

namespace UITools;

/// <summary>
///     Observable GameObject variable
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
///     Utility for UI
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class UIUtility
{
    static RectTransform canvas;

    /// <summary>
    ///     Rect Transform of the canvas
    /// </summary>
    public static RectTransform CanvasRectTransform => canvas ??= GetCanvasRect();

    /// <summary>
    ///     Get the canvas pixel size
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

    /// <summary>
    ///     Creates texture from base64 string
    /// </summary>
    public static Texture2D CreateTexture(string base64Image)
    {
        byte[] data = Convert.FromBase64String(base64Image);
        Texture2D result = new(1, 1, TextureFormat.ARGB32, 1, true);
        result.LoadImage(data);
        result.Apply();
        return result;
    }
}

/// <summary>
///     Opens MenuGenerator as async functions
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class AsyncDialogs
{
    /// <summary>
    ///     Works same as MenuGenerator.OpenConfirmation. Returns true if the user pressed the confirm button.
    /// </summary>
    public static async UniTask<bool> OpenConfirmation(CloseMode closeMode, Func<string> text, Func<string> confirmText)
    {
        ConfirmationAwaiter awaiter = new();
        MenuGenerator.OpenConfirmation(closeMode, text, confirmText, () => awaiter.OnAction(true),
            onDeny: () => awaiter.OnAction(false));
        return await awaiter.WaitForConfirmation();
    }

    class ConfirmationAwaiter
    {
        bool closed;
        bool result;

        internal async UniTask<bool> WaitForConfirmation()
        {
            while (!closed)
                await UniTask.Yield();
            return result;
        }

        internal void OnAction(bool res)
        {
            closed = true;
            result = res;
        }
    }
}