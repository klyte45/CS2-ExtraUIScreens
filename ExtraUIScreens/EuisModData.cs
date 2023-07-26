using Belzont.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ExtraUIScreens
{
    [XmlRoot("EuisData")]
    public class EuisModData : IBasicModData
    {
        [XmlAttribute]
        public bool DebugMode { get; set; }
        [XmlAttribute]
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
        [XmlElement]
        public List<string> DisabledAppsByMonitor { get; set; }

        public string[][] GetDisabledAppsByMonitor() => DisabledAppsByMonitor is null ? (new string[0][]) : (DisabledAppsByMonitor?.Select(x => (x ?? "").Split(',')).ToArray());
        public void SetDisabledAppsByMonitor(string[][] newVal) => DisabledAppsByMonitor = newVal?.Select(x => string.Join(",", x ?? new string[0])).ToList();
    }
}

