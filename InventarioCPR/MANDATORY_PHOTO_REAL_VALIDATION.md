# Fotografía obligatoria — validación real en Neon y Cloudinary

Validación realizada entre el **12 y el 13 de noviembre de 2026**, tras autorización expresa del usuario. Estado: migración aplicada y pruebas reales aprobadas **solo en `test-migracion-inicial`**. Sin commit, push ni despliegue. No se cambiaron credenciales, roles, cuentas ni claves/configuración Data Protection de la aplicación.

## 1. Preflight y migración

- Rama autorizada: `test-migracion-inicial`; PostgreSQL confirmó directamente `neon.branch_id = br-flat-smoke-b4kdmosq` y `neon.endpoint_id = ep-raspy-dream-b476uukh` antes de escribir.
- Endpoint utilizado: `ep-raspy-dream-b476uukh-pooler.c-6.us-east-2.aws.neon.tech`; base `neondb`, TLS `VerifyFull`. Coincide con `NeonMigrationTest`/`ExpectedHost`/`NeonDevelopmentApp`; el control existente rechaza el host de `NeonDirect`, al que no se abrió conexión.
- Única migración de inventario pendiente: `20261112200209_EquipmentDraftFinalization`.
- El SQL revisado coincidió con el script idempotente generado por EF. SHA-256 del contenido UTF-8 ejecutado: `B0697A7E61825ADDB143306BFBCBBAF2C10A4A91EA269EAAC8A00204BD368EE3`.
- Se comprobaron permisos de propietario efectivo de `equipment` y creación en `public`, sin emitir GRANT ni cambiar roles.
- Revisados backfill `Definitive/false`, defaults nuevos `Draft/true`, dos checks e índice de estado/código; se conserva el índice único global de código. Guardados, uploads, selección principal y finalización usan serialización sobre la misma fila; el borrador también tiene revisión optimista.
- El script solo amplía `equipment`, crea índice/checks y añade su entrada de historial. No contiene DELETE, DROP ni UPDATE de datos anteriores; no altera tablas de fotos, Identity ni `security`.

**Aplicada:** `20261112200209_EquipmentDraftFinalization`, una sola entrada en `__EFMigrationsHistory`.

Postflight inmediato: **42/42 equipos anteriores quedaron Definitive**, con `RequiresRegistrationPhoto=false`, sin inventar fecha de finalización ni JSON de borrador. Defaults verificados para nuevas altas: `Draft` y `true`; dos checks y ambos índices previstos presentes.

Se compararon conteos/checksums de las **33 tablas** `public` + `security`, excluyendo únicamente las seis columnas nuevas de `equipment` y la entrada autorizada de migración: **todas las columnas originales y las otras tablas coincidieron**. No apareció la discrepancia de Identity que se había observado en la fase inicial de Data Protection.

## 2. Entorno real utilizado

Se reutilizaron los componentes Razor, `InventoryService`, `EquipmentPhotoService`, `CloudinaryPhotoStorage`, endpoints privados y permisos actuales. Npgsql y Cloudinary apuntaron a los servicios reales; no se utilizó almacenamiento de imágenes simulado en estas pruebas.

El transporte web fue un **host local de validación HTTPS/Kestrel**, solo localhost, con la rama autorizada. Para evitar pedir contraseñas o escribir en Identity, emitió una cookie controlada usando los claims/sello de la cuenta de pruebas existente. No validó su contraseña real ni creó usuarios. La comprobación Consulta utilizó una cookie de prueba con claim Consulta, sin cambiar los roles persistidos. No equivale a probar password-login o logout completo contra Neon.

Ese host temporal usó protección de cookies efímera en memoria, exclusivamente para aislar su autenticación de pruebas. **No sustituyó la configuración de la aplicación, no tocó sus certificados ni leyó/escribió el llavero Neon de Data Protection.** No fue una ejecución de `Program.cs` en modo Render ni un despliegue.

Carpeta exclusiva de imágenes ficticias: **`inventariocpr-pruebas/foto-obligatoria-20261112`**, configurada solo en el harness; no se modificó el secreto/configuración habitual de carpeta.

## 3. Evidencias conservadas

| Propósito | Código | Equipo | Principal | Fotos |
| --- | --- | --- | --- | ---: |
| Concurrencia y recuperación Cloudinary | `VALIDA_OBL_CONC_1778092C56` | `8bf0f83c-844a-4de4-8689-f378bc2c9b71` | `2a121b34-20ad-491d-8a6f-d70ab42c322f` | 1 |
| Registro rápido, cierre y reapertura de navegador | `VALIDA_OBL_RAPIDO_1778092C56` | `03940b48-052d-46bb-9927-5f1cc90f8520` | `4a3cb409-33d2-4d2f-aef3-3309d3b6789f` | 2 |
| Registro completo y control de cámara | `VALIDA_OBL_COMPLETO_1778092C56` | `38f174e5-82df-4658-854d-1e8c5b79a583` | `fd18fb87-c078-4ac1-87a8-6a2dfb71d5d7` | 1 |

Los tres quedaron `Definitive`, `RequiresRegistrationPhoto=true`, fecha real de finalización y sin JSON de borrador. Están en el listado definitivo y no en Borradores. **Tres equipos nuevos, cuatro evidencias y cuatro reservas completadas; cero expedientes duplicados y cero borradores de estos fixtures pendientes.**

Cloudinary confirmó por API las cuatro imágenes `authenticated`, carpeta esperada y versión coincidente con Neon. La entrega privada desde el servidor devolvió bytes que pasaron la verificación de integridad.

Ejemplo concreto: principal del registro rápido, PublicId `inventario/equipos/03940b48052d46bb99275f1cc90f8520/bde24343febd4bca8fe91842e071d38b`, versión **1794575698**, PNG **7861 bytes**. No se guardan URLs firmadas en este documento ni se entregan al navegador.

## 4. Pruebas solicitadas y resultados

| Prueba | Resultado real |
| --- | --- |
| Crear borrador sin foto | Aprobada, por servicio y formulario rápido; queda Draft y no habilita Finalizar. |
| Guardar y cerrar navegador | Aprobada; Chrome se cerró realmente y se verificó la desconexión. |
| Reabrir y retomar | Aprobada en un nuevo proceso de navegador, reautenticado con cookie controlada; se recuperaron código, nombre y categoría de Neon. |
| Subir foto real de prueba | Aprobada mediante endpoints privados y Cloudinary real; también varias fotos. |
| Confirmar principal | Aprobada; primera confirmada automática y cambio posterior conservando ambos periodos. |
| Finalizar | Aprobada con verificación remota de objeto/hash en servidor. |
| Aparecer en definitivos y salir de borradores | Aprobada por UI y servicio para los tres fixtures. |
| Recargar y recuperar datos/foto | Aprobada; miniatura real cargada y entrega privada 200. Registro completo conserva procesador ficticio, RAM manual 16 GB y SO ficticio. |
| Subida fallida y recuperación | Aprobada en dos capas, detalladas abajo. |
| Finalizar sin fotografía | Rechazado por el servicio, sin cambiar el borrador. |
| Saltarse la regla por SaveAsync | Rechazado para un Draft existente. La variante de código repetido tampoco creó otro expediente. |
| Equipos históricos sin foto | Aprobada lectura de expediente anterior sin foto; backfill conserva los 42 históricos. |
| Dos finalizaciones simultáneas | Aprobada con dos invocaciones concurrentes `Task.WhenAll` contra Neon; un equipo, una foto y un único hecho de observación. |

### Reintentos reales

1. **Fallo de cliente Cloudinary controlado:** un cliente temporal separado usó una API key deliberadamente inválida; no se cambió ninguna credencial existente. La llamada al servicio real falló y no dejó evidencia ni finalizó el equipo. Con el cliente normal se recuperó la misma reserva `85a8d546-e8be-454e-9d59-bf3091f4784f`, mismo PublicId y una sola evidencia. Reenviar los mismos bytes devolvió el mismo ID, sin sobrescribir.
2. **Fallo de transporte en navegador:** Playwright abortó el primer POST de contenido de un lote de dos archivos. La otra imagen sí se confirmó en Cloudinary. La UI mostró el fallo y mantuvo Finalizar bloqueado por el archivo pendiente; al reintentar se completó sobre el mismo equipo y reservas. Permanecieron dos evidencias, no cuatro; ninguna imagen histórica fue eliminada.

La interrupción de red fue inducida por el harness, no una caída espontánea de Cloudinary. La recuperación sí usó Cloudinary y Neon reales.

## 5. Seguridad y conservación

| Comprobación HTTP/servicio | Resultado |
| --- | --- |
| Cambiar principal sin antiforgery | **400** |
| Usar foto de otro equipo como principal | **404** |
| Leer foto por el equipo equivocado | **404** |
| Enviar bytes inválidos a reserva ya completada | **400**, evidencia anterior intacta |
| POST a ruta alternativa de finalización no expuesta | **405**; no existe una ruta que acepte `registrationState` del cliente |
| Leer foto anónimamente | **302** a acceso |
| Cookie manipulada | **302** a acceso |
| Sesión controlada Consulta | Galería **200**, `CanWrite=false`, foto **200**; escritura **302** a acceso denegado incluso con token antiforgery válido |
| SaveAsync sobre Draft intentando alta definitiva | `InventoryValidationException` |
| Duplicar código de fixture | Rechazado; no aparece otro equipo |

Auditoría final por hashes de filas: **todas las filas anteriores de las 33 tablas siguen presentes e idénticas**. Identity y Data Protection conservaron también sus conteos, sin filas nuevas ni modificadas. Se mantienen las dos claves históricas de protección y la cuenta existente.

Únicas adiciones observadas: `equipment` +3, `equipment_categories` +1 (categoría canónica necesaria para el fixture), `equipment_observations` +1, `photo_evidence` +4, `photo_principal_periods` +4, `photo_uploads` +4 y `technical_configurations` +1. No hubo borrados ni cambios en equipos anteriores. Los fixtures e imágenes permanecen como evidencia; no se ejecutó limpieza remota.

El control inicial de Data Protection que difirió en `access_users` sigue siendo un hecho histórico no atribuible con los datos disponibles. Esta migración y estas pruebas **no reprodujeron la diferencia ni mostraron una modificación nueva**.

## 6. Responsive, Impeccable y compilación

- Skill Impeccable cargada realmente; contexto y diseño existente consultados. Se mantuvo la UI existente sin cambios de diseño en esta fase.
- **42 combinaciones actuales de viewport/estado**: 36 del flujo/borradores y seis adicionales de galería con fotos reales, a 360×800, 390×844, 768×1024, 1024×768, 1440×900 y 1920×1080.
- **Axe: 0 violaciones**, sin overflow horizontal en esos controles y con footer de formulario visible. Cero errores no controlados de página en la prueba completa.
- Se comprobó foco por teclado dentro del modal y viewport de teclado emulado 390×430.
- En Chromium con `isMobile`/`hasTouch`, eventos `tap()` abrieron la tarjeta, la imagen y cerraron el visor. Son eventos táctiles sintetizados, no un teléfono físico.
- El control `input capture="environment"` recibió una imagen ficticia y el servidor conservó origen **Camara** en la reserva completada. No se abrió una cámara nativa real.
- El navegador no pidió recursos directamente a `res.cloudinary.com`: las imágenes se entregaron por las rutas privadas de la aplicación.
- Detector Impeccable de fuentes: **`[]`**.
- Publish Release aislado correcto, sin errores/advertencias y con `wwwroot/_framework/blazor.web.js` incluido.

La prueba relacional local anterior (64 aprobadas, 0 fallidas, 10 omitidas) se conserva como evidencia de implementación; los resultados de esta fase son las comprobaciones reales descritas arriba, no una nueva ejecución de aquella suite.

## 7. Evidencia local y límites antes de Render

Informe de pruebas JSON/capturas sin secretos en `.impeccable/review/mandatory-real-*` (ignorados por Git). Capturas destacadas:

- `mandatory-real-quick-definitive-reloaded.png`
- `mandatory-real-complete-definitive-reloaded.png`
- `mandatory-real-photo-detail-390x844.png`
- `mandatory-real-touch-viewer-390x844.png`
- `mandatory-real-upload-failed-{width}x{height}.png`

Harness temporal protegido: `C:\Users\jose1\AppData\Local\Temp\opencode\mandatory-photo-real`. Sus archivos privados de autenticación/snapshots no se copiaron al repositorio. Los resultados `migration.result.json`, `service.result.json`, `browser.result.json`, `security.result.json`, `photo-responsive.result.json`, `postflight.result.json` y `preservation.result.json` separan cada comprobación.

**Pendiente:** autorización de publicación/despliegue y validación de esta versión en Render real; ejecutar allí el arranque completo, login con contraseña real de forma manual, nueva alta/foto y reinicio/reconexión. No se configuraron certificados definitivos ni se reabrió la implementación Data Protection. Las condiciones de activación ya documentadas de esa infraestructura siguen vigentes.

Android/iPhone físicos, Safari y cámara nativa siguen sin certificación. La versión publicada anteriormente en Render no se ha actualizado con esta funcionalidad por haber migrado la base. No se afirma lo contrario.
