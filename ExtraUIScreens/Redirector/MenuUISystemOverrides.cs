using Belzont.Utils;
using Game.UI;
using Game.UI.Menu;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ExtraUIScreens
{
    public class MenuUISystemOverrides : Redirector, IRedirectableWorldless
    {
        public void Awake()
        {
            AddRedirect(typeof(MenuUISystem).GetMethod("SaveGame", RedirectorUtils.allFlags, null, new Type[] { typeof(string) }, null), GetType().GetMethod("OnSaveGame", RedirectorUtils.allFlags));
        }
        private static FieldInfo previewSettings = typeof(MenuUISystem).GetField("m_PreviewSettings", RedirectorUtils.allFlags);

        private static bool OnSaveGame(MenuUISystem __instance, string saveName)
        {
            RenderTexture target = ScreenCaptureHelper.CreateRenderTarget("SaveGamePanel", 680, 383, GraphicsFormat.R8G8B8A8_UNorm);
            ScreenCaptureHelper.CaptureScreenshot(Camera.main, target, (MenuHelpers.SaveGamePreviewSettings)previewSettings.GetValue(__instance));
            ScreenCaptureHelper.AsyncRequest request = new ScreenCaptureHelper.AsyncRequest(target);
            __instance.SaveGame(saveName, request, delegate (Guid x)
            {
                UnityEngine.Object.Destroy(target);
            }, false);
            return false;
        }
    }
}
