///<reference path="cohtml.d.ts" />
///<reference path="../root/src/euis.d.ts" />
import { LifeCycles, getAppNames, registerApplication, start, unregisterApplication } from "single-spa";

const ROOT_APP_NAME = "@k45-euis/root";

type ApplicationInjectionData = {
  appName: string,
  displayName: string,
  jsUrl: string,
  iconUrl: string
}

let __euis_main: EuisMainModel = {
  currentTime: getCurrentTime(),
  currentDate: getCurrentDate(),
  monitorIdentifierText: "",
  monitorNumber: -1
}

engine.whenReady.then(() => {
  engine.call("k45::euis.getMonitorId").then(function (x) {
    engine.createJSModel('__euis_main', __euis_main);
    engine.synchronizeModels();
    updateCurrentTime();
    __euis_main.monitorIdentifierText = `MON #${x}`;
    __euis_main.monitorNumber = x;
    engine.updateWholeModel(__euis_main);
    engine.synchronizeModels();
    applicationRegistering();
  })

  engine.on("k45::euis.removeAppButton->", (monitorId: number, appName: string) => {
    if (monitorId == __euis_main.monitorNumber) removeAppButton_impl(appName);
  })
  engine.on("k45::euis.reintroduceAppButton->", (monitorId: number, appName: string) => {
    if (monitorId == __euis_main.monitorNumber) reintroduceAppButton_impl(appName);
  })
  engine.on("k45::euis.listActiveAppsInMonitor", () => {
    engine.trigger("k45::euis.listActiveAppsInMonitor->", Object.entries(appDatabase).filter(x => x[1].button).map(x => x[0]))
  })

  engine.on("k45::euis.registerApplication", (appName: string, displayName: string, jsUrl: string, iconUrl: string) => {
    fullRegisterApp({ appName, displayName, iconUrl, jsUrl })
  });

  engine.on("k45::euis.unregisterApplication", (appName: string) => {
    fullUnregisterApp(appName);
  });

  getAppsDisabledByDefault().then(() => engine.call("k45::euis.frontEndReady"))

})

let maximumOpenWindows = 1;
let appDatabase: Record<string, ApplicationInjectionData & { button?: HTMLDivElement }> = {}

function updateCurrentTime() {
  setTimeout(() => { updateTime(); setInterval(updateTime, 60000) }, 60500 - new Date().getSeconds() * 1000 - new Date().getMilliseconds());
}

function updateTime() {
  __euis_main.currentTime = getCurrentTime();
  __euis_main.currentDate = getCurrentDate();
  engine.updateWholeModel(__euis_main);
  engine.synchronizeModels();
}

function getCurrentTime() {
  const date = new Date();
  return `${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}`;
}
function getCurrentDate() {
  const date = new Date();
  return `${date.getFullYear().toString().padStart(4, "0")}-${(1 + date.getMonth()).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}`;
}

function applicationRegistering() {
  var rootAppData: ApplicationInjectionData = {
    appName: ROOT_APP_NAME,
    displayName: "EUIS Settings",
    iconUrl: "./euis.png",
    jsUrl: "coui://k45.euis/UI/k45-euis-root.js"//"http://localhost:8500/k45-euis-root.js"
  }

  registerApplicationCommons(rootAppData,
    {
      getAppNames,
      getAppData: () => appDatabase,
      removeAppButton,
      reintroduceAppButton,
      listActiveAppsInMonitor,
      getQuantityMonitors
    });
  const taskbar = document.getElementById("taskbar")
  const startMenu = document.createElement("div");
  startMenu.setAttribute('id', 'startMenu');
  startMenu.setAttribute('class', 'startMenu');
  startMenu.onclick = () => toggleNavigationToApp(ROOT_APP_NAME)
  taskbar.appendChild(startMenu);
  appDatabase[ROOT_APP_NAME] = rootAppData;
  appDatabase[ROOT_APP_NAME].button = startMenu;
}

const getApplicationScreenPositionOrdinal = (location: Location, appname: string) => {
  return location.search.slice(1).split("&").indexOf("app=" + appname);
}

let appsDisabledByDefault: string[];
async function getAppsDisabledByDefault() {
  const disabledByDisplay: string[][] = await engine.call("k45::euis.getDisabledAppsByDisplay")
  appsDisabledByDefault = disabledByDisplay?.[__euis_main.monitorNumber - 1] ?? [];
}

async function fullRegisterApp(appData: ApplicationInjectionData) {
  if (appDatabase[appData.appName]) {
    await fullUnregisterApp(appData.appName);
  }
  appDatabase[appData.appName] = appData
  registerApplicationCommons(appData)
  if (!appsDisabledByDefault.includes(appData.appName)) {
    registerTaskbarButtonForApplication(appData)
    engine.trigger("k45::euis.reintroduceAppButton->");
  } else {
    engine.trigger("k45::euis.removeAppButton->");
  }
}

function registerApplicationCommons(appData: ApplicationInjectionData, customProps?: any) {
  const appName = appData.appName;
  registerApplication(appName,
    async () => {
      let lifecycle: LifeCycles;
      try {
        lifecycle = await System.import<LifeCycles>(appName);
      } catch {
        const importMap = document.createElement("script");
        importMap.setAttribute("type", "systemjs-importmap")
        importMap.text = JSON.stringify({ imports: { [appName]: appData.jsUrl } });
        document.appendChild(importMap);
        lifecycle = await System.import<LifeCycles>(appData.jsUrl);
      }
      return {
        bootstrap: lifecycle.bootstrap,
        mount(props) {
          return new Promise(async (resolve, reject) => {
            resolve(undefined);
            if (Array.isArray(lifecycle.mount)) {
              await Promise.all(lifecycle.mount.map(x => x(props)))
            } else {
              await lifecycle.mount(props)
            }
            appDatabase[appName]?.button?.classList.add("active");
          });
        },
        unmount(props) {
          return new Promise(async (resolve, reject) => {
            resolve(undefined);
            if (Array.isArray(lifecycle.unmount)) {
              await Promise.all(lifecycle.unmount.map(x => x(props)))
            } else {
              await lifecycle.unmount(props)
            }
            const rootEl = document.getElementById("single-spa-application:" + appName);
            rootEl.parentNode.removeChild(rootEl);
            appDatabase[appName]?.button?.classList.remove("active");
          });
        },
        update: lifecycle.update,
      } as LifeCycles
    },
    (location) => getApplicationScreenPositionOrdinal(location, appName) >= 0,
    customProps);
}

function registerTaskbarButtonForApplication(appData: ApplicationInjectionData) {
  const appName = appData.appName;
  const taskbar = document.getElementById("taskbar")
  const appBtn = document.createElement("div");
  appBtn.setAttribute('id', 'appButton:' + appName);
  appBtn.setAttribute('class', 'appButton');
  appBtn.setAttribute('style', `--appBtnIcon: url(${appData.iconUrl})`)
  appBtn.setAttribute('title', engine.translate(`k45-euis.applicationName.${appName}`))
  appBtn.onclick = () => toggleNavigationToApp(appName)
  taskbar.appendChild(appBtn);
  appDatabase[appName].button = appBtn;
}

function removeAppButton(appName: string, monitor: number) {
  engine.call("k45::euis.removeAppButton", appName, monitor)
}

function removeAppButton_impl(appName: string) {
  if (appName != ROOT_APP_NAME && appDatabase[appName]?.button) {
    appDatabase[appName].button.parentNode.removeChild(appDatabase[appName].button);
    appDatabase[appName].button = undefined;
    const rootEl = document.getElementById("single-spa-application:" + appName);
    if (rootEl) {
      toggleNavigationToApp(appName);
    }
  }
}
function reintroduceAppButton(appName: string, monitor: number) {
  engine.call("k45::euis.reintroduceAppButton", appName, monitor)
}

function reintroduceAppButton_impl(appName: string) {
  if (appName != ROOT_APP_NAME && appDatabase[appName] && !appDatabase[appName].button) {
    registerTaskbarButtonForApplication(appDatabase[appName]);
  }
}

async function listActiveAppsInMonitor(monitor: number) {
  return new Promise<string[]>((res, rej) => {
    engine.on("k45::euis.getMonitorEnabledApplcations->" + monitor, (result: string[]) => {
      engine.off("k45::euis.getMonitorEnabledApplcations->" + monitor)
      res(result);
    })
    engine.trigger("k45::euis.getMonitorEnabledApplcations", monitor)
    setTimeout(() => rej("TIMEOUT!"), 3000)
  })
}

async function fullUnregisterApp(appName: string) {
  removeAppButton_impl(appName)
  await unregisterApplication(appName)
  engine.trigger("k45::euis.removeAppButton->")
}

function toggleNavigationToApp(appname: string) {
  if (!getAppNames().includes(appname)) {
    console.log("INVALID APP: " + appname)
    return;
  }
  let queryArray = location.search.slice(1).split("&").slice(0, maximumOpenWindows);
  const appNameQuery = "app=" + appname;
  let ordinalPlace = queryArray.indexOf(appNameQuery);
  if (ordinalPlace >= 0) {
    queryArray = queryArray.filter(x => x != appNameQuery)
  } else {
    queryArray = queryArray.slice(0, maximumOpenWindows - 1)
    queryArray.push(appNameQuery);
  }
  window.history.pushState(null, null, `?${queryArray.join("&")}`)
}

async function getQuantityMonitors(): Promise<number> {
  return await engine.call("k45::euis.getQuantityMonitors");
}

start({
  urlRerouteOnly: true
});