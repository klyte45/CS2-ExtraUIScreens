using Belzont.Utils;
using Game.UI.Menu;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Belzont.Interfaces
{
    public class OptionsUISystemRedirector : Redirector, IRedirectable
    {
        public void Awake()
        {
            AddRedirect(typeof(OptionsUISystem).GetMethod("OnCreate", RedirectorUtils.allFlags), null, GetType().GetMethod("AfterOnCreate", RedirectorUtils.allFlags));
        }
        private static PropertyInfo OptionsUISystemOptions = typeof(OptionsUISystem).GetProperty("options", RedirectorUtils.allFlags);

        private static void AfterOnCreate(OptionsUISystem __instance)
        {
            var optionsList = ((List<OptionsUISystem.Page>)OptionsUISystemOptions.GetValue(__instance));
            optionsList.Add(BasicIMod.Instance.BuildModPage());
        }
    }
}
