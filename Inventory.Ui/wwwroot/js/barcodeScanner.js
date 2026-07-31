let dotNetReference = null;
let keyboardHandler = null;
let resetTimer = null;

let buffer = "";
let startedAt = 0;
let lastKeyAt = 0;
let largestGap = 0;
let inputSnapshot = null;

const maximumGapMilliseconds = 100;
const maximumEndGapMilliseconds = 150;
const minimumBarcodeLength = 6;
const resetDelayMilliseconds = 250;

const ignoredModifierKeys = new Set([
    "Shift",
    "Control",
    "Alt",
    "Meta",
    "CapsLock",
    "NumLock"
]);

const invocation =
    dotNetReference?.invokeMethodAsync(
        "OnHardwareScan",
        barcode
    );

invocation?.catch(error => {
    console.error(
        "Hardware barcode processing failed.",
        error
    );
});

export function initialize(reference) {
    dispose();

    dotNetReference = reference;

    keyboardHandler = event => {
        handleKeyDown(event);
    };

    document.addEventListener(
        "keydown",
        keyboardHandler,
        true
    );
}

function handleKeyDown(event) {
    if (event.isComposing) {
        return;
    }

    if (event.repeat) {
        resetCandidate();
        return;
    }

    if (ignoredModifierKeys.has(event.key)) {
        return;
    }

    if (
        event.ctrlKey ||
        event.altKey ||
        event.metaKey
    ) {
        return;
    }

    if (
        event.isComposing ||
        event.ctrlKey ||
        event.altKey ||
        event.metaKey
    ) {
        return;
    }

    const currentTime = performance.now();

    if (event.key.length === 1) {
        handleCharacter(
            event,
            currentTime
        );

        return;
    }

    if (
        event.key !== "Enter" &&
        event.key !== "Tab"
    ) {
        resetCandidate();
        return;
    }

    const endGap =
        lastKeyAt === 0
            ? Number.POSITIVE_INFINITY
            : currentTime - lastKeyAt;

    const isHardwareScan =
        buffer.length >= minimumBarcodeLength &&
        largestGap <= maximumGapMilliseconds &&
        endGap <= maximumEndGapMilliseconds;

    if (!isHardwareScan) {
        resetCandidate();
        return;
    }

    const barcode = buffer;

    restoreInputSnapshot();
    resetCandidate();

    event.preventDefault();
    event.stopPropagation();
    event.stopImmediatePropagation();

    void dotNetReference
        ?.invokeMethodAsync(
            "OnHardwareScan",
            barcode
        );
}

function handleCharacter(event, currentTime) {
    const gap =
        lastKeyAt === 0
            ? Number.POSITIVE_INFINITY
            : currentTime - lastKeyAt;

    if (
        buffer.length === 0 ||
        gap > maximumGapMilliseconds
    ) {
        beginCandidate(
            event,
            currentTime
        );

        return;
    }

    largestGap = Math.max(
        largestGap,
        gap
    );

    buffer += event.key;
    lastKeyAt = currentTime;

    scheduleReset();
}

function beginCandidate(event, currentTime) {
    resetCandidate();

    buffer = event.key;
    startedAt = currentTime;
    lastKeyAt = currentTime;
    largestGap = 0;

    captureInputSnapshot(
        event.target
    );

    scheduleReset();
}

function captureInputSnapshot(target) {
    if (
        !(target instanceof HTMLInputElement) &&
        !(target instanceof HTMLTextAreaElement)
    ) {
        inputSnapshot = null;
        return;
    }

    inputSnapshot = {
        element: target,
        value: target.value,
        selectionStart: target.selectionStart,
        selectionEnd: target.selectionEnd
    };
}

function restoreInputSnapshot() {
    if (!inputSnapshot) {
        return;
    }

    const {
        element,
        value,
        selectionStart,
        selectionEnd
    } = inputSnapshot;

    if (!element.isConnected) {
        return;
    }

    element.value = value;

    element.dispatchEvent(
        new Event("input", {
            bubbles: true
        })
    );

    if (
        selectionStart !== null &&
        selectionEnd !== null
    ) {
        element.setSelectionRange(
            selectionStart,
            selectionEnd
        );
    }
}

function scheduleReset() {
    clearTimeout(resetTimer);

    resetTimer = setTimeout(
        resetCandidate,
        resetDelayMilliseconds
    );
}

function resetCandidate() {
    clearTimeout(resetTimer);

    resetTimer = null;
    buffer = "";
    startedAt = 0;
    lastKeyAt = 0;
    largestGap = 0;
    inputSnapshot = null;
}

export function dispose() {
    clearTimeout(resetTimer);

    if (keyboardHandler) {
        document.removeEventListener(
            "keydown",
            keyboardHandler,
            true
        );
    }

    keyboardHandler = null;
    dotNetReference = null;

    resetCandidate();
}