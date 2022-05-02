async function loadCss(path: string): Promise<void> {
    // Check if the resource exists, and isn't already loaded.    
    if (!isPathLoaded(path) && await urlExists(path)) {
        let link = document.createElement("link");
        link.type = "text/css";
        link.rel = "stylesheet";
        link.href = path;
        document.head.appendChild(link);
        
        link.onload = function () {
            console.debug(`Loaded CSS:`, path);
        }
        
        link.onerror = function () {
            console.error(`Failed to load CSS:`, path);
        }
    }
    else
    {
        console.debug(`Skipped loading CSS:`, path);
    }
}

async function loadJs(path: string, ignoreErrors?: boolean): Promise<void> {
    // Check if the resource exists, and isn't already loaded.    
    if (!isPathLoaded(path) && await urlExists(path)) {
        let link = document.createElement('script');
        link.src = path;
        link.type = "text/javascript";

        link.onload = function () {
            console.debug(`Loaded JS:`, path);
        }

        link.onerror = function () {
            console.error(`Failed to load JS:`, path);
        }

        document.body.appendChild(link);
    }
    else
    {
        console.debug(`Skipped loading JS:`, path);
    }
}

function setBase(baseUrl: string): void {
    document.querySelector('base').href = baseUrl;
    console.debug(`Base URL set:`, baseUrl);
}

async function urlExists(url: string): Promise<boolean> {
    let http = new XMLHttpRequest();
    http.open('HEAD', url, true);

    // Return a promise with the result
    return new Promise<boolean>((resolve) => {
        http.onreadystatechange = () => {
            if (http.readyState == XMLHttpRequest.DONE) {
                resolve(http.status == 200);
            }
        }
        http.send();
    });
}

function isPathLoaded(link: string): boolean {
    return  document.querySelector(`link[href="${link}"]`) !== null;
}