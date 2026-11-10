# Fotografías etapa A — migración y validación real

## Resultado

Migración `20261109161923_EquipmentPhotoHistory` aplicada únicamente en `test-migracion-inicial`, después de la autorización expresa y la confirmación manual de proyecto/rama/endpoint. Ciclo real comprobado con ASP.NET Core Identity, Neon y Cloudinary privados. No se inició la etapa B ni se publicó en GitHub/Render.

## Destino y esquema

- Rama confirmada por el usuario: `test-migracion-inicial`, ID `br-flat-smoke-b4kdmosq`.
- Endpoint contrastado contra la configuración local: `ep-raspy-dream-b476uukh-pooler.c-6.us-east-2.aws.neon.tech`.
- Conexiones de aplicación/migración/ExpectedHost coincidentes, normalizando `-pooler`, y distintas de `NeonDirect`; TLS VerifyFull.
- Antes de aplicar: cero evidencias, migración ausente y cero tablas nuevas. SQL revisado, sin eliminación ni actualización de registros existentes; reemplazo de un índice no único por otro compuesto.
- EF mostró únicamente la migración autorizada pendiente y ejecutó solamente esa migración.
- Postflight: historial EF aplicado=1; tres tablas de fotografías presentes (`photo_evidence` existente, `photo_uploads` y `photo_principal_periods` nuevas); tres índices únicos nuevos y dos FK compuestas nuevas verificadas, con borrado restrictivo.
- Unicidad de principal vigente y rechazo de fotografía de otro equipo comprobados contra PostgreSQL, con fixtures transaccionales y rollback.

La asociación nombre de rama/ID/endpoint fue confirmada por la captura y los datos del usuario, no por la cuenta de CLI de Neon disponible, que corresponde a otro proyecto. No se utilizó la cadena de conexión enviada en el chat para actualizar archivos o comandos.

## Datos ficticios conservados

- Equipo: `VALIDA_FOTOS_A_MW9RWNV6`.
- ID: `337728c5-15e4-48bc-83ba-526f69266fdd`.
- Seis imágenes raster generadas exclusivamente para pruebas, rotuladas como ficticias y no empresariales.
- Carpeta de Cloudinary: `inventariocpr-pruebas/fotos-etapa-a-20261109`.
- Los seis recursos confirmados son `authenticated`. Todos devolvieron 401 al solicitar entrega sin firma y 404 al intentar la ruta pública de tipo upload.
- PostgreSQL conserva seis referencias con autor, observación, hash SHA-256 y versión Cloudinary; una sola principal vigente y cuatro periodos de principal. La imagen principal elegida no cambió automáticamente al subir fotos más recientes.
- No se borraron recursos Cloudinary ni registros existentes. Los dos intentos de archivo inválido/excesivo conservan reservas sin contenido preparado y sin evidencia confirmada; el mecanismo actual no tiene descarte/limpieza de reservas rechazadas.

## Pruebas reales

| Caso | Resultado |
|---|---|
| Selección múltiple y subida desde escritorio | Dos imágenes guardadas y confirmadas |
| Persistencia tras recargar | Galería, principal, miniatura de listado y panel recuperados |
| Cambio de principal | Foto anterior conservada; historial de periodos intacto |
| Dos cambios de principal simultáneos | Ambos respondieron 204; una sola selección vigente |
| Duplicado de los mismos bytes | Misma evidencia reutilizada, sin cambiar clasificación/observación originales |
| Archivo falso con extensión de imagen | Rechazado con 400 |
| Archivo de más de 10 MB | Rechazado con 413 |
| Tipo de mantenimiento sin contexto de etapa B | Rechazado con 400 |
| Mutación sin antiforgery | Rechazada con 400 |
| Foto solicitada bajo otro expediente | Rechazada con 404 |
| Consulta/descarga anónima | Redirección a login; no se entregan bytes de imagen |
| Acceso directo sin firma a Cloudinary | 401 para los seis recursos |
| Intento por ruta pública Cloudinary upload | 404 para los seis recursos |
| Nueva selección y entrada de cámara emulada en móvil | Dos imágenes adicionales subidas realmente, conservando notas y cola |
| Edición ordinaria del expediente después de subir fotos | Guardado correcto; seis fotos y su historial principal conservados |
| URLs/credenciales en navegador | Cero solicitudes directas a Cloudinary; imágenes mediante rutas autorizadas de la aplicación |

## Recuperación de fallos reales

### PostgreSQL después de la subida

Se instaló un trigger de prueba que rechazaba **solo** inserciones de evidencia para el equipo ficticio anterior. La subida a Cloudinary se completó, pero PostgreSQL rechazó la confirmación: el conteo de evidencias siguió siendo 2 y la reserva permaneció recuperable.

Se retiraron el trigger y su función en un bloque finally. Antes del reintento se comprobó que el recurso Cloudinary ya existía. El reintento recuperó el mismo PublicId y la misma versión `1794259892`, verificó su contenido y confirmó una sola nueva evidencia. No se subió una versión sustituta ni se duplicó el archivo. El conteo pasó de 2 a 3.

### Cloudinary

Una instancia local de prueba utilizó temporalmente un identificador API deliberadamente inválido, sin modificar los secretos reales ni Identity. El proveedor rechazó la comprobación/subida, el conteo permaneció en 3 y la reserva quedó pendiente. Con la configuración normal restaurada se comprobó que no existía recurso remoto para esa reserva y se reintentó: se creó y confirmó una única imagen, pasando a 4. Después se subieron las dos imágenes móviles, hasta un total de 6.

Los mecanismos de fallo ya no están activos. El postflight confirmó cero triggers y cero funciones de fallo, dos reservas de los rechazos de archivo sin contenido preparado y ninguna reserva preparada pendiente de recuperar. No se alteraron permisos, cuentas ni contraseñas para realizar las pruebas.

## Corrección encontrada

La recarga móvil detectó un fallo transitorio al abrir el expediente inmediatamente después de buscar. `UserManager` y `SignInManager` compartían el AccessDbContext del circuito Blazor y podían solapar consultas de autorización. `IdentityInventoryAccess` ahora serializa esas consultas por circuito mediante SemaphoreSlim; sigue consultando cuenta activa, sello y roles en cada operación, sin relajar las políticas.

Se añadió una integración PostgreSQL de solo lectura que ejecuta 12 comprobaciones de acceso concurrentes con Identity. La nueva suite y la repetición real de la UI pasaron.

## Responsive e Impeccable

Chromium con emulación móvil/táctil y servicios reales, a 360×800, 390×844, 768×1024, 1024×768, 1440×900 y 1920×1080:

- Seis imágenes cargadas correctamente en cada tamaño.
- Cero overflow horizontal de documento/body.
- Cero objetivos activos menores de 44 px en el componente de fotografías.
- Cero violaciones Axe WCAG A/AA en página y visor.
- Visor ampliado, flechas, Escape y conservación del expediente abiertos correctos.
- Cero errores de navegador en la ejecución final.
- Detector Impeccable sobre la galería real autenticada mediante CDP: `[]`.

Capturas reales en `.impeccable/review/`:

- `photos-real-{ancho}x{alto}-primary.png`
- `photos-real-{ancho}x{alto}-gallery.png`
- `photos-real-{ancho}x{alto}-viewer.png`
- `photos-real-mobile-selection.png` y `photos-real-mobile-progress.png`

Resultados automatizados: `photos-real-validation.json` y `photos-real-responsive.json`. Las capturas anteriores con la palabra fixture no deben confundirse con estas pruebas reales.

## Pruebas y configuración

- Suite Release: 43 superadas, 0 fallidas, 1 omitida; ocho integraciones PostgreSQL autorizadas superadas.
- Se omitió el test persistente antiguo que usa acceso stub y deja otro expediente; el flujo de persistencia de esta etapa se comprobó con la sesión manual real.
- El repositorio mantiene PhotosEnabled=false por defecto. User Secrets local tiene PhotosEnabled=true y TestAssetFolder con la carpeta anterior, después de aplicar la migración autorizada. No se trasladó esa configuración a Production/Render.
- No se mostraron firmas de entrega, claves API ni contraseñas en los resultados. La credencial de PostgreSQL expuesta por el usuario en el chat debe rotarse fuera del chat y actualizarse únicamente en el almacén local correspondiente.
- Se retiraron los loggers automáticos del HttpClient de entrega privada para evitar que registren URLs firmadas de servidor. Tras el ajuste se volvió a comprobar una descarga autenticada real: 200, image/png, bytes no vacíos y Cache-Control private/no-store.

## Limitaciones pendientes

1. Android/iPhone físicos y cámara real no comprobados. La entrada de cámara se ejercitó con archivos ficticios en emulación; Safari/iOS no fue ejecutado. LAN/HTTPS para dispositivos externos sigue sin habilitarse.
2. JPEG/PNG únicamente; HEIC no admitido. El origen cámara/archivo es declarado y no certifica fecha de captura. Las fechas de carga y actores sí se asignaron en servidor.
3. No se creó una cuenta Consulta para iniciar una sesión separada; sus políticas se cubren con tests y el servidor revalida roles. La sesión administrativa de pruebas y la denegación anónima sí se ejercitaron realmente.
4. Descarte/limpieza administrativa de reservas rechazadas y política de retención/antimalware aún pendientes. Los dos intentos negativos de esta prueba no tienen archivo Cloudinary ni evidencia confirmada.
5. Incidencias y mantenimientos (etapa B), preparación de Production y despliegue Render siguen fuera de esta entrega.
