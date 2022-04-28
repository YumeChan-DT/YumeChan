function loadCss(path: string) {
    let element = document.createElement("link");
    element.setAttribute("rel", "stylesheet");
    element.setAttribute("type", "text/css");
    element.setAttribute("href", path);
    document.getElementsByTagName("head")[0].appendChild(element);
}

function loadJs(sourceUrl: string) {
    if (sourceUrl.length === 0) {
        console.error("Invalid source URL");
        return;
    }

    let tag = document.createElement('script');
    tag.src = sourceUrl;
    tag.type = "text/javascript";

    tag.onload = function () {
        console.log("Script loaded successfully");
    }

    tag.onerror = function () {
        console.error("Failed to load script");
    }

    document.body.appendChild(tag);
}

function setBase(baseUrl: string) {
    document.querySelector('base').href = baseUrl;
}