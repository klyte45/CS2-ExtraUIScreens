#if !BEPINEX_CS2
using Belzont.Interfaces;
using Belzont.Utils;
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
        public static string kBaseUrlVos = new UriBuilder() { Scheme = "coui", Host = BasicIMod.Instance.CouiHost, Path = @"UI/vos" }.Uri.AbsoluteUri;
        // public static string kBaseUrlVos = new UriBuilder { Scheme = "http", Host = "localhost", Port = 8450, Path = @"" }.Uri.AbsoluteUri[..^1];
        private readonly HashSet<IEUISOverlayRegister> registeredApplications = new();
        private event Action OnReady;
        private event Action OnceOnReady;
        private bool Ready;
        private bool HasBeenReadyOnce;

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
                    Ready = false;
                    ResetVanillaOverlay();
                }
            };
            GameManager.instance.onGameLoadingComplete += (x, y) => StartCoroutine(ResetVanillaOverlay(y));
        }

        public void ResetVanillaOverlay()
        {
            StartCoroutine(ResetVanillaOverlay(GameManager.instance.gameMode));
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
            var defView = GameManager.instance.userInterface.view;
            if (eventName.StartsWith("^"))
            {
                var appNameFull = $"@{modderId}/{appName}";
                switch (eventName)
                {
                    case EUISSpecialEventEmitters.kOpenModAppCmd:
                        {
                            defView.View.TriggerEvent("k45::euis.switchToApp", appNameFull);
                            break;
                        }
                }
            }
            else
            {
                var eventNameFull = $"{modderId}::{appName}.{eventName}";
                LogUtils.DoLog("Calling event VOS: {0}", eventNameFull);
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
        private IEnumerator InitializeVOS_Impl()
        {
            yield return 0;
            var defView = GameManager.instance.userInterface.view;
            defView.enabled = true;
            var onReadyForBindings = () =>
            {
                LogUtils.DoLog("[VOS] Registering calls");
                defView.View.BindCall("k45::euis.getDisabledAppsByDisplay", new Func<string[][]>(() =>
                {
                    LogUtils.DoLog("[VOS] k45::euis.getDisabledAppsByDisplay");
                    return EuisModData.EuisDataInstance.GetDisabledAppsByMonitor();
                }));
                defView.View.BindCall("k45::euis.vosFrontEndReady", new Action(() =>
                {
                    OnReady?.Invoke();
                    OnceOnReady?.Invoke();
                }));
                InjectScript();
            };

            defView.Listener.ReadyForBindings += onReadyForBindings;
            if (defView.View.IsReadyForBindings())
            {
                onReadyForBindings();
            }
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
        public void DoWhenReady(Action action)
        {
            OnReady += action;
            if (Ready)
            {
                action();
            }
        }
        public void DoOnceWhenReady(Action action)
        {
            OnceOnReady += action;
        }

        public IEnumerator ResetVanillaOverlay(Game.GameMode mode)
        {
            LogUtils.DoWarnLog($"mode = {mode}");
            if (!Ready && (mode == Game.GameMode.Game || mode == Game.GameMode.Editor))
            {
                if (!HasBeenReadyOnce)
                {
                    yield return InitializeVOS_Impl();
                    HasBeenReadyOnce = true;
                    yield break;
                }
            }

        }

        private static void InjectScript()
        {
            GameManager.instance.userInterface.view.View.ExecuteScript($@"(function() {{ const scriptX = document.createElement('script'); scriptX.src = ""{kBaseUrlVos}/vos-loader.js""; document.head.appendChild(scriptX)}})()");

        }
    }
}
#endif