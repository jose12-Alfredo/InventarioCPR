# Empresas, responsables y disponibilidad

Implementación inicial: 16 de noviembre de 2026. **Actualización 17 de noviembre:** migración aplicada con autorización exclusivamente a Neon `test-migracion-inicial`, tras preflight y recuperación lógica ensayada. Suite Release **89 aprobadas / 0 fallidas / 0 omitidas**, 23 checks de servicio reales y 78 checks de navegador con Neon/Cloudinary reales. Evidencia, fixtures, conservación y límites en `ASSIGNMENTS_NEON_VALIDATION.md`. Sin commit/push/despliegue del módulo.

Preparación final posterior: auditoría READ ONLY sin pendientes/modelos nuevos y datos conservados. Informe `../RELEASE_READINESS.md`; retiro solo propuesto en `TEST_DATA_RETIREMENT.md` y revisión por UI/hoja privada en `CONCILIATION_GUIDE.md`. Los 46 registros anteriores incluyen 41 con marcadores de pruebas previas; no equivalen a 46 activos empresariales confirmados. No se modificó este módulo ni se guardaron empresas reales en la preparación.

## Funcionalidad

- Catálogo configurable de empresas: nombre visible, razón social/identificación fiscal opcionales, edición, activación y desactivación auditadas. **No se precargaron CPRSA, DMA o MAA.** Confirmar denominaciones en la conciliación antes de registrarlas; no se supone ninguna relación legal entre ellas.
- Empresa propietaria, empresa usuaria y área administrativa del activo son tres relaciones opcionales e independientes. Se clasifican desde Asignaciones después de finalizar el registro. No se obliga a tener empresa o responsable para registrar un activo. Cambiar usuaria no abre/cierra custodia ni cambia propietario, área o ubicación.
- Trabajadores reutilizan `StaffMember` y no necesitan cuenta Identity. Se admite código de personal opcional para distinguir homónimos; la selección/relación usa ID, no nombres. Los verificadores anteriores no se habilitan automáticamente para recibir equipos.
- Administración de departamentos, áreas, sedes y ubicaciones/oficinas existentes. Desactivar catálogos impide nuevas selecciones incompatibles y conserva relaciones/historia. Desactivar un trabajador **no libera** sus equipos: se deben registrar transferencias o devoluciones explícitas; el responsable sigue visible hasta cerrarlas.
- Asignar, transferir atómicamente y liberar. Custodia individual o compartida en ubicación/oficina existente con un solo custodio principal; la oficina no sustituye área ni empresa. No se implementan usuarios secundarios de un equipo compartido como custodias principales duplicadas.
- Historial permanente de custodios, periodos, movimientos y cambios de clasificación, con actor Identity, hora de registro UTC y fecha efectiva separadas. Los periodos conservan nombres del trabajador/área/departamento/empresas/oficina al abrirse; las operaciones conservan valores antes/después e IDs. Renombrar catálogos no reescribe esos hechos.
- Entrega/devolución exige condición física y operativa observadas e inspector StaffMember activo, separado del usuario Identity. La transferencia registra recepción y nueva entrega por separado en la auditoría; la inspección del estado resultante conserva la nueva entrega. Devolución/entrega no copian automáticamente las condiciones previas. La conciliación inicial no simula una nueva inspección de entrega antigua.
- Inventario conserva sidebar, Registro rápido, Nuevo equipo, Definitivos/Borradores, fotografías y apertura de expediente en **ventana amplia**. Se añade sección Asignaciones al expediente y ruta `/asignaciones?equipo=<id>` al flujo separado.

## Disponibilidad: exclusivamente Libre u Ocupado

`Libre = NOT EXISTS custodia con EndedAtUtc NULL`. `Ocupado = EXISTS esa custodia`, individual o compartida. No existe una columna editable `Availability`, ni estados «Pendiente», «Por verificar» o «Compartido» de disponibilidad. Borrador/Definitiva siguen siendo estados de registro independientes.

Los indicadores muestran exactamente Total, Libres y Ocupados sobre **definitivos**, nunca sobre los 25 registros de la página. Un snapshot PostgreSQL RepeatableRead mantiene coherencia de conteos y filas ante concurrencia; `Libres + Ocupados = Total`. Comparten empresa, búsqueda, categoría, condición, área y responsable; el filtro Libre/Ocupado se aplica a las filas **después** de calcular el desglose, para no convertir la tarjeta seleccionada en el único total. En Borradores los indicadores siguen contando definitivos del mismo ámbito.

Empresa principal significa **propietaria**, con opciones Todas/Sin registrar/empresas configuradas. Empresa usuaria tiene filtro independiente: no se consulta con un OR ambiguo entre propietario y usuario. Las compañías inactivas permanecen disponibles para consultar historia y filtrar activos existentes. Este filtro **no es aislamiento de seguridad multitenant**; continúan los permisos globales actuales de inventario.

Tabla: Foto | Código | Equipo | Categoría | Área | Responsable | Condición | Disponibilidad | Acciones. Área es la administrativa del activo; se muestra «Sin área» cuando no consta. Responsable viene de la custodia actual, «Sin asignar» si no existe. Tarjetas móviles conservan esos hechos. Los filtros adicionales se despliegan bajo «Área, responsable y disponibilidad».

**Libres y funcionales** exige no tener custodia y que la última inspección operativa sea exactamente `Funcional` o `Operativo`, sin distinguir mayúsculas y recortando espacios. No clasifica como funcional una condición vacía, avería ni una frase arbitraria. Listado, filtro y detalle usan el mismo desempate por fecha/ID de inspección. La custodia no depende de esa condición: una asignación de un equipo limitado exige revisión humana, no una aptitud inferida por Libre. No se implementa un catálogo completo de estados técnicos o bloqueos de módulos futuros.

## Conciliación inicial

La migración marca los registros ya existentes `NeedsCustodyReconciliation=true`; las altas nuevas normales tienen `false` y no se inventa custodia. Este indicador administrativo **no es disponibilidad**.

Procedimiento:
1. Confirmar empresas y denominaciones; crear catálogos reales cuando estén autorizados. No asignar activos antiguos a CPRSA por defecto.
2. Revisar áreas/trabajadores existentes por ID, evitando convertir verificadores homónimos en receptores sin revisión. Completar área y habilitación cuando corresponda.
3. Para cada definitivo: registrar empresa propietaria/usuaria/área solo cuando existan datos; confirmar físicamente si tiene custodio.
4. Usar **Conciliar custodia existente** para registrar al custodio comprobado, o **Confirmar libre físicamente** si se comprobó que no tiene custodia. Se auditan actor y fecha de confirmación digital. La fecha original de entrega es opcional y queda nula si se desconoce; no se inventan movimientos antiguos ni fechas.
5. Conservar el aviso mientras haya registros sin revisar. Libre/Ocupado refleja **asignaciones registradas**, no una garantía de disponibilidad física ya conciliada.

Reconciliar no importa Excel ni fusiona personas automáticamente. Las correcciones posteriores se agregan como nuevas operaciones; no hay editor para reescribir periodos cerrados.

## Integridad, fechas y permisos

- Escritura: Administrador/Sistemas; Consulta lee. Cada operación obtiene actor desde el validador Identity existente, con cuenta activa, sello y roles actuales; nunca desde un campo de formulario. Se mantiene el opt-in ambiental de escrituras.
- Contexto de inventario por operación, transacción, bloqueo `FOR UPDATE` del activo, revisión `OperationsRevision` y custodia esperada. En catálogos hay revisión y bloqueo de fila. Consultas compartidas bloquean trabajador/área/departamento/oficina para no confirmar simultáneamente una desactivación incompatible.
- RequestId único y huella del comando con actor: un reintento idéntico retorna el recibo existente; reutilizarlo con contenido diferente falla. Repetir una devolución antigua no cierra una custodia posterior.
- Transferencia: cerrar origen, insertar destino y auditoría dentro de la misma transacción. Un fallo posterior de auditoría revierte también el cierre y el nuevo periodo.
- Índice único parcial de custodia activa. FKs restrictivas y compuestas al mismo equipo. Trigger de inserción verifica equipo definitivo y solapamientos de periodos; triggers de historia impiden DELETE/UPDATE de auditoría y solo permiten cerrar una custodia vigente sin modificar sus datos originales. Guardas EF duplican esa protección en la aplicación.
- No se permiten fechas efectivas futuras ni devoluciones anteriores a la entrega; tiempos almacenados en UTC. La UI etiqueta UTC explícitamente: no se adoptó sin confirmación una zona horaria empresarial. La conciliación admite fecha original conocida como metadato, separada del comienzo de la confirmación digital.
- Las denominaciones son snapshots de los catálogos confirmados al registrar. El módulo no es un historial de contratación/afiliación laboral capaz de deducir automáticamente el área de una persona años atrás: al registrar fechas pasadas el operador debe confirmar el contexto real. La conciliación distingue la confirmación actual de una fecha original desconocida.
- Las operaciones incluyen la referencia/valores de la inspección previa si existe, y registran una **nueva inspección explícita** con la persona y condiciones observadas que el operador confirmó. En transferencias, la auditoría conserva tanto recepción como entrega; la inspección del estado final muestra la entrega. No se reutilizan condiciones antiguas como si se acabaran de comprobar. Firmas, comprobantes, nuevas fotos obligatorias por movimiento y políticas definitivas para equipos limitados siguen fuera de esta fase.
- Inactivar al trabajador no borra historia ni cierra custodia. Puede registrarse la devolución de una persona actualmente inactiva.

## Arquitectura y entidades

Reutilizadas: Equipment, EquipmentCategory, StaffMember, Area, Department, Site, Location, EquipmentInspection y los servicios/permisos actuales. Identity sigue en AccessDbContext; Data Protection y Cloudinary no se alteran.

Nuevas tablas:
- `companies`: catálogo dinámico, nombre normalizado único y actividad/revisión.
- `equipment_custodies`: periodos de custodia, responsable principal, modalidad y snapshots históricos.
- `equipment_operations`: movimientos, auditoría inmutable, cambios de clasificación/catálogos y recibo de idempotencia.

`Equipment` añade referencias nullable a propietaria/usuaria/área, revisión operativa y seguimiento de conciliación. Los catálogos existentes añaden actividad/revisión; StaffMember añade habilitación para recibir y código opcional.

`EquipmentCategory.TechnicalProfile` distingue metadatos `Computer`/`General` sin implementar formularios nuevos ni reescribir RAM/discos/procesador. Se conservan nombres de entidades, tablas, rutas privadas e IDs. Custodia, empresas y auditoría dependen del ID general del activo, no de un formulario informático; una prueba asigna un activo de categoría general ficticia. No se anuncia como implementado el inventario completo de muebles/electrodomésticos ni un editor de atributos dinámicos: será otra fase. El registro actual mantiene sus categorías admitidas y compatibilidad de borradores.

## Migración aplicada únicamente a la rama de pruebas autorizada

**`20261116181150_CompaniesAndEquipmentCustodies`**, posterior a EquipmentDraftFinalization, en InventoryDbContext. Aplicada el 17 de noviembre solo a `test-migracion-inicial` (`br-flat-smoke-b4kdmosq`, endpoint `ep-raspy-dream-b476uukh`). SQL idempotente: `Infrastructure/Persistence/Migrations/CompaniesAndEquipmentCustodies.review.sql`. No repetirla en esa rama ni asumir autorización para otro destino.

Backfill: empresas/área del activo nulas; no cambia códigos, datos técnicos, fotos, responsables físicos ni registro definitivo. Registros existentes requieren conciliación. Personal previo permanece activo, pero `CanReceiveEquipment=false` hasta revisión. No hay INSERT de compañías/personas/custodias. La única clasificación automática es metadato Computer para categorías informáticas ya reconocidas; no crea categorías nuevas. `Down` rechaza retirar el módulo si hay empresas, custodias u operaciones para evitar perder historia.

El script crea/amplía únicamente el inventario y sus triggers/índices; no toca `access_*`, `security` o tablas de fotografías. Revisar locks/tiempo de mantenimiento y permisos CREATE FUNCTION/TRIGGER del rol migrador antes de cualquier aplicación autorizada. La aplicación **no aplica** migraciones al arrancar.

```powershell
dotnet ef migrations script 20261112200209_EquipmentDraftFinalization 20261116181150_CompaniesAndEquipmentCustodies --idempotent --project .\InventarioCPR\InventarioCPR.csproj --context InventoryDbContext --configuration Debug --output .\InventarioCPR\Infrastructure\Persistence\Migrations\CompaniesAndEquipmentCustodies.review.sql -- --registration-design-only
dotnet ef migrations has-pending-model-changes --project .\InventarioCPR\InventarioCPR.csproj --context InventoryDbContext --configuration Debug -- --registration-design-only
```

Ambos comandos usan la factoría ficticia de diseño, sin cargar credenciales ni aplicar cambios a Neon.

## Pruebas y resultados

Pruebas relacionales: `InventarioCPR.Tests/AssignmentTests.cs`, opt-in **INVENTARIOCPR_ASSIGNMENTS_LOCAL_POSTGRES**. Solo acepta base administrativa postgres en loopback y crea/elimina bases aleatorias `inventariocpr_custody_test_<GUID>`. No usa User Secrets ni Neon. Fixtures de empresas/personas/imágenes son exclusivos de esas bases desechables.

Fase local inicial **en Release: 79 aprobadas, 0 fallidas, 10 omitidas de 89**, incluyendo 14 casos del módulo y las regresiones locales de fotografías obligatorias/Data Protection. Las diez pruebas Neon se habilitaron posteriormente con autorización: **resultado final 89/0/0**, documentado en `ASSIGNMENTS_NEON_VALIDATION.md`. Cobertura del módulo: ciclo Libre/Ocupado, trabajador sin Identity, empresas/áreas independientes, labels históricos tras cambios, desactivación, compartidos, concurrencia, idempotencia, revisión obsoleta, rollback por fallo real de auditoría local, reglas SQL y triggers, backfill previo, paginación de 60 definitivos, filtros combinados, exclusión de borradores y roles reales Identity/revocación. Se comprueba también rechazo sin condiciones observadas/inspector y conservación de la entrega original al registrar una devolución averiada. El SQL idempotente se ejecutó dos veces sobre una base desechable y preservó filas previas, sin precargar empresas o custodias.

UI real sobre Kestrel HTTPS local con PostgreSQL desechable e Identity de pruebas; imágenes simuladas en memoria. Flujo completo: filtro empresa, libres funcionales, expediente amplio, cambio de usuaria, asignar, transferir compartido, liberar con condiciones observadas, crear catálogo, comprobar indicadores después y confirmar que Borradores no altera los totales ni introduce otra disponibilidad. **66 combinaciones de estado/viewport**, Axe 0, overflow de página 0, errores de página 0; incluye las tres formas de movimiento con sus campos de condición. Teclado en modal y `tap()` móvil sintetizado aprobados. Evidencia `.impeccable/review/assignments-browser-results.json` y `assignments-{estado}-{ancho}x{alto}.png` (ignoradas por Git).

Build/publish Release correctos, sin advertencias/errores y con boot Blazor incluido. El primer test Release con binarios externos reveló una suposición antigua en BlazorRestoreTests sobre encontrar el checkout desde el directorio bin; se ajustó su localización de fuentes para admitir `--artifacts-path`, se mantuvo la comprobación real de restore Docker y la suite final completa pasó.

Impeccable: skill/contexto/sistema existentes consultados; detector final `[]`. Revisión independiente inicial identificó que `aria-pressed` booleano se emitía con mayúscula y no activaba el selector CSS; se corrigió a `true/false` explícitos y se recapturaron las mismas pantallas. El harness ahora verifica también los colores computados del estado seleccionado. Revisor independiente: fix material **resuelto**, disposition **ship** para esa corrección. Documentador independiente confirmó compatibilidad con el sistema CPR; no hay tokens nuevos y DESIGN.md solo amplía prosa de indicadores/nueve columnas/rutas. Se actualizaron PRODUCT.md y documentación desfasada de la fase visual inicial. Impeccable informó actualización disponible de 4.7.0 a 4.9.1; no se actualizó la herramienta durante esta tarea.

Capturas de referencia: `assignments-inventory-occupied-1440x900.png`, `assignments-inventory-free-390x844.png`, `assignments-assign-form-390x844.png`, `assignments-shared-history-768x1024.png`, `assignments-company-directory-1440x900.png`, todas en `.impeccable/review/`. Las capturas de formularios/historia enfocan la sección de trabajo mediante scroll; las de inventario muestran el inicio. Todas corresponden a fixtures locales y no se insertaron esos nombres en servicios empresariales.

Estas capturas iniciales son locales y simuladas. La fase posterior documenta por separado Neon/Cloudinary reales con fixtures ficticios; cámara física y Safari siguen sin certificar. Axe 0 y revisión ship no son una certificación de accesibilidad ni un resultado de producción.

## Pendientes de activación

1. La migración ya está validada en la rama autorizada. Para cualquier otro destino, coordinar y autorizar el esquema antes de arrancar esta versión.
2. Confirmar denominaciones empresariales y cargar catálogos reales mediante UI, no mediante seeds arbitrarios de la migración.
3. Conciliar físicamente activos anteriores. La validación real autorizada ya pasó sobre fixtures nuevos; no se conciliaron ni asignaron los activos anteriores.
4. Autorización independiente para commit/push/despliegue y validación posterior en Render. No hay nuevas contraseñas o certificados que pedir para desarrollar este módulo.
