#if !THUNDERSTORE
using Belzont.Utils;
using cohtml.Net;
using Game.SceneFlow;
using K45EUIS_Ext;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ExtraUIScreens
{
    public class EuisVanillaOverlayManager : MonoBehaviour
    {
        public static bool DebugMode { get; set; } = true;

        public static EuisVanillaOverlayManager Instance { get; private set; }
        //public static string kBaseUrlVos = new UriBuilder() { Scheme = "coui", Host = IBasicIMod.Instance.CouiHost, Path = @"UI/vos" }.Uri.AbsoluteUri;
        public static string kBaseUrlVos = new UriBuilder { Scheme = "http", Host = "localhost", Port = 8450, Path = @"" }.Uri.AbsoluteUri[..^1];
        private readonly HashSet<IEUISOverlayRegister> registeredApplications = new();
        private event Action OnReady;
        private event Action<int> OnceOnReady;

        public void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this);
        }

        public void Start()
        {
            ResetVanillaOverlay();
            var defView = GameManager.instance.userInterface.view;
            defView.Listener.FinishLoad += (x) =>
            {
                if (x?.StartsWith("coui://GameUI/index.html") ?? false)
                {
                    ResetVanillaOverlay();
                }
            };
            GameManager.instance.onGameLoadingComplete += (x, y) => ResetVanillaOverlay(y);
        }

        public void ResetVanillaOverlay()
        {
            ResetVanillaOverlay(GameManager.instance.gameMode);
        }

        internal void RegisterApplication(IEUISOverlayRegister appRegisterData)
        {
            var defView = GameManager.instance.userInterface.view;
            if (ValidateAppRegister(appRegisterData))
            {
                if (DebugMode) LogUtils.DoLog($"Sending app for main game overlay: {appRegisterData.GetFullAppName()}");
                defView.View.TriggerEvent("k45::euis.registerVosApplication", appRegisterData.GetFullAppName(), appRegisterData.DisplayName, appRegisterData.UrlJs, appRegisterData.UrlCss, appRegisterData.UrlIcon);
                registeredApplications.Add(appRegisterData);
            }
        }

        private bool ValidateAppRegister(IEUISOverlayRegister overlayRegisterData)
        {
            if (!Regex.IsMatch(overlayRegisterData.ModderIdentifier, "^[a-z0-9\\-]{3,10}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{overlayRegisterData.GetType().FullName}': Modder name must be 3-10 characters and must contain only characters in this regex: [a-z0-9\\-]");
                return false;
            }
            if (!Regex.IsMatch(overlayRegisterData.ModAcronym, "^[a-z0-9]{2,5}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{overlayRegisterData.GetType().FullName}': Mod acronym must be 2-5 characters and must contain only characters in this regex: [a-z0-9]");
                return false;
            }
            if (!Regex.IsMatch(overlayRegisterData.ModAppIdentifier, "^[a-z0-9\\-]{0,24}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{overlayRegisterData.GetType().FullName}': App name must be 0-24 characters and must contain only characters in this regex: [a-z0-9\\-]");
                return false;
            }
            if (!overlayRegisterData.UrlJs.StartsWith("coui://") && !overlayRegisterData.UrlJs.StartsWith("http://localhost"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{overlayRegisterData.GetType().FullName}': The overlay js must be registered in the game ui system (under coui://) or under localhost if under development (starting with http://localhost).");
                return false;
            }
            if (!overlayRegisterData.UrlCss.StartsWith("coui://") && !overlayRegisterData.UrlCss.StartsWith("http://localhost"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{overlayRegisterData.GetType().FullName}': The overlay css must be registered in the game ui system (under coui://) or under localhost if under development (starting with http://localhost).");
                return false;
            }
            if (!overlayRegisterData.UrlIcon.StartsWith("coui://"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{overlayRegisterData.GetType().FullName}': The overlay icon must be registered in the game ui system (under coui://).");
                return false;
            }
            return true;
        }

        internal void SendEventToApp(string modderId, string appName, string eventName, params object[] args)
        {
            var eventNameFull = $"{modderId}::{appName}.{eventName}";
            LogUtils.DoLog("Calling event VOS: {0}", eventNameFull);
            var defView = GameManager.instance.userInterface.view;
            {
                switch (args is null ? 0 : args.Length)
                {
                    case 0: defView.View.TriggerEvent(eventNameFull); break;
                    case 1: defView.View.TriggerEvent(eventNameFull, args[0]); break;
                    case 2: defView.View.TriggerEvent(eventNameFull, args[0], args[1]); break;
                    case 3: defView.View.TriggerEvent(eventNameFull, args[0], args[1], args[2]); break;
                    case 4: defView.View.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3]); break;
                    case 5: defView.View.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4]); break;
                    case 6: defView.View.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4], args[5]); break;
                    case 7: defView.View.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4], args[5], args[6]); break;
                    default: defView.View.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]); break;
                }

            }
        }
        internal void RegisterCall(IEUISModRegister appRegisterData, string callName, Delegate action)
        {
            var callAddress = $"{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}.{callName}";
            var defView = GameManager.instance.userInterface.view;
            LogUtils.DoLog("Sending call '{0}' for register @ VOS", callAddress);
            void registerCall()
            {
                defView.View.BindCall(callAddress, action);
                LogUtils.DoLog("Registered call '{0}' @ VOS", callAddress);
            }
            if (defView.View.IsReadyForBindings())
            {
                registerCall();
            }
            else
            {
                defView.Listener.ReadyForBindings += registerCall;
            }
        }

        internal void RegisterEvent(IEUISModRegister appRegisterData, string eventName, Delegate action)
        {
            var eventAddress = $"{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}.{eventName}";
            var defView = GameManager.instance.userInterface.view;
            LogUtils.DoLog("Sending event '{0}' for register @ VOS", eventAddress);
            void registerCall()
            {
                defView.View.RegisterForEvent(eventAddress, action);
                LogUtils.DoLog("Registered for event '{0}' @ VOS", eventAddress);
            }
            if (defView.View.IsReadyForBindings())
            {
                registerCall();
            }
            else
            {
                defView.Listener.ReadyForBindings += registerCall;
            }
        }
        public void InitializeVOS() => StartCoroutine(InitializeVOS_Impl());
        private IEnumerator InitializeVOS_Impl()
        {
            yield return 0;
            var defView = GameManager.instance.userInterface.view;
            defView.enabled = true;
            defView.Listener.ReadyForBindings += () =>
            {
                defView.View.BindCall("k45::euis.getDisabledAppsByDisplay", new Func<string[][]>(() => EuisModData.EuisDataInstance.GetDisabledAppsByMonitor()));
                defView.View.BindCall("k45::euis.getActiveMonitorMask", new Func<int>(() => EuisModData.EuisDataInstance.InactiveMonitors));
                defView.View.BindCall("k45::euis.frontEndReady", new Action<int>((x) =>
                {
                    OnReady?.Invoke();
                    OnceOnReady?.Invoke(x);
                }));
            };
            GameManager.instance.localizationManager.onActiveDictionaryChanged += () => defView.View.TriggerEvent("k45::euis.localeChanged");
            GameManager.instance.onGameLoadingComplete += (x, y) => { defView.View.TriggerEvent("k45::euis.deselectAll"); };
        }
        internal void RegisterModActions(IEUISModRegister modRegisterData)
        {
            if (EuisScreenManager.ValidateModRegister(modRegisterData))
            {
                var internalAppName = modRegisterData.ModAcronym;
                var modderId = modRegisterData.ModderIdentifier;
                modRegisterData.OnGetEventEmitter((string eventName, object[] args) => SendEventToApp(modderId, internalAppName, eventName, args));
                modRegisterData.OnGetCallsBinder((string eventName, Delegate action) => RegisterCall(modRegisterData, eventName, action));
                modRegisterData.OnGetEventsBinder((string eventName, Delegate action) => RegisterEvent(modRegisterData, eventName, action));
            }
        }

        public void ResetVanillaOverlay(Game.GameMode mode)
        {
            LogUtils.DoWarnLog($"mode = {mode}");
            if (mode == Game.GameMode.Game || mode == Game.GameMode.Editor)
            {

                GameManager.instance.userInterface.view.View.ExecuteScript(
    $@"(function() {{ 
let createEuisContainer = function(){{
    const k = document.createElement(""div"");
    k.id = ""EUIS_Container"";
    function onComplete () {{
        k.insertAdjacentHTML('afterbegin',this.responseText )
        document.body.appendChild(k);
    }}

    var oReq = new XMLHttpRequest();
    oReq.addEventListener(""load"", onComplete);
    oReq.open(""GET"", ""{kBaseUrlVos}/include.html"");
    oReq.send();
}}

if(document.getElementById(""EUIS_Container"")) {{ 
   return
}}else{{

    const css = document.createElement('link');
    css.href=""{kBaseUrlVos}/base.css"";
    css.rel = ""stylesheet""
    const script = document.createElement('script');
    script.src = ""{kBaseUrlVos}/dependencies/system.min.js"";
    const script2 = document.createElement('script');
    script2.src = ""{kBaseUrlVos}/dependencies/single-spa.min.js"";
    const script3 = document.createElement('script');
    script3.src = ""{kBaseUrlVos}/dependencies/import-map-overrides.js"";
    const script4 = document.createElement('script');
    script4.src = ""{kBaseUrlVos}/dependencies/amd.min.js"";
    const script5 = document.createElement('script');
    script5.type=""systemjs-importmap""
    script5.insertAdjacentText('afterbegin', `
        {{
          ""imports"": {{
            ""single-spa"": ""{kBaseUrlVos}/dependencies/single-spa.min.js"",
            ""react"": ""{kBaseUrlVos}/dependencies/react.production.min.js"",
            ""react-dom"": ""{kBaseUrlVos}/dependencies/react-dom.production.min.js"",
            ""@k45-euis/vos-config"": ""{kBaseUrlVos}/k45-euis-vos-config.js""
          }}
        }}`);


    script.onload = function ()  {{console.log( document.head.appendChild(script2))}}
    script2.onload = function () {{console.log( document.head.appendChild(script3))}}
    script3.onload = function () {{console.log( document.head.appendChild(script4))}}
    script4.onload = function () {{console.log( document.head.appendChild(script5));console.log( createEuisContainer())}}
   console.log( document.head.appendChild(script))
   console.log( document.head.appendChild(css))

}}
}})()

");
            }

        }

    }
}
#endif