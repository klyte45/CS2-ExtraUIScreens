﻿using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using Game.UI.Menu;
using Game.UI.Widgets;
using K45EUIS_Ext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ExtraUIScreens
{
    public class ExtraUIScreensMod : BasicIMod<EuisModData>, IMod
    {
        public static new ExtraUIScreensMod Instance => (ExtraUIScreensMod)BasicIMod.Instance;
        public override string SimpleName => "Extra UI Screens Mod";

        public override string SafeName => "ExtraScreens";

        public override string Acronym => "EUIS";

        public override string Description => "Adds extra screens!";

        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            LoadExtraScreenFromMods();
        }



        public override void OnDispose()
        {
            GameObject.Destroy(EuisScreenManager.Instance);
        }


        public override void DoOnLoad()
        {
            new GameObject().AddComponent<EuisScreenManager>();
        }

        public override EuisModData CreateNewModData()
        {
            return new EuisModData();
        }

        protected override IEnumerable<OptionsUISystem.Section> GenerateModOptionsSections()
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
                  return AddBoolField($"K45.EUIS.UseMonitor{displayId + 1}", new Game.Reflection.DelegateAccessor<bool>(() => ModData.IsMonitorActive(displayId), (x) =>
                  {
                      ModData.SetMonitorActive(displayId, x);
                      SaveModData();
                  }));
              }).ToList();

        }
        private static void LoadExtraScreenFromMods()
        {
            string[] allEuisAssemblies = Directory.GetFiles(AssetDatabase.kModsRootPath, "*.euis", SearchOption.AllDirectories);
            LogUtils.DoLog($"EUIS Files found:\n {string.Join("\n", allEuisAssemblies)}");
            foreach (var assemblyPath in allEuisAssemblies)
            {
                try
                {
                    var assembly = Assembly.LoadFile(assemblyPath);
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