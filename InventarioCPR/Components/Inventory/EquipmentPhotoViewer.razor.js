export function open(dialog) {
    if (!dialog || dialog.open) return;
    dialog._previousPhotoFocus = document.activeElement;
    if (!dialog._photoViewerBound) {
        dialog.addEventListener('keydown', event => {
            event.stopPropagation(); // Escape must close only the image, not its parent detail.
            if (event.key !== 'Tab') return;
            const controls = [...dialog.querySelectorAll('button:not([disabled])')].filter(node => node.getClientRects().length);
            const first = controls[0], last = controls.at(-1);
            if (!first) return;
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        });
        dialog.addEventListener('cancel', event => event.stopPropagation());
        dialog.addEventListener('close', () => {
            window.visualViewport?.removeEventListener('resize', dialog._photoViewport);
            if (dialog._previousPhotoFocus?.isConnected) dialog._previousPhotoFocus.focus({ preventScroll: true });
        });
        dialog.addEventListener('click', event => {
            if (event.target !== dialog) return;
            const rect = dialog.getBoundingClientRect();
            if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) close(dialog);
        });
        dialog._photoViewerBound = true;
    }
    dialog._photoViewport = () => dialog.style.setProperty('--photo-viewport-height', `${window.visualViewport?.height ?? window.innerHeight}px`);
    dialog._photoViewport();
    window.visualViewport?.addEventListener('resize', dialog._photoViewport);
    dialog.showModal();
}

export function close(dialog) { if (dialog?.open) dialog.close(); }

export function focusClose(dialog) { dialog?.querySelector('header button')?.focus({ preventScroll: true }); }
