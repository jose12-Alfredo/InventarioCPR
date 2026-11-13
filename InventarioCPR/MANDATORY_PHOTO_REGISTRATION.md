# Registro obligatorio con fotografía — implementación y validación

Estado al 13 de noviembre de 2026: implementado sobre el formulario simplificado y la integración privada existentes. **Migración aplicada con autorización únicamente a `test-migracion-inicial` y flujo validado con Neon/Cloudinary reales. Sin commit, push ni despliegue.** Data Protection y sus certificados/configuración no se modificaron. Evidencia y límites de la fase real: `MANDATORY_PHOTO_REAL_VALIDATION.md`; las pruebas locales previas se conservan abajo como historial de implementación.

## Comportamiento

1. Registro rápido y completo comienzan como **Borrador**. Se puede guardar aun sin código, nombre o categoría, y retomar desde la vista Borradores.
2. El borrador tiene ID estable; sus datos parciales se guardan en JSON acotado. Si falta código se usa una clave técnica interna, nunca un código patrimonial definitivo. La UI muestra «Código pendiente». Si falta categoría, el anclaje interno usa «Otro dispositivo», pero el formulario recupera la categoría vacía original.
3. Guardar borrador no emite inspecciones, verificaciones ni revisiones técnicas como hechos confirmados. Conserva sus campos incompletos para que se corrijan antes de finalizar.
4. Añadir fotografías reutiliza el ID del borrador y las reservas idempotentes de Etapa A. Se admiten varias imágenes JPEG/PNG, selección de archivos y cámara cuando el navegador la soporte, previews, progreso y retry. La primera imagen confirmada pasa a principal si no existe otra.
5. **Finalizar registro** se habilita únicamente tras confirmación del servidor de la principal y validez del borrador guardado. También queda bloqueado mientras haya subida activa, archivos pendientes o cambios sin guardar.
6. El servicio vuelve a comprobar todo bajo bloqueo de fila: permisos, revisión, datos, principal vigente, pertenencia al mismo equipo, reserva completada y metadatos íntegros; recupera el objeto privado de Cloudinary y valida imagen/hash antes de confirmar Definitiva.
7. Un fallo remoto conserva borrador, ID, reservas y evidencias. Una finalización repetida o concurrente no crea otra alta ni repite sus hechos.
8. `ListAsync` y su total excluyen borradores por defecto; estos requieren `draftsOnly: true`. Los filtros de condiciones confirmadas solo se usan en el listado definitivo. Los borradores no son una condición física u operativa.
9. Los equipos anteriores a la migración permanecen **Definitivos** y editables sin añadir fotos ficticias. No se eliminan ni reemplazan evidencias históricas. Nuevos definitivos solo pueden cambiar a una principal confirmada; no existe operación para quitarla sin reemplazo ni para borrar evidencia.

## Esquema aprobado y aplicado solo en la rama autorizada

Migración **`20261112200209_EquipmentDraftFinalization`**, del contexto de inventario, posterior a `20261109161923_EquipmentPhotoHistory`.

Únicamente se amplía `public.equipment` y se añade la entrada de su historial EF:

| Columna nueva | Tipo | Uso |
| --- | --- | --- |
| `RegistrationState` | varchar(20), no nula | `Draft` / `Definitive` |
| `RequiresRegistrationPhoto` | boolean, no nula | Exigencia de foto para nuevas altas; excepción legacy explícita |
| `DraftPayload` | text, nullable | Datos parciales del formulario; máximo 65 536 caracteres |
| `RegistrationRevision` | integer, no nula | Revisión optimista e idempotencia del borrador |
| `RegistrationUpdatedAtUtc` | timestamptz, nullable | Último guardado de borrador/finalización por servidor |
| `FinalizedAtUtc` | timestamptz, nullable | Fecha real de finalización; no se inventa para equipos anteriores |

**Backfill:** agregar estado con default `Definitive` y exigencia con default `false` para los registros ya existentes; después cambiar defaults a `Draft` / `true` para nuevas altas. Ningún UPDATE de códigos, fotos, usuarios, condiciones o historia. No se modifica el esquema `security` ni los contextos Data Protection/Identity.

**Índice:** `(RegistrationState, CodeNormalized)`, conservando el índice único global de código también para los borradores.

**Checks:** estado válido, revisión no negativa, tamaño acotado del borrador; un Definitivo no lleva JSON de borrador y, si exige foto, debe tener fecha de finalización. La relación con foto confirmada y su existencia remota se comprueban en servidor con la misma serialización que usan uploads/principal. No se pretende que un CHECK SQL consulte Cloudinary.

**Reversión:** `Down` se rechaza si ya hay equipos nuevos sujetos a la regla o borradores; no se convierten silenciosamente en altas sin foto al retirar columnas. Cualquier retirada con esos datos requiere una decisión de conservación aparte.

SQL idempotente revisado: `Infrastructure/Persistence/Migrations/EquipmentDraftFinalization.review.sql`. Solo contiene ALTER de `equipment`, creación de índice/checks y entrada del historial de inventario. No altera tablas de fotos, usuarios ni llavero; se aplicó únicamente a la rama `br-flat-smoke-b4kdmosq`, endpoint `ep-raspy-dream-b476uukh`. Los 42 equipos previos conservaron Definitive y las columnas originales coincidieron tras migrar. No repetir en otra rama sin autorización.

Generación/revisión offline (no conectan ni aplican):

```powershell
dotnet ef migrations script 20261109161923_EquipmentPhotoHistory 20261112200209_EquipmentDraftFinalization --idempotent --project .\InventarioCPR\InventarioCPR.csproj --context InventoryDbContext --configuration Debug --output .\InventarioCPR\Infrastructure\Persistence\Migrations\EquipmentDraftFinalization.review.sql -- --registration-design-only
dotnet ef migrations has-pending-model-changes --project .\InventarioCPR\InventarioCPR.csproj --context InventoryDbContext --configuration Debug -- --registration-design-only
```

La nueva opción de factoría `--registration-design-only` usa un destino ficticio localhost para scaffolding, no credenciales ni la rama Neon. No usarla como comando de activación.

## Pruebas locales previas y límites de esa fase

`MandatoryPhotoRegistrationTests` ejecuta casos relacionales en PostgreSQL local desechable. Opt-in `INVENTARIOCPR_REGISTRATION_LOCAL_POSTGRES`: conexión a base administrativa `postgres` en loopback. El harness rechaza destinos externos y crea/elimina exclusivamente bases con nombre aleatorio `inventariocpr_registration_test_<GUID>`; no reutiliza ninguna base de la aplicación. La migración se ensaya allí, no en Neon.

Cobertura: borrador completamente incompleto y retomable, exclusión de listado/conteo, código duplicado también entre borradores, imposibilidad de saltarse Finalizar mediante SaveAsync, fallo/retry de upload, varias fotos y principal automática, bytes remotos faltantes/corruptos, evidencia sin confirmación, foto ajena, cambios obsoletos y peticiones concurrentes, permisos, fotografías deshabilitadas, compatibilidad legacy sin foto, reversión protegida y creación de hechos técnicos/inspecciones/verificación una sola vez. Storage remoto simulado con PNG reales generados para tests; no certifica una operación nueva contra Cloudinary.

Validación de navegador sobre Kestrel HTTPS local, Identity ficticio y PostgreSQL local, usando los componentes reales. Cloudinary simulado en memoria, con fallo inicial intencionado. Se prueban guardado vacío/retoma, rápido con dos fotos/fallo/retry/finalización, completo por tres pasos/guardar/cerrar/retomar/subir/finalizar, foco de teclado, footer y ancho responsive. Evidencias ignoradas en `.impeccable/review/mandatory-*`.

Resultados finales:

- Suite completa: **64 aprobadas, 0 fallidas, 10 omitidas**, de 74 casos. Se ejecutaron los nueve casos nuevos de registro obligatorio contra PostgreSQL local y también la prueba PostgreSQL local de Data Protection existente, sin cambiar su implementación. Las diez omitidas requieren opt-in de Neon; no se habilitaron.
- Navegador Chromium: **36 combinaciones** (vista Borradores, rápido sin/ con fotos confirmadas y tres pasos completos × seis tamaños obligatorios), **0 violaciones Axe**, sin overflow horizontal, footer siempre dentro de pantalla y sin errores de página. Flujo rápido y completo finalizaron después de subir; una falla inicial y su retry conservaron el mismo ID y dos evidencias.
- Comprobados teclado/foco dentro del modal, botones visibles de al menos 44 px en el estado revisado, y viewport de teclado emulado **390×430** con Finalizar accesible. Cámara: selector `capture="environment"` existente, sin certificación de hardware físico.
- Impeccable cargado realmente y detector final de fuentes: `[]`. Capturas revisadas en celular, tablet y escritorio. Se corrigieron la carrera de cierre de galería durante JS async, el reinicio de estado al abrir otro equipo y el landmark del panel de detalle anidado.
- Build/publish Release aislado correctos, incluido `wwwroot/_framework/blazor.web.js`; modelo EF sin cambios pendientes respecto de la migración preparada y `git diff --check` sin errores de whitespace.

Evidencia JSON: `.impeccable/review/mandatory-registration-browser-results.json`; capturas `mandatory-{draft-list,quick-empty,quick-confirmed,complete-step1,complete-step2,complete-step3}-{width}x{height}.png`, `mandatory-upload-failure.png` y `mandatory-keyboard-390x430.png` (ignoradas por Git).

Esa fase local no certificó Cloudinary real ni Neon migrado; ambas comprobaciones se completaron posteriormente con autorización y se detallan en `MANDATORY_PHOTO_REAL_VALIDATION.md`. Cámara física Android/iPhone, Safari y Render desplegado continúan pendientes.

## Revisión de `access_users`

Se reutilizó el diagnóstico protegido de solo lectura sobre la rama autorizada. Confirmó ID remoto de rama/endpoint, dos claves Data Protection y una entrada de su migración; la comparación de conteos/checksums de las **31 tablas de `public` coincide con el baseline posterior a la migración Data Protection**, incluida `access_users`. Sigue existiendo una cuenta.

No hay indicio nuevo de modificación respecto de ese baseline. La discrepancia inicial anterior/posterior a aquella migración permanece sin causa determinada. No hay valores por columna anteriores ni auditoría temporal suficiente para atribuirla; no se presenta como resuelta ni se cambia la cuenta para forzar un checksum. Esta etapa no ejecutó DML de Identity en Neon.

## Estado de activación y pendientes

1. Completado: autorización, identidad remota, SQL y aplicación de la migración exclusivamente en `test-migracion-inicial`; backfill/defaults/restricciones verificados y 42 equipos previos conservados.
2. Completado: tres fixtures definitivos, cuatro imágenes reales en Cloudinary, cierre/reapertura de navegador, recuperación y finalización concurrente. Todas las filas originales de 33 tablas preservadas; Identity y Data Protection sin cambios.
3. Pendiente: autorización de publicación/despliegue y prueba de la versión completa en Render. La versión anterior publicada no incorpora estos cambios automáticamente por migrar Neon.
4. Pendiente: cámara física y Safari. Las condiciones de activación Data Protection ya documentadas se mantienen; no se modificaron certificados ni credenciales en esta validación.
