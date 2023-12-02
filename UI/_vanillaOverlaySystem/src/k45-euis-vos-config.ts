///<reference path="cohtml.d.ts" />
///<reference path="../../root/src/euis.d.ts" />
import { LifeCycles, getAppNames, registerApplication, start, unregisterApplication, triggerAppChange } from "single-spa";
import prefixer from 'postcss-prefix-selector';
import postcss from "postcss";
import '/src/styles/base.scss'
import '/src/styles/tooltip.scss'
import 'regenerator-runtime/runtime'

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
  monitorIdentifierText: "Vanilla!",
  monitorNumber: -1,
}
var _currentApp: string = null;

engine.whenReady.then(() => {
  engine.createJSModel('__euis_main', __euis_main);
  engine.synchronizeModels();
  updateCurrentTime();
  engine.updateWholeModel(__euis_main);
  engine.synchronizeModels();
  initiateTooltip();
  const toolbox = document.querySelector<HTMLDivElement>("div#toolbox");
  const toolboxToggleBtn = document.querySelector<HTMLButtonElement>("div#toolbox .toggleToolbox");
  toolboxToggleBtn.onclick = () => {
    if (toolbox.classList.contains("closed")) {
      toolbox.classList.remove("closed");
    } else {
      toolbox.classList.add("closed");
    }
  }
})

engine.on("k45::euis.removeVosApp->", (appName: string) => {
  removeAppButton_impl(appName);
})
engine.on("k45::euis.reintroduceVosApp->", (appName: string) => {
  reintroduceAppButton_impl(appName);
})
engine.on("k45::euis.listActiveAppsInMonitor", () => {
  engine.trigger("k45::euis.listActiveAppsInMonitor->", Object.entries(appDatabase).filter(x => x[1].button).map(x => x[0]))
})

engine.on("k45::euis.registerVosApplication", (appName: string, displayName: string, jsUrl: string, cssUrl: string, iconUrl: string) => {
  fullRegisterApp({ appName, displayName, iconUrl, jsUrl, cssUrl })
});

engine.on("k45::euis.unregisterVosApplication", (appName: string) => {
  fullUnregisterApp(appName);
});

getVosAppsDisabledByDefault().then(() => engine.call("k45::euis.vosFrontEndReady"))


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

let appsDisabledByDefault: string[];
async function getVosAppsDisabledByDefault() {
  const disabledByDisplay: string[][] = await engine.call("k45::euis.getDisabledAppsByDisplay")
  appsDisabledByDefault = disabledByDisplay?.[0] ?? [];
}

async function fullRegisterApp(appData: ApplicationInjectionData) {
  if (appDatabase[appData.appName]) {
    await fullUnregisterApp(appData.appName);
  }
  appDatabase[appData.appName] = appData
  registerApplicationCommons(appData)
  if (!appsDisabledByDefault.includes(appData.appName)) {
    registerDockButtonForApplication(appData)
    engine.trigger("k45::euis.reintroduceVosApp->");
  } else {
    engine.trigger("k45::euis.removeVosApp->");
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
    () => _currentApp == appName,
    Object.assign({
      selfSelect: () => _currentApp != appName ? toggleNavigationToApp(appName, true) : null,
      selfUnselect: () => _currentApp == appName ? toggleNavigationToApp(null, true) : null,
    }, customProps ?? {}));
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

function registerDockButtonForApplication(appData: ApplicationInjectionData) {
  const appName = appData.appName;
  const toolbox = document.querySelector("div#toolbox #buttons")
  const appBtn = document.createElement("div");
  appBtn.setAttribute('id', 'appButton:' + appName);
  appBtn.setAttribute("data-tooltip", appData.displayName);
  appBtn.setAttribute("data-tootip-position", "bottom left");
  appBtn.setAttribute('class', 'appButton');
  appBtn.setAttribute('style', `--appBtnIcon: url(${appData.iconUrl})`)
  appBtn.setAttribute('title', engine.translate(`k45-euis.applicationName.${appName}`))
  appBtn.onclick = () => toggleNavigationToApp(appName)
  toolbox.appendChild(appBtn);
  appDatabase[appName].button = appBtn;
}

function removeAppButton_impl(appName: string) {
  if (appDatabase[appName]?.button) {
    appDatabase[appName].button.parentNode.removeChild(appDatabase[appName].button);
    appDatabase[appName].button = undefined;
    const rootEl = document.getElementById("single-spa-application:" + appName);
    if (rootEl) {
      toggleNavigationToApp(appName);
    }
  }
}

function reintroduceAppButton_impl(appName: string) {
  if (appDatabase[appName] && !appDatabase[appName].button) {
    registerDockButtonForApplication(appDatabase[appName]);
  }
}
async function fullUnregisterApp(appName: string) {
  removeAppButton_impl(appName)
  await unregisterApplication(appName)
  engine.trigger("k45::euis.removeVosApp->")
}


function toggleNavigationToApp(appname: string, force: boolean = false) {
  if (appname == null) {
    _currentApp = null;
  } else {
    if (!getAppNames().includes(appname)) {
      console.warn("INVALID APP: " + appname)
      return;
    }
    if (appname == _currentApp && !force) {
      _currentApp = null;
    } else {
      _currentApp = appname;
    }
  }
  triggerAppChange()
}

function initiateTooltip() {
  /* 
  * Attaching one mouseover and one mouseout listener to the document
  * instead of listeners for each trigger 
  */
  document.body.addEventListener("mouseover", function (ev) {
    var el = ev.target as HTMLElement;
    if (!(el.hasAttribute('data-tooltip'))) return;

    const targetId = "tooltip-" + getPathTo(el);
    const existingTooltip = document.getElementById(targetId);
    if (existingTooltip) {
      const removeList = [...(existingTooltip.classList as any)].filter(x => x.startsWith("tooltip-removing"));
      existingTooltip.classList.remove(...removeList)
    } else {
      const tooltip = document.createElement("div");
      tooltip.className = "b-tooltip";
      tooltip.id = targetId
      tooltip.innerHTML = el.getAttribute('data-tooltip');

      document.body.appendChild(tooltip);

      const pos = el.getAttribute('data-tooltip-position') || "center top";
      const splitPos = pos.split(" ");
      const posHorizontal = splitPos[0];
      const posVertical = splitPos[1];
      const pivot = el.getAttribute('data-tooltip-pivot');
      const splitPivot = pivot?.split(" ");
      const pivotHorizontal = splitPivot?.[0];
      const pivotVertical = splitPivot?.[1];

      const target = el;
      const distanceX = parseInt(el.getAttribute('data-tooltip-distanceX'));
      const distanceY = parseInt(el.getAttribute('data-tooltip-distanceY'));
      setTimeout(() => {
        positionAt(target, tooltip, posHorizontal, posVertical, pivotHorizontal, pivotVertical, isNaN(distanceX) ? 0 : distanceX, isNaN(distanceY) ? 0 : distanceY);
        tooltip.classList.add("tooltip-visible");
      }, 150);
    }
  });

  document.body.addEventListener("mouseout", function (e) {
    if ((e.target as Element).hasAttribute('data-tooltip')) {
      const el = [...(document.querySelectorAll(".b-tooltip") as any)];
      const timestamp = Date.now();
      const customTimerClass = "tooltip-removing-" + timestamp;
      el.map((x: Element) => x.classList.add("tooltip-removing", customTimerClass))
      setTimeout(function () {
        el.map(x => x.classList.contains(customTimerClass) && x.parentNode?.removeChild(x));
      }, 750);
    }
  });

  const validHorizontal = ["left", 'center', "right"];
  const validVertical = ["top", "middle", "bottom"];

  /**
   * Positions the tooltip.
   * 
   * @param {object} parent - The trigger of the tooltip.
   * @param {object} tooltip - The tooltip itself.
   * @param {string} posHorizontal - Desired horizontal position of the tooltip relatively to the trigger (left/center/right)
   * @param {string} posVertical - Desired vertical position of the tooltip relatively to the trigger (top/center/bottom)
   * 
   */
  function positionAt(parent, tooltip, posHorizontal, posVertical, pivotHorizontal, pivotVertical, distanceX, distanceY) {

    let parentCoords = parent.getBoundingClientRect();
    let left;
    let top;

    let pivotX, pivotY;
    posHorizontal = validHorizontal.includes(posHorizontal) ? posHorizontal : "center";
    posVertical = validVertical.includes(posVertical) ? posVertical : "top";
    pivotHorizontal = validHorizontal.includes(pivotHorizontal) ? pivotHorizontal : validHorizontal[2 - validHorizontal.indexOf(posHorizontal)];
    pivotVertical = validVertical.includes(pivotVertical) ? pivotVertical : validVertical[2 - validVertical.indexOf(posVertical)];

    switch (pivotHorizontal) {
      case "right":
        pivotX = (parseInt(tooltip.offsetWidth)) + distanceX;
        break;
      case "left":
        pivotX = - distanceX;
        break;
      case "center":
        pivotX = (parseInt(tooltip.offsetWidth) / 2);
        break;
    }

    switch (pivotVertical) {
      case "middle":
        pivotY = (parseInt(tooltip.offsetHeight) / 2);
        break;
      case "top":
        pivotY = -distanceY;
        break;
      case "bottom":
        pivotY = parseInt(tooltip.offsetHeight) + distanceY;
        break;
    }


    switch (posHorizontal) {
      case "left":
        left = parseInt(parentCoords.left) + pivotX;
        break;
      case "right":
        left = parentCoords.right - pivotX;
        break;
      case "center":
        left = -pivotX + parseInt(parentCoords.left) + (parseInt(parentCoords.width) / 2);
    }

    left = Math.min(document.documentElement.offsetWidth - tooltip.offsetWidth, Math.max(0, left));

    switch (posVertical) {
      case "middle":
        top = parseInt(parentCoords.top) + (parseInt(parentCoords.height) / 2) - pivotY;
        break;
      case "bottom":
        top = parseInt(parentCoords.bottom) - pivotY;
        break;
      case "top":
        top = parseInt(parentCoords.top) - pivotY;
    }

    top = Math.min(document.documentElement.offsetHeight - tooltip.offsetHeight, Math.max(0, top));

    tooltip.style.left = left + "px";
    tooltip.style.top = top + "px";
  }

  function getPathTo(element: Element) {
    if (element.id !== '')
      return 'id("' + element.id + '")';
    if (element === document.body)
      return element.tagName;

    var ix = 0;
    var siblings = element.parentNode.childNodes;
    for (var i = 0; i < siblings.length; i++) {
      var sibling = siblings[i];
      if (sibling === element)
        return getPathTo(element.parentNode as Element) + '/' + element.tagName + '[' + (ix + 1) + ']';
      if (sibling.nodeType === 1 && (sibling as Element).tagName === element.tagName)
        ix++;
    }
  }
}

start({
  urlRerouteOnly: true
});
