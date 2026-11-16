let formPreviousFocus;
let formPreviousOverflow;
let detailState;

export function openForm(dialog, callback) {
    if (!dialog || dialog.open) return;
    formPreviousFocus = document.activeElement;
    formPreviousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    if (!dialog.dataset.cancelBound) {
        dialog.addEventListener('cancel', event => {
            event.preventDefault();
            callback.invokeMethodAsync('OnDialogCancelAsync');
        });
        dialog.addEventListener('keydown', event => {
            if (event.key !== 'Tab' || event.target.closest('dialog') !== dialog) return;
            const controls = [...dialog.querySelectorAll('button, a[href], input, select, textarea, summary, [tabindex]')]
                .filter(element => !element.disabled && element.tabIndex >= 0 && element.getClientRects().length);
            const first = controls[0], last = controls.at(-1);
            if (!first) return;
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
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
    document.body.style.overflow = formPreviousOverflow ?? '';
    formPreviousFocus?.focus?.();
}

export function bindInventoryKeys(list) {
    if (!list) return;
    if (!list.dataset.keyboardBound) {
        list.addEventListener('keydown', event => {
            if (event.key === ' ' && event.target.matches('.inventory-workbench__table tbody tr')) event.preventDefault();
        });
        list.dataset.keyboardBound = 'true';
    }

}

export function disposeInventoryLayout(list) {
    if (detailState?.list === list) closeDetail(detailState.panel, list);
}

export function openDetail(panel, list, callback) {
    if (!panel || !list) return;
    if (detailState?.panel === panel && panel.open) return;
    if (detailState) closeDetail(detailState.panel, detailState.list);

    const selectedTrigger = findSelectedTrigger(list);
    detailState = {
        panel,
        list,
        callback,
        previousFocus: selectedTrigger || document.activeElement,
        equipmentId: selectedTrigger?.dataset.equipmentId,
        previousOverflow: document.body.style.overflow
    };
    detailState.onKeyDown = event => {
        if (event.target.closest('dialog') !== panel) return;
        if (event.key !== 'Tab') return;
        const focusable = [...panel.querySelectorAll('button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])')]
            .filter(element => element.getClientRects().length);
        if (focusable.length === 0) {
            event.preventDefault();
            panel.focus();
            return;
        }
        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    };
    detailState.onCancel = event => {
        if (event.target !== panel) return;
        event.preventDefault();
        callback.invokeMethodAsync('OnDetailCancelAsync');
    };
    detailState.onClick = event => {
        if (event.target !== panel) return;
        const rect = panel.getBoundingClientRect();
        if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom)
            callback.invokeMethodAsync('OnDetailCancelAsync');
    };
    detailState.updateViewport = () => panel.style.setProperty('--detail-viewport-height', `${window.visualViewport?.height ?? window.innerHeight}px`);
    detailState.updateViewport();
    window.visualViewport?.addEventListener('resize', detailState.updateViewport);
    panel.addEventListener('keydown', detailState.onKeyDown);
    panel.addEventListener('cancel', detailState.onCancel);
    panel.addEventListener('click', detailState.onClick);
    document.body.style.overflow = 'hidden';
    if (!panel.open) panel.showModal();
    const body = panel.querySelector('.inventory-workbench__detail-body');
    if (body) body.scrollTop = 0;
    panel.querySelector('.inventory-workbench__detail-close')?.focus();
}

function findSelectedTrigger(list) {
    return [...list.querySelectorAll('tbody tr.is-selected, .inventory-workbench__cards li.is-selected .inventory-workbench__card')]
        .find(element => element.getClientRects().length);
}

export function closeDetail(panel, list) {
    if (!detailState || detailState.panel !== panel) return;
    window.visualViewport?.removeEventListener('resize', detailState.updateViewport);
    panel.removeEventListener('keydown', detailState.onKeyDown);
    panel.removeEventListener('cancel', detailState.onCancel);
    panel.removeEventListener('click', detailState.onClick);
    if (panel.open) panel.close();
    document.body.style.overflow = detailState.previousOverflow ?? '';
    const currentTrigger = [...list.querySelectorAll('[data-equipment-id]')]
        .find(element => element.dataset.equipmentId === detailState.equipmentId && element.getClientRects().length);
    const previousFocus = currentTrigger || detailState.previousFocus;
    detailState = undefined;
    if (previousFocus?.isConnected && previousFocus.getClientRects().length) requestAnimationFrame(() => previousFocus.focus?.());
}

export function confirmDiscard(message) {
    return window.confirm(message || 'Hay cambios sin guardar. ¿Desea cerrar el formulario y descartarlos?');
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
