(function () {
    const thisFileSource = (document.currentScript.src + "").split(/\/[^\/]+$/)[0];
    let createEuisContainer = async function () {
        const k = document.createElement("div");
        k.id = "EUIS_Container";
        async function onComplete() {
            k.insertAdjacentHTML('afterbegin', this.responseText)
            document.body.appendChild(k);
            await System.import('@k45-euis/vos-config')
        }

        var oReq = new XMLHttpRequest();
        oReq.addEventListener("load", onComplete);
        oReq.open("GET", thisFileSource + "/include.html");
        oReq.send();
    }

    if (document.getElementById("EUIS_Container")) {
        return
    } else {

        const css = document.createElement('link');
        css.href = thisFileSource + "/base.css";
        css.rel = "stylesheet"
        const script1 = document.createElement('script');
        script1.src = thisFileSource + "/dependencies/system.min.js";
        const script2 = document.createElement('script');
        script2.src = thisFileSource + "/dependencies/single-spa.min.js";
        const script3 = document.createElement('script');
        script3.src = thisFileSource + "/dependencies/import-map-overrides.js";
        const script4 = document.createElement('script');
        script4.src = thisFileSource + "/dependencies/amd.min.js";
        const script5 = document.createElement('script');
        script5.type = "systemjs-importmap"
        script5.insertAdjacentText('afterbegin', `
            {
              "imports": {
                "single-spa": "${thisFileSource}/dependencies/single-spa.min.js",
                "react": "${thisFileSource}/dependencies/react.production.min.js",
                "react-dom": "${thisFileSource}/dependencies/react-dom.production.min.js",
                "@k45-euis/vos-config":"${thisFileSource}/k45-euis-vos-config.js"
              }
            }`);
        script1.onload = function () { document.head.appendChild(script2) }
        script2.onload = function () { document.head.appendChild(script3) }
        script3.onload = function () { document.head.appendChild(script4) }
        script4.onload = function () { document.head.appendChild(script5); createEuisContainer() }
        document.head.appendChild(script1);
        document.head.appendChild(css);
    }
})()
