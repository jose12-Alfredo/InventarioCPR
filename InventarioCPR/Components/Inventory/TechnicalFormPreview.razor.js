let formPreviousFocus;
let formPreviousOverflow;
let detailState;

const inlineDetailMedia = window.matchMedia('(min-width: 98.5rem)');
const shortCoarseMedia = window.matchMedia('(max-height: 30rem) and (pointer: coarse)');
const inlineDetailWidth = 1240;

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

    const workbench = list.closest('.inventory-workbench');
    if (!workbench || workbench._inventoryLayout) return;
    const update = () => workbench.classList.toggle('is-narrow', !canShowInlineDetail(workbench));
    const observer = new ResizeObserver(update);
    observer.observe(workbench);
    inlineDetailMedia.addEventListener('change', update);
    shortCoarseMedia.addEventListener('change', update);
    workbench._inventoryLayout = { observer, update };
    update();
}

export function disposeInventoryLayout(list) {
    const workbench = list?.closest('.inventory-workbench');
    const layout = workbench?._inventoryLayout;
    if (!layout) return;
    layout.observer.disconnect();
    inlineDetailMedia.removeEventListener('change', layout.update);
    shortCoarseMedia.removeEventListener('change', layout.update);
    delete workbench._inventoryLayout;
}

export function openDetail(panel, list, callback) {
    if (!panel || !list) return;
    if (detailState?.panel === panel) {
        detailState.previousFocus = findSelectedTrigger(list) || detailState.previousFocus;
        applyDetailMode();
        return;
    }
    if (detailState) closeDetail(detailState.panel, detailState.list);

    const workbench = list.closest('.inventory-workbench');
    const selectedTrigger = findSelectedTrigger(list);
    detailState = {
        panel,
        list,
        workbench,
        callback,
        overlay: false,
        previousFocus: selectedTrigger || document.activeElement,
        previousOverflow: document.body.style.overflow
    };
    detailState.observer = new ResizeObserver(applyDetailMode);
    if (workbench) detailState.observer.observe(workbench);
    detailState.onChange = applyDetailMode;
    detailState.onKeyDown = event => {
        if (!detailState?.overlay) return;
        if (event.key === 'Escape') {
            event.preventDefault();
            callback.invokeMethodAsync('OnDetailCancelAsync');
            return;
        }
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
    inlineDetailMedia.addEventListener('change', detailState.onChange);
    shortCoarseMedia.addEventListener('change', detailState.onChange);
    panel.addEventListener('keydown', detailState.onKeyDown);
    applyDetailMode();
}

function findSelectedTrigger(list) {
    return [...list.querySelectorAll('tbody tr.is-selected, .inventory-workbench__cards li.is-selected .inventory-workbench__card')]
        .find(element => element.getClientRects().length);
}

export function closeDetail(panel, list) {
    if (!detailState || detailState.panel !== panel) return;
    detailState.observer.disconnect();
    inlineDetailMedia.removeEventListener('change', detailState.onChange);
    shortCoarseMedia.removeEventListener('change', detailState.onChange);
    panel.removeEventListener('keydown', detailState.onKeyDown);
    releaseDetailOverlay(detailState);
    const previousFocus = detailState.previousFocus;
    detailState = undefined;
    if (previousFocus?.isConnected) requestAnimationFrame(() => previousFocus.focus?.());
}

function applyDetailMode() {
    if (!detailState) return;
    const overlay = !canShowInlineDetail(detailState.workbench);
    if (overlay && !detailState.overlay) {
        detailState.overlay = true;
        detailState.previousOverflow = document.body.style.overflow;
        detailState.list.inert = true;
        document.body.style.overflow = 'hidden';
        requestAnimationFrame(() => detailState?.panel.querySelector('.inventory-workbench__detail-close')?.focus());
    } else if (!overlay && detailState.overlay) {
        releaseDetailOverlay(detailState);
    }
}

function canShowInlineDetail(workbench) {
    return !!workbench && inlineDetailMedia.matches && !shortCoarseMedia.matches &&
        workbench.getBoundingClientRect().width >= inlineDetailWidth;
}

function releaseDetailOverlay(state) {
    if (!state.overlay) return;
    state.overlay = false;
    state.list.inert = false;
    document.body.style.overflow = state.previousOverflow ?? '';
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
