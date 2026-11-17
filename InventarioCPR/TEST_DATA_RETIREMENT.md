# Retiro controlado de los fixtures de asignaciones — preparado, NO ejecutado

Auditoría: **2026-11-17, 18:19 UTC**, exclusivamente `test-migracion-inicial`, `br-flat-smoke-b4kdmosq`, endpoint `ep-raspy-dream-b476uukh`, base `neondb`, TLS VerifyFull. Ninguna eliminación PostgreSQL/Cloudinary, desactivación de catálogo ni cambio de trigger se ejecutó.

## 1. Lista cerrada de nueve equipos

La selección se comprobó por **ID y código**, contra el baseline anterior de 46 registros. No usar borrados por prefijo, nombre, fechas, carpeta completa o «todos los registros de prueba».

| Código | ID exacto | Estado | Fotos |
|---|---|---|---:|
| `PRUEBA_2c7b9db0b221` | `6061d8ff-e0e4-4241-b760-bab3b11bda02` | Borrador | 1 |
| `PRUEBA_361e4e48b307` | `98fecc9d-1025-406b-8396-6d0113d803a5` | Definitivo | 1 |
| `PRUEBA_9b3b35eb85b9` | `060a2425-b81b-44ce-8c26-b15f485c5108` | Definitivo | 1 |
| `REG_SIMPLE_META_67c9023eab6a` | `67c9023e-ab6a-46f0-a51a-22c3cf2d3573` | Definitivo | 1 |
| `REG_SIMPLE_META_fde723ec029c` | `fde723ec-029c-4612-9258-3d13de67c8b4` | Definitivo | 1 |
| `REG_SIMPLE_META_57d330d8b0ce` | `57d330d8-b0ce-48d5-90f4-36a1828a2b6a` | Definitivo | 1 |
| `VALIDA_ASIG_DFB5860FEE_LAPTOP` | `c0cb6690-56c7-4e8a-988c-09c302d5b4c8` | Definitivo, ocupado | 1 |
| `VALIDA_ASIG_DFB5860FEE_MONITOR` | `a82aa720-4190-455b-aebb-09716134bd08` | Definitivo, libre | 1 |
| `VALIDA_ASIG_DFB5860FEE_BORRADOR` | `69c00dd2-4255-4452-a370-9144d486a72a` | Borrador | 0 |

## 2. Ocho fotografías identificadas

Todas se comprobaron existentes como recursos **image/authenticated**, con versión coincidente y AssetFolder `inventariocpr-pruebas/asignaciones-20261117`. Son PNG de 950 bytes; SHA-256 almacenado común `843803EFABC4A7412F4F8A64D0B33202E40932AC575797C7C840458247582788`. Compartir contenido/hash **no autoriza** ampliar el conjunto de recursos.

| Equipo | PhotoEvidence.Id | Versión Cloudinary |
|---|---|---:|
| `PRUEBA_2c7b9db0b221` | `bfd32428-bc57-4157-a89a-8cf14d179ae0` | 1794928533 |
| `PRUEBA_361e4e48b307` | `409f67cd-484c-4618-8a05-14fd490407c2` | 1794928855 |
| `PRUEBA_9b3b35eb85b9` | `ba02088a-09d6-4ef4-97c6-f406efc0cdb1` | 1794928695 |
| `REG_SIMPLE_META_67c9023eab6a` | `ffb5fbc8-8624-4f64-8a9b-f7c5b3feed10` | 1794928852 |
| `REG_SIMPLE_META_fde723ec029c` | `64f3fc8d-7bb8-4b4b-ae19-ec7493f874b1` | 1794928533 |
| `REG_SIMPLE_META_57d330d8b0ce` | `73422d0d-9f82-4127-80de-b1d3cfe4c9e5` | 1794928695 |
| `VALIDA_ASIG_DFB5860FEE_LAPTOP` | `cb19a150-8539-4189-a4db-4130d26477b6` | 1794928572 |
| `VALIDA_ASIG_DFB5860FEE_MONITOR` | `66a87a8d-b3d9-49fa-9a1a-0e8e970165a4` | 1794928584 |

El manifiesto local contiene los **public_id completos**, versiones, equipo, PhotoEvidence.Id y huellas; no contiene URLs firmadas ni credenciales:

`C:\Users\jose1\AppData\Local\Temp\opencode\assignments-neon-validation\release-fixture-manifest.json`

**AssetFolder no es public_id**: los identificadores reales comienzan por `inventario/equipos/<GUID-sin-guiones>/...`. No construir un public_id a partir de la carpeta o el nombre del archivo. Comparar el manifiesto con la API de solo lectura antes de cualquier operación futura.

## 3. Relaciones comprobadas del conjunto

| Tabla | Filas vinculadas a los nueve IDs |
|---|---:|
| equipment_custodies | 6 |
| equipment_operations | 20 |
| equipment_data_markers | 3 |
| equipment_inspections | 12 |
| equipment_location_observations | 2 |
| equipment_observations | 7 |
| photo_evidence | 8 |
| photo_uploads | 8 |
| photo_principal_periods | 8 |
| technical_configurations | 12 |
| technical_verifications | 2 |
| ram_modules | 20 |
| storage_units | 22 |
| technical_attribute_values | 7 |

Total: **137 filas relacionadas + 9 equipos = 146 filas** en este alcance. `technical_verifications` referencia directamente EquipmentId; RAM, discos y valores técnicos se seleccionan por los 12 TechnicalConfigurationId, no por nombre de categoría. La auditoría de catálogos tiene otras **10 operaciones**, fuera de las 20 del equipo; no son parte de ese borrado.

Los IDs de las relaciones y las FKs se conservaron fuera de Git en `release-related.private.json`. El procedimiento deberá recalcular la clausura de referencias usando `pg_constraint`, incluidas referencias entrantes desde fuera del conjunto. Si aparece otra fila, columna, referencia o uso empresarial, detenerse y revisar el alcance; no añadirla automáticamente.

## 4. Por qué no se puede usar «Eliminar equipo»

No existe una acción de purga ordinaria de expedientes. La aplicación conserva fotografías, auditoría y periodos históricos. EF impide borrar custodias/catálogos y PostgreSQL bloquea DELETE/UPDATE de auditoría y DELETE de custodias mediante `equipment_operations_immutable` y `equipment_custodies_immutable`. Las FKs son restrictivas.

Por tanto, el retiro físico necesita una **excepción de mantenimiento expresamente autorizada para estos fixtures**, después de archivar su evidencia. Este documento no concede esa excepción y no incorpora SQL destructivo ejecutable, cambios de migración ni opciones nuevas en los módulos.

## 5. Procedimiento futuro, sujeto a autorización

### A. Congelar, volver a comprobar y respaldar

1. Autorizar por separado el conjunto de nueve IDs, sus relaciones auditadas y la fase de borrado de las ocho imágenes. Mantener fuera de alcance los 46 registros anteriores, Identity, certificados, llavero y migraciones.
2. Acordar ventana y detener **todas** las fuentes de escritura a esta rama: Render, aplicaciones locales, pruebas y subidas. Un flag aislado no demuestra ausencia de escritores.
3. Volver a comprobar host/base/branch_id/endpoint_id, ID+código, ocho versiones/public_id, huellas y conteos. Cualquier cambio invalida el manifiesto para ejecutar.
4. Obtener respaldo **actual de las 36 tablas y su esquema completo**, secuencias, auditoría y metadatos; conservar también los bytes originales de las ocho fotos mediante entrega privada y verificar sus hashes. El respaldo previo a la migración de 33 tablas no sustituye este respaldo actualizado.
5. Custodiar respaldo, certificados y manifiesto con acceso restringido y ensayar recuperación/retirada en una copia aislada autorizada. No se ha hecho todavía ese ensayo de purga ni un respaldo nuevo de los ocho archivos para ella.

### B. PostgreSQL: una sola transacción revisada

6. Preparar y revisar el script específico después de la autorización; modo inicial de ensayo con ROLLBACK. Establecer límites de locks/sentencias y bloquear las tablas afectadas contra escritores. Mantener FKs y comprobaciones activas.
7. La excepción a los dos triggers de inmutabilidad debe limitarse a **los nombres indicados**, dentro de la transacción y con exclusión de otros escritores; aplicar únicamente el manifiesto. Rehabilitarlos antes de cualquier COMMIT y comprobar que están habilitados. No usar `session_replication_role=replica`, `DISABLE TRIGGER ALL`, CASCADE ni retirar guardas de la aplicación.
8. Orden de dependencias: operaciones del equipo → custodias; reservas y periodos de principal → evidencias fotográficas; módulos RAM/discos/valores técnicos → configuraciones; verificaciones técnicas, observaciones de ubicación, observaciones, marcadores e inspecciones → equipos. Resolver cualquier dependencia adicional del esquema actual antes de proceder. No eliminar personal/ubicaciones/categorías para satisfacer una FK.
9. Comparar huellas de **todas las filas fuera de las 146 autorizadas**, no solo conteos. Confirmar retiro exacto, sin huérfanos, y guardas habilitadas. Con el estado auditado y sin actividad intermedia: 55→46 equipos y 21→13 fotos. Si algo difiere, ROLLBACK y revisión.
10. Solo autorizar COMMIT tras el ensayo y la comprobación anterior. Conservar el archivo de auditoría exportada con actor, solicitud, fecha y autorización de retirada; la excepción no debe borrar la única copia de la evidencia.

### C. Cloudinary: fase separada y reconciliable

11. Tras confirmar el COMMIT PostgreSQL y comprobar el respaldo privado, retirar uno a uno **los ocho public_id exactos**, con resource_type=image, type=authenticated e invalidación de caché si la API lo admite. No borrar por carpeta ni efectuar operaciones masivas por prefijo.
12. Mantener un registro durable por recurso: pendiente/resultado/fecha, sin credenciales ni URL firmada. Verificar ausencia mediante API autenticada. Un fallo deja una imagen de fixture sin referencia en PostgreSQL: se reintenta desde ese registro, sin tocar otros activos.
13. PostgreSQL y Cloudinary **no comparten transacción**. Una vez destruido el archivo remoto, restaurar su antiguo public_id mediante una nueva subida puede generar otra versión. No prometer restauración exacta de las URLs versionadas anteriores. Antes de autorizar esta fase, confirmar qué backup/restauración ofrece la cuenta Cloudinary y acordar la recuperación de metadatos/versiones; si no puede garantizarse, mantener las imágenes archivadas/remotas hasta resolverlo.

### D. Catálogos auxiliares y cierre

14. Conservar por defecto empresas, trabajadores, áreas, departamentos, sedes/ubicaciones y definiciones de atributos: son catálogos independientes. Las 28 adiciones auxiliares fuera de las 146 filas incluyen diez operaciones de catálogo. Revisar sus IDs y usos globales antes de proponer desactivación separada de entradas ficticias. No borrar categorías existentes, personas o sedes compartidas. Definiciones técnicas nuevas sin uso tampoco autorizan ampliar esta purga automáticamente.
15. Registrar autorización y resultados, verificar fotos históricas empresariales, login, consultas, disponibilidad y totales, y conservar los respaldos según la política acordada.

## Fuera de alcance

De los 46 registros anteriores, **41 tienen prefijos de pruebas previas**. No están incluidos en estos nueve IDs. Deben clasificarse en la conciliación y, si se desea retirarlos, necesitarán otro manifiesto y autorización. No borrar esos 41 ni sus fotos a partir de esta lista.
