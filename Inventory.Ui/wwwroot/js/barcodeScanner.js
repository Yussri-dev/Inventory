let dotNetReference = null;
let keyboardHandler = null;
let resetTimer = null;

let buffer = "";
let lastKeyAt = 0;
let largestGap = 0;

const maximumGapMilliseconds = 120;
const maximumEndGapMilliseconds = 150;
const resetDelayMilliseconds = 200;

const validBarcodeLengths = new Set([
    4,
    8,
    12,
    13
]);

const ignoredModifierKeys = new Set([
    "Shift",
    "Control",
    "Alt",
    "Meta",
    "CapsLock",
    "NumLock"
]);

export function initialize(reference) {
    dispose();

    dotNetReference = reference;

    keyboardHandler = handleKeyDown;

    document.addEventListener(
        "keydown",
        keyboardHandler,
        true
    );
}


function getDigit(event) {
    // Digit1..Digit0 (rangée du haut) ou Numpad0..Numpad9
    const match = /^(?:Digit|Numpad)([0-9])$/.exec(event.code);
    return match ? match[1] : null;
}

function handleKeyDown(event) {
    if (
        event.isComposing ||
        event.repeat ||
        event.ctrlKey ||
        event.altKey ||
        event.metaKey
    ) {
        return;
    }

    if (ignoredModifierKeys.has(event.key)) {
        return;
    }

    const now = event.timeStamp; // au lieu de performance.now()

    const digit = getDigit(event);

    if (digit !== null) {
        const gap = lastKeyAt === 0 ? 0 : now - lastKeyAt;

        if (buffer.length > 0 && gap > maximumGapMilliseconds) {
            resetCandidate();
        }

        if (buffer.length > 0) {
            largestGap = Math.max(largestGap, gap);
        }

        buffer += digit;
        lastKeyAt = now;
        scheduleReset();
        return;
    }

    // Autre caractère imprimable (lettre, etc.) : ce n'est pas un code-barres numérique
    if (event.key.length === 1) {
        resetCandidate();
        return;
    }

    if (event.key !== "Enter" && event.key !== "Tab") {
        return;
    }

    if (buffer.length === 0) {
        return;
    }

    const endGap = now - lastKeyAt;
    const barcode = buffer;

    const valid =
        validBarcodeLengths.has(barcode.length) &&
        largestGap <= maximumGapMilliseconds &&
        endGap <= maximumEndGapMilliseconds;

    resetCandidate();

    if (!valid) {
        return;
    }

    event.preventDefault();
    event.stopPropagation();
    event.stopImmediatePropagation();

    // Vider le champ code-barres (il contient les caractères tapés par le scanner)
    const active = document.activeElement;
    if (active && active.classList.contains("barcode-input")) {
        active.value = "";
    }

    void dotNetReference
        ?.invokeMethodAsync("OnHardwareScan", barcode)
        .catch(error => console.error("Hardware barcode processing failed.", error));
}

function scheduleReset() {
    clearTimeout(resetTimer);

    resetTimer =
        setTimeout(
            resetCandidate,
            resetDelayMilliseconds
        );
}

function resetCandidate() {
    clearTimeout(resetTimer);

    resetTimer = null;

    buffer = "";
    lastKeyAt = 0;
    largestGap = 0;
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