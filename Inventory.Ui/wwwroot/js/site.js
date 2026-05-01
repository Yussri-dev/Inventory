window.focusElement = (element) => {
    if (element) element.focus();
};

window.forceFocus = (element) => {
    if (!element) return;

    setInterval(() => {
        if (document.activeElement !== element) {
            element.focus();
        }
    }, 100);
};