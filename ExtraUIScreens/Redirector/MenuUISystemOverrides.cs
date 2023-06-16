using Belzont.Utils;
using Game.UI;
using Game.UI.Menu;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ExtraUIScreens
{
    public class MenuUISystemOverrides : Redirector, IRedirectable
    {
        public void Awake()
        {
            AddRedirect(typeof(MenuUISystem).GetMethod("SaveGame", RedirectorUtils.allFlags, null, new Type[] { typeof(string) }, null), GetType().GetMethod("OnSaveGame", RedirectorUtils.allFlags));
        }
        private static FieldInfo previewSettings = typeof(MenuUISystem).GetField("m_PreviewSettings", RedirectorUtils.allFlags);

        private static bool OnSaveGame(MenuUISystem __instance, string saveName)
        {
            Texture2D target = ScreenshotHelper.CreateTarget("SaveGamePanel", 680, 383, GraphicsFormat.R8G8B8A8_UNorm);
            ScreenshotHelper.TakeScreenshot(Camera.main, target, (MenuHelpers.SaveGamePreviewSettings)previewSettings.GetValue(__instance));
            __instance.SaveGame(saveName, target, delegate (Guid x)
            {
                UnityEngine.Object.Destroy(target);
            }, false);
            return false;
        }
    }
}
