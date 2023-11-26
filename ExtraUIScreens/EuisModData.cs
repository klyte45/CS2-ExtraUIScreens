using Belzont.Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if !THUNDERSTORE
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
#else
using Game.UI.Menu;
using Game.UI.Widgets;
using IMod = Belzont.Interfaces.BasicIMod;
#endif

namespace ExtraUIScreens
{

#if THUNDERSTORE
    [XmlRoot("EuisData")]
#else
    [FileLocation("K45_EUIS")]
    [SettingsUITabOrder(kMonitorsTab, kAboutTab)]
#endif
    public class EuisModData : BasicModData
    {
        public const string kMonitorsTab = "MonitorsData";
        public static ExtraUIScreensMod EuisInstance { get; private set; }
        public static EuisModData EuisDataInstance { get; private set; }

#if THUNDERSTORE
        public EuisModData() : base()
        {
            EuisInstance = ExtraUIScreensMod.Instance;
#else
        public EuisModData(IMod mod) : base(mod)
        {
            EuisInstance = mod as ExtraUIScreensMod;
#endif
            EuisDataInstance = this;
        }
#if THUNDERSTORE
        [XmlAttribute]
#else
        [SettingsUIHidden]
#endif
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
                EuisScreenManager.Instance.InitializeMonitor(displayId);
            }
            else
            {
                InactiveMonitors |= (1 << displayId);
            }
            EuisScreenManager.Instance.OnMonitorActivityChanged();
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
#if !THUNDERSTORE
            Apply();
#endif
        }


        public override void OnSetDefaults() { }

        public bool IsMonitor1Unavailable() => Display.displays.Length < 2;
        public bool IsMonitor2Unavailable() => Display.displays.Length < 3;
        public bool IsMonitor3Unavailable() => Display.displays.Length < 4;
        public bool IsMonitor4Unavailable() => Display.displays.Length < 5;
        public bool IsMonitor5Unavailable() => Display.displays.Length < 6;
        public bool IsMonitor6Unavailable() => Display.displays.Length < 7;
        public bool IsMonitor7Unavailable() => Display.displays.Length < 8;

#if THUNDERSTORE
        [XmlIgnore]
#else
        [SettingsUISection(kMonitorsTab, null)]
#endif
        public bool UseMonitor1 { get => IsMonitorActive(0); set => SetMonitorActive(0, value); }
#if THUNDERSTORE
        [XmlIgnore]
#else
        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor1Unavailable))]
#endif
        public bool UseMonitor2 { get => IsMonitorActive(1); set => SetMonitorActive(1, value); }
#if THUNDERSTORE
        [XmlIgnore]
#else
        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor2Unavailable))]
#endif
        public bool UseMonitor3 { get => IsMonitorActive(2); set => SetMonitorActive(2, value); }
#if THUNDERSTORE
        [XmlIgnore]
#else
        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor3Unavailable))]
#endif
        public bool UseMonitor4 { get => IsMonitorActive(3); set => SetMonitorActive(3, value); }
#if THUNDERSTORE
        [XmlIgnore]
#else
        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor4Unavailable))]
#endif
        public bool UseMonitor5 { get => IsMonitorActive(4); set => SetMonitorActive(4, value); }
#if THUNDERSTORE
        [XmlIgnore]
#else
        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor5Unavailable))]
#endif
        public bool UseMonitor6 { get => IsMonitorActive(5); set => SetMonitorActive(5, value); }
#if THUNDERSTORE
        [XmlIgnore]
#else
        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor6Unavailable))]
#endif
        public bool UseMonitor7 { get => IsMonitorActive(6); set => SetMonitorActive(6, value); }
#if THUNDERSTORE
        [XmlIgnore]
#else
        [SettingsUISection(kMonitorsTab, null)]
        [SettingsUIHideByCondition(typeof(EuisModData), nameof(IsMonitor7Unavailable))]
#endif
        public bool UseMonitor8 { get => IsMonitorActive(7); set => SetMonitorActive(7, value); }
    }
}

