# Etapa A — revisión previa a publicación en Render

## Estado de esta revisión

**La funcionalidad está validada localmente y el usuario autorizó posteriormente subir el código a GitHub para que Render lo despliegue.** Los resultados siguientes corresponden a la revisión previa a esa publicación. La configuración interna de Render todavía no está verificada y no se autorizó modificar production de Neon ni aplicar migraciones durante el arranque.

La inspección interna del servicio requiere la sesión manual del usuario en el panel de Render. Hasta completarla no se puede afirmar qué rama Git despliega, a qué endpoint Neon se conecta ni si tiene un disco persistente.

## Evidencia comprobada

| Elemento | Resultado |
|---|---|
| URL del servicio existente | `https://inventariocpr.onrender.com` |
| TLS público | Solicitudes HTTPS con validación de certificado normal correctas |
| `/healthz` | 204; no consulta Neon/Cloudinary y no certifica el esquema |
| `/inventario` sin sesión | 302 a login HTTPS |
| `/acceso` | 200, formulario de login; HSTS presente |
| Módulo público `EquipmentPhotos.razor.js` | 404: Etapa A no disponible todavía en esta instancia |
| Módulo existente `TechnicalFormPreview.razor.js` | 200 |
| Git local | Rama `main`; origen `https://github.com/jose12-Alfredo/InventarioCPR.git` |
| HEAD predeterminado remoto | `main`, commit `87971ca760d7bdbf081d43994ed8872800e737a2`; no prueba la rama conectada a Render |
| Fuente local Etapa A | Cambios sin commit y archivos nuevos sin seguimiento; no publicados |
| Rama Neon de pruebas | `test-migracion-inicial`, `br-flat-smoke-b4kdmosq`; EquipmentPhotoHistory aplicada |
| Esquema de pruebas | Tres tablas de fotos, tres índices únicos nuevos y dos FK compuestas verificados |
| Datos confirmados | Seis fotos privadas, una principal vigente y cuatro periodos históricos |
| Cloudinary | Seis recursos disponibles authenticated; entrega sin firma 401 y ruta pública ordinaria 404 |
| Reservas fallidas | Dos, sin contenido preparado ni evidencia; recursos remotos 404 |
| Capturas | Revisadas: escritorio, tablet y celular reales de la validación anterior |

Resultados previos completos: `InventarioCPR/PHOTOS_STAGE_A_VALIDATION.md`.

## Datos internos de Render por confirmar

1. ID de servicio `srv-…`, repositorio conectado, rama Git y commit del último despliegue.
2. Runtime Docker, Root Directory, Dockerfile y contexto de build. Este Dockerfile espera la raíz del repositorio.
3. Estado de Auto-Deploy. Un push futuro a la rama conectada podría desplegar automáticamente; coordinarlo antes de autorizar la publicación.
4. Host y base de `ConnectionStrings__Database`, comparados sin mostrar usuario/contraseña. **Production de ASP.NET Core no identifica una rama de Neon.**
5. Asociación de ese host con su rama/ID en Neon y estado de las migraciones de inventario, Identity y EquipmentPhotoHistory.
6. Disco persistente montado en `/var/data`, ruta de claves `/var/data/keys`, permisos del usuario no root y número de instancias.
7. Presencia y valores no secretos de flags; presencia de secretos Cloudinary y correspondencia con la cuenta que contiene las seis imágenes. No copiar cadenas de conexión o secretos a este documento.

Si Render apunta al endpoint de `test-migracion-inicial` ya verificado, ese destino tiene la migración de fotos. Si apunta a otro, sus migraciones están **sin verificar**. Si apunta a `production`, esta revisión no autoriza migrarla, cambiar sus datos ni usarla para pruebas de subida: detener la publicación con escritura hasta acordar un destino aislado autorizado.

## Configuración que exige esta versión

Valores a contrastar, **no modificados** en el servicio:

| Variable | Requisito |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` en Render |
| `RENDER` / `PORT` | Valores nativos de Render; la app detecta el servicio y escucha HTTP interno |
| `ConnectionStrings__Database` | Destino explícitamente autorizado con esquema compatible; solo en Render Secrets |
| `Security__EnableIdentity` | `true` |
| `Inventory__AllowAnonymousLocalTests` | Ausente o false; nunca activar en Render |
| `Inventory__PhotosEnabled` | `true` únicamente después de comprobar el destino y autorizar publicación |
| `Inventory__ProductionWritesEnabled` | Opt-in necesario para escribir bajo el runtime Production; activar solo contra el destino aislado aprobado, nunca por asumir que el nombre de entorno identifica la rama |
| `DataProtection__KeysPath` | `/var/data/keys` en disco realmente persistente y escribible |
| `AllowedHosts` | Host nativo detectado con `RENDER_EXTERNAL_HOSTNAME`; explícito al usar dominios propios |
| `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret` | Configurados solo en servidor, cuenta correcta; no se muestran sus valores |
| `Cloudinary__TestAssetFolder` | Para esta prueba física: una carpeta exclusiva como `inventariocpr-pruebas/fotos-render-telefono` |

No copiar el archivo User Secrets ni configurar `NeonDevelopmentApp:BranchConfirmed` como sustituto de la comprobación del destino de Render.

## Seguridad y persistencia revisadas en código

- Identity es obligatoria fuera del modo anónimo local explícito. Read/Write y cuenta/sello/roles se verifican en servidor; contenido y reserva deben pertenecer al expediente.
- Imágenes entregadas por rutas autorizadas, private/no-store y nosniff; las URLs firmadas de Cloudinary permanecen en servidor y se suprimen de sus logs HTTP.
- HTTPS externo terminado por Render; la app procesa encabezados reenviados antes de autenticación y redirección HTTPS. HTTPS público ya comprobado; sesión autenticada y redirecciones del nuevo despliegue se verificarán después de autorizarlo.
- Blazor Interactive Server utiliza SignalR. Render debe mantener la conexión `wss`; el código está preparado, pero la conexión 101 autenticada, reconexión y comportamiento en suspensión del servicio están pendientes de comprobación real allí.
- Data Protection guarda claves con nombre de aplicación estable `InventarioCPR` y ruta configurable. **La existencia de la carpeta no demuestra que exista un disco persistente.** Revisar montaje, permisos y supervivencia de sesión después de un reinicio autorizado. Mantener una instancia hasta diseñar escalado de circuitos/claves.
- No se ejecutan Migrate/EnsureCreated ni limpieza automáticamente al arrancar. Solo se comprueba si faltan migraciones Identity; las fotos también se bloquean cuando su migración está pendiente.

## Archivos y límites

La subida usa XMLHttpRequest binaria → request body → lectura incremental acotada en MemoryStream → validación/recodificación → Cloudinary. Las fotos no se escriben a disco del contenedor; PostgreSQL conserva referencias, metadatos, reservas y principal. El disco de Data Protection es una necesidad distinta de almacenar fotografías.

- Hasta 10 imágenes por lote, 10 MB y 25 megapíxeles por archivo.
- JPEG/PNG reales; se comprueba el contenido y se retiran EXIF. HEIC no está disponible.
- Procesamiento de píxeles serializado para acotar memoria; cuatro descargas concurrentes y límites por usuario para mutaciones.
- Estados de rechazo, progreso, reintento y confirmación separados. Un 100 % de transferencia no implica commit confirmado.
- Reservas persistidas antes de subir; reintento con misma reserva y hash, sin sobrescritura. El ciclo real con fallo PostgreSQL después de Cloudinary ya está validado.

Los límites del proxy, plan y memoria reales de Render deben contrastarse y ejercitarse desde el teléfono; la prueba local con imágenes ficticias pequeñas no certifica rendimiento de archivos de 10 MB bajo red móvil lenta.

## Limpieza preparada, no ejecutada

SQL revisable: `InventarioCPR/Infrastructure/Persistence/Maintenance/ReviewPhotoTestReservations.sql`.

- Solo dos IDs exactos del equipo `VALIDA_FOTOS_A_MW9RWNV6`, con nombre y fecha originales.
- Todas las propiedades de contenido, evidencia y confirmación deben seguir nulas; sin referencia en photo_evidence y remoto authenticated 404.
- Bloqueo del equipo/reservas y comprobación de que el conjunto completo de fotos y periodos no cambia.
- Marcador de ejecución comentado y ROLLBACK por defecto; no forma parte de las migraciones o del arranque.
- Antes de ejecutar con autorización: confirmar nuevamente host, detener todas las fuentes de subida a esa rama, repetir comprobaciones remotas y revisar el SQL. No borrar ningún recurso Cloudinary ni fotografía confirmada.

## Compilación y artefacto local

- 16 pruebas focalizadas de configuración de despliegue y fotografías superadas.
- `dotnet publish` Release en directorios de artefactos aislados completado correctamente. Un primer publish sobre bin activo emitió un aviso por el EXE local bloqueado; la repetición aislada terminó sin ese aviso.
- Se verifican módulo EquipmentPhotos, runtime net10.0 y bibliotecas nativas Skia para Linux en el paquete.
- El paquete no debe contener User Secrets, configuraciones Development, certificados, PDB ni estado de agentes; no contiene una configuración de conexión Render prellenada.
- Docker no está instalado/disponible en esta sesión: no se ejecutó `docker build` ni se certificó Skia dentro del contenedor Linux. Esto se comprobará en el build/arranque autorizado de Render.

## Condición de salida para autorizar publicación

El usuario autorizó commit/push para publicar el código. La activación y prueba de subida en el servicio sigue condicionada a cerrar los datos internos pendientes de Render, confirmar un destino aislado compatible y verificar variables y disco. La publicación no aplica migraciones ni modifica por sí sola las variables de fotografías. Tras comprobar el nuevo despliegue, ejecutar `InventarioCPR/PHONE_STAGE_A_CHECKLIST.md` en Android/iPhone. No iniciar etapa B.
