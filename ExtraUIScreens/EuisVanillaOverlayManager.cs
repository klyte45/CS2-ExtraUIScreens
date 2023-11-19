using Belzont.Utils;
using Game.SceneFlow;
using System;
using UnityEngine;

namespace ExtraUIScreens
{
    public class EuisVanillaOverlayManager : MonoBehaviour
    {
        public static EuisVanillaOverlayManager Instance { get; private set; }
        //public static string kBaseUrlVos = new UriBuilder() { Scheme = "coui", Host = IBasicIMod.Instance.CouiHost, Path = @"UI/vos" }.Uri.AbsoluteUri;
        public static string kBaseUrlVos = new UriBuilder { Scheme = "http", Host = "localhost", Port = 8450, Path = @"" }.Uri.AbsoluteUri[..^1];

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
                LogUtils.DoInfoLog($"RELOAD URL: {x}");
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
        public void ResetVanillaOverlay(Game.GameMode mode)
        {
            LogUtils.DoWarnLog($"mode = {mode}");
            if (mode == Game.GameMode.Game|| mode == Game.GameMode.Editor)
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
