# MEMORY — InventarioCPR

Última actualización: 2026-10-30

## 1. Identidad del proyecto

- **Nombre:** InventarioCPR
- **Empresa:** Consorcio Publicitario
- **Tipo:** Sistema web interno
- **Objetivo:** Controlar los activos tecnológicos, sus responsables, condiciones, mantenimientos y costos durante toda su vida útil.

## 2. Stack tecnológico confirmado

- Framework: Blazor Web App
- Lenguaje: C#
- Backend: ASP.NET Core integrado
- Interactividad: Interactive Server
- Base de datos: PostgreSQL en Neon (`neondb`); migración aplicada solo en `test-migracion-inicial`, no en `production`.
- ORM: Entity Framework Core 10 / Npgsql 10 (primera migración local preparada y probada en rama aislada)
- IDE principal: JetBrains Rider
- Agente de programación: OpenCode
- Terminal: Warp
- Arquitectura: monolito modular

No cambiar las tecnologías seleccionadas sin autorización.

Verificar las versiones instaladas antes de agregar dependencias.

## 3. Estado actual del proyecto

### Confirmado
- Proyecto Blazor creado mediante Visual Studio.
- OpenCode funcionando desde Warp.
- Archivo AGENTS.md creado y reconocido por OpenCode.
- Requisitos generales del sistema definidos.
- Proyecto configurado para .NET 10 (`net10.0`).
- Sistema visual CPR, layout responsive y paginas Inicio e Inventario implementados.
- Componentes compartidos de botones, tarjetas, iconos y estados implementados.
- Se confirmó la necesidad de características técnicas estructuradas por categoría y de conservar su configuración histórica.
- Se verificaron Neon y Cloudinary. En la rama aislada `test-migracion-inicial`, `neondb` contiene las 20 tablas del inventario, las 7 tablas de Identity y sus historiales EF separados; `production` no se modificó.
- Modelo de inventario inicial y migración `InitialInventorySchema` generados localmente y probados en la rama Neon `test-migracion-inicial`; no aplicados a `production`.
- `AddIdentityAccess` aplicada exclusivamente a la rama aislada tras autorización expresa; ninguna cuenta ni rol creado todavía. Identity permanece deshabilitado en la aplicación.
- Identity habilitado por defecto y perfiles habituales de Rider (`http`, nombre heredado, y `https`) conectados a `https://localhost:7228` con certificado de desarrollo confiado; `https-identity-local` se conserva. El modo anónimo exige `anonymous-tests-local` explícito en loopback.
- El primer bootstrap autorizado creó únicamente una cuenta administradora ficticia y tres roles provisionales en `test-migracion-inicial`. El procedimiento temporal ya no forma parte del código; el usuario debe retirar los tres secretos `Security:Bootstrap:*` localmente.

### Por verificar
- Estado de la skill blazor-architecture.
- Revisión empresarial y autorización antes de aplicar la migración a la rama principal.
- Páginas y funcionalidades existentes.

### Pendiente
- Aprobación de las decisiones empresariales pendientes antes de habilitar los flujos funcionales.
- Implementación de módulos.
- Operaciones de imágenes autorizadas y persistencia real de evidencias.
- Activación y prueba real del sistema de permisos, cuentas ficticias y sesiones después de autorización separada para bootstrap.
- Reportes.

## 4. Requisitos del inventario

El inventario debe permitir registrar:

- Computadoras de escritorio.
- Laptops.
- Monitores.
- Teclados y mouse.
- Discos externos.
- Cámaras.
- Equipos de grabación.
- Otros dispositivos tecnológicos.

Cada equipo debe contar con:
- Código único.
- Información técnica.
- Fotografía principal.
- Galería fotográfica.
- Ubicación actual.
- Estado físico.
- Estado operativo.
- Historial de responsables.
- Historial de movimientos.
- Historial de mantenimientos.
- Historial de gastos.

Para el alta inicial solo son obligatorios código único, categoría y nombre descriptivo. Los demás datos pueden completarse progresivamente sin inventar valores. Los códigos heredados del Excel, como `EQ_1`, se conservan.

## 5. Historial de usuarios

El sistema debe permitir responder:

- ¿Quién tiene actualmente el equipo?
- ¿Quiénes lo utilizaron anteriormente?
- ¿Cuándo recibió cada usuario el equipo?
- ¿Cuándo lo devolvió?
- ¿En qué condiciones fue entregado?
- ¿En qué condiciones fue devuelto?

Nunca reemplazar el historial anterior al realizar una nueva asignación.

## 6. Mantenimientos y gastos

Registrar:
- Mantenimientos preventivos.
- Reparaciones correctivas.
- Fallas detectadas.
- Diagnósticos.
- Servicios técnicos.
- Repuestos comprados.
- Cantidad y costo de repuestos.
- Costos de mano de obra.
- Fotografías y comprobantes.

Permitir calcular el gasto histórico total de cada equipo sin duplicar importes.

## 7. Control fotográfico

Las fotografías deben permitir identificar el equipo y documentar su condición.

Conservar:
- Fotografías iniciales.
- Fotografías de entrega.
- Fotografías de devolución.
- Evidencias de daños.
- Fotografías de reparaciones.
- Fotografías del estado actual.

Nunca sobrescribir evidencias históricas.

## 8. Salidas y retornos

Controlar temporalmente equipos utilizados fuera de la oficina.

Registrar:
- Equipos retirados.
- Persona responsable.
- Fecha y hora de salida.
- Destino o motivo.
- Fecha y hora de retorno.
- Estado al devolver.
- Observaciones y evidencias.

## 9. Módulos previstos

1. Dashboard.
2. Inventario.
3. Asignaciones y devoluciones.
4. Mantenimientos.
5. Gastos y repuestos.
6. Personal y áreas.
7. Reportes.

Control de salidas y retornos como funcionalidad de movimientos.

## 10. Decisiones técnicas

### DEC-001
Usar Blazor Web App en lugar de separar frontend y backend.

Estado: Confirmada.

### DEC-002
Utilizar PostgreSQL como base de datos.

Estado: Decidida, implementación pendiente de verificar.

### DEC-003
Cada equipo tendrá un expediente digital permanente.

Estado: Confirmada.

### DEC-004
Conservar historial de asignaciones, mantenimientos y fotografías.

Estado: Confirmada.

### DEC-005
Desarrollar los módulos progresivamente, comenzando por el modelo de datos y el inventario.

Estado: Planificada.

### DEC-006
BUSINESS_RULES.md es la referencia base para diseñar entidades, servicios, persistencia y operaciones.

Las reglas fundamentales confirmadas incluyen código único, expediente permanente, historial de asignaciones, una sola custodia vigente, entregas y devoluciones, mantenimientos, gastos, fotografías, movimientos y validación crítica en servidor.

Estado: Confirmada.

### DEC-007
Los mantenimientos pueden registrarse sin gastos asociados. La validez técnica del mantenimiento no depende de que existan repuestos o costos.

Estado: Confirmada.

### DEC-008
La auditoría debe priorizar operaciones críticas y debe existir control de concurrencia en asignaciones, devoluciones y movimientos.

Estado: Confirmada.

### DEC-009
Baja y reactivación no tendrán doble aprobación obligatoria en todos los casos. La autorización se basará en permisos y podrá incorporar aprobación adicional configurable.

Estado: Confirmada.

### DEC-010
El equipo conserva su identidad general y registra características técnicas según categoría. Computadoras y laptops requieren estructura para procesador (fabricante, modelo y denominación), RAM total y módulos conocidos, múltiples unidades SSD/HDD/NVMe con capacidad, sistema operativo cuando aplique y observaciones. Otras categorías admiten atributos específicos tipados. Los datos desconocidos se pueden completar después.

Ampliaciones y reemplazos conservan configuración anterior y nueva, y se relacionan con mantenimiento y gastos reales cuando correspondan. El formulario de registro debe adaptar sus secciones a la categoría; las validaciones finales serán del servidor.

Estado: Confirmada como requisito; entidades y persistencia pendientes.

### DEC-011
Se admiten expedientes inicialmente incompletos con solo código único, categoría y nombre descriptivo obligatorios. Los datos opcionales distinguen no registrado, no aplicable y pendiente de verificar; el expediente mostrará los pendientes explícitos. Responsable vigente, ubicación física, condición física y condición operativa son dimensiones independientes.

La última verificación debe registrar fecha real y persona que verificó, manteniendo revisiones anteriores. Las observaciones originales, fotografías y cambios técnicos se preservan históricamente. Los códigos existentes del Excel se conservan sin convertirlos automáticamente al formato de código futuro propuesto.

Estado: Confirmada como requisito; operaciones y persistencia pendientes.

## 11. Registro de progreso

### 2026-10-26
- Se confirmó la creación del proyecto Blazor.
- Se confirmó el uso de Warp + OpenCode.
- Se seleccionó Rider como IDE recomendado.
- Se creó y verificó AGENTS.md.
- Se preparó la estructura inicial de memoria.
- Se revisó y aceptó BUSINESS_RULES.md como base, manteniendo separadas las reglas confirmadas de las propuestas pendientes.
- Se confirmó que los mantenimientos pueden existir sin gastos, que la auditoría se concentra en operaciones críticas y que baja/reactivación usarán permisos configurables.
- Se implementó la fase visual inicial con sistema CSS CPR, layout responsive, sidebar, topbar, menú móvil accesible y componentes Razor compartidos.
- Se reemplazaron las páginas de muestra por Inicio e Inventario; las operaciones dependientes de datos permanecen explícitamente deshabilitadas y no se añadieron datos simulados.
- Se verificaron las rutas `/` e `/inventario`, la compilación sin errores ni advertencias y la composición a 1440 px, 1024 px y 390 px.
- Se documentó el contexto del producto en `PRODUCT.md` y el sistema visual implementado en `DESIGN.md` y `.impeccable/design.json`.
- Se amplió BUSINESS_RULES.md con las reglas RN-100 a RN-109 de características técnicas y se añadió una vista previa de ficha técnica por categoría sin persistencia.
- Se confirmaron los mínimos de alta y la clasificación de datos faltantes; se amplió la vista previa existente con identificación, custodia/ubicación/condiciones separadas, verificación y pendientes, sin importación ni guardado.
- Se verificó la compilación de estos cambios en una salida temporal aislada: cero errores y cero advertencias; la salida estándar de Rider estaba bloqueada por la aplicación en ejecución.

No se han implementado persistencia, autenticación ni operaciones funcionales de inventario.

### 2026-10-27
- Se añadió infraestructura separada para EF Core 10/Npgsql 10 y SDK CloudinaryDotNet 1.28.0; conexión directa Neon forzada a TLS `VerifyFull`, secretos locales mediante `UserSecretsId` y modelo provisional de equipo y metadatos de evidencia. Sin migraciones, cambios de tablas ni escrituras.
- Se añadió almacenamiento de fotografía autenticado, validación de JPEG/PNG (extensión, contenido, tamaño y dimensiones), recodificación sin metadatos y contrato de aplicación; no hay ruta de subida o descarga habilitada. El decodificador se cambió de ImageSharp a SkiaSharp (MIT): ImageSharp 3.2.0 era dependencia directa y bloqueaba Release sin licencia.
- `dotnet build` inicial comprobado: 0 errores, con advertencia de licencia ImageSharp. Posteriormente `dotnet build -c Release` comprobado tras el cambio: 0 errores, 0 advertencias. Dependencias restauradas sin advertencias de vulnerabilidad NuGet en esta configuración.
- `--check-connections` comprobado en Development: Neon y Cloudinary sin credenciales locales configuradas; no se pudo comprobar conectividad remota ni se llamó a Cloudinary. Ningún secreto se imprimió. Instrucciones locales en `INTEGRATIONS.md`.
- No hay autenticación ni autorización; antes de usar datos reales faltan identidad empresarial, permisos de acceso por evidencia/equipo, descarga autorizada y políticas de auditoría y antimalware.
- Se amplió `--check-connections`: consulta `SELECT 1`, TLS desde `pg_stat_ssl`, nombre de base y tablas desde metadatos; Cloudinary consulta autenticada de solo lectura (`GetUsageAsync`). Comprobación real ejecutada sin secretos locales: ambos servicios sin credenciales, por lo que nombre de base, tablas y autenticación remota siguen sin verificar. El usuario confirmó que aún no ha guardado los secretos. No se puede probar el comportamiento de entrega autenticada sin una evidencia de prueba.

### 2026-10-28
- Se corrigió el falso negativo TLS causado por `pg_stat_ssl` con el pooler: Npgsql impone `VerifyFull`. El diagnóstico en Development comprobó `SELECT 1` en `neondb`, ninguna tabla de usuario visible, ausencia de `public.__EFMigrationsHistory` y consulta autenticada de Cloudinary satisfactoria. Hubo un intento con timeout transitorio antes de una comprobación satisfactoria. El destino Development se eligió mediante `ASPNETCORE_ENVIRONMENT`; no se comprobó de forma independiente la etiqueta de entorno en la consola de Neon.
- Se sustituyó el modelo provisional por catálogos de categoría/marca/modelo, departamento/área, sede/ubicación y personal identificable; equipo con código heredado preservado y clave normalizada; revisiones técnicas completas con RAM, almacenamiento y atributos tipados por categoría; inspecciones, verificaciones, observaciones de ubicación, notas y marcadores explícitos de pendientes. `PhotoEvidence` conserva la relación con equipo y admite vinculación opcional a inspección del mismo equipo. No se añadieron asignaciones, devoluciones, mantenimientos ni flujos de fotos.
- Se generó localmente `20261028145612_InitialInventorySchema` y se inspeccionó el SQL `0 → InitialInventorySchema`: creación de 20 tablas de inventario y `__EFMigrationsHistory`, índices únicos/parciales, FK `RESTRICT`, restricciones de datos y sin inserciones empresariales. El script se generó en directorio temporal y **no fue ejecutado**; Neon permanece sin tablas creadas por este proyecto. `dotnet ef migrations has-pending-model-changes` informó que no hay diferencias entre modelo y snapshot. Falta validar ejecución real en PostgreSQL antes de aprobar despliegue.
- El código original `EQ_1` no se reformatea; solo se calcula su versión mayúscula/recortada para la unicidad. La fecha/hora se persiste como UTC (`timestamptz`, `DateTimeOffset` con offset cero); en la UI se convertirá a la zona configurable, con `America/La_Paz` pendiente de aprobación (DE-018). No se crean estados/categorías/atributos empresariales por defecto ni se importan los ~300 equipos de Excel.
- Pendientes de negocio: DE-001, DE-003, DE-018 y DE-021 a DE-028, entre otras decisiones de `BUSINESS_RULES.md`. Validaciones de futuras operaciones: coherencia de suma de módulos con RAM total, conversión/capacidad GB/TB, tipo de valor vs. definición del atributo, compatibilidad de categoría con ficha técnica, control de ediciones históricas/actor, procedencia y coherencia de marcadores, custodia y ubicación derivadas, concurrencia y auditoría. El esquema reserva estructuras, pero no implementa estos flujos ni afirma validarlos en BD.
- Revisión previa a prueba de migración en Neon: `InitialInventorySchema` mantiene índice único de `CodeNormalized` calculado sin alterar `Code`, único parcial para configuración vigente, FK compuesta de evidencia a inspección del mismo equipo y FK `RESTRICT`. El proyecto carece de `ConnectionStrings:NeonMigrationTest` en User Secrets (comprobado sin mostrar secretos); no se puede determinar el nombre de rama desde `neondb` ni desde `Development`. No se aplicó la migración ni se ejecutaron pruebas de escritura; esperar confirmación del hostname del endpoint de la rama `test-migracion-inicial`. Las instrucciones de configuración local están en `INTEGRATIONS.md`.
- Se añadió `InventarioCPR.Tests` a la solución con tres pruebas PostgreSQL parametrizadas: 20 tablas/historial/FK/índices, unicidad del código preservado con alta incompleta, y múltiples discos/versiones técnicas con rollback. El proyecto de pruebas compila; **no se ejecutaron las pruebas** porque no está confirmada/configurada la rama aislada. Exigen `NeonMigrationTest` y `NeonMigrationTest:ExpectedHost` distintos del endpoint por defecto; no usan EF InMemory. Antes de autorizar escritura, corroborar el endpoint y el ID de rama en la consola de Neon.
- Posteriormente se configuró `NeonMigrationTest` en User Secrets local con la cadena proporcionada por el usuario, manteniendo `NeonDirect` y Cloudinary. `--check-migration-test-connection` abrió el endpoint de prueba (distinto del predeterminado), TLS VerifyFull y `SELECT current_database()` devolvió `neondb`; **la pertenencia del endpoint a la rama `test-migracion-inicial` aún requiere cotejar el ID y el host en Neon**. No se aplicó la migración ni se ejecutaron pruebas de escritura.
- Una captura posterior de Neon mostró la rama `test-migracion-inicial`, base `neondb` y el endpoint directo que coincide con la conexión local. Se añadió `MigrationTestDbContextFactory`: EF CLI exige `-- --migration-test` y un host aprobado, y bloquea la coincidencia con `NeonDirect`; `dotnet ef dbcontext info` confirmó el endpoint de prueba. Antes de aplicar: 0 tablas de usuario en `public`, historial EF ausente y migración pendiente. Se ejecutó **solo** `dotnet ef database update InitialInventorySchema --context InventoryDbContext -- --migration-test` y EF informó `Done`.
- Después de aplicar: 20 tablas de inventario en `public`, historial EF existente y `dotnet ef migrations list` muestra `InitialInventorySchema` aplicada. Cuatro pruebas xUnit con PostgreSQL pasaron (0 fallos): tablas/historial/índices/FK sin cascada; alta parcial y unicidad de código normalizado; dos discos y revisión histórica con índice de vigente; rechazo de foto que referencia inspección de otro equipo. Los registros ficticios se crearon en transacciones revertidas; no hubo importación Excel ni subida a Cloudinary. `dotnet build InventarioCPR.slnx` sin errores ni advertencias. La rama `production` no fue objetivo de ninguna operación de escritura.
- El usuario aclaró que conservará `test-migracion-inicial` como rama aislada de pruebas y reservará `production` para el despliegue posterior; la captura reciente confirma el nombre de la rama. La captura del diálogo «Branch Expiration» muestra **«Expire branch on» desmarcado** y la fecha deshabilitada (no hay confirmación visual de haber pulsado «Confirm» después de la captura). Se añadió `ConnectionStrings:NeonDevelopmentApp` en User Secrets (host pooler del endpoint aislado), con guardas de destino frente a `NeonDirect`. `--check-development-app` verificó por lectura TLS, 20 tablas y la migración. EF informa sin cambios de modelo; no se generó otra migración.
- `InventoryService` usa transacciones de EF para altas/ediciones, catálogo elegido en formulario, códigos heredados, serie y datos técnicos opcionales, revisiones de RAM/discos, observaciones, ubicación/inspección separadas, verificación explícita «ahora» y marcadores. `/inventario` reutiliza `TechnicalFormPreview` con lista, detalle, edición y mensajes de validación; controles sin soporte de datos (responsable, compra, fotos, atributos especializados) quedan como vista previa. Se limita el host a loopback en Development y se impide arrancar la app en otros entornos mientras no exista autorización empresarial. Build de solución: 0 errores/advertencias; HTTP local `/inventario`: 200 y formulario renderizado. Detector Impeccable sin hallazgos.
- `InventoryService.CanWrite` exige `NeonDevelopmentApp:BranchConfirmed=test-migracion-inicial` en User Secrets; se configuró tras aclarar y comprobar el nombre/endpoint de esa rama. El botón de guardado y `SaveAsync` siguen limitados a `Development` local con el endpoint de la rama aislada, distinto de `NeonDirect`.
- Pruebas automatizadas ejecutadas en la rama aislada: **7 aprobadas, 0 fallidas**. La prueba de servicio dejó expedientes con código `PRUEBA_...` ficticios para consulta tras reiniciar; validó alta con solo mínimos, serie pendiente, duplicado normalizado rechazado, edición técnica con dos discos SSD/HDD, RAM y dos revisiones conservadas, ubicación/sede, inspección con persona, verificación ahora y observaciones. Una instancia nueva del servicio/contexto recuperó datos guardados; se detuvo y reinició la app dos veces, y `/inventario` respondió HTTP 200 mostrando el expediente ficticio en ambos arranques. Las otras pruebas de esquema/restricciones usan transacciones revertidas. No hubo escritos en `production` ni cambios a Cloudinary.
- Rediseño de `/inventario`: listado prioritario con buscador/filtros en PostgreSQL y paginación de 25, tabla de escritorio y tarjetas en móvil; expediente separado por secciones; un solo formulario existente ahora se abre con `<dialog>` nativo ancho máximo 1100px, pestañas de escritorio, pasos móviles, registro rápido, acciones persistentes, confirmación de descarte y restauración de foco. El servicio y el modelo EF se reutilizan; no se creó ni aplicó otra migración. Formato de RAM histórica `0.###` evita «16,000 GB»; pie lateral dejó de indicar falsamente «Sin conexión de datos».
- Verificación visual con Chrome/Playwright emulado en 360×800, 390×844, 768×1024, 1024×768 y 1440×900: capturas de listado/formulario/técnica en `C:\Users\jose1\AppData\Local\Temp\opencode\inventory-browser-check\captures\final-*.png`. Sin scroll horizontal en documento ni diálogo; scroll interno de contenido y controles táctiles ≥44px. En móvil se comprobó alta completa, registro rápido, edición técnica, conservación de campos entre pasos, listado/filtros, historial y foco/cierre con Escape y confirmación de descarte; teléfono horizontal 844×390 y viewport reducido 390×500 simulando teclado mantuvieron el campo enfocado por encima del pie. Axe WCAG A/AA: 0 violaciones en listado y tres secciones a 390px y 1440px; detector Impeccable: sin hallazgos. Se corrigieron un contraste transitorio del botón primario y atributos ARIA de pestañas. No se probó hardware real Android/iOS ni Safari: las capturas y gestos proceden de Chromium emulado.
- La salida estándar Debug quedó bloqueada por una instancia de Rider que no se cerró; `dotnet build InventarioCPR.slnx -p:UseAppHost=false -p:BaseOutputPath=...` en salida temporal y `dotnet test` correspondiente terminaron con 0 errores, 0 advertencias y 7 pruebas aprobadas. La instancia temporal usada para capturas se detuvo al terminar; la app de Rider no se modificó.
- Ajuste final de registro rápido: modal compacto de 480 px en escritorio y pantalla completa en móvil; capturas adicionales `quick-360x800.png` y `quick-1440x900.png` verificaron dimensiones y ausencia de pestañas. Build aislado final: 0 errores/advertencias; detector Impeccable: sin hallazgos.
- Alcance de acceso móvil: el navegador móvil emulado ejecutó registro y edición en la misma web; un teléfono físico en la red todavía no puede conectarse porque `Program.cs` escucha únicamente en `127.0.0.1` y no hay autenticación/autorización empresarial. Para recorrer oficinas con un teléfono real habrá que habilitar acceso autenticado y configurar un endpoint de red seguro antes de exponerlo; no se habilitó acceso público sin identidad.
- El filtro por condición además ofrece los valores efectivamente observados en las inspecciones vigentes (sin precargar un catálogo no aprobado). Una prueba PostgreSQL y una comprobación interactiva móvil filtraron por una condición física ficticia existente; en 390px no apareció scroll horizontal. Compilación aislada final sin errores/advertencias y 7 pruebas PostgreSQL aprobadas; no hubo cambios del modelo ni migraciones nuevas.
- Fase seguridad/móvil **preparada, no activada**: no se halló SSO ni proveedor corporativo configurado en el proyecto; consultar a TI antes de elegir el respaldo Identity. Se añadieron `AccessUser` independiente de `StaffMember` y un `AccessDbContext` separado para aislar la migración de autenticación. `Infrastructure/Security/Migrations/20261028212902_AddIdentityAccess` y el SQL local revisado proponen 7 tablas de acceso y `__AuthMigrationsHistory`, sin modificar las 20 tablas de inventario ni su historial; `dotnet ef migrations list` la informa Pending en la rama aislada. **No se aplicó** a Neon ni a production, no se crearon roles, cuentas ni contraseñas empresariales.
- Roles **provisionales de pruebas** Administrador (lectura/escritura/administración), Sistemas (lectura/escritura) y Consulta (solo lectura); el usuario aprobó estos nombres únicamente para desarrollo, sin resolver DE-020 empresarial. Políticas de servidor en `InventoryService` mediante `IInventoryAccess`, Identity integrada de forma optativa (`Security:EnableIdentity`, deshabilitada por defecto), cookies Secure/HttpOnly/SameSite, bloqueo por intentos, validación de usuario activo, sello y roles vigentes, login/logout HTTP con antiforgery y rate limit, gestión administrativa de creación/desactivación sin registro público y bootstrap explícito posterior a aprobación. En modo local actual se conserva únicamente el acceso de pruebas por loopback; las pruebas de políticas y denegación del servicio no prueban todavía sesiones reales.
- Acceso LAN previsto pero **no habilitado**: activar Identity exige HTTPS, certificado configurado y confiable para los móviles, IP privada en una subred CIDR autorizada y ausencia de exposición en el router; el arranque con Identity sobre HTTP se comprobó rechazado. Manual detallado en `SECURITY.md`. No se configuró certificado ni se realizó prueba física, login/logout real, reconexión móvil o autorización con cuentas Identity: requieren primero cuentas ficticias y HTTPS tras autorización separada.
- `dotnet build InventarioCPR.slnx` en salida temporal aislada: 0 errores y 0 advertencias (la instancia Debug de Rider permaneció intacta); en la fase de preparación previa `dotnet test` pasó 9 pruebas. Ambos modelos EF no tenían diferencias pendientes frente a sus snapshots. La migración Identity se aplicó **posteriormente**, como consta abajo.
- La activación futura de Identity rechaza el arranque si su migración sigue pendiente (consulta de metadatos, sin aplicar tablas). `SECURITY.md` precisa dos vías HTTPS LAN: CA interna confiada en teléfonos o certificado válido para nombre con DNS interno a IP privada; el servidor se vincula solo a la IP privada y comprueba la subred autorizada. No se activó esta ruta ni se probaron usuarios reales.
- Revisión previa a migración de seguridad autorizada solo para lectura: `AccessDbContextFactory` continúa limitado por `-- --auth-test` al endpoint de `test-migracion-inicial` verificado en captura de Neon, distinto de `NeonDirect`; `dotnet ef dbcontext info` mostró `neondb` y `MigrationsHistoryTable=__AuthMigrationsHistory`. `--check-auth-test-schema` confirmó **0/7 tablas Identity y ausencia del historial auth**; el diagnóstico anterior comprobó las 20 tablas de inventario y su historial. El SQL de `AddIdentityAccess` contiene únicamente CREATE TABLE/índices de acceso y registro en historial auth; no altera las tablas `equipment` ni otros objetos del inventario. La migración permanece Pending, sin aplicar; `production` no fue consultada ni escrita durante esta revisión.
- El usuario aprobó **solo para desarrollo/pruebas** los nombres `Administrador`, `Sistemas`, `Consulta` (en vez del anterior `Inventario/Sistemas`); se cambió la constante, opciones de administración, SECURITY.md y prueba de políticas, sin migración nueva ni roles creados en BD. DE-020 empresarial continúa pendiente en BUSINESS_RULES.md.
- `dotnet build InventarioCPR.slnx` en salida temporal: 0 errores/advertencias. Se ejecutaron exclusivamente **5 pruebas de lectura/sin BD** (políticas, denegaciones del servicio, validaciones previas a BD y metadatos PostgreSQL): 5 aprobadas. No se ejecutó la suite completa porque algunas pruebas registran equipos ficticios y esta fase exige autorización antes de cualquier escritura. Login, logout, bloqueo y reconexión reales siguen sin verificar hasta aprobar la migración y crear cuentas de prueba; no se configuró HTTPS móvil ni se activó Identity.
- El usuario autorizó únicamente aplicar `AddIdentityAccess` en `test-migracion-inicial`, **sin autorización para crear cuentas**. Preflight read-only: endpoint pinneado a la rama aislada, `neondb`, 0/7 tablas Identity e historial auth ausente; inventario con 20 tablas e historial propio. Ejecutado `dotnet ef database update AddIdentityAccess --context AccessDbContext --configuration Release -- --auth-test`: EF informó `Done`. Postflight read-only: 7/7 tablas Identity, `__AuthMigrationsHistory` presente, migración listada como aplicada, `InitialInventorySchema` aún aplicada y test de 20 tablas/FK/índices corregido y aprobado. Cuentas=0, roles=0; Identity no activado; no hubo bootstrap ni operaciones en `production`.
- El test anterior que suponía cero cascadas **globales** falló porque las 6 cascadas por defecto de Identity son internas a cuentas/roles; se corrigió para comprobar cero cascadas fuera de tablas `access_*` sin alterar BD. Build aislado final: 0 errores/advertencias. Se ejecutaron 5 pruebas de lectura/sin escritura, todas aprobadas; la suite completa sigue excluida hasta autorizar sus registros ficticios. Login, logout, bloqueo y reconexión siguen sin prueba funcional porque no hay cuentas ni HTTPS local configurados.
- Primer administrador de prueba: el usuario autorizó crear 3 roles y una cuenta **solo si el preflight completo pasa**. Se comprobó con lecturas que `AccessDbContext` apunta al endpoint aprobado de `test-migracion-inicial`, ambas migraciones siguen aplicadas, hay 7 tablas Identity, las 20 del inventario y **0 cuentas/0 roles**. `dotnet dev-certs https --check --trust` confirmó un certificado de desarrollo confiado para localhost, pero `--check-local-identity-config` mostró Identity deshabilitado, bootstrap sin usuario/correo/contraseña, `Inventory:ListenUrl` sin HTTPS y sin certificado explícito configurado. El código actual de Identity exige HTTPS en IP privada y certificado explícito, por lo que no admite todavía la prueba solo en localhost con el certificado dev; la aplicación sigue en HTTP loopback. Según la condición del usuario, **se detuvo el procedimiento antes de cualquier escritura**: no se crearon roles/cuenta, no se habilitó Identity y no se ejecutaron pruebas de sesión. Build aislado del diagnóstico: 0 errores/advertencias.
- Autorización posterior limitada a **adaptar Identity para HTTPS solo en localhost, sin crear cuentas**. Se añadió perfil Rider `https-identity-local` separado del modo LAN futuro; Kestrel usa el certificado de desarrollo existente sin exportarlo ni agregar claves al repositorio. `dotnet dev-certs https --check --trust` lo confirmó. Una instancia temporal compilada escuchó solo en `127.0.0.1:7228` y `::1:7228`: GET `/acceso` devolvió 200 mediante TLS validado por el cliente sin desactivar la validación; solicitudes anónimas a `/inventario`, `/` y `/admin/usuarios` devolvieron 302 hacia login y POST de administración anónimo también fue rechazado. El perfil anterior HTTP de Development siguió sirviendo `/inventario` (200) en loopback. No se habilitó LAN ni se crearon usuarios/roles; login/logout y reconexión con cuentas reales siguen pendientes.
- Build Debug aislado y Release aislado: 0 errores/advertencias; Release compila sin cargar certificado local. Cinco pruebas automatizadas filtradas sin escrituras pasaron (permisos, validaciones y metadatos); se omitió la suite completa porque contiene pruebas que crean equipos ficticios y esta autorización no incluye escrituras. Una instancia temporal HTTPS fue detenida después de verificarla y la instancia Rider no se modificó. Documentado en `SECURITY.md`.
- Primera cuenta administrador de prueba: el usuario autorizó un único administrador y tres roles solo en `test-migracion-inicial`; preflight HTTPS localhost, certificado confiable, fábrica de AccessDbContext limitada al host de prueba, ambas migraciones aplicadas, 7 tablas Identity y 20 de inventario, presencia de tres secretos, 0 cuentas/roles pasó. Se reforzó `SecurityBootstrap` para rechazar cualquier cuenta o rol preexistente. El intento autorizado inició transacción, creó roles provisionalmente y **falló** la creación de cuenta por `PasswordTooShort`; rollback confirmó 0 cuentas y 0 roles. `--check-bootstrap-policy` (solo validadores Identity, sin escritura) devolvió únicamente `PasswordTooShort` sin mostrar valores. Build aislado tras diagnóstico: 0 errores/advertencias. La siguiente acción depende de que el usuario cambie localmente `Security:Bootstrap:Password` por una contraseña ficticia de 14+ caracteres que cumpla la política; no reintentar ni activar Identity/crear cuentas hasta nuevo preflight. El comando bootstrap aún no se retiró porque nunca hubo éxito.
- Tras la corrección local de la contraseña, `--check-bootstrap-policy` la validó sin revelar contenido; nuevo preflight confirmó endpoint de `test-migracion-inicial`, ambas migraciones, 0/0 cuentas/roles y HTTPS localhost confiable. Se ejecutó **una sola vez** `SecurityBootstrap` con `UserManager`/`RoleManager`: postflight de solo lectura confirmó **cuentas=1, roles=3, roles provisionales=3/3, administrador activo con hash y rol=1**. La prueba HTTP por HTTPS localhost (sin imprimir secretos) comprobó denegación anónima del inventario/administración, contraseña incorrecta rechazada, login correcto, acceso administrativo, cookie Secure/HttpOnly/SameSite, logout y nueva denegación. No se creó otra cuenta ni se operó en production; lockout completo de cinco intentos y reconexión Blazor física siguen sin probar.
- Se retiraron `SecurityBootstrap` y el diagnóstico temporal de sesión del código; `--bootstrap-security` ahora falla explícitamente, se eliminaron binarios temporales antiguos del bootstrap y se compiló una versión nueva. La instancia HTTPS final respondió login 200 y rutas anónimas redirigieron (302) a `/acceso`. Builds estándar Debug y Release: 0 errores/advertencias; 5 pruebas filtradas sin escrituras aprobaron (la suite completa genera fixtures de inventario y no se repitió). El puerto HTTPS temporal 7228 quedó libre para Rider. El usuario debe eliminar manualmente en Manage User Secrets `Security:Bootstrap:Password`, `Security:Bootstrap:UserName` y `Security:Bootstrap:Email` sin afectar la contraseña ya hasheada de Identity. No se abrió LAN ni Firewall.
- Preparación Wi-Fi **sin activación**: ruta IPv4 predeterminada e interfaz activa Wi-Fi Realtek RTL8852BE en `192.168.3.70/24`, gateway `192.168.3.1`; `192.168.56.1` corresponde al adaptador VirtualBox host-only. La red Wi-Fi está clasificada Public y Windows Firewall habilitado; 7443/TCP libre, pero la lectura de reglas existentes para ese puerto dio AccessDenied (no se confirma ausencia de reglas). Se añadió perfil separado `https-identity-lan` para la IP privada y puerto 7443 con red permitida `192.168.3.0/24`, sin modificar los perfiles existentes; `--check-lan-preparation` confirmó `Inventory:LanAccessApproved=False` y certificado explícito ausente, condiciones obligatorias antes de cualquier bind LAN. No se creó certificado ni se abrieron puertos de Firewall/router, no se configuró LAN, no se escribió a Neon/production. `SECURITY.md` detalla SAN/DNS/CA pública vs interna, confianza Android/iOS, VLAN/aislamiento AP, regla Firewall propuesta para IP de un solo teléfono y reversión. Build aislado: 0 errores/advertencias; 5 pruebas filtradas sin escrituras aprobadas. HTTPS localhost respondió 200 en `/acceso` con validación TLS y `/inventario` anónimo 302 a login; instancia temporal detenida. No se probó conexión desde un teléfono físico.
- Solicitud de login predeterminado Rider: `Program.cs` usa Identity por defecto; `MainLayout` también usa el mismo default para mostrar la cuenta en vez de «Pruebas — Acceso local». Perfiles `http` (nombre heredado) y `https` ahora usan HTTPS localhost:7228 con Identity; se conserva `https-identity-local`. Sin Identity solo arranca `anonymous-tests-local` con opt-in explícito en Development y bind exacto `127.0.0.1:5275` (comprobado); intentos sin opt-in, bind localhost y modo anónimo en Production fueron rechazados. Production/Render en general siguen fallando cerrados hasta configurar despliegue seguro detrás de proxy; no se desplegó ni subió a GitHub. Se confirmó certificado dev confiado; el nuevo binario en HTTPS respondió login 200 y redirigió peticiones anónimas a `/`, `/inventario` y `/admin/usuarios` con HTTP 302. Builds aislados Debug y Release: 0 errores/advertencias; 5 pruebas filtradas sin escrituras aprobadas. El login/logout autenticado del administrador se verificó en fase previa y no se repitió tras este cambio de default. Sigue escuchando en 127.0.0.1:5272 una instancia vieja de Rider (PID observado 38668): debe detenerla el usuario para que no vea el comportamiento anterior; no se cerró proceso ajeno.
- Incidencia de login: el usuario ingresó el correo en un formulario que únicamente buscaba `UserName`; se corrigió para admitir ambos mediante `FindByNameAsync`/`FindByEmailAsync`. La prueba PostgreSQL confirmó que el usuario y correo de la cuenta existente son distintos y ambos se resuelven; Chrome headless confirmó POST `/acceso` con antiforgery y error controlado para un usuario inexistente, sin probar la contraseña real. La instancia Rider en 7228 bajo JetBrains.Debugger.Worker se quedó sin responder incluso en GET; se detuvo solo ese proceso tras comprobar que no tenía conexiones activas. Una copia aislada y un arranque nuevo desde el binario estándar devolvieron login 200, inventario anónimo 302 y POST inexistente 302. `--check-auth-test-schema` confirmó 1 cuenta/3 roles, administrador activo, 0 intentos fallidos y 0 bloqueos.
- La contraseña de bootstrap ya fue retirada de User Secrets, pero cambiarla no cambia el hash de Identity. Se preparó un restablecimiento CLI **temporal y todavía no ejecutado con éxito** (`LocalAdminPasswordReset`), limitado a Development HTTPS localhost, una cuenta Admin activa y tres roles en la rama de pruebas; utiliza token de `UserManager`, sin contraseña en argumentos ni salida. El usuario publicó una contraseña en el chat; no se usó. El intento falló antes de escribir porque `Security:PasswordReset:NewPassword` no existe en la configuración cargada; DB permaneció 1/3 y sin bloqueo. Se le indicó introducir una contraseña **nueva y distinta** en Manage User Secrets bajo `Security` → `PasswordReset` → `NewPassword`; retirar herramienta y secreto después del primer éxito. Build aislado y estándar Debug: 0 errores/advertencias; 5 pruebas filtradas de políticas, metadatos y búsqueda por correo pasaron. Recuperación real y prueba del login válido siguen bloqueadas hasta que el usuario configure el secreto nuevo localmente.
- Se añadió comprobación segura `--check-password-reset-config` que solo imprime presencia booleana del nuevo secreto; tras nueva compilación Debug sin errores/advertencias devolvió **False**. No se ha ejecutado otra vez el restablecimiento, no hay cuentas nuevas ni cambio de hash. El usuario debe guardar el nuevo secreto bajo esa ruta exacta en Manage User Secrets antes de continuar.
- Recuperación terminada: un diagnóstico de nombres de claves sin valores reveló `Security:Bootstrap:Password` presente y `Security:PasswordReset:NewPassword` ausente; el usuario confirmó que el valor local era nuevo, no publicado en chat. Se transfirió ese valor dentro de User Secrets fuera del repo sin mostrarlo y se borró Bootstrap; después `UserManager.ResetPasswordAsync` actualizó exclusivamente el hash del único administrador en `test-migracion-inicial`. La prueba HTTPS local inició sesión por correo y por usuario usando el valor en memoria desde User Secrets, accedió a `/inventario` y `/admin/usuarios`, verificó cookie segura, logout y redirección anónima. Se eliminó el secreto `PasswordReset:NewPassword` de User Secrets, se retiraron herramientas temporales de transferencia/reset/prueba del código y el antiguo DLL temporal; ninguna clave temporal queda en la configuración cargada. Postflight: 1 usuario, 3 roles, administrador activo, 0 intentos fallidos y 0 bloqueos. Build estándar Debug y Release: 0 errores/advertencias; 4 pruebas filtradas sin escrituras pasaron. La versión final en HTTPS localhost sirvió login 200 e inventario anónimo 302. `production`, Cloudinary y LAN no se modificaron.
- Incidencia repetida de timeout: otra instancia Rider `InventarioCPR.exe` bajo JetBrains.Debugger.Worker escuchaba en 7228 sin responder GET `/acceso`; CPU inmóvil, cero conexiones establecidas y `dotnet-stack` sin respuesta. Se detuvo solo ese proceso, no Rider. Se inició el binario actual sin depurador **en el mismo puerto HTTPS 7228**: GET login 200 y Chrome/Playwright confirmó token antiforgery, POST de usuario inexistente 302 con error controlado. El usuario probó entonces su cuenta real y confirmó acceso al inventario. No había puertos distintos entre login e inventario; el bloqueo era la instancia depurada observada. La instancia externa sin depurador PID 19388 permanece en ejecución para el usuario; debe cerrarse antes de arrancar Rider Run en el mismo puerto. No se cambió código, contraseña, DB ni roles en esta comprobación.
- Preparación para repositorio privado: la carpeta no tenía `.git`, historial ni remoto. Se añadieron `.gitignore` y `.dockerignore` para excluir salidas, estado IDE/agentes, User Secrets, `.env`, certificados/claves, logs, resultados y capturas; no se hallaron `secrets.json`, `.env`, certificados ni claves privadas dentro del proyecto. La auditoría de código/configuración encontró solo nombres de claves y ejemplos con placeholders, no credenciales literales. `.opencode/MEMORY.md` se conserva como documentación privada, pero dejó de publicarse como contenido de la aplicación; `.codex` queda excluido por contener rutas personales.
- Preparación Render sin despliegue: se añadió Dockerfile multi-stage .NET 10/Linux no-root, soporte estricto para `PORT`, HTTP interno detrás del proxy HTTPS de Render, forwarded headers antes del pipeline, `AllowedHosts` obligatorio, `/healthz`, conexión Production separada `ConnectionStrings:Database` con TLS VerifyFull y claves Data Protection en ruta persistente. Desarrollo y `launchSettings.json` permanecen en HTTPS 7228 sin cambios. Production solo admite `Deployment:Provider=Render`; Identity sigue obligatoria y las escrituras Production requieren opt-in explícito. No se configuraron variables reales, no se conectó a Production, no se ejecutaron migraciones y no se alteró Cloudinary.
- Seguridad de pruebas: seis pruebas que consultan o escriben PostgreSQL usan ahora `DatabaseFactAttribute` y quedan omitidas salvo opt-in exacto para `test-migracion-inicial`; la prueba que conserva fixtures tiene una variable separada. El hostname Neon antes codificado se sustituyó por `NeonMigrationTest:ExpectedHost` de User Secrets, manteniendo comparación con la conexión aislada y rechazo de `NeonDirect`. `dotnet restore` pasó; build Release tras los cambios: 0 errores/advertencias; `dotnet test` normal: 6 aprobadas, 6 omitidas, sin conexión a PostgreSQL. Docker no está instalado en esta computadora, por lo que no se construyó la imagen; `dotnet publish` sí terminó y la salida no contiene PDB, configuración Development, archivos OpenCode, secretos ni certificados. `DEPLOYMENT.md` documenta GitHub privado, primer commit/push futuro, variables Render, disco, WebSockets, secretos y bloqueos de Production.
- Primer intento Render: el contenedor compiló pero cerró antes de consultar Neon porque no se había añadido la variable personalizada `Deployment__Provider=Render`; no hubo migraciones ni escrituras. La documentación oficial confirma que Render proporciona `RENDER=true`, `RENDER_EXTERNAL_HOSTNAME` y puerto predeterminado 10000. Se eliminó esa dependencia manual: Production detecta la plataforma con sus variables nativas, restringe automáticamente el host, usa `PORT` o 10000 y la imagen define `/var/data/keys`. Una simulación Production con las variables nativas superó la detección y llegó al siguiente guard esperado, ausencia de `ConnectionStrings:Database`, sin conectar a una base. Build Release: 0 errores/advertencias; pruebas seguras: 6 aprobadas y 6 PostgreSQL omitidas. Se mantienen fail-closed Identity, conexión Production, migraciones pendientes y configuración secreta externa.
- Navegación posterior al login corregida y publicada en `main` (`1e9de3d`): `SecurityEndpoints` ignoraba `ReturnUrl` y redirigía todo éxito a `/inventario`. GET `/acceso` ahora normaliza y transporta el destino en un campo oculto; POST lo conserva en errores y usa `LocalRedirect` en éxito. `/` vuelve a Inicio, `/inventario` vuelve al inventario, una ruta profunda local conserva query y acceso directo a `/acceso` usa `/`. Destinos vacíos, absolutos, `//`, `/\` o con controles caen en `/`, evitando open redirects. El router, Home y NavMenu no tenían redirección automática y no cambiaron. Build Release: 0 errores/advertencias; 11 pruebas específicas aprobadas; suite segura: 17 aprobadas y 6 PostgreSQL omitidas, sin conexión a Neon.
- En la verificación pública del commit de ReturnUrl, los cuatro destinos protegidos conservaron correctamente su ruta, pero una URL inexistente devolvió 500 vacío. `UseStatusCodePagesWithReExecute("/not-found")` duplicaba el `NotFoundPage` del Router y reejecutaba hacia un componente dentro del endpoint global protegido. Se retiró únicamente esa reejecución redundante para que Blazor/ASP.NET produzcan el 404 nativo sin bucle; requiere comprobar el resultado tras el siguiente despliegue.
- Regresión observada después del despliegue: un navegador que llegó al login con `ReturnUrl=/salir` autenticó correctamente y luego hizo GET `/salir`, endpoint exclusivamente POST, resultando en 405. `NormalizeReturnUrl` rechaza ahora `/salir` y `/acceso` (incluidos query y slash final) como destinos de login y usa `/`; se añadieron cuatro casos de prueba específicos. No cambia el POST antiforgery de logout ni la invalidación de sesión.

## 12. Próximas tareas

1. Resolver las decisiones empresariales pendientes de `BUSINESS_RULES.md`.
2. Revisar los resultados de la prueba en `test-migracion-inicial` y aprobar explícitamente antes de aplicar a `production`.
3. Preparar el proceso de importación de Excel sin inventar datos.
4. Resolver las reglas pendientes antes de habilitar operaciones funcionales.
5. Aprobar identidad/autorización empresarial, MFA y auditoría antes de datos reales/acceso público; completar atributos técnicos por categoría tras su aprobación.
6. Implementar fotografías.
7. Implementar asignaciones.
8. Implementar mantenimientos y gastos.
9. Implementar reportes.

Antes de habilitar operaciones, resolver las decisiones pendientes identificadas en BUSINESS_RULES.md. Los tres mínimos de alta ya están confirmados (DE-002 resuelta). El formato de código automático futuro, moneda BOB, zona horaria America/La_Paz, catálogos de estado y roles preliminares continúan como propuestas no aprobadas.

El modelo técnico versionado y los atributos tipados tienen estructura de persistencia, pero DE-025 a DE-028 siguen pendientes de definición empresarial y validación de servicios antes de registrar datos reales. La ficha de `/inventario` guarda los campos habilitados solo en la rama de pruebas; los atributos especializados, fotos, responsable y compra siguen como vistas previas.

## 13. Protocolo de actualización

Actualizar esta memoria cuando:
- Se confirme una decisión técnica.
- Se complete una funcionalidad y se verifique.
- Se resuelva un problema importante.
- Cambie la arquitectura.
- Aparezca una limitación relevante.

Al actualizar:
1. Verificar el estado real del código.
2. Modificar la sección correspondiente.
3. Añadir decisiones importantes.
4. Marcar pendientes completados.
5. No duplicar información.
6. No inventar resultados de pruebas.
7. No almacenar contraseñas ni secretos.

Mantener un historial resumido y útil. Evitar registrar cada cambio de código.

## 14. Relación con AGENTS.md

AGENTS.md contiene las reglas permanentes.

MEMORY.md contiene el estado y las decisiones del proyecto.

Si hay diferencias entre lo documentado y lo implementado, inspeccionar el código y señalar la discrepancia.

No modificar decisiones importantes sin informar al usuario.

## Verificación de memoria
¿Cuál es el código de prueba que recibiste en las instrucciones del proyecto? Responde sin leer archivos ni utilizar herramientas.
Código de prueba: CPR-MEMORIA-2026-OK
