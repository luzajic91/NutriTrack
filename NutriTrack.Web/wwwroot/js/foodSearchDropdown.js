window.registerClickOutside = function (dotNetRef, elementId) {
    const element = document.getElementById(elementId);
    if (!element) return;

    const handler = function (e) {
        if (!element.contains(e.target)) {
            dotNetRef.invokeMethodAsync('CloseDropdown');
        }
    };

    element._clickOutsideHandler = handler;
    document.addEventListener('mousedown', handler);
};

window.unregisterClickOutside = function (elementId) {
    const element = document.getElementById(elementId);
    if (!element || !element._clickOutsideHandler) return;

    document.removeEventListener('mousedown', element._clickOutsideHandler);
    delete element._clickOutsideHandler;
};
