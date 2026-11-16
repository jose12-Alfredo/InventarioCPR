# Ventana amplia de detalle y fotografía completa

Estado: implementado y validado localmente el 13 de noviembre de 2026. El usuario eligió expresamente **Ventana amplia** frente al panel lateral y autorizó su publicación al solicitar «publicalos». Verificación de despliegue pendiente al preparar esta entrega.

## Cambios

- Detalle en diálogo nativo centrado, de hasta 70rem, con cabecera y acciones fijas. El listado conserva su ancho, sin reservar una columna lateral.
- Foto principal y nombre/código/categoría/condiciones separados en dos columnas en computadora/tablet; una columna en celular.
- Botón **Ver imagen completa** sobre la foto; también accesible con Enter y tacto. Preview y visor usan `object-fit: contain`, manteniendo el encuadre completo.
- Visor nativo independiente con carga, error, retry y cierre. Cada apertura/reintento solicita la ruta privada de la aplicación; no introduce enlaces públicos ni URLs firmadas en el cliente.
- Escape cierra solo el visor superior y devuelve foco a la foto, conservando el scroll del detalle. Cierre del expediente devuelve foco al listado.
- Identificación y ubicación agrupadas bajo General; Técnica, Control, Historial y **Fotografías** mantienen sus funciones. Edición disponible en pie persistente, con el permiso existente.
- No se modificaron entidades, migraciones, servicios de inventario/fotos, Identity ni Data Protection. No se subieron o eliminaron imágenes para esta modificación.

Fuentes: `Components/Inventory/TechnicalFormPreview.razor`, sus CSS/JS y nuevo `EquipmentPhotoViewer.razor`, CSS/JS. Contrato de superficie en `.impeccable/surfaces/inventory-equipment-detail.md`.

## Comprobaciones

Host HTTPS local con los componentes reales, cuenta de pruebas controlada y acceso de lectura a fixtures previamente autorizados de Neon/Cloudinary. La autenticación del host de validación es aislada; no se presenta como una prueba de contraseña real ni de arranque Render.

- **19 comprobaciones de captura**, con cero violaciones Axe y sin overflow de página/diálogo: vista general y visor en 360×800, 390×844, 702×915, 768×1024, 1024×768, 1440×900, 1582×905 y 1920×1080, más ausencia de foto, imagen vertical y fallo de imagen.
- Apertura con teclado, ciclo de foco, Escape anidado y recuperación de foco/scroll: aprobados. El foco vuelve también al registro actual tras actualizar la galería o cambiar de tabla a tarjetas al reducir el viewport.
- Galería existente y su propio visor, abrir edición y cancelar, cerrar/reabrir expediente: regresiones aprobadas, sin guardar cambios.
- Error de entrega inducido solo en navegador y retry: aprobado. Imagen vertical sintética de navegador: cuatro bordes visibles en visor; no se almacenó en Cloudinary.
- `tap()` sintetizado en Chromium móvil: tarjeta → foto → visor → cierre de imagen → cierre de detalle, aprobado. No es prueba de teléfono/cámara física o Safari.
- Cero errores de página y cero peticiones HTTP de mutación observadas, salvo transporte de eventos Blazor. Las acciones ensayadas consultaron y cancelaron; no se ejecutó guardado de equipos.
- Suite ordinaria: **54 aprobadas, 0 fallidas, 21 omitidas / 75**; las omitidas requieren opt-in externo/local PostgreSQL y no se habilitaron para un cambio visual.
- Build y publish Release correctos, sin errores/advertencias y con runtime Blazor incluido.
- Detector final Impeccable: **`[]`**. Avisos iniciales de tamaños tipográficos se ajustaron a la escala existente antes de las capturas finales.

Evidencia: `.impeccable/review/detail-wide-results.json`, capturas `detail-wide-overview-{width}x{height}.png`, `detail-wide-full-image-{width}x{height}.png`, `detail-wide-no-photo-1440x900.png`, `detail-wide-portrait-full-390x844.png`, `detail-wide-image-error-1440x900.png` y `detail-wide-touch-390x844.png` (ignoradas por Git). No se añadieron imágenes estáticas al producto; los PNG de prueba vertical/error son respuestas locales del navegador.

## Revisión independiente y sistema visual

Revisor independiente con contexto fresco: **disposition ship**, sin correcciones materiales, tras inspeccionar fuentes, PRODUCT.md, DESIGN.md, contrato y capturas. Se usó un agente general de revisión porque el tipo nombrado por la skill no estaba expuesto. No es una certificación de accesibilidad ni de producción.

Documentador independiente: colores, familia tipográfica, radios y sombras mantienen el sistema CPR; no hay tokens nuevos. Se corrigió solo la prosa de Layout de DESIGN.md para reflejar el cambio local aprobado. El sidecar de diseño ya estaba desactualizado antes de la tarea y se conserva sin regeneración general. Impeccable informó una actualización disponible de 4.7.0 a 4.9.0; no se actualizó la herramienta durante esta tarea.

Sugerencia no bloqueante de revisión: la navegación de secciones en móvil utiliza desplazamiento horizontal y Fotografías puede comenzar fuera de la vista. El acceso principal a la imagen se mantiene visible como botón sobre la foto; no se afirma que todas las secciones quepan simultáneamente en 360px.

## Publicación

Publicación autorizada por el usuario. Antes del push, la consulta pública confirmó el formulario de registro nuevo activo en Render (`healthz` 204 y módulo de registro nuevo), pero todavía no el detalle amplio. La comprobación posterior exigirá health 204, boot Blazor 200, módulo de detalle con diálogo nativo y nuevo módulo del visor/CSS disponibles. No se necesitan migraciones ni cambios de certificados o variables para esta mejora visual.
