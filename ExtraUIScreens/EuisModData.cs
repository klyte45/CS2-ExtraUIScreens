using Belzont.Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.Input;
using UnityEngine.InputSystem;


namespace ExtraUIScreens
{

    [FileLocation("ModsData\\Klyte45Mods\\ExtraUIScreens\\settings")]
    [SettingsUITabOrder(kMonitorsTab, kAboutTab)]

    public class EuisModData : BasicModData
    {
        public const string kMonitorsTab = "MonitorsData";
        public const string kActionToggleScreen1 = "K45_EUIS_ToggleMonitor1";
        public static ExtraUIScreensMod EuisInstance { get; private set; }
        public static EuisModData EuisDataInstance { get; private set; }


        public EuisModData(IMod mod) : base(mod)
        {
            EuisInstance = mod as ExtraUIScreensMod;

            EuisDataInstance = this;
            RegisterKeyBindings();
        }

        [SettingsUIHidden]

        public int InactiveMonitors { get; set; }
        public bool IsMonitorActive(int displayIdx)
        {
            return (InactiveMonitors & (1 << displayIdx)) == 0;
        }
        public void SetMonitorActive(int displayId, bool newValue)
        {
            if (newValue)
            {
                InactiveMonitors &= ~(1 << displayId);
                EuisScreenManager.Instance?.InitializeMonitor(displayId);
            }
            else
            {
                InactiveMonitors |= (1 << displayId);
            }
            EuisScreenManager.Instance?.OnMonitorActivityChanged();
        }

        private string[][] m_DisabledApps { get; set; }
        public List<string> DisabledAppsByMonitor
        {
            get => m_DisabledApps?.Select(x => string.Join(",", new HashSet<string>(x ?? new string[0]))).ToList();
            set => m_DisabledApps = value?.Select(x => new HashSet<string>(x?.Split(",") ?? new string[0]).ToArray())?.ToArray() ?? new string[0][];
        }
        public string[][] GetDisabledAppsByMonitor() => m_DisabledApps ?? new string[0][];
        public void SetDisabledAppsByMonitor(string[][] newVal)
        {
            m_DisabledApps = newVal;
        }


        public override void OnSetDefaults() { }

        public bool IsMonitor1Unavailable() => Display.displays.Length < 2;
        public bool IsMonitor2Unavailable() => Display.displays.Length < 3;
        public bool IsMonitor3Unavailable() => Display.displays.Length < 4;
        public bool IsMonitor4Unavailable() => Display.displays.Length < 5;
        public bool IsMonitor5Unavailable() => Display.displays.Length < 6;
        public bool IsMonitor6Unavailable() => Display.displays.Length < 7;
        public bool IsMonitor7Unavailable() => Display.displays.Length < 8;
        public bool IsMonitor1Disabled() => !IsMonitorActive(0);


        [SettingsUISection(kMonitorsTab, null)]
        public bool UseMonitor1 { get => IsMonitorActive(0); set => SetMonitorActive(0, value); }

        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIKeyboardBinding(BindingKeyboard.Tab, kActionToggleScreen1, ctrl: true)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor1Disabled))]
        public ProxyBinding Monitor1ToggleAction { get; set; }



        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor1Unavailable))]
        public bool UseMonitor2 { get => IsMonitorActive(1); set => SetMonitorActive(1, value); }

        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor2Unavailable))]
        public bool UseMonitor3 { get => IsMonitorActive(2); set => SetMonitorActive(2, value); }

        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor3Unavailable))]
        public bool UseMonitor4 { get => IsMonitorActive(3); set => SetMonitorActive(3, value); }

        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor4Unavailable))]
        public bool UseMonitor5 { get => IsMonitorActive(4); set => SetMonitorActive(4, value); }

        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor5Unavailable))]
        public bool UseMonitor6 { get => IsMonitorActive(5); set => SetMonitorActive(5, value); }

        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor6Unavailable))]
        public bool UseMonitor7 { get => IsMonitorActive(6); set => SetMonitorActive(6, value); }

        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor7Unavailable))]
        public bool UseMonitor8 { get => IsMonitorActive(7); set => SetMonitorActive(7, value); }
    }
}

