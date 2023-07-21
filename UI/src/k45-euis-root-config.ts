///<reference path="cohtml.d.ts" />
///<reference path="../root/src/euis.d.ts" />
import { LifeCycles, getAppNames, registerApplication, start, unregisterApplication } from "single-spa";
import prefixer from 'postcss-prefix-selector';
import postcss from "postcss";
import '/src/styles/base.scss'
import '/src/styles/tooltip.scss'
import translate from "./utility/translate";

const ROOT_APP_NAME = "@k45-euis/root";

type ApplicationInjectionData = {
  appName: string,
  displayName: string,
  jsUrl: string,
  cssUrl: string,
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
    if (x != 1) {
      document.querySelector("body.transparent").classList.remove("transparent");
    } else {
      document.querySelector("body").classList.add("mon1");
      engine.on("k45::euis.toggleMon1", (enabled: boolean) => {
        if (enabled) {
          document.querySelector("body").classList.remove("transparent");
        } else {
          document.querySelector("body").classList.add("transparent");
        }
      });
    }
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

  engine.on("k45::euis.registerApplication", (appName: string, displayName: string, jsUrl: string, cssUrl: string, iconUrl: string) => {
    fullRegisterApp({ appName, displayName, iconUrl, jsUrl, cssUrl })
  });

  engine.on("k45::euis.unregisterApplication", (appName: string) => {
    fullUnregisterApp(appName);
  });

  engine.on("k45::euis.activeMonitorMaskChange->", (mask: number) => {
    if (mask & (1 << (__euis_main.monitorNumber - 1))) {
      document.querySelector("body").classList.add("disabled");
    } else {
      document.querySelector("body").classList.remove("disabled");
    }
  });
  getAppsDisabledByDefault().then(() => engine.call("k45::euis.frontEndReady", __euis_main.monitorNumber))
  engine.on("k45::euis.interfaceStyle.update", (styleName: string) => {
    for (var t = document.body.classList.length - 1; t >= 0; t--) {
      var n = document.body.classList[t];
      if (n.startsWith("style--")) {
        document.body.classList.remove(n);
      }
    }
    document.body.classList.add("style--" + styleName);
  });

  try {
    engine.trigger("k45::euis.interfaceStyle.subscribe")
  } catch (e) { console.log(e) }

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
    cssUrl: null,
    jsUrl: "coui://euis.k45/UI/k45-euis-root.js"//"http://localhost:8500/k45-euis-root.js"
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
  startMenu.setAttribute("data-tooltip", `<b>${translate("startButton.tooltipTitle")}</b>${translate("startButton.tooltipDesc")}`);
  startMenu.setAttribute("data-tootip-position", "top left");
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

async function load(url: string): Promise<string> {
  return new Promise((res, rej) => {
    var xhr = new XMLHttpRequest();
    xhr.timeout = 1800;
    xhr.onreadystatechange = function () {
      if (xhr.readyState === 4) {
        if (xhr.status >= 200 && xhr.status < 300) res(xhr.responseText);
        else rej(xhr)
      }
    };
    xhr.open("GET", url, true);
    xhr.send()
    setTimeout(() => rej("Timeout!"), 2200);
  });
}

function registerApplicationCommons(appData: ApplicationInjectionData, customProps?: any) {
  const appName = appData.appName;
  registerApplication(appName,
    async () => {
      let lifecycle: LifeCycles;
      lifecycle = await System.import<LifeCycles>(appData.jsUrl);
      return {
        bootstrap: [lifecycle.bootstrap],
        mount(props) {
          return new Promise(async (resolve, reject) => {
            resolve(undefined);
            if (Array.isArray(lifecycle.mount)) {
              await Promise.all(lifecycle.mount.map(x => x(props)))
            } else {
              await lifecycle.mount(props)
            }
            const rootEl = document.getElementById("single-spa-application:" + appName);
            rootEl.querySelectorAll("style, link[type=stylesheet]").forEach((x) => {
              removeCssElement(x);
            })

            if (appData.cssUrl) {
              try {
                const safeName = appName.replace(/[@\/]/g, "_");
                rootEl.setAttribute("data-safe-name", safeName);
                const cssContent = await load(appData.cssUrl);
                const parsedContent = postcss().use(prefixer({
                  prefix: `div[data-safe-name=${safeName}]`,
                  transform(prefix, selector, prefixedSelector) {
                    if (selector.match(/^(html|body)/)) {
                      return selector.replace(/^([^\s]*)/, `$1 ${prefix}`);
                    }
                    if (selector.match(/^:root/)) {
                      return selector.replace(/^([^\s]*)/, prefix);
                    }
                    return prefixedSelector;
                  },
                })).process(cssContent).css

                const cssNode = document.createElement("style");
                cssNode.innerHTML = parsedContent;
                cssNode.id = "style:" + rootEl.id.split(":")[1];
                document.querySelector("head").appendChild(cssNode);
              } catch (e) {
                console.error("Failed to load css: " + appData.cssUrl, e)
              }
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
            const cssEl = document.getElementById("style:" + appName);
            if (cssEl) {
              removeCssElement(cssEl);
            }
            appDatabase[appName]?.button?.classList.remove("active");
          });
        },
        update: lifecycle.update,
      } as LifeCycles
    },
    (location) => getApplicationScreenPositionOrdinal(location, appName) >= 0,
    customProps);
}

function removeCssElement(cssEl: Element) {
  cssEl.parentNode.removeChild(cssEl);
  [...(document.styleSheets as any)].forEach(
    (x: CSSStyleSheet) => {
      if ((x.ownerNode as any) == cssEl) {
        do {
          x.deleteRule(0);
        } while (x.cssRules.length > 0);
      }
    }
  );
  cssEl.id = "";
}

function registerTaskbarButtonForApplication(appData: ApplicationInjectionData) {
  const appName = appData.appName;
  const taskbar = document.getElementById("taskbar")
  const appBtn = document.createElement("div");
  appBtn.setAttribute('id', 'appButton:' + appName);
  appBtn.setAttribute("data-tooltip", appData.displayName);
  appBtn.setAttribute("data-tootip-position", "top left");
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