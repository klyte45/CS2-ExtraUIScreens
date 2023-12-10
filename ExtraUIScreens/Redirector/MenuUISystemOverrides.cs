using Belzont.Utils;
using cohtml.InputSystem;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Colossal.UI;
using Game;
using Game.SceneFlow;
using Game.Settings;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ExtraUIScreens
{
    public class UIInputSystemOverrides : Redirector, IRedirectableWorldless
    {
        public void Awake()
        {
            AddRedirect(typeof(UIInputSystem).GetMethod("SetMousePosition", RedirectorUtils.allFlags), GetType().GetMethod("BeforeSetMousePosition", RedirectorUtils.allFlags));
        }
        private static bool BeforeSetMousePosition(ref UIView uiView, ref Vector2 mousePosition, ref MouseEventDataCached mouseData)
        {
            var monitor = (int)Display.RelativeMouseAt(Input.mousePosition).z;
            mouseData.X = (int)(mousePosition.x / Display.displays[monitor].renderingWidth * uiView.width);
            mouseData.Y = (int)((1f - (mousePosition.y / Display.displays[monitor].renderingHeight)) * uiView.height);
            return false;
        }
    }

    public class InterfaceSettingsOverrides : Redirector, IRedirectableWorldless
    {
        public void Awake()
        {
            AddRedirect(typeof(InterfaceSettings).GetProperty("interfaceStyle", RedirectorUtils.allFlags).SetMethod, null, GetType().GetMethod("AfterSetInterfaceStyle", RedirectorUtils.allFlags));
        }
        private static void AfterSetInterfaceStyle()
        {
            EuisScreenManager.Instance?.OnInterfaceStyleChanged();
        }
    }

    public class GameManagerOverrides : Redirector, IRedirectableWorldless
    {

        public void Awake()
        {
            // private Task<bool> Load(GameMode mode, Purpose purpose, AsyncReadDescriptor descriptor, Guid sessionGuid)
            AddRedirect(typeof(GameManager).GetMethod("Load", RedirectorUtils.allFlags, null, new[] { typeof(GameMode), typeof(Purpose), typeof(AsyncReadDescriptor), typeof(Guid) }, null),
                GetType().GetMethod("BeforeSceneLoad", RedirectorUtils.allFlags), GetType().GetMethod("AfterSceneLoad", RedirectorUtils.allFlags));
        }
        private static void BeforeSceneLoad()
        {
            EuisScreenManager.Instance.RunOnBeforeSceneLoad();
        }
        private static void AfterSceneLoad(Task<bool> __result)
        {
            // __result.ContinueWith((x) => new Task(() => EuisScreenManager.Instance.RunOnAfterSceneLoad()));
        }
    }

    public class ScreenshotOverride : Redirector, IRedirectableWorldless
    {
        public void Awake()
        {
            var overrideMethod = GetType().GetMethod("TakeScreenShot", RedirectorUtils.allFlags);
            AddRedirect(typeof(ScreenCapture).GetMethod("CaptureScreenshotAsTexture", RedirectorUtils.allFlags, null, new Type[] { }, null), overrideMethod);
            AddRedirect(typeof(ScreenCapture).GetMethod("CaptureScreenshotAsTexture", RedirectorUtils.allFlags, null, new[] { typeof(int) }, null), overrideMethod);
            AddRedirect(typeof(ScreenCapture).GetMethod("CaptureScreenshotAsTexture", RedirectorUtils.allFlags, null, new[] { typeof(ScreenCapture.StereoScreenCaptureMode) }, null), overrideMethod);
        }
        public static bool TakeScreenShot(ref Texture2D __result)
        {
            int photoWidth = Display.displays[0].renderingWidth;
            int photoHeight = Display.displays[0].renderingHeight;
            RenderTexture rt = new RenderTexture(photoWidth, photoHeight, 24);
            Camera.main.targetTexture = rt;
            RenderTexture.active = rt;
            Camera.main.Render();
            __result = new Texture2D(photoWidth, photoHeight, TextureFormat.RGB24, false);
            __result.ReadPixels(new Rect(0, 0, photoWidth, photoHeight), 0, 0);
            __result.Apply();
            Camera.main.targetTexture = null;
            Camera.main.targetTexture = null;
            RenderTexture.active = null;
            GameObject.Destroy(rt);
            return false;
        }
    }
}
