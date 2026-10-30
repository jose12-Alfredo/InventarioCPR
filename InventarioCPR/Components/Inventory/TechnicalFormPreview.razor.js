let previousFocus;
let previousOverflow;

export function openForm(dialog, callback) {
    if (!dialog || dialog.open) return;
    previousFocus = document.activeElement;
    previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    if (!dialog.dataset.cancelBound) {
        dialog.addEventListener('cancel', event => {
            event.preventDefault();
            callback.invokeMethodAsync('OnDialogCancelAsync');
        });
        dialog.dataset.cancelBound = 'true';
    }
    const tabs = dialog.querySelector('.technical-preview__tabs');
    if (tabs && !tabs.dataset.keyboardBound) {
        tabs.addEventListener('keydown', event => {
            if (['ArrowRight', 'ArrowLeft', 'Home', 'End'].includes(event.key)) event.preventDefault();
        });
        tabs.dataset.keyboardBound = 'true';
    }
    dialog.showModal();
    const updateViewport = () => {
        if (!dialog.open || !window.visualViewport) return;
        dialog.style.setProperty('--dialog-viewport-height', `${window.visualViewport.height}px`);
        dialog.style.setProperty('--dialog-viewport-top', `${window.visualViewport.offsetTop}px`);
    };
    dialog._updateViewport = updateViewport;
    window.visualViewport?.addEventListener('resize', updateViewport);
    window.visualViewport?.addEventListener('scroll', updateViewport);
    updateViewport();
    requestAnimationFrame(() => focusCode(dialog));
}

export function closeForm(dialog) {
    if (!dialog?.open) return;
    window.visualViewport?.removeEventListener('resize', dialog._updateViewport);
    window.visualViewport?.removeEventListener('scroll', dialog._updateViewport);
    dialog.close();
    document.body.style.overflow = previousOverflow ?? '';
    previousFocus?.focus?.();
}

export function confirmDiscard() {
    return window.confirm('Hay cambios sin guardar. ¿Desea cerrar el formulario y descartarlos?');
}

export function focusTab(index) {
    const tab = document.getElementById(`form-tab-${index}`);
    if (tab?.getClientRects().length) tab.focus();
}

export function focusError(dialog) {
    dialog?.querySelector('[role="alert"]')?.focus({ preventScroll: false });
}

export function focusCode(dialog) {
    dialog?.querySelector('#asset-code')?.focus({ preventScroll: false });
}
