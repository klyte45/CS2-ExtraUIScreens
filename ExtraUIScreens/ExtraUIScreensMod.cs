using Belzont.Interfaces;
using Belzont.Utils;
using Game;
using K45EUIS_Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if THUNDERSTORE
using Game.UI.Menu;
using Game.UI.Widgets;
using BepInEx;
#else
using System.IO;
using Colossal.IO.AssetDatabase;
using Game.Modding;
#endif

namespace ExtraUIScreens
{
#if THUNDERSTORE
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class ExtraUIScreensMod : BaseUnityPlugin, IBasicIMod<EuisModData>
    {
        protected UpdateSystem UpdateSystem { get; set; }
        public string IconName { get; } = "ModIcon";
        public string GitHubRepoPath { get; } = "";
        public string[] AssetExtraDirectoryNames { get; } = new string[0];
        public string[] AssetExtraFileNames { get; } = new string[] { };
        public Dictionary<string, Coroutine> TasksRunningOnController { get; } = new Dictionary<string, Coroutine>();
        IEnumerable<OptionsUISystem.Section> IBasicIMod.GenerateModOptionsSections() => GenerateModOptionsSections();
        public EuisModData CreateNewModData() => new(Instance);
        UpdateSystem IBasicIMod.UpdateSystem { get; set; }

#else
    public class ExtraUIScreensMod : BasicIMod, IMod
    {
#endif
        public static
#if !THUNDERSTORE 
            new
#endif 
            ExtraUIScreensMod Instance => (ExtraUIScreensMod)IBasicIMod.Instance;

        public
#if !THUNDERSTORE
            override
#endif
            string SimpleName => "Extra UI Screens Mod";

        public
#if !THUNDERSTORE
            override
#endif
            string SafeName => "ExtraScreens";

        public
#if !THUNDERSTORE
            override
#endif
             string Acronym => "EUIS";

        public
#if !THUNDERSTORE
            override
#endif
             string Description => "Adds extra screens!";


        public
#if !THUNDERSTORE
            override
#endif
            void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            euisGO.AddComponent<EuisVanillaOverlayManager>();
        }



        public
#if !THUNDERSTORE
            override
#endif
             void OnDispose()
        {
            GameObject.Destroy(euisGO);
        }

        GameObject euisGO;

        public
#if !THUNDERSTORE
            override
#endif
            void DoOnLoad()
        {
            euisGO = new GameObject("EUIS");
            euisGO.AddComponent<EuisScreenManager>();
            LoadExtraScreenFromMods();
        }

        private static void LoadExtraScreenFromMods()
        {
            HashSet<string> allEuisAssemblies;
#if THUNDERSTORE
            allEuisAssemblies = new HashSet<string>();
#else
            allEuisAssemblies = AssetDatabase.global.GetAssets<ExecutableAsset>().SelectMany(x => Directory.GetFiles(Path.GetDirectoryName(x.GetMeta().path), "*.euis", SearchOption.AllDirectories).Select(x => x.Trim())).ToHashSet();
#endif
            LogUtils.DoLog($"EUIS Files found:\n {string.Join("\n", allEuisAssemblies)}");
            foreach (var assemblyPath in allEuisAssemblies)
            {
                try
                {
                    LogUtils.DoLog($"Loading assemblyPath: {assemblyPath}");
                    var assembly = Assembly.LoadFile(assemblyPath);
                    LogUtils.DoLog($"Loaded assemblyPath: {assembly}");
                    LogUtils.DoLog($"Instance = {EuisScreenManager.Instance}");
                    EuisScreenManager.Instance.DoWhenReady(() =>
                    {
                        var apps = BridgeUtils.GetAllLoadableClassesByTypeName<IEUISAppRegister, IEUISAppRegister>(() => new EUISAppRegisterCurrent(), assembly);
                        if (IBasicIMod.DebugMode) LogUtils.DoLog($"Apps to load from '{{0}}':\n {string.Join("\n", apps.Select(x => x.DisplayName))}", assemblyPath);
                        foreach (var app in apps)
                        {
                            EuisScreenManager.Instance.RegisterApplication(app);
                        }
                    });
                    EuisScreenManager.Instance.DoOnceWhenReady((x) =>
                    {
                        var mods = BridgeUtils.GetAllLoadableClassesByTypeName<IEUISModRegister, IEUISModRegister>(() => new EUISModRegisterCurrent(), assembly);
                        if (IBasicIMod.DebugMode) LogUtils.DoLog($"Apps to load from '{{0}}':\n {string.Join("\n", mods.Select(x => x.ModAcronym))}", assemblyPath);
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

        public
#if !THUNDERSTORE
            override
#endif
            BasicModData CreateSettingsFile()
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


#if THUNDERSTORE
        protected IEnumerable<OptionsUISystem.Section> GenerateModOptionsSections()
        {
            return new[]
            {
                new OptionsUISystem.Section
                {
                    id = "K45.EUIS.MonitorsData",
                    items = GetMonitorsMenuOptions()
                }
           };
        }
        private List<IWidget> GetMonitorsMenuOptions()
        {

            return Display.displays.Select((_, i) =>
            {
                var displayId = i;
                return this.AddBoolField($"K45.EUIS.UseMonitor{displayId + 1}", new Game.Reflection.DelegateAccessor<bool>(() => IBasicIMod<EuisModData>.ModData.IsMonitorActive(displayId), (x) =>
                {
                    IBasicIMod<EuisModData>.ModData.SetMonitorActive(displayId, x);
                    (this as IBasicIMod<EuisModData>).SaveModData();
                }));
            }).ToList();

        }
#endif
    }
}
