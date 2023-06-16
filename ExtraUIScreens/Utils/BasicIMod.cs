using Belzont.Utils;
using Colossal;
using Colossal.Localization;
using Game;
using Game.Modding;
using Game.Reflection;
using Game.SceneFlow;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using FileInfo = Colossal.IO.AssetDatabase.Internal.FileInfo;

namespace Belzont.Interfaces
{
    public abstract class BasicIMod
    {
        public void OnCreateWorld(UpdateSystem updateSystem)
        {
            LoadLocales();
            DoOnCreateWorld(updateSystem);
        }
        public abstract void OnDispose();
        public void OnLoad()
        {
            Instance = this;
            KFileUtils.EnsureFolderCreation(ModSettingsRootFolder);
            LoadModData();

            Redirector.PatchAll();
            DoOnLoad();
        }

        public abstract void DoOnCreateWorld(UpdateSystem updateSystem);

        public abstract void DoOnLoad();
        public string ModDataFilePath => Path.Combine(ModSettingsRootFolder, "settings.xml");
        public abstract void SaveModData();
        public abstract void LoadModData();
        public abstract IBasicModData BasicModData { get; }

        #region Saved shared config
        public static bool DebugMode
        {
            get => Instance.BasicModData.DebugMode;
            set
            {
                Instance.BasicModData.DebugMode = value;
                Instance.SaveModData();
            }
        }
        public static string CurrentSaveVersion { get; }
        #endregion

        #region Old CommonProperties Overridable
        public abstract string SimpleName { get; }
        public virtual string IconName { get; } = "ModIcon";
        public abstract string SafeName { get; }
        public virtual string GitHubRepoPath { get; } = "";
        public abstract string Acronym { get; }
        public abstract Color ModColor { get; }
        public virtual string[] AssetExtraDirectoryNames { get; } = new string[0];
        public virtual string[] AssetExtraFileNames { get; } = new string[] { };
        public virtual string ModRootFolder => KFileUtils.BASE_FOLDER_PATH + SafeName;
        public abstract string Description { get; }

        #endregion

        #region Old CommonProperties Static
        public static BasicIMod Instance { get; private set; }

        private static ulong m_modId;
        public static ulong ModId
        {
            get
            {
                if (m_modId == 0)
                {
                    m_modId = ulong.MaxValue;
                }
                return m_modId;
            }
        }

        private static string m_rootFolder;

        public static string ModSettingsRootFolder
        {
            get
            {
                if (m_rootFolder == null)
                {
                    m_rootFolder = Instance.ModRootFolder;
                }
                return m_rootFolder;
            }
        }

        public static string ModInstallFolder
        {
            get
            {
                if (m_modInstallFolder is null)
                {
                    var modsInfo = (List<ModManager.ModInfo>)(typeof(ModManager).GetField("m_ModsInfo", RedirectorUtils.allFlags).GetValue(GameManager.instance.m_ModManager));

                    var fieldAssemblyInfo = typeof(ModManager.ModInfo).GetField("m_Assembly", RedirectorUtils.allFlags);
                    var fieldFileInfo = typeof(ModManager.ModInfo).GetField("fileInfo", RedirectorUtils.allFlags);

                    ModManager.ModInfo thisInfo = null;

                    foreach (var info in modsInfo)
                    {
                        var infoAssembly = (Assembly)fieldAssemblyInfo.GetValue(info);
                        if (infoAssembly == Instance.GetType().Assembly)
                        {
                            thisInfo = info;
                            break;
                        }
                    }

                    if (thisInfo is null)
                    {
                        throw new Exception("This mod info was not found!!!!");
                    }

                    FileInfo fileInfo = (FileInfo)fieldFileInfo.GetValue(thisInfo);
                    m_modInstallFolder = Path.GetDirectoryName(fileInfo.fullPath);
                }
                return m_modInstallFolder;
            }
        }

        private static string m_modInstallFolder;

        public static string MinorVersion => Instance.MinorVersion_;
        public static string MajorVersion => Instance.MajorVersion_;
        public static string FullVersion => Instance.FullVersion_;
        public static string Version => Instance.Version_;
        #endregion

        #region Old CommonProperties Fixed
        public string Name => $"{SimpleName} {Version}";
        public string GeneralName => $"{SimpleName} (v{Version})";

        private readonly Dictionary<string, Coroutine> TasksRunningOnController = new Dictionary<string, Coroutine>();
        private string MinorVersion_ => MajorVersion + "." + GetType().Assembly.GetName().Version.Build;
        private string MajorVersion_ => GetType().Assembly.GetName().Version.Major + "." + GetType().Assembly.GetName().Version.Minor;
        private string FullVersion_ => MinorVersion + " r" + GetType().Assembly.GetName().Version.Revision;
        private string Version_ =>
           GetType().Assembly.GetName().Version.Minor == 0 && GetType().Assembly.GetName().Version.Build == 0
                    ? GetType().Assembly.GetName().Version.Major.ToString()
                    : GetType().Assembly.GetName().Version.Build > 0
                        ? MinorVersion
                        : MajorVersion;

        #endregion

        internal OptionsUISystem.Page BuildModPage()
        {
            OptionsUISystem.Page page = new OptionsUISystem.Page
            {
                id = $"K45_{Instance.Acronym}"
            };
            List<OptionsUISystem.Section> sections = page.sections;
            sections.AddRange(GenerateModOptionsSections());
            OptionsUISystem.Section section = new OptionsUISystem.Section
            {
                id = $"K45.{Instance.Acronym}.About",
                items = new List<IWidget>
                {
                    AddBoolField($"K45.{Acronym}.DebugMode", new DelegateAccessor<bool>(()=>DebugMode,(x)=>DebugMode=x)),
                    AddValueField($"K45.{Instance.Acronym}.Version",()=>FullVersion)
                }
            };
            sections.Add(section);
            return page;
        }

        protected abstract IEnumerable<OptionsUISystem.Section> GenerateModOptionsSections();

        private void LoadLocales()
        {
            foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
            {
                var file = Path.Combine(ModInstallFolder, $"i18n/{lang}.csv");
                if (File.Exists(file))
                {
                    var i18nFile = new MemorySource(File.ReadAllLines(file).Select(x => x.Split('\t')).ToDictionary(x => x[0], x => x.ElementAtOrDefault(1)));
                    GameManager.instance.localizationManager.AddSource(lang, i18nFile);             
                }

                GameManager.instance.localizationManager.AddSource(lang, new ModGenI18n());
            }
        }

        private static bool GetButtonsGroup(string groupName, out ButtonRow buttons, Button item)
        {
            var AutomaticSettingsSButtonGroups = typeof(AutomaticSettings).GetField("sButtonGroups", RedirectorUtils.allFlags);
            var sButtonGroups = (Dictionary<string, ButtonRow>)AutomaticSettingsSButtonGroups.GetValue(null);
            if (sButtonGroups.TryGetValue(groupName, out buttons))
            {
                Button[] array = new Button[buttons.children.Length + 1];
                buttons.children.CopyTo(array, 0);
                array[array.Length - 1] = item;
                buttons.children = array;
                return false;
            }
            buttons = new ButtonRow
            {
                children = new Button[]
                {
                    item
                }
            };
            sButtonGroups.Add(groupName, buttons);
            return true;
        }

        protected IWidget AddOptionField<T>(string path, string groupName, Action<T> onSet, T value, Func<bool> disabledFn = null, string warningI18n = null)
        {
            return GetButtonsGroup(groupName, out ButtonRow item, new ButtonWithConfirmation
            {
                path = path,
                displayName = $"Options.OPTION[{path}]",
                action = delegate
                {
                    onSet(value);
                },
                disabled = disabledFn,
                confirmationMessage = (warningI18n != null ? new LocalizedString?(LocalizedString.Id(warningI18n)) : null)
            })
                ? item
                : (IWidget)null;
        }

        protected IWidget AddBoolField(string path, DelegateAccessor<bool> accessor, Func<bool> disabledFn = null)
        {
            return new ToggleField
            {
                path = path,
                displayName = $"Options.OPTION[{path}]",
                accessor = accessor,
                disabled = disabledFn
            };
        }

        protected IWidget AddValueField(string path, Func<string> getter)
        {
            return new ValueField
            {
                path = path,
                displayName = $"Options.OPTION[{path}]",
                accessor = new DelegateAccessor<string>(getter, (x) => { })
            };
        }

        private class ModGenI18n : IDictionarySource
        {

            public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
            {
                return new Dictionary<string, string>
                {
                    [$"Options.SECTION[K45_{Instance.Acronym}]"] = Instance.GeneralName,
                    [$"Options.TAB[K45.{Instance.Acronym}.About]"] = "About",
                    [$"Options.OPTION[K45.{Instance.Acronym}.DebugMode]"] = "Debug Mode",
                    [$"Options.OPTION_DESCRIPTION[K45.{Instance.Acronym}.DebugMode]"] = "Turns on the log debugging for this mod",
                    [$"Options.OPTION[K45.{Instance.Acronym}.Version]"] = "Mod Version",
                    [$"Options.OPTION_DESCRIPTION[K45.{Instance.Acronym}.Version]"] = "The current mod version.\n\nThe 4th digit being higher than 1000 indicates beta version.",
                };
            }

            public void Unload()
            {
            }
        }
    }

    public abstract class BasicIMod<D> : BasicIMod where D : class, IBasicModData
    {
        public abstract D CreateNewModData();

        public D ModData { get; private set; }

        public sealed override IBasicModData BasicModData => ModData;

        public sealed override void SaveModData()
        {
            File.WriteAllText(ModDataFilePath, XmlUtils.DefaultXmlSerialize(ModData));
        }
        public sealed override void LoadModData()
        {
            if (File.Exists(ModDataFilePath))
            {
                try
                {
                    ModData = XmlUtils.DefaultXmlDeserialize<D>(File.ReadAllText(ModDataFilePath));
                }
                catch
                {
                    LogUtils.DoWarnLog("The settings.json was invalid! Generating new data.");
                    ModData = CreateNewModData();
                }
            }
            else
            {
                ModData = CreateNewModData();
            }
        }
    }
}
