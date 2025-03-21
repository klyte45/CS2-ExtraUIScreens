﻿using Belzont.Interfaces;
using Belzont.Utils;
using cohtml.InputSystem;
using cohtml.Net;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Colossal.UI;
using Game.Input;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Localization;
using Game.UI.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;
using Action = System.Action;
using DefaultResourceHandler = Colossal.UI.DefaultResourceHandler;
using UISystem = Colossal.UI.UISystem;

namespace ExtraUIScreens
{

    public partial class EuisScreenManager : SystemBase
    {
        private static readonly PropertyInfo HostLocationsMap = typeof(DefaultResourceHandler).GetProperty("HostLocationsMap", ReflectionUtils.allFlags);
        private static readonly FieldInfo DatabaseHostLocationsMap = typeof(DefaultResourceHandler).GetRuntimeFields().Where(a => Regex.IsMatch(a.Name, $"\\A<DatabaseHostLocationsMap>k__BackingField\\Z")).FirstOrDefault();
        private static EuisScreenManager instance;
        private int ReadyCount { get; set; }
        private int ReadyCountTarget { get; set; } = -1;
        private bool Ready { get; set; }

        private UIInputSystem[] inputSystemArray;
        private UISystem[] uiSystemArray;

        public bool ShowMonitor1 { get; private set; }
        public ProxyAction ActionToggleScreen1 { get; private set; }

        private InputBarrier NonEuisInputBarrier;

        protected override void OnCreate()
        {
            if (instance != null) return;
            instance = this;
            base.OnCreate();
            uiSystemArray = new UISystem[8];
            ReadyCount = 0;
            ReadyCountTarget = Display.displays.Length;

            var defView = GameManager.instance.userInterface.view;
            var uiSys = GameManager.instance.userInterface.view.uiSystem;
            inputSystemArray = new UIInputSystem[9];
            inputSystemArray[0] = GameManager.UIInputSystem;
            m_GlobalBarrier = InputManager.instance.CreateGlobalBarrier("K45_EUIS");
            defView.Listener.NodeMouseEvent += (a, eventData, c, d) =>
            {
                UpdateInputSystem(eventData, 0);
                return Actions.ContinueHandling;
            };
            for (int i = 0; i < Display.displays.Length; i++)
            {
                if (EuisModData.EuisDataInstance.IsMonitorActive(i))
                {
                    InitializeMonitor(i);
                }
                else
                {
                    ReadyCountTarget--;
                }
            }
            ActionToggleScreen1 = EuisModData.EuisDataInstance.GetAction(EuisModData.kActionToggleScreen1);
            var allMaps = typeof(InputManager).GetField("m_Maps", RedirectorUtils.allFlags).GetValue(InputManager.instance) as Dictionary<string, ProxyActionMap>;
            NonEuisInputBarrier = new InputBarrier("K45::EUIS-Barrier", allMaps.Select(x => x.Value).Where(x => x != ActionToggleScreen1.map).ToArray(), InputManager.DeviceType.All);
        }
        private static readonly FieldInfo fieldUIInput = typeof(GameManager).GetField("m_UIInputSystem", RedirectorUtils.allFlags);

        protected override void OnUpdate()
        {
            ActionToggleScreen1.shouldBeEnabled = true;
            if (lastMonitorId < 2 && EuisModData.EuisDataInstance.IsMonitorActive(0) && ActionToggleScreen1.WasPressedThisFrame())
            {
                ShowMonitor1 = !ShowMonitor1;
                uiSystemArray[0].UIViews[0].View.TriggerEvent("k45::euis.toggleMon1", ShowMonitor1);
                UpdateActiveMonitor();
                NonEuisInputBarrier.blocked = ShowMonitor1;
            }
        }

        public void InitializeMonitor(int displayId) => GameManager.instance.StartCoroutine(InitializeMonitor_impl(displayId));

        private EuisResourceHandler defaultResourceHandlerDisplays;

        private IEnumerator InitializeMonitor_impl(int displayId)
        {
            var counter = (5 * displayId);
            while (counter-- > 0)
            {
                yield return 0;
            }

            if (displayId >= Display.displays.Length) yield break;
            if (displayId > 0 && Display.displays[displayId].active) yield break;
            if (displayId == 0 && uiSystemArray[0] != null) yield break;
            yield return 0;
            var defView = GameManager.instance.userInterface.view;
            if (displayId > 0) Display.displays[displayId].Activate();
            if (uiSystemArray[displayId] is not null) yield break;
            if (defaultResourceHandlerDisplays is null)
            {
                defaultResourceHandlerDisplays = new EuisResourceHandler();
                var defaultRH = defView.uiSystem.resourceHandler as DefaultResourceHandler;
                HostLocationsMap.SetValue(defaultResourceHandlerDisplays, HostLocationsMap.GetValue(defaultRH));
                DatabaseHostLocationsMap.SetValue(defaultResourceHandlerDisplays, DatabaseHostLocationsMap.GetValue(defaultRH));
                defaultResourceHandlerDisplays.coroutineHost = defaultRH.coroutineHost;
                defaultResourceHandlerDisplays.userImagesManager = defaultRH.userImagesManager;
            }
            uiSystemArray[displayId] = UIManager.instance.CreateUISystem(new UISystem.Settings
            {
                debuggerPort = 9450 + displayId,
                enableDebugger = LogUtils.IsLogLevelEnabled(Level.Debug),
                localizationManager = new UILocalizationManager(GameManager.instance.localizationManager),
                resourceHandler = defaultResourceHandlerDisplays
            });

            var thisMonitorId = displayId + 1;
            var inputSys = inputSystemArray[thisMonitorId] = new UIInputSystem(uiSystemArray[displayId]);
            yield return 0;
            Camera cam;
            UIView.Settings settings = UIView.Settings.New;
            settings.isTransparent = true;
            settings.acceptsInput = true;
            settings.pixelPerfect = true;
            if (displayId != 0)
            {
                var camgo = new GameObject();
                GameObject.DontDestroyOnLoad(camgo);
                cam = camgo.AddComponent<Camera>();
                cam.farClipPlane = 1;
                cam.nearClipPlane = 0.9f;
                cam.cameraType = CameraType.Preview;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.targetDisplay = displayId;
            }
            else
            {
                cam = defView.RenderingCamera;
                settings.enableBackdropFilter = false;
            }
            var baseUri = new UriBuilder() { Scheme = "coui", Host = EuisModData.EuisInstance.Host, Path = @"UI/esos/index.html" }.Uri.AbsoluteUri;
            //var baseUri = new UriBuilder { Scheme = "http", Host = "localhost", Port = 8425, Path = "index.html" }.Uri.AbsoluteUri;
            yield return 0;
            UIView modView = null;
            modView = uiSystemArray[displayId].CreateView("", settings, cam);
            modView.enabled = true;
            modView.Listener.ReadyForBindings += () =>
            {
                modView.View.BindCall("k45::euis.getMonitorId", new Func<int>(() => thisMonitorId));
                modView.View.BindCall("k45::euis.getQuantityMonitors", new Func<int>(() => Display.displays.Length));
                modView.View.BindCall("k45::euis.getDisabledAppsByDisplay", new Func<string[][]>(() => EuisModData.EuisDataInstance.GetDisabledAppsByMonitor()));
                modView.View.RegisterForEvent("k45::euis.getMonitorEnabledApplcations", new Action<int>((monitorId) => GameManager.instance.StartCoroutine(GetMonitorEnabledApplcations(monitorId, modView.View))));
                modView.View.BindCall("k45::euis.saveAppSelectionAsDefault", new Action(() => SaveAppSelectionAsDefault()));
                modView.View.BindCall("k45::euis.removeAppButton", new Action<string, int>(RemoveAppFromMonitor));
                modView.View.BindCall("k45::euis.reintroduceAppButton", new Action<string, int>(AddAppToMonitor));
                modView.View.BindCall("k45::euis.getActiveMonitorMask", new Func<int>(() => EuisModData.EuisDataInstance.InactiveMonitors));
                modView.View.BindCall("k45::euis.frontEndReady", new Action<int>((x) =>
                {
                    foreach (var mod in modsRegistered.Values)
                    {
                        RegisterModActions(mod, x);
                    }
                    OnMonitorActivityChanged();
                }));

                modView.View.BindCall("k45::euis.interfaceStyle", () => SharedSettings.instance.userInterface.interfaceStyle);
                modView.View.BindCall("k45::euis.getUnits", () => new OptionsUISystem.UnitSettings(SharedSettings.instance.userInterface));

            };
            modView.Listener.NodeMouseEvent += (a, eventData, c, d) =>
            {
                UpdateInputSystem(eventData, thisMonitorId);
                return Actions.ContinueHandling;
            };
            GameManager.instance.localizationManager.onActiveDictionaryChanged += () => modView.View.TriggerEvent("k45::euis.localeChanged");
            OnBeforeSceneLoad += () => GameManager.instance.StartCoroutine(DisableViewOnLoad(modView, displayId != 0 ? cam : null));
            GameManager.instance.onGameLoadingComplete += (x, y) => LoadScreen(displayId, cam, baseUri, modView);
            if (!GameManager.instance.isGameLoading)
            {
                LoadScreen(displayId, cam, baseUri, modView);
            }
        }

        private static void LoadScreen(int displayId, Camera cam, string baseUri, UIView modView)
        {
            if (displayId != 0) cam.enabled = true;
            modView.enabled = true;
            modView.View.LoadURL(baseUri);
        }

        private event Action OnBeforeSceneLoad;

        public void RunOnBeforeSceneLoad()
        {
            OnBeforeSceneLoad?.Invoke();
        }

        private IEnumerator DisableViewOnLoad(UIView view, Camera cam)
        {
            view.url = "";
            view.enabled = false;
            yield return 0;
            if (cam != null)
            {
                cam.enabled = false;
            }
        }


        private Coroutine RunningAppSelectionDefaultSave;

        private void SaveAppSelectionAsDefault()
        {
            if (RunningAppSelectionDefaultSave != null) return;
            RunningAppSelectionDefaultSave = GameManager.instance.StartCoroutine(SaveAppSelectionAsDefault_impl());
        }
        private IEnumerator SaveAppSelectionAsDefault_impl()
        {
            yield return 0;
            var result = new string[Display.displays.Length + 1][];
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Registered apps: {string.Join("|", registeredApplications.Select(x => x.GetFullAppName()))}");
            for (int i = 0; i <= Display.displays.Length; i++)
            {
                var wrapper = new Wrapper<string[]>();
                yield return DoCallToMonitorToGetApplicationsEnabled(i, wrapper);
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Apps enabled in display {i}: {string.Join("|", wrapper.Value ?? new string[0])}");
                result[i] = wrapper.Value != null
                    ? registeredApplications.Select(x => x.GetFullAppName()).Where(x => !wrapper.Value.Contains(x)).ToArray()
                    : new string[0];
            }
            EuisModData.EuisDataInstance.SetDisabledAppsByMonitor(result);
            RunningAppSelectionDefaultSave = null;
        }

        private int lastMonitorId = -1;
        private InputBarrier m_GlobalBarrier;
        private bool UpdateInputSystem(IMouseEventData b, int thisMonitorId)
        {
            if (b.Type == MouseEventData.EventType.MouseMove || b.Type == MouseEventData.EventType.MouseWheel || b.Type == MouseEventData.EventType.MouseDown)
            {
                UpdateActiveMonitor();
            }
            return thisMonitorId == lastMonitorId;
        }

        private void UpdateActiveMonitor()
        {
            var relativePos = Display.RelativeMouseAt(Input.mousePosition);
            var targetIdx = Mathf.RoundToInt(relativePos.z); ;
            //  if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog("inputPos: {0} relPos: {1} mouseCurrent: ??", Input.mousePosition, relativePos);
            if (targetIdx != 0 || ShowMonitor1) { targetIdx++; }
            if (targetIdx > 0) InputManager.instance.mouseOverUI = true;
            if (targetIdx != lastMonitorId)
            {
                lastMonitorId = targetIdx;
                fieldUIInput.SetValue(GameManager.instance, inputSystemArray[lastMonitorId]);
                for (int i = 0; i < inputSystemArray.Length; i++)
                {
                    UIInputSystem inputSys = inputSystemArray[i];
                    if (inputSys is null) continue;
                    if (lastMonitorId == i)
                    {
                        var fieldInputQueue = typeof(UIInputSystem).GetField("m_InputEvents", RedirectorUtils.allFlags);
                        ((Queue<GenericInputEvent>)fieldInputQueue.GetValue(inputSys)).Clear();
                        inputSys.Enable();
                    }
                    else
                    {
                        inputSys.Disable();
                    }
                }
                m_GlobalBarrier.blocked = lastMonitorId > 1;
            }

        }

        private IEnumerator GetMonitorEnabledApplcations(int monitorId, View callerView)
        {
            yield return 0;
            var wrapper = new Wrapper<string[]>();
            yield return DoCallToMonitorToGetApplicationsEnabled(monitorId, wrapper);
            callerView.TriggerEvent("k45::euis.getMonitorEnabledApplcations->" + monitorId, wrapper.Value);
        }


        private IEnumerator DoCallToMonitorToGetApplicationsEnabled(int monitorId, Wrapper<string[]> appList)
        {
            if (monitorId != 0)
            {
                if (monitorId < 0 || monitorId >= uiSystemArray.Length || uiSystemArray[monitorId - 1] is null)
                {
                    appList.Value = new string[0];
                    yield break;
                }
                while (uiSystemArray[monitorId - 1].UIViews.Count < 1)
                {
                    yield return new WaitForSeconds(0.2f);
                }
            }
            var targetView = monitorId == 0 ? GameManager.instance.userInterface.view.View : uiSystemArray[monitorId - 1].UIViews[0].View;
            while (!targetView.IsReadyForBindings())
            {
                yield return new WaitForSeconds(0.2f);
            }
            string[] localAppList = null;
            var eventRegister = targetView.RegisterForEvent("k45::euis.listActiveAppsInMonitor->", new Action<string[]>((x) =>
            {
                localAppList = x;
            }));
            targetView.TriggerEvent("k45::euis.listActiveAppsInMonitor", monitorId);
            for (int framesRemaining = 5; framesRemaining > 0; framesRemaining--)
            {
                if (localAppList != null) break;
                yield return 0;
            }
            targetView.UnregisterFromEvent(eventRegister);
            appList.Value = localAppList;
        }

        private void RemoveAppFromMonitor(string appName, int monitorId)
        {
            if (monitorId > 0 && monitorId <= uiSystemArray.Length && uiSystemArray[monitorId - 1] is not null)
            {
                DoWithEachMonitorView(View => View.TriggerEvent("k45::euis.removeAppButton->", monitorId, appName));
            }
        }


        private void AddAppToMonitor(string appName, int monitorId)
        {
            if (monitorId > 0 && monitorId <= uiSystemArray.Length && uiSystemArray[monitorId - 1] is not null)
            {
                DoWithEachMonitorView(View => View.TriggerEvent("k45::euis.reintroduceAppButton->", monitorId, appName));
            }
        }

        private void DoWithEachMonitorView(Action<View> action)
        {
            foreach (var uiSys in uiSystemArray)
            {
                if (uiSys != null && uiSys.UIViews[0].enabled && uiSys.UIViews[0].View.IsReadyForBindings())
                {
                    action(uiSys.UIViews[0].View);
                }
            }
        }

        private readonly HashSet<EUISAppRegister> registeredApplications = new();

        private const string kOpenModAppCmd = "^openApp";

        private void SendEventToApp(string modderId, string appName, string eventName, params object[] args)
        {
            if (GameManager.instance.isLoading) return;
            if (eventName.StartsWith("^"))
            {
                var appNameFull = $"@{modderId}/{appName}";
                switch (eventName)
                {
                    case kOpenModAppCmd:

                        View targetMonitor;
                        if (args.Length >= 1 && args[0] is int monitorNum)
                        {
                            targetMonitor = monitorNum < uiSystemArray.Length && monitorNum > 0 && uiSystemArray[monitorNum] is UISystem sys && sys.UIViews[0].enabled ? sys.UIViews[0].View
                                : monitorNum == 0 ? GameManager.instance.userInterface.view.View
                                : throw new Exception($"Invalid monitor index! It's out of bounds (1 to {uiSystemArray.Length - 1}) or it's not activated. Check the mod code! Value: {monitorNum}");
                        }
                        else
                        {
                            var inactiveAppsMonitor = EuisModData.EuisDataInstance.GetDisabledAppsByMonitor();
                            targetMonitor = uiSystemArray.Select((x, i) => (x, i)).FirstOrDefault((x) => x.i > 0 && (x.x?.UIViews[0].enabled ?? false) && (inactiveAppsMonitor is null || !inactiveAppsMonitor[x.i].Contains(appNameFull))).x?.UIViews[0].View;
                            if (targetMonitor is null)
                            {
                                throw new Exception($"The app {appName} is not active in any EUIS extra screen or there's no active EUIS extra screen! Check the mod code.");
                            }
                        }
                        targetMonitor.TriggerEvent("k45::euis.switchToApp", appNameFull);
                        break;
                }
            }
            else
            {
                var eventNameFull = $"{modderId}::{appName}.{eventName}";
                if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog("Calling event: {0}", eventNameFull);
                for (int i = -1; i < uiSystemArray.Length; i++)
                {
                    UISystem uiSys = i < 0 ? GameManager.instance.userInterface.view.uiSystem : uiSystemArray[i];
                    if (uiSys != null && uiSys.UIViews[0].enabled && uiSys.UIViews[0].View.IsReadyForBindings())
                    {
                        var targetView = uiSys.UIViews[0].View;
                        SendTriggerToView(args, eventNameFull, targetView);
                    }
                }
            }
        }

        private static void SendTriggerToView(object[] args, string eventNameFull, View targetView)
        {
            if (GameManager.instance.isLoading) return;
            var argsLenght = args is null ? 0 : args.Length;
            switch (argsLenght)
            {
                case 0: targetView.TriggerEvent(eventNameFull); break;
                case 1: targetView.TriggerEvent(eventNameFull, args[0]); break;
                case 2: targetView.TriggerEvent(eventNameFull, args[0], args[1]); break;
                case 3: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2]); break;
                case 4: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3]); break;
                case 5: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4]); break;
                case 6: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4], args[5]); break;
                case 7: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4], args[5], args[6]); break;
                case 8: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]); break;
                default:
                    LogUtils.DoWarnLog($"Too much arguments for trigger event! {argsLenght}: {args}");
                    break;
            }
        }

        private void RegisterApplication(EUISAppRegister appRegisterData, int targetMonitor)
        {

            for (int i = 0; i < uiSystemArray.Length; i++)
            {
                if (i >= 0 && i != targetMonitor) continue;
                UISystem uiSys = i == 0 ? GameManager.instance.userInterface.view.uiSystem : uiSystemArray[i - 1];
                if (uiSys != null)
                {
                    if (uiSys.UIViews[0].View.IsReadyForBindings())
                    {
                        if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Sending app for monitor {i}: {appRegisterData.GetFullAppName()}");
                        uiSys.UIViews[0].View.TriggerEvent("k45::euis.registerApplication", appRegisterData.GetFullAppName(), appRegisterData.DisplayName, appRegisterData.UrlJs, appRegisterData.UrlCss, appRegisterData.UrlIcon);
                        registeredApplications.Add(appRegisterData);
                    }
                }
            }
        }
        private void RegisterModActions(EUISModRegister modRegisterData, int targetMonitor)
        {

            var internalAppName = modRegisterData.ModAcronym;
            var modderId = modRegisterData.ModderIdentifier;
            modRegisterData.RegisterBindCalls((string eventName, Delegate action) => RegisterCall(modRegisterData, eventName, action, targetMonitor));
            modRegisterData.RegisterEventsCalls((string eventName, Delegate action) => RegisterEvent(modRegisterData, eventName, action, targetMonitor));
            if (targetMonitor >= 0)
            {
                modRegisterData.RegisterEventsEmiter((string eventName, object[] args) => SendEventToApp(modderId, internalAppName, eventName, args));
                foreach (var app in modRegisterData.Applications.Values)
                {
                    RegisterApplication(app, targetMonitor);
                }
            }
        }

        private void RegisterCall(EUISModRegister appRegisterData, string callName, Delegate action, int targetMonitor)
        {
            if (targetMonitor < 0)
            {
                for (int i = 0; i < uiSystemArray.Length; i++)
                {
                    RegisterCallInDisplay(appRegisterData, callName, action, i);
                }
            }
            else
            {
                RegisterCallInDisplay(appRegisterData, callName, action, targetMonitor - 1);
            }
        }

        private void RegisterCallInDisplay(EUISModRegister appRegisterData, string callName, Delegate action, int displayId)
        {
            UISystem uiSys = displayId < 0 ? GameManager.instance.userInterface.view.uiSystem : uiSystemArray[displayId];
            if (uiSys != null)
            {
                var monitorId = displayId + 1;
                var callAddress = $"{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}.{callName}";
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog("Sending call '{0}' for register @ Monitor #'{1}'", callAddress, monitorId);
                void registerCall()
                {
                    try
                    {
                        uiSys.UIViews[0].View.BindCall(callAddress, action);
                        if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog("Registered call '{0}' @ Monitor #'{1}'", callAddress, monitorId);
                    }
                    catch (Exception e)
                    {
                        LogUtils.DoErrorLog("Error linking call'{0}' @ Monitor #'{1}'", e, callAddress, monitorId);
                    }
                }
                var ready = uiSys.UIViews[0].View.IsReadyForBindings();
                if (ready)
                {
                    registerCall();
                }
                if (!ready || displayId < 0)
                {
                    uiSys.UIViews[0].Listener.ReadyForBindings += registerCall;
                }

            }
        }

        private void RegisterEvent(EUISModRegister appRegisterData, string eventName, Delegate action, int targetMonitor)
        {
            if (targetMonitor < 0)
            {
                for (int i = -1; i < uiSystemArray.Length; i++)
                {
                    RegisterEventToDisplay(appRegisterData, eventName, action, i);
                }
            }
            else
            {
                RegisterEventToDisplay(appRegisterData, eventName, action, targetMonitor - 1);
            }
        }

        private void RegisterEventToDisplay(EUISModRegister appRegisterData, string eventName, Delegate action, int displayId)
        {
            UISystem uiSys = displayId < 0 ? GameManager.instance.userInterface.view.uiSystem : uiSystemArray[displayId];
            if (uiSys != null)
            {
                var monitorId = displayId + 1;
                var eventAddress = $"{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}.{eventName}";
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog("Sending event '{0}' for register @ Monitor #'{1}'", eventAddress, monitorId);
                void registerCall()
                {
                    uiSys.UIViews[0].View.RegisterForEvent(eventAddress, action);
                    if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog("Registered for event '{0}' @ Monitor #'{1}'", eventAddress, monitorId);
                }
                var ready = uiSys.UIViews[0].View.IsReadyForBindings();
                if (ready)
                {
                    registerCall();
                }
                if (!ready || displayId < 0)
                {
                    uiSys.UIViews[0].Listener.ReadyForBindings += registerCall;
                }
            }
        }


        private bool ValidateAppRegister(EUISAppRegister appRegisterData)
        {
            if (!Regex.IsMatch(appRegisterData.ModderIdentifier, "^[a-z0-9\\-]{3,10}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for '{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}/{appRegisterData.ModAppIdentifier}':  Modder name must be 3-10 characters and must contain only characters in this regex: [a-z0-9\\-]");
                return false;
            }
            if (!Regex.IsMatch(appRegisterData.ModAcronym, "^[a-z0-9]{2,5}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for '{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}/{appRegisterData.ModAppIdentifier}':  Mod acronym must be 2-5 characters and must contain only characters in this regex: [a-z0-9]");
                return false;
            }
            if (!Regex.IsMatch(appRegisterData.ModAppIdentifier, "^[a-z0-9\\-]{0,24}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for '{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}/{appRegisterData.ModAppIdentifier}':  App name must be 0-24 characters and must contain only characters in this regex: [a-z0-9\\-]");
                return false;
            }
            if (!appRegisterData.UrlJs.StartsWith("coui://") && !appRegisterData.UrlJs.StartsWith("http://localhost"))
            {
                LogUtils.DoWarnLog($"Invalid app register for '{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}/{appRegisterData.ModAppIdentifier}':  The application js must be registered in the game ui system (under coui://) or under localhost if under development (starting with http://localhost).");
                return false;
            }
            if (!appRegisterData.UrlCss.StartsWith("coui://") && !appRegisterData.UrlCss.StartsWith("http://localhost"))
            {
                LogUtils.DoWarnLog($"Invalid app register for '{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}/{appRegisterData.ModAppIdentifier}':  The application css must be registered in the game ui system (under coui://) or under localhost if under development (starting with http://localhost).");
                return false;
            }
            if (!appRegisterData.UrlIcon.StartsWith("coui://"))
            {
                LogUtils.DoWarnLog($"Invalid app register for '{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}/{appRegisterData.ModAppIdentifier}':  The application icon must be registered in the game ui system (under coui://). Value: '{appRegisterData.UrlIcon}'");
                return false;
            }
            return true;
        }
        private static bool ValidateModRegister(EUISModRegister registerData)
        {
            if (!Regex.IsMatch(registerData.ModderIdentifier, "^[a-z0-9\\-]{3,10}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for mod '{registerData.ModderIdentifier}::{registerData.ModAcronym}': Modder name must be 3-10 characters and must contain only characters in this regex: [a-z0-9\\-]");
                return false;
            }
            if (!Regex.IsMatch(registerData.ModAcronym, "^[a-z0-9]{2,5}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for mod '{registerData.ModderIdentifier}::{registerData.ModAcronym}':  Mod acronym must be 2-5 characters and must contain only characters in this regex: [a-z0-9]");
                return false;
            }
            if (registerData.RegisterBindCalls is null)
            {
                LogUtils.DoWarnLog($"Invalid app register for mod '{registerData.ModderIdentifier}::{registerData.ModAcronym}':  OnGetCallsBinder must not be null!");
                return false;
            }
            if (registerData.RegisterEventsCalls is null)
            {
                LogUtils.DoWarnLog($"Invalid app register for mod '{registerData.ModderIdentifier}::{registerData.ModAcronym}':  OnGetEventsBinder must not be null!");
                return false;
            }
            if (registerData.RegisterEventsEmiter is null)
            {
                LogUtils.DoWarnLog($"Invalid app register for mod '{registerData.ModderIdentifier}::{registerData.ModAcronym}':  OnGetEventEmitter must not be null!");
                return false;
            }
            return true;
        }

        internal void UnregisterApplication(string appName)
        {
            for (int i = 0; i < uiSystemArray.Length; i++)
            {
                UISystem uiSys = uiSystemArray[i];
                if (uiSys != null)
                {
                    if (uiSys.UIViews[0].View.IsReadyForBindings())
                    {
                        if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Removing app from monitor {i + 1}: {appName}");
                        uiSys.UIViews[0].View.TriggerEvent("k45::euis.unregisterApplication", appName);
                    }
                }
            }
        }

        internal void OnMonitorActivityChanged()
        {
            for (int i = 0; i < uiSystemArray.Length; i++)
            {
                UISystem uiSys = uiSystemArray[i];
                if (uiSys != null)
                {
                    if (uiSys.UIViews[0].View.IsReadyForBindings())
                    {
                        uiSys.UIViews[0].View.TriggerEvent("k45::euis.activeMonitorMaskChange->", EuisModData.EuisDataInstance.InactiveMonitors);
                    }
                }
            }
        }

        internal void OnInterfaceStyleChanged()
        {
            DoWithEachMonitorView(View => View.TriggerEvent("k45::euis.interfaceStyle->", SharedSettings.instance.userInterface.interfaceStyle));
        }

        internal bool InitialRegisterApplication(string modderIdentifier, string modAcronym, string modAppIdentifier, string displayName, string urlJs, string urlCss, string urlIcon)
        {
            if (!modsRegistered.TryGetValue((modderIdentifier, modAcronym), out var modRegister)) throw new Exception($"The mod '{modAcronym}' from modder '{modderIdentifier}' wasn't registered yet. Register the mod before registering apps!");
            var app = new EUISAppRegister(modderIdentifier, modAcronym, modAppIdentifier, displayName, urlJs, urlCss, urlIcon);
            if (!modRegister.Applications.ContainsKey(modAppIdentifier) && ValidateAppRegister(app))
            {
                modRegister.Applications.Add(modAppIdentifier, app);
                return true;
            }
            return false;
        }

        internal bool InitialRegisterModActions(string modderIdentifier, string modAcronym, Action<Action<string, object[]>> registerEventsEmmiter, Action<Action<string, Delegate>> registerEventsCalls, Action<Action<string, Delegate>> registerBindCalls)
        {
            var modData = new EUISModRegister(modderIdentifier, modAcronym, registerEventsEmmiter, registerEventsCalls, registerBindCalls);
            if (!modsRegistered.ContainsKey((modderIdentifier, modAcronym)) && ValidateModRegister(modData))
            {
                modsRegistered[(modderIdentifier, modAcronym)] = modData;
                return true;
            }
            return false;
        }

        private readonly Dictionary<(string, string), EUISModRegister> modsRegistered = new();

        private record EUISAppRegister(string ModderIdentifier, string ModAcronym, string ModAppIdentifier, string DisplayName, string UrlJs, string UrlCss, string UrlIcon)
        {
            public string GetFullAppName() => $"@{ModderIdentifier}/{ModAcronym}{(ModAppIdentifier?.Length > 0 ? "-" : "")}{ModAppIdentifier}";
            public string GetInternalAppName() => $"{ModAcronym}{(ModAppIdentifier?.Length > 0 ? "-" : "")}{ModAppIdentifier}";
        }

        private record EUISModRegister(string ModderIdentifier, string ModAcronym, Action<Action<string, object[]>> RegisterEventsEmiter, Action<Action<string, Delegate>> RegisterEventsCalls, Action<Action<string, Delegate>> RegisterBindCalls)
        {
            public Dictionary<string, EUISAppRegister> Applications { get; } = new();
        }
    }
}
