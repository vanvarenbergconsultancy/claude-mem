// Set up event handlers
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

const retryButton = document.getElementById("components-reconnect-button");
retryButton.addEventListener("click", retry);

const resumeButton = document.getElementById("components-resume-button");
resumeButton.addEventListener("click", resume);

function setReconnectClass(className) {
    reconnectModal.classList.remove(
        "components-reconnect-show",
        "components-reconnect-retrying",
        "components-reconnect-failed"
    );
    if (className) {
        reconnectModal.classList.add(className);
    }
}

function handleReconnectStateChanged(event) {
    const { state, seconds } = event.detail;

    if (state === "show") {
        setReconnectClass("components-reconnect-show");
        reconnectModal.showModal();
    } else if (state === "retrying") {
        setReconnectClass("components-reconnect-retrying");
        const secondsSpan = document.getElementById("components-seconds-to-next-attempt");
        if (secondsSpan) {
            secondsSpan.textContent = seconds;
        }
    } else if (state === "failed") {
        setReconnectClass("components-reconnect-failed");
        document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } else if (state === "hide") {
        document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
        setReconnectClass(null);
        reconnectModal.close();
    } else if (state === "rejected") {
        location.reload();
    }
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);

    try {
        // Reconnect will asynchronously return:
        // - true to mean success
        // - false to mean we reached the server, but it rejected the connection (e.g., unknown circuit ID)
        // - exception to mean we didn't reach the server (this can be sync or async)
        const successful = await Blazor.reconnect();
        if (!successful) {
            // We have been able to reach the server, but the circuit is no longer available.
            // We'll reload the page so the user can continue using the app as quickly as possible.
            const resumeSuccessful = await Blazor.resumeCircuit();
            if (!resumeSuccessful) {
                location.reload();
            } else {
                setReconnectClass(null);
                reconnectModal.close();
            }
        }
    } catch (err) {
        // We got an exception, server is currently unavailable
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    }
}

async function resume() {
    try {
        const successful = await Blazor.resumeCircuit();
        if (!successful) {
            location.reload();
        }
    } catch {
        reconnectModal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
    }
}
