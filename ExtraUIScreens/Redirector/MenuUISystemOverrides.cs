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
    public class MenuUISystemOverrides : Redirector, IRedirectableWorldless
    {
        //public void Awake()
        //{
        //    AddRedirect(typeof(MenuUISystem).GetMethod("SaveGame", RedirectorUtils.allFlags, null, new Type[] { typeof(string) }, null), GetType().GetMethod("OnSaveGame", RedirectorUtils.allFlags));
        //}
        //private static FieldInfo previewSettings = typeof(MenuUISystem).GetField("m_PreviewSettings", RedirectorUtils.allFlags);

        //private static bool OnSaveGame(MenuUISystem __instance, string saveName)
        //{
        //    RenderTexture target = ScreenCaptureHelper.CreateRenderTarget("SaveGamePanel", 680, 383, GraphicsFormat.R8G8B8A8_UNorm);
        //    ScreenCaptureHelper.CaptureScreenshot(Camera.main, target, (MenuHelpers.SaveGamePreviewSettings)previewSettings.GetValue(__instance));
        //    ScreenCaptureHelper.AsyncRequest request = new ScreenCaptureHelper.AsyncRequest(target);
        //    __instance.(saveName, request, delegate (Guid x)
        //    {
        //        UnityEngine.Object.Destroy(target);
        //    }, false);
        //    return false;
        //}
    }

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
            EuisScreenManager.Instance.OnInterfaceStyleChanged();
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
}
