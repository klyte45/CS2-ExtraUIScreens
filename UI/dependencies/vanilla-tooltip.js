
Tooltip = function (options) {
    var delay = options.delay || 0;

    /* 
    * Attaching one mouseover and one mouseout listener to the document
    * instead of listeners for each trigger 
    */
    document.body.addEventListener("mouseover", function (e) {
        if (!e.target.hasAttribute('data-tooltip')) return;

        const targetId = "tooltip-" + getPathTo(e.target);
        const existingTooltip = document.getElementById(targetId);
        if (existingTooltip) {
            existingTooltip.classList.remove(...[...existingTooltip.classList].filter(x => x.startsWith("tooltip-removing")))
        } else {
            const tooltip = document.createElement("div");
            tooltip.className = "b-tooltip";
            tooltip.id = targetId
            tooltip.innerHTML = e.target.getAttribute('data-tooltip');

            document.body.appendChild(tooltip);

            const pos = e.target.getAttribute('data-tooltip-position') || "center top";
            const splitPos = pos.split(" ");
            const posHorizontal = splitPos[0];
            const posVertical = splitPos[1];
            const pivot = e.target.getAttribute('data-tooltip-pivot');
            const splitPivot = pivot?.split(" ");
            const pivotHorizontal = splitPivot?.[0];
            const pivotVertical = splitPivot?.[1];

            const target = e.target;
            const distanceX = parseInt(e.target.getAttribute('data-tooltip-distanceX'));
            const distanceY = parseInt(e.target.getAttribute('data-tooltip-distanceY'));
            setTimeout(() => {
                positionAt(target, tooltip, posHorizontal, posVertical, pivotHorizontal, pivotVertical, isNaN(distanceX) ? 0 : distanceX, isNaN(distanceY) ? 0 : distanceY);
                tooltip.classList.add("tooltip-visible");
            }, 150);
        }
    });

    document.body.addEventListener("mouseout", function (e) {
        if (e.target.hasAttribute('data-tooltip')) {
            const el = [...document.querySelectorAll(".b-tooltip")];
            const timestamp = Date.now();
            const customTimerClass = "tooltip-removing-" + timestamp;
            el.map(x => x.classList.add("tooltip-removing", customTimerClass))
            setTimeout(function () {
                el.map(x => x.classList.contains(customTimerClass) && x.parentNode?.removeChild(x));
            }, delay);
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

    function getPathTo(element) {
        if (element.id !== '')
            return 'id("' + element.id + '")';
        if (element === document.body)
            return element.tagName;

        var ix = 0;
        var siblings = element.parentNode.childNodes;
        for (var i = 0; i < siblings.length; i++) {
            var sibling = siblings[i];
            if (sibling === element)
                return getPathTo(element.parentNode) + '/' + element.tagName + '[' + (ix + 1) + ']';
            if (sibling.nodeType === 1 && sibling.tagName === element.tagName)
                ix++;
        }
    }
};