using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.UI.Binding;
using Game;
using Game.Modding;
using Game.UI.Menu;
using Game.UI.Widgets;
using K45EUIS_Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraUIScreens
{
    public class ExtraUIScreensMod : BasicIMod<EUISModData>, IMod
    {
        public static new ExtraUIScreensMod Instance => (ExtraUIScreensMod)BasicIMod.Instance;
        public override string SimpleName => "Extra UI Screens Mod";

        public override string SafeName => "ExtraScreens";

        public override string Acronym => "EUIS";

        public override string Description => "Adds extra screens!";

        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            EUISScreenManager.Instance.DoWhenReady(() =>
            {
                var apps = BridgeUtils.GetAllLoadableClassesByTypeName<IEUISAppRegister, IEUISAppRegister>(() => new EUISAppRegisterCurrent());
                foreach (var app in apps)
                {
                    EUISScreenManager.Instance.RegisterApplication(app);
                }
            });
        }

        public override void OnDispose()
        {
            GameObject.Destroy(EUISScreenManager.Instance);
        }


        public override void DoOnLoad()
        {
            new GameObject().AddComponent<EUISScreenManager>();
        }

        public override EUISModData CreateNewModData()
        {
            return new EUISModData();
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


            public Action<Action<string, object[]>> OnGetEventEmitter { get; set; }

            public Action<Action<string, Delegate>> OnGetEventsBinder { get; set; }

            public Action<Action<string, Delegate>> OnGetCallsBinder { get; set; }

            public Action<Func<string, Action<IJsonWriter>, RawValueBinding>> OnGetRawValueBindingRegisterer { get; set; }
        }
    }
}