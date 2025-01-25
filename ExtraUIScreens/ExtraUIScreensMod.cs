using Belzont.Interfaces;
using Game;
using Game.Modding;

namespace ExtraUIScreens
{
    public class ExtraUIScreensMod : BasicIMod, IMod
    {
        public static new ExtraUIScreensMod Instance => (ExtraUIScreensMod)BasicIMod.Instance;

        public override string Acronym => "EUIS";
        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            updateSystem.UpdateAt<EuisScreenManager>(SystemUpdatePhase.UIUpdate);
        }
        public override void OnDispose() { }
        public override void DoOnLoad() { }
        public override BasicModData CreateSettingsFile()
        {
            var modData = new EuisModData(this);
            return modData;
        }

        public string Host => CouiHost;
    }
}
