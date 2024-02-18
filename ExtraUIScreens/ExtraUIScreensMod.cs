using Belzont.Interfaces;
using Belzont.Utils;
using Game;
using K45EUIS_Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using Game.Modding;
#if BEPINEX_CS2
using BepInEx;
#else
using Colossal.IO.AssetDatabase;
#endif

namespace ExtraUIScreens
{
    public class ExtraUIScreensMod : BasicIMod, IMod
    {
        public static new ExtraUIScreensMod Instance => (ExtraUIScreensMod)BasicIMod.Instance;

        public override string Acronym => "EUIS";
        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            LoadExtraScreenFromMods();
        }

        public override void OnDispose()
        {
            GameObject.Destroy(euisGO);
        }

        GameObject euisGO;

        public override void DoOnLoad()
        {
            euisGO = new GameObject("EUIS");
            euisGO.AddComponent<EuisScreenManager>();
        }

        private static void LoadExtraScreenFromMods()
        {
            HashSet<string> allEuisAssemblies;
            allEuisAssemblies = AssetDatabase.global.GetAssets<ExecutableAsset>().SelectMany(x => Directory.GetFiles(Path.GetDirectoryName(x.GetMeta().path), "*.euis", SearchOption.AllDirectories).Select(x => x.Trim())).ToHashSet();
            if (BasicIMod.DebugMode) LogUtils.DoLog($"EUIS Files found:\n {string.Join("\n", allEuisAssemblies)}");
            foreach (var assemblyPath in allEuisAssemblies)
            {
                try
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Loading assemblyPath: {assemblyPath}");
                    var assembly = Assembly.LoadFile(assemblyPath);
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Loaded assemblyPath: {assembly}");
                    if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog($"Instance = {EuisScreenManager.Instance}");
                    EuisScreenManager.Instance.DoWhenReady(() =>
                    {
                        var apps = BridgeUtils.GetAllLoadableClassesByTypeName<IEUISAppRegister, IEUISAppRegister>(() => new EUISAppRegisterCurrent(), assembly);
                        if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"[ESOS] Apps to load from '{{0}}':\n {string.Join("\n", apps.Select(x => x.DisplayName))}", assemblyPath);
                        foreach (var app in apps)
                        {
                            EuisScreenManager.Instance.RegisterApplication(app);
                        }
                    });
                    EuisScreenManager.Instance.DoOnceWhenReady((x) =>
                    {
                        var mods = BridgeUtils.GetAllLoadableClassesByTypeName<IEUISModRegister, IEUISModRegister>(() => new EUISModRegisterCurrent(), assembly);
                        if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"[ESOS] Apps to load from '{{0}}':\n {string.Join("\n", mods.Select(x => x.ModAcronym))}", assemblyPath);
                        foreach (var mod in mods)
                        {
                            EuisScreenManager.Instance.RegisterModActions(mod, x);
                        }
                    });
                }
                catch (Exception e)
                {
                    LogUtils.DoErrorLog("Error loading euis assembly file @ {0}", e, assemblyPath);
                }
            }
        }

        public override BasicModData CreateSettingsFile()
        {
            var modData = new EuisModData(this);
            return modData;
        }


        private class EUISAppRegisterCurrent : IEUISAppRegister
        {
            public string AppName { get; set; }

            public string DisplayName { get; set; }

            public string UrlJs { get; set; }

            public string UrlCss { get; set; }

            public string UrlIcon { get; set; }

            public string ModderIdentifier { get; set; }
            public string ModAcronym { get; set; }
            public string ModAppIdentifier { get; set; }
        }
        private class EUISModRegisterCurrent : IEUISModRegister
        {
            public Action<Action<string, object[]>> OnGetEventEmitter { get; set; }

            public Action<Action<string, Delegate>> OnGetEventsBinder { get; set; }

            public Action<Action<string, Delegate>> OnGetCallsBinder { get; set; }
            public string ModderIdentifier { get; set; }

            public string ModAcronym { get; set; }
        }

    }
}
