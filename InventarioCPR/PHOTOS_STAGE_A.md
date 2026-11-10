# Fotografías — etapa A: arquitectura y revisión de migración

## Estado

**Migración aplicada y flujo real validado en `test-migracion-inicial` tras autorización expresa.** El repositorio conserva `Inventory:PhotosEnabled=false` por defecto; User Secrets local habilita la función y la carpeta exclusiva de pruebas. La ejecución, los resultados y las limitaciones están en `PHOTOS_STAGE_A_VALIDATION.md`. Este documento conserva el diseño y la revisión previa. No se inició la etapa B ni se habilitó Production.

## Esquema propuesto

Migración nueva: `20261109161923_EquipmentPhotoHistory`.

SQL para revisión: `Infrastructure/Persistence/Migrations/EquipmentPhotoHistory.review.sql`.

### photo_evidence (tabla existente)

Añade columnas **nullable**: `UploadedById`, `UploadedByName`, `Observation`, `Source`, `Sha256`, `Format`, `Width`, `Height`, `ByteLength`, `CloudinaryVersion`. Los registros históricos conservan sus referencias y no reciben autores, fechas ni huellas inventados.

- Clave alternativa `(Id, EquipmentId)` para relaciones compuestas.
- Índice único parcial `(EquipmentId, Sha256)` cuando la huella no es nula: una imagen idéntica ya guardada se reutiliza, sin cambiar sus metadatos originales. La UI informa de esa reutilización y del tipo original.
- Índice para paginación por equipo/fecha/ID. Reemplaza el antiguo índice no único de EquipmentId; no se elimina ningún dato.
- Check de integridad de los metadatos nuevos y sus límites.
- La relación compuesta con inspección del mismo equipo se conserva.

### photo_uploads (nueva)

Reservas de subida por archivo: equipo, lote, posición 0–9, usuario y nombre de usuario del servidor, nombre original de selección, tipo, observación, origen declarado, fecha de reserva, huella/dimensiones/formato/tamaño y confirmación final.

- La reserva se confirma en Neon **antes** de escribir en Cloudinary.
- Su ID determina un PublicId privado inmutable. Un reintento usa la misma reserva.
- Unicidad `(EquipmentId, ActorId, BatchId, Position)`; reintentar el mismo lote no crea otro lote.
- FK compuesta `(EvidenceId, EquipmentId)` impide confirmar una foto de otro equipo.
- Solo el usuario de la reserva puede subir/reintentar sus archivos.
- Máximo 10 archivos por lote y 50 reservas pendientes por usuario/equipo. Las reservas no se eliminan automáticamente; la UI permite recuperarlas con el archivo correspondiente. La limpieza administrativa de reservas abandonadas queda fuera de esta etapa.

### photo_principal_periods (nueva)

Foto seleccionada, equipo, inicio/fin UTC y actor del cambio.

- **Índice único parcial por EquipmentId con ToUtc IS NULL**: como máximo una principal vigente.
- FK compuesta `(PhotoId, EquipmentId)`: no puede seleccionarse una fotografía de otro expediente.
- Cambio transaccional: bloqueo de la fila del equipo, cierre del periodo anterior, nueva selección. Repetir la selección vigente no duplica periodos.
- Las fotos anteriores siguen en la galería; EF impide borrar evidencias y modificar periodos cerrados mediante el contexto de aplicación.

Los IDs y nombres de los actores son snapshots validados contra Identity en cada operación, no custodios StaffMember. No se añade una FK entre los dos DbContexts ni se modifican tablas de Identity. La fecha de carga se genera al confirmar la evidencia; no se ofrece una fecha de captura como si estuviera comprobada.

No existen todavía tablas/FK de incidencias o mantenimientos. Esta etapa no admite tipos de mantenimiento/daño sin contexto; esas asociaciones compuestas se incorporarán en una migración separada de etapa B.

## Compatibilidad y revisión del SQL

- No hay `DELETE`, actualizaciones de datos antiguos ni eliminación de tablas en el script de avance.
- Las diez columnas nuevas aceptan NULL y los checks permiten registros históricos sin metadatos nuevos.
- No se asigna automáticamente la última foto como principal; se selecciona explícitamente. Sin principal se conserva el placeholder.
- Las migraciones históricas no se modificaron. Se actualizó el snapshot del modelo y se añadió la nueva migración.
- El método Down se niega a revertir si existen periodos, reservas o evidencias nuevas: no elimina historia silenciosamente.
- Preflight local de solo lectura: endpoint de aplicación coincide con ExpectedHost/migración y difiere de NeonDirect; marcador `test-migracion-inicial`; evidencias existentes=0; migración aplicada=0; columna nueva=0; tablas nuevas=0/2. La asociación hostname/rama sigue siendo la previamente confirmada en Neon; verificarla nuevamente antes de ejecutar.

La autorización posterior permitió aplicar únicamente esta migración mediante el factory `--migration-test`, después de contrastar el hostname y el ID de rama confirmados por el usuario. El postflight verificó tablas, índices y claves foráneas. No repetir la aplicación ni utilizar este factory para Production; cualquier otro destino requiere autorización separada.

## Servicio y seguridad

- Rutas bajo `/api/equipment/{equipmentId}/photos`.
- GET galería paginada y token antiforgery; POST reserva de lote, bytes del archivo y selección de principal; GET contenido protegido con variante fija `size=thumb`.
- Policies Read/Write más usuario activo, sello de seguridad y roles actuales consultados en servidor. Se conserva la compuerta de entorno para escrituras.
- Validación de pertenencia foto/reserva/equipo en cada consulta. Reserva de otro usuario no puede recuperarse.
- CSRF obligatorio en todas las mutaciones. Límite de solicitudes por usuario y tamaños máximos de body.
- Transferencia de archivos binaria por HTTP, sin base64 ni bytes por el circuito Blazor. Lectura incremental limitada a 10 MB; procesamiento en memoria acotada, serializado globalmente para limitar memoria de píxeles decodificados.
- JPEG/PNG reales, hasta 25 MP, no animados. Se verifica contenido sin confiar en MIME o nombre. Se aplica orientación y se recodifica sin EXIF; el hash corresponde a los bytes saneados guardados, no al archivo original.
- El origen Cámara/Archivo es declarado por la interfaz, no una certificación del dispositivo. No se extrae una fecha de captura verificada de EXIF.
- Cloudinary `authenticated`, Overwrite=false e ID por reserva. Reintentar primero consulta ese recurso; si ya existe, verifica su huella antes de reutilizarlo.
- Si falla Cloudinary o la confirmación en Neon, la reserva queda recuperable. No se elimina ningún recurso histórico ni se declara una evidencia confirmada antes del commit.
- Descarga servidor→Cloudinary con TLS y sin redirecciones, límite de bytes y timeout. URL firmada solo dentro del servidor. El navegador recibe únicamente una ruta de la aplicación con autorización por solicitud.
- Se verifica la huella del original antes de entregar/crear miniatura. Para legados sin huella se mantiene esa ausencia explícita; se validan formato y límites de imagen.
- Cache-Control private/no-store, nosniff y sin caché pública de fotografías. No hay endpoints de borrado ni sobrescritura.

## Interfaz

Componente `EquipmentPhotos` dentro de Evidencias del panel existente. Selector múltiple, cámara progresiva cuando el navegador la soporte, cola de vistas previas, tipo General/Serial/Accesorio, observación y progreso por archivo. El 100 % de transferencia no equivale a guardado; se espera confirmación del servidor. Los archivos confirmados se omiten en reintentos.

Galería paginada, selección explícita de principal, historial de periodos y visor nativo modal con imagen proporcional, navegación por teclado y restauración de foco. Escape cierra el visor sin cerrar el expediente subyacente. Tabla, tarjetas móviles y cabecera del detalle consultan solo la principal vigente y utilizan rutas protegidas.

En iPhone/Android la cámara y formatos dependen del navegador/dispositivo. HEIC no está admitido; usar JPEG/PNG. No se habilitó LAN ni se certificó todavía la cámara de un dispositivo físico.

## Verificación previa a migrar

- Build Release correcto, sin errores ni advertencias. Suite local: 35 superadas, 0 fallidas, 6 integraciones PostgreSQL omitidas; sin opt-in de escritura/integración en esta fase.
- EF `has-pending-model-changes`: modelo y snapshot coinciden. SQL generado/revisado, no ejecutado.
- Tests locales de formato real, tamaño, invariantes históricos, cierre de periodos, permisos antes de acceder a servicios, flag desactivado y SQL generado sin conectar a una BD.
- Capturas de interfaz con fixtures explícitamente ficticios para 360×800, 390×844, 768×1024, 1024×768, 1440×900 y 1920×1080.
- En esas pruebas de interfaz **todas** las rutas de fotos son interceptadas: no prueban persistencia, concurrencia PostgreSQL ni Cloudinary. Se ejercitan cola/cámara emulada, error parcial/reintento, principal, consulta y visor.
- Las rutas reales se comprueban aparte para denegación anónima, CSRF y bloqueo de fotografías antes de migrar.
- Resultado real previo a migración: galería desactivada, contenido devuelve 503, mutación sin antiforgery devuelve 400 y anónimo redirige al login sin recibir imagen.
- Interfaz con fixtures: seis tamaños con 0 violaciones Axe A/AA, 0 overflow horizontal y 0 objetivos activos menores de 44 px en la galería. Selección múltiple, captura emulada añadida a la cola, observaciones preservadas, contenido falso rechazado, reintento parcial sin repetir el archivo confirmado, visor/Escape y perfil de consulta correctos. Cámara de dispositivo físico no comprobada.
- Impeccable sobre Razor/CSS/JS nuevos: `[]` tras usar el token de color existente para el backdrop. No es una prueba de conexión a Cloudinary.
- Resultados y capturas locales: `.impeccable/review/photos-stage-a-*.json`, `photos-fixture-*.png`, `photos-viewer-fixture-*.png`.

## Lista de verificación posterior a migrar

1. Seleccionar → subir → confirmar en Neon → recargar → recuperar imagen privada desde Cloudinary.
2. Probar dos o más imágenes ficticias distintas, duplicados, actualización de principal y conservación de periodos previos.
3. Probar solicitudes simultáneas para la misma principal, la misma reserva y el mismo contenido.
4. Provocar fallo Cloudinary, pérdida de respuesta y fallo de commit; recuperar con la misma reserva y verificar que no se duplican archivos/evidencias.
5. Solicitar una foto de otro expediente, recuperar reserva ajena, sesión inválida y perfil Consulta; comprobar permisos reales.
6. Comprobar acceso directo no autorizado al recurso authenticated y que originales/miniaturas solo se entregan tras autorización en la aplicación.
7. Validar formato falso, animado, dimensiones excesivas, lote >10, tamaño >10 MB, hash distinto al reintentar e integridad alterada.
8. Verificar tabla/tarjetas/panel principal y galería tras recargar, y cámara/visor/teclado en Android e iPhone físicos por HTTPS autorizado.

El flujo real local, fallos de Cloudinary/PostgreSQL y los seis tamaños están comprobados; véase `PHOTOS_STAGE_A_VALIDATION.md`. La cámara en dispositivos físicos y las demás limitaciones allí descritas siguen pendientes. No comienza la etapa B ni se publica en Render con esta validación.
