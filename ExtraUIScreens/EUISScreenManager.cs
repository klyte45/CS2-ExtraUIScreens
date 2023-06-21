using Belzont.Interfaces;
using Belzont.Utils;
using cohtml.InputSystem;
using cohtml.Net;
using Colossal.UI;
using Game.Input;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Localization;
using K45EUIS_Ext;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UISystem = Colossal.UI.UISystem;

namespace ExtraUIScreens
{
    public class EUISScreenManager : MonoBehaviour
    {
        private const string MOD_HOST = "k45.euis";
        public static EUISScreenManager Instance { get; private set; }

        public static bool DebugMode { get; set; } = true;
        private int ReadyCount { get; set; }
        private int ReadyCountTarget { get; set; } = -1;
        private bool Ready { get; set; }
        private event Action OnReady;

        private UIInputSystem[] inputSystemArray;
        private UISystem[] uiSystemArray;

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
            if (Display.displays.Length > 1)
            {
                ReadyCount = 0;
                ReadyCountTarget = Display.displays.Length - 1;


                var defView = GameManager.instance.userInterface.view;
                var uiSys = GameManager.instance.userInterface.view.uiSystem;
                ((DefaultResourceHandler)uiSys.resourceHandler).HostLocationsMap.Add(MOD_HOST, new List<string> { BasicIMod.ModInstallFolder });
                inputSystemArray = new UIInputSystem[8];
                inputSystemArray[0] = GameManager.UIInputSystem;
                uiSystemArray[0] = uiSys;
                defView.Listener.NodeMouseEvent += (a, eventData, c, d) =>
                {
                    UpdateInputSystem(eventData, fieldUIInput, 0);
                    return Actions.ContinueHandling;
                };

                var interfaceSettings = SharedSettings.instance.userInterface;

                for (int i = 1; i < Display.displays.Length; i++)
                {
                    if (ExtraUIScreensMod.Instance.ModData.IsMonitorActive(i))
                    {
                        InitializeMonitor(i);
                    }
                    else
                    {
                        ReadyCountTarget--;
                    }
                }
            }
        }
        private static readonly FieldInfo fieldUIInput = typeof(GameManager).GetField("m_UIInputSystem", RedirectorUtils.allFlags);


        public void InitializeMonitor(int displayId) => StartCoroutine(InitializeMonitor_impl(displayId));

        private IEnumerator InitializeMonitor_impl(int displayId)
        {
            if (Display.displays[displayId].active) yield break;
            yield return 0;
            var defView = GameManager.instance.userInterface.view;
            Display.displays[displayId].Activate();
            uiSystemArray[displayId] = UIManager.instance.CreateUISystem(new UISystem.Settings
            {
                debuggerPort = 9450 + displayId,
                enableDebugger = DebugMode,
                localizationManager = new UILocalizationManager(GameManager.instance.localizationManager),
                resourceHandler = defView.uiSystem.resourceHandler
            });
            inputSystemArray[displayId] = new UIInputSystem(uiSystemArray[displayId]);
            yield return 0;

            var camgo = new GameObject();
            GameObject.DontDestroyOnLoad(camgo);
            var cam = camgo.AddComponent<Camera>();
            cam.cameraType = CameraType.Preview;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.gray;

            UIView.Settings settings = UIView.Settings.New;
            settings.isTransparent = true;
            settings.acceptsInput = true;
            var baseUri = new UriBuilder() { Scheme = "coui", Host = MOD_HOST, Path = @"UI/index.html" }.Uri.AbsoluteUri;
            yield return 0;

            var modView = uiSystemArray[displayId].CreateView(baseUri, settings, cam);
            modView.enabled = true;
            cam.targetDisplay = displayId;
            var thisMonitorId = displayId + 1;
            modView.Listener.ReadyForBindings += () =>
            {
                modView.View.BindCall("k45::euis.getMonitorId", new Func<int>(() => thisMonitorId));
                modView.View.BindCall("k45::euis.getQuantityMonitors", new Func<int>(() => Display.displays.Length));
                modView.View.BindCall("k45::euis.getDisabledAppsByDisplay", new Func<string[][]>(() => ExtraUIScreensMod.Instance.ModData.GetDisabledAppsByMonitor()));
                modView.View.RegisterForEvent("k45::euis.getMonitorEnabledApplcations", new Action<int>((monitorId) => StartCoroutine(GetMonitorEnabledApplcations(monitorId, modView.View))));
                modView.View.BindCall("k45::euis.saveAppSelectionAsDefault", new Action(() => SaveAppSelectionAsDefault()));
                modView.View.BindCall("k45::euis.removeAppButton", new Action<string, int>(RemoveAppFromMonitor));
                modView.View.BindCall("k45::euis.reintroduceAppButton", new Action<string, int>(AddAppToMonitor));
                modView.View.BindCall("k45::euis.getActiveMonitorMask", new Func<int>(() => ExtraUIScreensMod.Instance.ModData.InactiveMonitors));
                modView.View.BindCall("k45::euis.frontEndReady", new Action(() =>
                {
                    if (!Ready)
                    {
                        ReadyCount++;
                        Ready = ReadyCount >= ReadyCountTarget;
                        if (Ready)
                        {
                            OnReady?.Invoke();
                        }
                    }
                    else
                    {
                        OnReady?.Invoke();
                    }
                    OnMonitorActivityChanged();
                }));
            };
            modView.Listener.NodeMouseEvent += (a, eventData, c, d) =>
            {
                UpdateInputSystem(eventData, fieldUIInput, thisMonitorId);
                return Actions.ContinueHandling;
            };
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
            var result = new string[Display.displays.Length][];
            Console.WriteLine($"Registered apps: {string.Join("|", registeredApplications.Select(x => x.AppName))}");
            for (int i = 1; i < Display.displays.Length; i++)
            {
                var wrapper = new Wrapper<string[]>();
                yield return DoCallToMonitorToGetApplicationsEnabled(i + 1, wrapper);
                Console.WriteLine($"Apps enabled in display {i}: {string.Join("|", wrapper.Value ?? new string[0])}");
                result[i] = wrapper.Value != null
                    ? registeredApplications.Select(x => x.AppName).Where(x => !wrapper.Value.Contains(x)).ToArray()
                    : new string[0];
            }
            ExtraUIScreensMod.Instance.ModData.SetDisabledAppsByMonitor(result);
            ExtraUIScreensMod.Instance.SaveModData();
            RunningAppSelectionDefaultSave = null;
        }

        private int lastMonitorId = -1;
        private bool UpdateInputSystem(IMouseEventData b, FieldInfo fieldUIInput, int thisMonitorId)
        {
            var targetIdx = Mathf.RoundToInt(Display.RelativeMouseAt(Input.mousePosition).z);
            if (b.Type == MouseEventData.EventType.MouseMove && targetIdx != lastMonitorId)
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
            }
            if (lastMonitorId != 0)
            {
                InputManager.instance.mouseOverUI = true;
            }
            return thisMonitorId == lastMonitorId;
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
            if (monitorId <= 1 || monitorId > uiSystemArray.Length || uiSystemArray[monitorId - 1] is null)
            {
                appList.Value = new string[0];
                yield break;
            }
            while (uiSystemArray[monitorId - 1].UIViews.Count < 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
            var targetView = uiSystemArray[monitorId - 1].UIViews[0].View;
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
            for (int framesRemaining = 20; framesRemaining > 0; framesRemaining--)
            {
                if (localAppList != null) break;
                yield return 0;
            }
            targetView.UnregisterFromEvent(eventRegister);
            appList.Value = localAppList;
        }

        private void RemoveAppFromMonitor(string appName, int monitorId)
        {
            if (monitorId > 1 && monitorId <= uiSystemArray.Length && !(uiSystemArray[monitorId - 1] is null))
            {
                foreach (var uiSys in uiSystemArray)
                {
                    if (uiSys != null && uiSys.UIViews[0].View.IsReadyForBindings())
                    {
                        uiSys.UIViews[0].View.TriggerEvent("k45::euis.removeAppButton->", monitorId, appName);
                    }
                }
            }
        }


        private void AddAppToMonitor(string appName, int monitorId)
        {
            if (monitorId > 1 && monitorId <= uiSystemArray.Length && !(uiSystemArray[monitorId - 1] is null))
            {
                foreach (var uiSys in uiSystemArray)
                {
                    if (uiSys != null && uiSys.UIViews[0].View.IsReadyForBindings())
                    {
                        uiSys.UIViews[0].View.TriggerEvent("k45::euis.reintroduceAppButton->", monitorId, appName);
                    }
                }
            }
        }

        private HashSet<IEUISAppRegister> registeredApplications = new HashSet<IEUISAppRegister>();

        internal void RegisterApplication(IEUISAppRegister appRegisterData)
        {
            if (ValidateAppRegister(appRegisterData))
            {

                for (int i = 1; i < uiSystemArray.Length; i++)
                {
                    UISystem uiSys = uiSystemArray[i];
                    if (uiSys != null)
                    {
                        if (uiSys.UIViews[0].View.IsReadyForBindings())
                        {
                            if (DebugMode) Console.WriteLine($"Sending app for monitor {i + 1}: {appRegisterData.AppName}");
                            uiSys.UIViews[0].View.TriggerEvent("k45::euis.registerApplication", appRegisterData.AppName, appRegisterData.DisplayName, appRegisterData.UrlJs, appRegisterData.UrlCss, appRegisterData.UrlIcon);
                            registeredApplications.Add(appRegisterData);
                            foreach (var call in appRegisterData.CallsToBind)
                            {
                                var callAddress = $"{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}.{call.Key}";
                                uiSys.UIViews[0].View.BindCall(callAddress, call.Value);
                                LogUtils.DoLog("Registered call '{0}' @ Monitor #'{1}'", callAddress, i + 1);
                            }
                            foreach (var eventItem in appRegisterData.EventsToBind)
                            {
                                var bindAddress = $"{appRegisterData.ModderIdentifier}::{appRegisterData.ModAcronym}.{eventItem.Key}";
                                uiSys.UIViews[0].View.RegisterForEvent(bindAddress, eventItem.Value);
                                LogUtils.DoLog("Registered for event '{0}' @ Monitor #'{1}'", bindAddress, i + 1);
                            }
                        }
                    }
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
            if (!Regex.IsMatch(appRegisterData.AppName, $"^@{appRegisterData.ModderIdentifier}\\/[a-z0-9\\-]{{3,20}}$"))
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': Application must start with '@' + Modder identifier + '/'. After that, the application name must be 3-20 characters and must contain only characters in this regex: [a-z0-9\\-]");
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
            if (appRegisterData.CallsToBind is null)
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': Calls to bind must not be null!");
                return false;
            }
            if (appRegisterData.EventsToBind is null)
            {
                LogUtils.DoWarnLog($"Invalid app register for type '{appRegisterData.GetType().FullName}': Events to bind must not be null!");
                return false;
            }

            return true;
        }

        internal void UnregisterApplication(string appName)
        {
            for (int i = 1; i < uiSystemArray.Length; i++)
            {
                UISystem uiSys = uiSystemArray[i];
                if (uiSys != null)
                {
                    if (uiSys.UIViews[0].View.IsReadyForBindings())
                    {
                        if (DebugMode) Console.WriteLine($"Removing app from monitor {i + 1}: {appName}");
                        uiSys.UIViews[0].View.TriggerEvent("k45::euis.unregisterApplication", appName);
                    }
                }
            }
        }

        internal void OnMonitorActivityChanged()
        {
            for (int i = 1; i < uiSystemArray.Length; i++)
            {
                UISystem uiSys = uiSystemArray[i];
                if (uiSys != null)
                {
                    if (uiSys.UIViews[0].View.IsReadyForBindings())
                    {
                        uiSys.UIViews[0].View.TriggerEvent("k45::euis.activeMonitorMaskChange->", ExtraUIScreensMod.Instance.ModData.InactiveMonitors);
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
    }
}
