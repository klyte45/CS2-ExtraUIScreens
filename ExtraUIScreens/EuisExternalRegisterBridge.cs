using System;
using Unity.Entities;

namespace ExtraUIScreens
{
    public static class EuisExternalRegisterBridge
    {
        public static bool RegisterAppForEUIS(string modderIdentifier, string modAcronym, string modAppIdentifier, string displayName, string urlJs, string urlCss, string urlIcon)
            => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EuisScreenManager>().InitialRegisterApplication(modderIdentifier, modAcronym, modAppIdentifier, displayName, urlJs, urlCss, urlIcon);
        public static bool RegisterModForEUIS(string modderIdentifier, string modAcronym, Action<Action<string, object[]>> registerEventsEmmiter, Action<Action<string, Delegate>> registerEventsCalls, Action<Action<string, Delegate>> registerBindCalls)
            => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EuisScreenManager>().InitialRegisterModActions(modderIdentifier, modAcronym, registerEventsEmmiter, registerEventsCalls, registerBindCalls);
    }
}
