const states = new WeakMap();

export function bind(root, callback) {
    states.set(root, { callback, files: [], batch: crypto.randomUUID(), disposed: false, busy: false });
}

export async function request(url, method = 'GET', body = null) {
    const options = { method, credentials: 'same-origin', cache: 'no-store', headers: {} };
    if (method !== 'GET') {
        const base = url.split('/photos')[0] + '/photos';
        const tokenResponse = await fetch(base + '/token', { credentials: 'same-origin', cache: 'no-store' });
        if (!tokenResponse.ok || tokenResponse.redirected) throw new Error('La sesión no está disponible.');
        const token = await tokenResponse.json();
        options.headers.RequestVerificationToken = token.token;
        if (body !== null) { options.headers['Content-Type'] = 'application/json'; options.body = JSON.stringify(body); }
    }
    const response = await fetch(url, options);
    if (!response.ok || response.redirected) {
        let message = 'No se confirmó la operación. Compruebe su sesión.';
        try { message = (await response.json()).error || message; } catch { }
        throw new Error(message);
    }
    return response.status === 204 ? null : response.json();
}

export function bindInputs(root) {
    const state = states.get(root);
    if (!state) return;
    for (const input of root.querySelectorAll('input[type=file]')) {
        if (input.dataset.photoBound) continue;
        input.dataset.photoBound = 'true';
        input.addEventListener('change', async () => {
            if (state.busy) { input.value = ''; return; }
            const retry = input.classList.contains('photo-retry-file') ? state.retry : null;
            const incoming = [...input.files];
            input.value = '';
            if (!incoming.length) return;
            if (incoming.length + state.files.length > 10 || incoming.some(file => file.size > 10 * 1024 * 1024 || file.size === 0 || file.name.length > 200)) {
                await state.callback.invokeMethodAsync('SelectionError', 'Seleccione hasta 10 imágenes de hasta 10 MB cada una, con nombres de hasta 200 caracteres.');
                return;
            }
            for (const file of incoming) {
                const header = new Uint8Array(await file.slice(0, 8).arrayBuffer());
                const png = [137,80,78,71,13,10,26,10].every((value,index) => header[index] === value);
                const jpg = header[0] === 255 && header[1] === 216 && header[2] === 255;
                if (!png && !jpg) {
                    await state.callback.invokeMethodAsync('SelectionError', 'Seleccione JPEG o PNG. HEIC y otros formatos no están habilitados.');
                    return;
                }
            }
            const currentMetadata = await state.callback.invokeMethodAsync('GetSelectionMetadata');
            for (const item of state.files) {
                const selected = currentMetadata.find(value => value.key === item.key);
                if (selected && !item.id) { item.type = selected.type; item.note = selected.note; }
            }
            state.retry = null;
            state.files.push(...incoming.map(file => ({ file, key: crypto.randomUUID(), url: URL.createObjectURL(file), id: retry?.id || null,
                type: retry?.type || 'General', note: retry?.note || '', source: retry?.source || (input.classList.contains('photo-camera') ? 'Camara' : 'Archivo'), completed: false })));
            state.batch = crypto.randomUUID();
            await notify(state);
        });
    }
}

async function notify(state) {
    if (state.disposed) return;
    await state.callback.invokeMethodAsync('SelectionChanged', state.files.map(item => ({ key: item.key, name: item.file.name,
        url: item.url, type: item.type, note: item.note, source: item.source, reserved: !!item.id,
        percent: item.completed ? 100 : 0, message: item.completed ? 'Fotografía guardada.' : 'Pendiente de subir' })));
}

export async function remove(root, index, metadata) {
    const state = states.get(root);
    if (!state || state.busy) return;
    for (const item of state.files) {
        const current = metadata.find(value => value.key === item.key);
        if (current && !item.id) { item.type = current.type; item.note = current.note; }
    }
    URL.revokeObjectURL(state.files[index].url);
    state.files.splice(index, 1);
    state.batch = crypto.randomUUID();
    await notify(state);
}

export function chooseRetry(root, pending) {
    const state = states.get(root);
    if (!state || state.busy) return;
    state.retry = pending;
    root.querySelector('.photo-retry-file').click();
}

export async function upload(root, base, metadata) {
    const state = states.get(root);
    if (!state || state.busy) return;
    state.busy = true;
    let failed = false;
    try {
        const fresh = state.files.filter(item => !item.id);
        if (fresh.length) {
            const drafts = fresh.map(item => {
                const selected = metadata.find(x => x.key === item.key);
                item.type = selected.type; item.note = selected.note;
                return { fileName: item.file.name, type: item.type, note: item.note, source: item.source };
            });
            const reserved = await request(base + '/batches', 'POST', { batchId: state.batch, files: drafts });
            fresh.forEach((item, index) => item.id = reserved[index].id);
        }
        for (const item of state.files) {
            if (state.disposed || item.completed) continue;
            try {
                await state.callback.invokeMethodAsync('UploadProgress', item.key, 0, 'Subiendo…', true);
                const token = await request(base + '/token');
                const receipt = await new Promise((resolve, reject) => {
                    const xhr = new XMLHttpRequest();
                    state.xhr = xhr;
                    xhr.open('POST', `${base}/uploads/${item.id}/content`);
                    xhr.withCredentials = true;
                    xhr.timeout = 120000;
                    xhr.setRequestHeader('RequestVerificationToken', token.token);
                    xhr.setRequestHeader('Content-Type', 'application/octet-stream');
                    xhr.upload.onprogress = event => {
                        if (event.lengthComputable && !state.disposed) state.callback.invokeMethodAsync('UploadProgress', item.key,
                            Math.min(99, Math.round(100 * event.loaded / event.total)), 'Transfiriendo; guardado pendiente…', true).catch(() => {});
                    };
                    xhr.onload = () => {
                        if (xhr.status >= 200 && xhr.status < 300 && xhr.getResponseHeader('Content-Type')?.includes('application/json')) {
                            try { resolve(JSON.parse(xhr.responseText)); } catch { reject(new Error('El servidor no confirmó el archivo.')); }
                        }
                        else { let message = 'No se confirmó la subida. Reintente el mismo archivo.'; try { message = JSON.parse(xhr.responseText).error || message; } catch { } reject(new Error(message)); }
                    };
                    xhr.onerror = xhr.ontimeout = xhr.onabort = () => reject(new Error('Conexión interrumpida. Reintente el mismo archivo.'));
                    xhr.send(item.file);
                });
                item.completed = true;
                await state.callback.invokeMethodAsync('UploadProgress', item.key, 100, receipt.alreadyExists
                    ? `Ya estaba guardada como ${receipt.type}. Se conserva su clasificación y observación originales.` : 'Fotografía guardada.', true);
            } catch (error) {
                failed = true;
                if (!state.disposed) await state.callback.invokeMethodAsync('UploadProgress', item.key, 0, error.message, true);
            }
        }
    } finally { state.busy = false; state.xhr = null; }
    if (failed) throw new Error('Hay archivos pendientes.');
}

export function showViewer(dialog, callback) {
    if (!dialog.dataset.viewerBound) {
        dialog.dataset.viewerBound = 'true';
        dialog.addEventListener('keydown', event => {
            event.stopPropagation(); // Escape must close only this viewer, not the underlying detail drawer.
            if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
                event.preventDefault(); callback.invokeMethodAsync('NavigateViewer', event.key === 'ArrowLeft' ? -1 : 1);
            }
        });
        dialog.addEventListener('close', () => {
            document.body.style.overflow = dialog._photoOverflow ?? '';
            dialog._photoFocus?.focus?.();
        });
    }
    if (!dialog.open) {
        dialog._photoFocus = document.activeElement;
        dialog._photoOverflow = document.body.style.overflow;
        document.body.style.overflow = 'hidden';
        dialog.showModal();
    }
}

export function closeViewer(dialog) { if (dialog?.open) dialog.close(); }
export function dispose(root, dialog) {
    const state = states.get(root);
    if (state) { state.disposed = true; state.xhr?.abort(); state.files.forEach(item => URL.revokeObjectURL(item.url)); }
    closeViewer(dialog);
    states.delete(root);
}
