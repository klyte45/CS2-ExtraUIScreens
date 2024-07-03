using Belzont.Interfaces;
using Belzont.Utils;
using cohtml;
using cohtml.InputSystem;
using cohtml.Net;
using Colossal.Logging;
using Colossal.UI;
using Colossal.UI.Binding;
using Game.Input;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Localization;
using Game.UI.Menu;
using K45EUIS_Ext;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Action = System.Action;
using DefaultResourceHandler = Colossal.UI.DefaultResourceHandler;
using UISystem = Colossal.UI.UISystem;

namespace ExtraUIScreens
{

    public class EuisScreenManager : MonoBehaviour
    {
        public static EuisScreenManager Instance { get; private set; }
        private static PropertyInfo HostLocationsMap = typeof(DefaultResourceHandler).GetProperty("HostLocationsMap", ReflectionUtils.allFlags);
        private int ReadyCount { get; set; }
        private int ReadyCountTarget { get; set; } = -1;
        private bool Ready { get; set; }
        private event Action OnReady;
        private event Action<int> OnceOnReady;

        private UIInputSystem[] inputSystemArray;
        private UISystem[] uiSystemArray;

        private bool showMonitor1;
        private ProxyAction actionToggleScreen1;

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
            actionToggleScreen1 = EuisModData.EuisDataInstance.GetAction(EuisModData.kActionToggleScreen1);
        }
        private static readonly FieldInfo fieldUIInput = typeof(GameManager).GetField("m_UIInputSystem", RedirectorUtils.allFlags);

        public void Update()
        {
            if (lastMonitorId < 2 && EuisModData.EuisDataInstance.IsMonitorActive(0) && actionToggleScreen1.WasPressedThisFrame())
            {
                showMonitor1 = !showMonitor1;
                uiSystemArray[0].UIViews[0].View.TriggerEvent("k45::euis.toggleMon1", showMonitor1);
                UpdateActiveMonitor();
            }
        }

        public void InitializeMonitor(int displayId) => StartCoroutine(InitializeMonitor_impl(displayId));

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
            if (defaultResourceHandlerDisplays is null)
            {
                defaultResourceHandlerDisplays = new EuisResourceHandler();
                var defaultRH = defView.uiSystem.resourceHandler as DefaultResourceHandler;
                var currentHosts = HostLocationsMap.GetValue(defaultRH) as IDictionary<string, HashSet<string>>;
                HostLocationsMap.SetValue(defaultResourceHandlerDisplays, currentHosts);
                defaultResourceHandlerDisplays.coroutineHost = defaultRH.coroutineHost;
                defaultResourceHandlerDisplays.userImagesManager = defaultRH.userImagesManager;
                //defaultResourceHandlerDisplays.System = CohtmlUISystem.GetDefaultUISystem();
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
            var baseUri = new UriBuilder() { Scheme = "coui", Host = BasicIMod.Instance.CouiHost, Path = @"UI/esos/index.html" }.Uri.AbsoluteUri;
            //var baseUri = new UriBuilder { Scheme = "http", Host = "localhost", Port = 8425, Path = "index.html" }.Uri.AbsoluteUri;
            yield return 0;

            var modView = uiSystemArray[displayId].CreateView(baseUri, settings, cam);
            modView.enabled = true;
            modView.Listener.ReadyForBindings += () =>
            {
                modView.View.BindCall("k45::euis.getMonitorId", new Func<int>(() => thisMonitorId));
                modView.View.BindCall("k45::euis.getQuantityMonitors", new Func<int>(() => Display.displays.Length));
                modView.View.BindCall("k45::euis.getDisabledAppsByDisplay", new Func<string[][]>(() => EuisModData.EuisDataInstance.GetDisabledAppsByMonitor()));
                modView.View.RegisterForEvent("k45::euis.getMonitorEnabledApplcations", new Action<int>((monitorId) => StartCoroutine(GetMonitorEnabledApplcations(monitorId, modView.View))));
                modView.View.BindCall("k45::euis.saveAppSelectionAsDefault", new Action(() => SaveAppSelectionAsDefault()));
                modView.View.BindCall("k45::euis.removeAppButton", new Action<string, int>(RemoveAppFromMonitor));
                modView.View.BindCall("k45::euis.reintroduceAppButton", new Action<string, int>(AddAppToMonitor));
                modView.View.BindCall("k45::euis.getActiveMonitorMask", new Func<int>(() => EuisModData.EuisDataInstance.InactiveMonitors));
                modView.View.BindCall("k45::euis.frontEndReady", new Action<int>((x) =>
                {
                    if (!Ready)
                    {
                        ReadyCount++;
                        Ready = ReadyCount >= ReadyCountTarget;
                        if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"ReadyCount = {ReadyCount}, Target = {ReadyCountTarget}");
                        if (Ready)
                        {
                            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Ready = {Ready}");
                            OnReady?.Invoke();
                            OnceOnReady?.Invoke(-1);
                        }
                    }
                    else
                    {
                        OnReady?.Invoke();
                        OnceOnReady?.Invoke(x);
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
            OnBeforeSceneLoad += () => StartCoroutine(DisableViewOnLoad(modView, displayId != 0 ? cam : null));
            GameManager.instance.onGameLoadingComplete += (x, y) => { if (displayId != 0) cam.enabled = true; modView.enabled = true; modView.View.LoadURL(modView.url); };
        }

        private event Action OnBeforeSceneLoad;

        public void RunOnBeforeSceneLoad()
        {
            OnBeforeSceneLoad?.Invoke();
        }

        private IEnumerator DisableViewOnLoad(UIView view, Camera cam)
        {
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
            RunningAppSelectionDefaultSave = StartCoroutine(SaveAppSelectionAsDefault_impl());
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
        private Barrier m_GlobalBarrier;
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
            if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog("inputPos: {0} relPos: {1} mouseCurrent: ??", Input.mousePosition, relativePos);
            if (targetIdx != 0 || showMonitor1) { targetIdx++; }
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

        private readonly HashSet<IEUISAppRegister> registeredApplications = new();

        private void SendEventToApp(string modderId, string appName, string eventName, params object[] args)
        {
            if (GameManager.instance.isLoading) return;
            if (eventName.StartsWith("^"))
            {
                var appNameFull = $"@{modderId}/{appName}";
                switch (eventName)
                {
                    case EUISSpecialEventEmitters.kOpenModAppCmd:

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

        internal void RegisterApplication(IEUISAppRegister appRegisterData)
        {
            if (ValidateAppRegister(appRegisterData))
            {
                for (int i = -1; i < uiSystemArray.Length; i++)
                {
                    UISystem uiSys = i < 0 ? GameManager.instance.userInterface.view.uiSystem : uiSystemArray[i];
                    if (uiSys != null)
                    {
                        if (uiSys.UIViews[0].View.IsReadyForBindings())
                        {
                            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Sending app for monitor {i + 1}: {appRegisterData.GetFullAppName()}");
                            uiSys.UIViews[0].View.TriggerEvent("k45::euis.registerApplication", appRegisterData.GetFullAppName(), appRegisterData.DisplayName, appRegisterData.UrlJs, appRegisterData.UrlCss, appRegisterData.UrlIcon);
                            registeredApplications.Add(appRegisterData);
                        }
                    }
                }
            }
        }
        internal void RegisterModActions(IEUISModRegister modRegisterData, int targetMonitor)
        {
            if (ValidateModRegister(modRegisterData))
            {
                var internalAppName = modRegisterData.ModAcronym;
                var modderId = modRegisterData.ModderIdentifier;
                modRegisterData.OnGetEventEmitter((string eventName, object[] args) => SendEventToApp(modderId, internalAppName, eventName, args));
                modRegisterData.OnGetCallsBinder((string eventName, Delegate action) => RegisterCall(modRegisterData, eventName, action, targetMonitor));
                modRegisterData.OnGetEventsBinder((string eventName, Delegate action) => RegisterEvent(modRegisterData, eventName, action, targetMonitor));
            }
        }

        private readonly Dictionary<string, RawValueBinding> m_cachedBindings = new();

        private void RegisterCall(IEUISModRegister appRegisterData, string callName, Delegate action, int targetMonitor)
        {
            if (targetMonitor < 0)
            {
                for (int i = -1; i < uiSystemArray.Length; i++)
                {
                    RegisterCallInDisplay(appRegisterData, callName, action, i);
                }
            }
            else
            {
                RegisterCallInDisplay(appRegisterData, callName, action, targetMonitor - 1);
            }
        }

        private void RegisterCallInDisplay(IEUISModRegister appRegisterData, string callName, Delegate action, int displayId)
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

        private void RegisterEvent(IEUISModRegister appRegisterData, string eventName, Delegate action, int targetMonitor)
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

        private void RegisterEventToDisplay(IEUISModRegister appRegisterData, string eventName, Delegate action, int displayId)
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


        private bool ValidateAppRegister(IEUISAppRegister appRegisterData)
        {
            if (!Regex.IsMatch(appRegisterData.ModderIdentifier, "^[a-z0-9\\-]{3,10}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': Modder name must be 3-10 characters and must contain only characters in this regex: [a-z0-9\\-]");
                return false;
            }
            if (!Regex.IsMatch(appRegisterData.ModAcronym, "^[a-z0-9]{2,5}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': Mod acronym must be 2-5 characters and must contain only characters in this regex: [a-z0-9]");
                return false;
            }
            if (!Regex.IsMatch(appRegisterData.ModAppIdentifier, "^[a-z0-9\\-]{0,24}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': App name must be 0-24 characters and must contain only characters in this regex: [a-z0-9\\-]");
                return false;
            }
            if (!appRegisterData.UrlJs.StartsWith("coui://") && !appRegisterData.UrlJs.StartsWith("http://localhost"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': The application js must be registered in the game ui system (under coui://) or under localhost if under development (starting with http://localhost).");
                return false;
            }
            if (!appRegisterData.UrlCss.StartsWith("coui://") && !appRegisterData.UrlCss.StartsWith("http://localhost"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': The application css must be registered in the game ui system (under coui://) or under localhost if under development (starting with http://localhost).");
                return false;
            }
            if (!appRegisterData.UrlIcon.StartsWith("coui://"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': The application icon must be registered in the game ui system (under coui://).");
                return false;
            }
            return true;
        }
        internal static bool ValidateModRegister(IEUISModRegister appRegisterData)
        {
            if (!Regex.IsMatch(appRegisterData.ModderIdentifier, "^[a-z0-9\\-]{3,10}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': Modder name must be 3-10 characters and must contain only characters in this regex: [a-z0-9\\-]");
                return false;
            }
            if (!Regex.IsMatch(appRegisterData.ModAcronym, "^[a-z0-9]{2,5}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': Mod acronym must be 2-5 characters and must contain only characters in this regex: [a-z0-9]");
                return false;
            }
            if (appRegisterData.OnGetCallsBinder is null)
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': OnGetCallsBinder must not be null!");
                return false;
            }
            if (appRegisterData.OnGetEventsBinder is null)
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': OnGetEventsBinder must not be null!");
                return false;
            }
            if (appRegisterData.OnGetEventEmitter is null)
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': OnGetEventEmitter must not be null!");
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

        public void DoWhenReady(Action action)
        {
            OnReady += action;
            if (Ready)
            {
                action();
            }
        }
        public void DoOnceWhenReady(Action<int> action)
        {
            OnceOnReady += action;
        }

        internal void OnInterfaceStyleChanged()
        {
            DoWithEachMonitorView(View => View.TriggerEvent("k45::euis.interfaceStyle->", SharedSettings.instance.userInterface.interfaceStyle));
        }
    }
}
