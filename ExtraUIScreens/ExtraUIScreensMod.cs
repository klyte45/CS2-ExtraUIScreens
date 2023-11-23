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
#if THUNDERSTORE
using Game.UI.Menu;
using Game.UI.Widgets;
using BepInEx;
#else
using Colossal.IO.AssetDatabase;
#endif

namespace ExtraUIScreens
{
#if THUNDERSTORE

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class EUIBepinexPlugin : BaseUnityPlugin
    {
        public void Awake()
        {
            LogUtils.LogsEnabled = false;
            LogUtils.Logger = Logger;
            LogUtils.DoInfoLog($"STARTING MOD!");
            Redirector.PatchAll();
        }
    }

    public class ExtraUIScreensMod : BasicIMod<EuisModData>, IMod
    {
#else
    public class ExtraUIScreensMod : BasicIMod, IMod
    {
#endif
        public static new ExtraUIScreensMod Instance => (ExtraUIScreensMod)BasicIMod.Instance;

        public override string SimpleName => "Extra UI Screens Mod";

        public override string SafeName => "ExtraScreens";

        public override string Acronym => "EUIS";

        public override string Description => "Adds extra screens!";


        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
#if !THUNDERSTORE
            euisGO.AddComponent<EuisVanillaOverlayManager>();
#endif
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
            LoadExtraScreenFromMods();
        }

        private static void LoadExtraScreenFromMods()
        {
            HashSet<string> allEuisAssemblies;
#if THUNDERSTORE
            allEuisAssemblies = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(ModInstallFolder), ".."), "*.euis", SearchOption.AllDirectories).Select(x => x.Trim()).ToHashSet();
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
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Apps to load from '{{0}}':\n {string.Join("\n", apps.Select(x => x.DisplayName))}", assemblyPath);
                        foreach (var app in apps)
                        {
                            EuisScreenManager.Instance.RegisterApplication(app);
                        }
                    });
                    EuisScreenManager.Instance.DoOnceWhenReady((x) =>
                    {
                        var mods = BridgeUtils.GetAllLoadableClassesByTypeName<IEUISModRegister, IEUISModRegister>(() => new EUISModRegisterCurrent(), assembly);
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Apps to load from '{{0}}':\n {string.Join("\n", mods.Select(x => x.ModAcronym))}", assemblyPath);
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

#if !THUNDERSTORE
        public override BasicModData CreateSettingsFile()
        {
            var modData = new EuisModData(this);
            return modData;
        }
#endif


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
        protected override IEnumerable<OptionsUISystem.Section> GenerateModOptionsSections()
        {
            return new[]
            {
                new OptionsUISystem.Section
                {
                    id = ModData.GetPathForAggroupator("MonitorsData"),
                    items = GetMonitorsMenuOptions()
                }
           };
        }
        private List<IWidget> GetMonitorsMenuOptions()
        {

            return Display.displays.Select((_, i) =>
            {
                var displayId = i;
                return this.AddBoolField(ModData.GetPathForOption($"UseMonitor{displayId + 1}"), new Game.Reflection.DelegateAccessor<bool>(() => BasicIMod<EuisModData>.ModData.IsMonitorActive(displayId), (x) =>
                {
                    BasicIMod<EuisModData>.ModData.SetMonitorActive(displayId, x);
                    (this as BasicIMod<EuisModData>).SaveModData();
                }));
            }).ToList();

        }

        public override EuisModData CreateNewModData() => new();

#endif
    }
}
