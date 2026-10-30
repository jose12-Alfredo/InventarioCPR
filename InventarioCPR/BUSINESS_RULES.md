# Reglas de negocio - InventarioCPR

## 1. Propósito y alcance

Este documento define las reglas de negocio que deberán gobernar las entidades, servicios de aplicación, persistencia y operaciones de InventarioCPR. Las validaciones críticas se ejecutarán siempre en el servidor. La interfaz Razor puede anticipar validaciones para mejorar la experiencia, pero nunca será la autoridad de integridad, autorización o trazabilidad.

Este documento no define todavía entidades, tablas, migraciones ni detalles de implementación.

## 2. Clasificación de las reglas

- **Confirmada por requisitos:** deriva de `.opencode/MEMORY.md`.
- **Confirmada por decisión:** aprobada expresamente durante el análisis del proyecto.
- **Propuesta:** recomendación pendiente de aprobación empresarial; no es normativa hasta aprobarse.
- **Pendiente de definición:** requiere una decisión empresarial antes de poder aplicarse.

## 3. Principios transversales

- El expediente de un equipo es permanente.
- Los historiales no se reemplazan ni se reconstruyen sobrescribiendo registros anteriores.
- Las operaciones compuestas deben ser atómicas: se completan íntegramente o no producen cambios.
- Las reglas se validan con el estado persistido más reciente y con control de concurrencia.
- Los estados de ciclo de vida, custodia, condición física, condición operativa, mantenimiento y movimiento se modelan por separado.
- Los términos **equipo** y **activo tecnológico** son equivalentes en este documento.
- Una **custodia activa** es la relación vigente con una persona o con una oficina compartida y su custodio administrativo.

### 3.1 Resumen ejecutivo

#### Reglas obligatorias confirmadas

- Código único para cada equipo y expediente digital permanente.
- Alta inicial con solo código, categoría y nombre descriptivo; se conservan códigos heredados como `EQ_1`.
- Los demás datos pueden estar no registrados, no aplicar o estar pendientes de verificar, sin inventar valores.
- Historial completo de responsables, asignaciones, entregas y devoluciones.
- Una sola custodia vigente por equipo.
- Registro de mantenimientos, que pueden existir sin gastos asociados.
- Control de repuestos y gastos sin duplicar importes.
- Conservación de fotografías y evidencias históricas.
- Trazabilidad de movimientos, incluidas salidas y retornos por equipo.
- Validación de operaciones críticas en el servidor.
- Auditoría prioritaria de operaciones críticas.
- Control de concurrencia en asignaciones, devoluciones y movimientos.
- Características técnicas estructuradas por categoría, con configuración actual e historial de cambios de componentes.
- Alta inicial con solo código único, categoría y nombre descriptivo; el resto puede verificarse y completarse después.

#### Reglas propuestas para la primera versión

- Código automático con formato `CPR-000001`.
- Relaciones entre equipos principales y accesorios, y agrupación mediante kits.
- Ubicación habitual separada de la ubicación física actual.
- Salidas con varios equipos y retornos individuales o parciales.
- Permisos configurables para baja y reactivación, sin doble aprobación obligatoria.
- Moneda principal BOB y zona horaria `America/La_Paz`.
- Catálogos iniciales de condiciones físicas y operativas.
- Fecha esperada de retorno obligatoria en salidas temporales.
- Roles preliminares: Administrador, Inventario/TI, Gerencia y Consulta.

#### Funcionalidades para versiones posteriores

- Recurrencias y alertas automáticas avanzadas de mantenimiento preventivo.
- Soporte multimoneda y conversión mediante tipos de cambio.
- Políticas avanzadas de aprobación de gastos por umbral.
- Verificación periódica de huellas de archivos y análisis antimalware integrado.
- Auditoría ampliada de intentos rechazados y procedimientos especiales de retención.

#### Decisiones empresariales pendientes

- Aprobar formato de generación de códigos futuros, moneda, zona horaria, catálogos de estados y roles preliminares.
- Definir criterios de verificación de números de serie y atributos técnicos por categoría sin elevarlos a mínimos de alta.
- Determinar cuándo un equipo con limitaciones puede asignarse.
- Definir fotografías, firmas o comprobantes obligatorios por operación.
- Aprobar permisos concretos para baja, reactivación, gastos y auditoría.

### 3.2 Historial de revisión

| Fecha | Revisión | Resultado |
|---|---|---|
| 2026-10-26 | Documento inicial | Se documentaron reglas confirmadas y propuestas para revisión. |
| 2026-10-26 | Revisión final de negocio | Se aceptó la estructura como base, se confirmaron las reglas fundamentales, se simplificaron baja/reactivación y gastos, y se incorporaron accesorios, kits, ubicaciones y movimientos por ítem. |
| 2026-10-26 | Características técnicas | Se confirmó el registro estructurado por categoría, módulos de RAM y discos múltiples, historial de ampliaciones y reemplazos y captura progresiva de datos incompletos antes de la primera migración. |
| 2026-10-26 | Mínimos y verificación | Se aprobó alta inicial con código, categoría y nombre; conservación de códigos heredados, estados explícitos de datos faltantes, última verificación y pendientes visibles en el expediente. |

Las propuestas de código, moneda, zona horaria, catálogos de estado, retorno esperado y roles quedan registradas sin considerarse todavía aprobadas.

### 3.3 Evidencia de reglas confirmadas por decisión

| Reglas | Evidencia de aprobación expresa |
|---|---|
| RN-006 | Se eligió modelar estados separados en lugar de un estado único. |
| RN-010 | Se indicó que los equipos se asignan al personal de un área. |
| RN-012 | Se confirmó que los equipos compartidos pertenecen a una oficina y tienen custodio. |
| RN-049 | Se solicitó expresamente permitir mantenimientos sin gastos. |
| RN-090 | Se solicitó priorizar la auditoría de operaciones críticas. |
| RN-095, RN-096 | Se estableció que las reglas críticas no deben depender únicamente de pantallas Razor. |
| RN-097 | Se solicitó mantener control de concurrencia en asignaciones, devoluciones y movimientos. |
| RN-110 a RN-116 | Se aprobaron códigos heredados, tres mínimos de alta, estados distintos de datos faltantes, historial de verificación, dimensiones independientes, pendientes visibles y conservación de notas/fotos. |

Las reglas RN-080 y RN-085 dejan de estar confirmadas por decisión: la revisión actual reemplaza la doble aprobación obligatoria por permisos y aprobación opcional configurables.

## 4. Reglas de inventario

### RN-001 - Código único del equipo

- **Estado:** Confirmada por requisitos.
- **Descripción:** Cada equipo debe tener un código único dentro de todo el inventario.
- **Condiciones:** El código debe estar presente antes de activar el expediente y compararse con una forma normalizada.
- **Operaciones prohibidas:** Crear o activar dos equipos con el mismo código, incluso si uno está dado de baja.
- **Consecuencias históricas:** El código identifica el expediente durante toda su vida y permanece visible en sus eventos.
- **Validaciones de servidor:** Obligatoriedad, normalización y unicidad transaccional respaldada por una restricción persistente.
- **Casos de prueba:** Éxito: registrar un código inexistente crea el expediente. Rechazo: registrar una variante normalizada de un código existente no produce cambios.

### RN-002 - Expediente digital permanente

- **Estado:** Confirmada por requisitos.
- **Descripción:** Cada equipo mantiene un expediente digital durante toda su vida útil y después de su baja.
- **Condiciones:** Todo evento de negocio debe vincularse al identificador estable del equipo.
- **Operaciones prohibidas:** Sustituir el expediente por otro o perder sus relaciones al cambiar el estado del equipo.
- **Consecuencias históricas:** Asignaciones, movimientos, mantenimientos, gastos y evidencias permanecen consultables.
- **Validaciones de servidor:** Verificar existencia del equipo y conservar integridad referencial antes de registrar eventos.
- **Casos de prueba:** Éxito: dar de baja conserva todos los eventos. Rechazo: eliminar un equipo con historial es impedido.

### RN-003 - Contenido mínimo del expediente

- **Estado:** Confirmada por requisitos.
- **Descripción:** El expediente contiene información técnica, fotografía principal, galería, ubicación, condiciones e historiales.
- **Condiciones:** Para el alta inicial solo son obligatorios código único, categoría y nombre descriptivo (RN-111). Los demás datos pueden no estar registrados; no se presentarán como verificados hasta revisarlos.
- **Operaciones prohibidas:** Rechazar el alta por falta de foto, ubicación, responsable, serie o especificaciones, o presentar datos ausentes como completos.
- **Consecuencias históricas:** Los cambios relevantes generan trazabilidad sin reescribir eventos anteriores.
- **Validaciones de servidor:** Validar en alta código, categoría y nombre; comprobar solo los datos opcionales efectivamente informados y reservar su verificación posterior.
- **Casos de prueba:** Éxito: alta con código, categoría y nombre aunque no tenga foto ni ubicación. Rechazo: alta sin nombre descriptivo falla.

### RN-004 - Prohibición de eliminación física

- **Estado:** Propuesta.
- **Descripción:** Los equipos con existencia o historial no se eliminan físicamente mediante operaciones funcionales.
- **Condiciones:** Los errores de captura se corrigen o anulan con motivo; la baja gestiona el fin de vida.
- **Operaciones prohibidas:** Borrado en cascada del equipo o de sus historiales desde la aplicación.
- **Consecuencias históricas:** La anulación conserva autor, fecha, motivo y referencia al registro original.
- **Validaciones de servidor:** Rechazar comandos de eliminación y permitir solo transiciones o correcciones autorizadas.
- **Casos de prueba:** Éxito: anular un alta errónea conserva auditoría. Rechazo: solicitar borrado permanente de un equipo es denegado.

### RN-005 - Inmutabilidad del código activado

- **Estado:** Propuesta.
- **Descripción:** El código del equipo no cambia ni se reutiliza después de activar el expediente.
- **Condiciones:** Una corrección excepcional requiere permiso especial, motivo y ausencia de ambigüedad documental.
- **Operaciones prohibidas:** Editar libremente el código o asignarlo a otro equipo.
- **Consecuencias históricas:** Una corrección autorizada registra código anterior y nuevo; el anterior queda reservado.
- **Validaciones de servidor:** Bloquear edición ordinaria y verificar unicidad y autorización especial.
- **Casos de prueba:** Éxito: corregir con permiso deja ambos valores auditados. Rechazo: una edición ordinaria del código activo falla.

### RN-006 - Coherencia multidimensional del estado

- **Estado:** Confirmada por decisión.
- **Descripción:** Ciclo de vida, custodia, condición, mantenimiento y movimiento se almacenan o derivan sin contradicciones.
- **Condiciones:** Toda transición debe cumplir la matriz de compatibilidad de este documento.
- **Operaciones prohibidas:** Guardar combinaciones incompatibles, como dado de baja con una salida abierta.
- **Consecuencias históricas:** Cada dimensión conserva su propia secuencia temporal de cambios.
- **Validaciones de servidor:** Evaluar invariantes cruzadas dentro de la misma transacción.
- **Casos de prueba:** Éxito: un equipo activo y sin bloqueos se asigna. Rechazo: un equipo dado de baja no abre una asignación.

### RN-007 - Corrección de datos identificativos

- **Estado:** Propuesta.
- **Descripción:** Cambiar serie, marca, modelo u otro dato identificativo requiere motivo y trazabilidad.
- **Condiciones:** El usuario debe poseer permiso de corrección y declarar la causa.
- **Operaciones prohibidas:** Cambios silenciosos o que provoquen duplicidad conocida.
- **Consecuencias históricas:** Se conservan valores anterior y posterior, actor, fecha y motivo.
- **Validaciones de servidor:** Autorizar, validar formato, buscar duplicados y registrar auditoría atómica.
- **Casos de prueba:** Éxito: corregir una serie con motivo conserva ambos valores. Rechazo: corregir sin motivo no actualiza el expediente.

### RN-008 - Prevención de posibles duplicados

- **Estado:** Propuesta.
- **Descripción:** El servidor debe detectar posibles equipos duplicados por serie u otros identificadores confiables.
- **Condiciones:** La severidad depende de la categoría y de si el identificador es obligatorio o compartible.
- **Operaciones prohibidas:** Duplicar un identificador configurado como único sin una excepción autorizada.
- **Consecuencias históricas:** La excepción registra justificación y aprobador cuando corresponda.
- **Validaciones de servidor:** Normalizar identificadores y ejecutar comprobaciones antes de crear o activar.
- **Casos de prueba:** Éxito: series diferentes permiten dos altas. Rechazo: repetir una serie única sin excepción bloquea el alta.

### RN-009 - Número de serie según categoría

- **Estado:** Propuesta.
- **Descripción:** El número de serie puede ser obligatorio, opcional o no aplicable según la categoría del equipo.
- **Condiciones:** La serie es opcional en el alta inicial para todas las categorías (RN-111); la política futura puede exigir verificación o formato para una categoría, sin bloquear ese alta por ausencia del dato.
- **Operaciones prohibidas:** Exigir serie para el alta inicial o inventarla a partir del código patrimonial.
- **Consecuencias históricas:** Una corrección de serie conserva valor anterior, valor nuevo, actor y motivo.
- **Validaciones de servidor:** Si existe serie, normalizar y comprobar duplicados según política; si no existe, registrar estado del dato sin fabricar valor.
- **Casos de prueba:** Éxito: una laptop sin serie se da de alta con sus tres mínimos. Rechazo: enviar una serie duplicada que la política considera única falla.

### 4.1 Características técnicas por categoría

La identidad general permanece en `Equipo` y sus catálogos de categoría, marca y modelo. **Diseño de persistencia propuesto, aún no implementado:** una relación uno a uno con `EspecificaciónComputadora` para computadoras y laptops; `ConfiguraciónTécnica` por equipo y versión para reconstruir la configuración de una fecha; `MóduloRam` y `UnidadAlmacenamiento` como elementos repetibles dentro de cada versión, con identidad de componente cuando exista; `CambioComponente` como hecho inmutable que enlaza versión previa y posterior, mantenimiento opcional y línea de gasto opcional. En cada cambio se conservarán los componentes anteriores sin modificarlos. El conjunto de componentes de una versión describe una **configuración completa**, no diferencias acumuladas; una sola versión puede ser vigente por equipo. No se sumará dos veces el costo de un reemplazo: la única fuente financiera sigue siendo la línea de gasto de RN-052/053.

Para otras categorías se proponen `DefiniciónAtributoCategoría` (clave estable, etiqueta, tipo, unidad, opciones, vigencia y obligatoriedad aprobada) y `ValorAtributoTécnico` por versión, con valor tipado numérico, texto breve, booleano o una opción de catálogo, y exactamente un tipo activo. Los atributos importantes de uso frecuente tendrán controles estructurados, no solo observaciones ni un objeto libre sin esquema. Las definiciones cambiarán con versiones y los valores históricos conservarán su significado original. **No se creará una tabla general con cientos de columnas opcionales.** Estos nombres describen un modelo propuesto; ninguna entidad ni tabla existe todavía.

### RN-100 - Ficha técnica por categoría

- **Estado:** Confirmada por decisión.
- **Descripción:** Cada equipo conserva identidad general y características técnicas estructuradas según su categoría.
- **Condiciones:** La categoría determina secciones disponibles; marca y modelo se registran cuando correspondan.
- **Operaciones prohibidas:** Reducir características importantes a un único campo libre o agregar cientos de columnas opcionales al equipo.
- **Consecuencias históricas:** Los cambios de categoría o especificación no borran configuraciones anteriores.
- **Validaciones de servidor:** Verificar categoría, pertenencia del modelo a su marca y esquema técnico correspondiente.
- **Casos de prueba:** Éxito: una laptop admite procesador, memoria y almacenamiento estructurados. Rechazo: enviar atributos de cámara como ficha de laptop sin esquema compatible falla.

### RN-101 - Procesador de computadora o laptop

- **Estado:** Confirmada por decisión.
- **Descripción:** El procesador admite fabricante, modelo y denominación por separado.
- **Condiciones:** Los datos desconocidos pueden completarse más tarde sin inventar valores.
- **Operaciones prohibidas:** Obligar a registrar `Desconocido` como si fuera un fabricante real o usar solo observaciones para un procesador conocido.
- **Consecuencias históricas:** Un cambio de procesador conserva configuración previa, nueva y hecho de sustitución.
- **Validaciones de servidor:** Limitar longitud, normalizar texto y preservar campos faltantes como desconocidos, no como cadenas ficticias.
- **Casos de prueba:** Éxito: completar modelo después de registrar fabricante mantiene trazabilidad. Rechazo: una cadena vacía no se guarda como fabricante conocido.

### RN-102 - Memoria RAM total y módulos

- **Estado:** Confirmada por decisión.
- **Descripción:** La ficha registra capacidad total en GB y módulos individuales cuando se conozcan.
- **Condiciones:** La ausencia de detalle de módulos no invalida un total conocido; si ambos están completos deben ser coherentes, salvo ajuste documentado.
- **Operaciones prohibidas:** Perder el total anterior al ampliar RAM o inferir módulos inexistentes a partir del total.
- **Consecuencias históricas:** Cada ampliación registra módulos anteriores y posteriores y fecha efectiva.
- **Validaciones de servidor:** Capacidades positivas, suma coherente de módulos cuando el inventario sea completo y relación del cambio con el equipo.
- **Casos de prueba:** Éxito: registrar 16 GB sin conocer módulos y añadirlos después. Rechazo: declarar módulos completos de 8+8 GB y total de 32 GB sin corrección justificada falla.

### RN-103 - Múltiples unidades de almacenamiento

- **Estado:** Confirmada por decisión.
- **Descripción:** Una computadora puede tener varias unidades, cada una con tecnología de disco (por ejemplo SSD o HDD), interfaz (por ejemplo NVMe, SATA o USB cuando se conozca) y capacidad.
- **Condiciones:** Capacidad en GB o TB se normaliza a una unidad canónica para comparación, conservando unidad de presentación; tecnología e interfaz desconocidas permanecen sin inventar.
- **Operaciones prohibidas:** Sobrescribir una unidad anterior al añadir o reemplazar otra, o confundir cantidad de unidades con capacidad total.
- **Consecuencias históricas:** Altas, bajas y reemplazos de unidades preservan versión anterior, versión resultante y referencia del componente cuando se conozca.
- **Validaciones de servidor:** Tecnología e interfaz válidas si se informan, capacidad positiva, unidad válida y ausencia de duplicación de la misma unidad vigente.
- **Casos de prueba:** Éxito: registrar un SSD con interfaz NVMe y otro HDD en el mismo equipo. Rechazo: capacidad negativa o dos entradas para la misma unidad vigente fallan.

### RN-104 - Sistema operativo y observaciones

- **Estado:** Confirmada por decisión.
- **Descripción:** El sistema operativo se registra cuando aplique; las observaciones complementan, pero no sustituyen, los datos estructurados.
- **Condiciones:** Pueden permanecer vacíos si no corresponden o aún no se conocen.
- **Operaciones prohibidas:** Exigir sistema operativo a periféricos o ocultar capacidad de disco exclusivamente en texto libre.
- **Consecuencias históricas:** Una actualización relevante queda asociada a nueva versión técnica.
- **Validaciones de servidor:** Compatibilidad con categoría y longitudes máximas de los campos.
- **Casos de prueba:** Éxito: completar sistema operativo de laptop después del alta. Rechazo: presentar texto libre como capacidad numérica válida falla.

### RN-105 - Atributos especializados de otras categorías

- **Estado:** Confirmada por decisión.
- **Descripción:** Monitores, cámaras, discos externos, periféricos y equipos de grabación admiten características específicas de su categoría.
- **Condiciones:** Un atributo configurado tiene etiqueta, tipo y unidad u opciones cuando apliquen; las listas exactas requieren definición empresarial.
- **Operaciones prohibidas:** Aplicar una definición de otra categoría o almacenar un valor numérico importante como texto no validado.
- **Consecuencias históricas:** Definición y valor usados en cada versión permanecen interpretables aunque se amplíe el catálogo.
- **Validaciones de servidor:** Comprobar categoría, tipo, unidad, rango y pertenencia de opciones según la versión de definición.
- **Casos de prueba:** Éxito: registrar resolución de un monitor mediante atributo tipado aprobado. Rechazo: enviar texto para un atributo definido como número falla.

### RN-106 - Configuración actual e histórica

- **Estado:** Confirmada por decisión.
- **Descripción:** La configuración actual se distingue de todas las versiones técnicas históricas del mismo equipo.
- **Condiciones:** Cada nueva configuración conserva los valores anteriores y fecha, actor y motivo del cambio.
- **Operaciones prohibidas:** Actualizar componentes de una versión cerrada o mantener dos versiones vigentes incompatibles.
- **Consecuencias históricas:** Es posible consultar la configuración de antes y después de una ampliación o reemplazo.
- **Validaciones de servidor:** Crear versión nueva y cambiar la vigente de manera atómica con control de concurrencia.
- **Casos de prueba:** Éxito: ampliar RAM deja una configuración nueva y permite consultar la previa. Rechazo: editar directamente la versión histórica falla.

### RN-107 - Relación de sustituciones con servicio y costo

- **Estado:** Confirmada por decisión.
- **Descripción:** Un cambio de componente se vincula al mantenimiento y al gasto cuando esos registros existan.
- **Condiciones:** Mantenimiento y gasto son opcionales: puede haber reemplazo sin gasto y mantenimiento sin reemplazo.
- **Operaciones prohibidas:** Crear gastos ficticios para documentar el cambio o contabilizar una misma pieza dos veces.
- **Consecuencias históricas:** Se conservan pieza retirada, instalada, motivo, mantenimiento y gasto real enlazados.
- **Validaciones de servidor:** Comprobar que equipo, mantenimiento, gasto y cambio coinciden y que una línea financiera no se reutilice para sumar dos veces.
- **Casos de prueba:** Éxito: reemplazo de SSD durante mantenimiento gratuito conserva el cambio sin costo. Rechazo: enlazar al cambio un gasto de otro equipo falla.

### RN-108 - Ficha incompleta y ampliación progresiva

- **Estado:** Confirmada por decisión.
- **Descripción:** Se puede registrar información técnica parcial y completarla posteriormente sin inventar datos.
- **Condiciones:** El expediente distingue dato no registrado, no aplicable y pendiente de verificar; los únicos mínimos iniciales son los de RN-111, con especificaciones opcionales por categoría.
- **Operaciones prohibidas:** Rechazar automáticamente todo equipo incompleto o convertir datos faltantes en ceros y valores ficticios.
- **Consecuencias históricas:** Completar información técnica genera una versión rastreable sin reescribir la anterior.
- **Validaciones de servidor:** Validar tipos de los datos presentes; aplicar solo mínimos expresamente aprobados a la activación.
- **Casos de prueba:** Éxito: registrar laptop sin conocer módulos de RAM y completar esa información luego. Rechazo: un total RAM informado como número negativo falla.

### RN-109 - Formulario por categoría

- **Estado:** Confirmada por decisión.
- **Descripción:** El formulario muestra campos específicos al seleccionar categoría, con listas repetibles de RAM y discos donde corresponde.
- **Condiciones:** Usa selectores, números, unidades explícitas y secciones responsivas; cambiar categoría no debe descartar información ingresada sin advertencia.
- **Operaciones prohibidas:** Guardar valores ocultos de otra categoría o simular persistencia cuando aún no existe.
- **Consecuencias históricas:** Una futura edición mostrará la configuración vigente y generará una versión al confirmar cambios.
- **Validaciones de servidor:** El esquema de la categoría se valida nuevamente en servidor, sin confiar en la visibilidad de Razor.
- **Casos de prueba:** Éxito: seleccionar laptop muestra RAM y varias unidades de disco. Rechazo: enviar por un cliente manipulado una capacidad de categoría incompatible falla.

### RN-110 - Conservación de códigos heredados

- **Estado:** Confirmada por decisión.
- **Descripción:** Los códigos existentes del Excel, como `EQ_1`, se conservan como identificadores patrimoniales válidos y distintos del número de serie.
- **Condiciones:** Deben cumplir unicidad y quedar asociados permanentemente al mismo expediente.
- **Operaciones prohibidas:** Sustituir automáticamente un código heredado por el formato propuesto `CPR-000001` o usarlo para otro equipo.
- **Consecuencias históricas:** El código original sigue identificando asignaciones, movimientos y evidencias; las correcciones excepcionales mantienen trazabilidad.
- **Validaciones de servidor:** Normalización y unicidad global compatibles con formatos anteriores y futuros, sin exigir un prefijo específico.
- **Casos de prueba:** Éxito: dar de alta `EQ_1` conserva su escritura original. Rechazo: reutilizar `EQ_1` para otro equipo falla.

### RN-111 - Mínimos de alta inicial

- **Estado:** Confirmada por decisión.
- **Descripción:** Solo código único, categoría y nombre descriptivo son obligatorios para el alta inicial, incluso en computadoras y laptops.
- **Condiciones:** Los tres datos deben ser válidos; los restantes son opcionales y se completan posteriormente según corresponda.
- **Operaciones prohibidas:** Exigir responsable, ubicación, foto, serie, RAM, disco, estado físico u operativo para crear el expediente inicial.
- **Consecuencias históricas:** La incorporación parcial conserva la fecha real del alta; completar datos genera hechos posteriores sin modificar artificialmente el momento inicial.
- **Validaciones de servidor:** Obligatoriedad y unicidad del código; existencia de categoría y nombre no vacío; validar tipos de cualquier dato opcional informado.
- **Casos de prueba:** Éxito: crear expediente con `EQ_1`, categoría laptop y nombre, sin más datos. Rechazo: crear sin nombre o con código repetido falla.

### RN-112 - Estados de disponibilidad de datos

- **Estado:** Confirmada por decisión.
- **Descripción:** Cada dato opcional puede constar como informado, no registrado, no aplicable o pendiente de verificar.
- **Condiciones:** «No registrado» expresa ausencia de captura, «No aplica» una decisión contextual explícita y «Pendiente» una comprobación por realizar; un valor informado no debe contradecir «No aplica».
- **Operaciones prohibidas:** Sustituir ausencias por cero, texto inventado o fechas estimadas; equiparar automáticamente vacío con no aplicable o verificado.
- **Consecuencias históricas:** Cambios de valor o clasificación conservan estado anterior, actor y momento, cuando se habilite persistencia.
- **Validaciones de servidor:** Compatibilidad entre estado y valor, pertinencia por categoría y permisos para confirmar «No aplica» cuando corresponda.
- **Casos de prueba:** Éxito: una serie vacía queda «No registrada» y luego puede marcarse «Pendiente». Rechazo: serie con valor no puede quedar simultáneamente «No aplica».

### RN-113 - Última verificación del equipo

- **Estado:** Confirmada por decisión.
- **Descripción:** Se registra fecha de última verificación y persona que realizó la revisión, sin generar fechas o responsables automáticamente a partir del Excel.
- **Condiciones:** Un evento confirmado de verificación reúne fecha real, persona identificable y equipo; la fecha y la persona se consultan como par de un historial, no como texto independiente sin origen.
- **Operaciones prohibidas:** Marcar un equipo como verificado con solo una fecha o solo un nombre, o sustituir verificaciones anteriores.
- **Consecuencias históricas:** Cada revisión conserva su autor y fecha; «última verificación» se deriva del evento más reciente confirmado.
- **Validaciones de servidor:** Identidad de la persona, fecha válida, equipo existente y comprobación de concurrencia al agregar el evento.
- **Casos de prueba:** Éxito: una revisión completa pasa a ser la última, conservando la previa. Rechazo: confirmar verificación sin persona falla y deja el dato pendiente.

### RN-114 - Custodia, ubicación y condiciones independientes

- **Estado:** Confirmada por decisión.
- **Descripción:** Responsable vigente, ubicación física, condición física y condición operativa pertenecen a dimensiones separadas.
- **Condiciones:** Responsable procede de la custodia; ubicación de movimientos o constatación; condiciones de inspecciones. Un cambio en una dimensión no implica cambios automáticos en las otras.
- **Operaciones prohibidas:** Interpretar «Dañado» como nombre de responsable, deducir condición operativa de condición física o reescribir la historia de custodia al cambiar la ubicación.
- **Consecuencias históricas:** Cada dimensión conserva sus eventos y momentos de captura independientes.
- **Validaciones de servidor:** Destino y tipo de dato correcto, relación con equipo, cronología y rechazo o revisión de valores incompatibles en su campo.
- **Casos de prueba:** Éxito: equipo sin responsable conserva ubicación y condición conocidas. Rechazo: importar «Dañado» como persona responsable sin revisión se bloquea.

### RN-115 - Pendientes en el expediente

- **Estado:** Confirmada por decisión.
- **Descripción:** El expediente muestra los datos marcados expresamente como pendientes de verificar, distinguiéndolos de datos simplemente no registrados.
- **Condiciones:** La lista se deriva del estado explícito de cada dato; un valor completado o verificado deja de figurar como pendiente conforme a su evento.
- **Operaciones prohibidas:** Presentar todos los campos vacíos como incidencias o ocultar una verificación pendiente después del alta.
- **Consecuencias históricas:** Resolver un pendiente conserva qué dato se comprobó, por quién y cuándo.
- **Validaciones de servidor:** Consultar estados vigentes y no aceptar cierres de pendientes sin el dato o la confirmación correspondiente.
- **Casos de prueba:** Éxito: serie marcada «Pendiente» aparece en el expediente. Rechazo: serie solo «No registrada» no aparece como tarea pendiente.

### RN-116 - Observaciones y fotografías históricas

- **Estado:** Confirmada por decisión.
- **Descripción:** Las observaciones originales del Excel se conservan como fuente y nuevas notas y fotografías se añaden al expediente sin reemplazar las anteriores.
- **Condiciones:** Si no hay observación o imagen disponible, permanece no registrada; no se fabrica una evidencia para satisfacer el alta.
- **Operaciones prohibidas:** Sobrescribir el texto original al añadir una nota, exigir fotografía inicial para el alta o reemplazar imágenes históricas.
- **Consecuencias históricas:** Fuente original, notas posteriores y evidencias quedan fechadas y relacionadas con su contexto.
- **Validaciones de servidor:** Validar procedencia, relación con equipo, acceso autorizado y versionado según RN-060/061.
- **Casos de prueba:** Éxito: añadir nota y fotografía deja intacta observación original. Rechazo: actualizar una evidencia antigua con bytes nuevos falla.

## 5. Reglas de usuarios y responsables

### RN-010 - Responsable individual y área

- **Estado:** Confirmada por decisión.
- **Descripción:** Una asignación individual identifica a la persona responsable y su área de contexto.
- **Condiciones:** La persona debe existir y ser elegible en la fecha efectiva.
- **Operaciones prohibidas:** Asignar a una referencia inexistente o sin contexto organizacional válido.
- **Consecuencias históricas:** Persona y área quedan registradas en la asignación aunque luego cambien.
- **Validaciones de servidor:** Verificar elegibilidad, vigencia y pertenencia organizacional.
- **Casos de prueba:** Éxito: asignar a personal activo registra persona y área. Rechazo: asignar a una persona inexistente falla.

### RN-011 - Fotografía histórica del área

- **Estado:** Propuesta.
- **Descripción:** El área registrada en una asignación representa el contexto en la fecha de entrega.
- **Condiciones:** Se obtiene del vínculo organizacional vigente al iniciar la asignación.
- **Operaciones prohibidas:** Reescribir asignaciones anteriores cuando la persona cambia de área.
- **Consecuencias históricas:** Los reportes históricos conservan el área original.
- **Validaciones de servidor:** Copiar la referencia y denominación histórica dentro de la transacción de asignación.
- **Casos de prueba:** Éxito: cambiar de área no altera una asignación cerrada. Rechazo: una actualización masiva que la reescriba es bloqueada.

### RN-012 - Equipo compartido en oficina

- **Estado:** Confirmada por decisión.
- **Descripción:** Un equipo compartido se asigna a una oficina y a una persona custodio administrativa.
- **Condiciones:** La oficina y el custodio deben estar activos; los usuarios pueden pertenecer a distintas áreas.
- **Operaciones prohibidas:** Abrir custodia compartida sin oficina o sin custodio.
- **Consecuencias históricas:** Se conservan oficina, custodio y cambios posteriores de custodia.
- **Validaciones de servidor:** Verificar vigencias y que no exista otra custodia activa del equipo.
- **Casos de prueba:** Éxito: asignar un equipo a oficina y custodio activos. Rechazo: omitir custodio impide la asignación.

### RN-013 - Elegibilidad de responsables

- **Estado:** Propuesta.
- **Descripción:** Solo personas activas y habilitadas pueden recibir nuevas responsabilidades.
- **Condiciones:** La persona debe estar vigente en la fecha efectiva y no tener un bloqueo aplicable.
- **Operaciones prohibidas:** Asignar a personal inactivo, desvinculado o bloqueado.
- **Consecuencias históricas:** La inactivación posterior no borra responsabilidades anteriores.
- **Validaciones de servidor:** Consultar estado vigente al confirmar la operación, no solo al abrir el formulario.
- **Casos de prueba:** Éxito: una persona activa recibe el equipo. Rechazo: una persona desactivada durante la edición no lo recibe.

### RN-014 - Desactivación con custodias pendientes

- **Estado:** Propuesta.
- **Descripción:** Una persona no se desactiva definitivamente mientras mantenga custodias activas.
- **Condiciones:** Sus equipos deben devolverse o transferirse mediante operaciones explícitas.
- **Operaciones prohibidas:** Desactivar y dejar asignaciones activas huérfanas.
- **Consecuencias históricas:** Devoluciones o transferencias conservan la secuencia de cierre.
- **Validaciones de servidor:** Buscar custodias personales y custodias administrativas activas antes de desactivar.
- **Casos de prueba:** Éxito: desactivar después de resolver custodias. Rechazo: desactivar con un equipo asignado es impedido.

### RN-015 - Cambio de área de una persona

- **Estado:** Propuesta.
- **Descripción:** El cambio de área afecta el contexto vigente, no los registros históricos.
- **Condiciones:** Las custodias actuales pueden requerir revisión administrativa sin reescribir su inicio.
- **Operaciones prohibidas:** Modificar retrospectivamente el área de entregas o devoluciones.
- **Consecuencias históricas:** Se conserva el área inicial y, si se decide, un evento de cambio organizacional.
- **Validaciones de servidor:** Separar datos vigentes de instantáneas históricas.
- **Casos de prueba:** Éxito: el perfil muestra nueva área y el historial conserva la anterior. Rechazo: editar el perfil no actualiza eventos cerrados.

### RN-016 - Cambio de custodio compartido

- **Estado:** Propuesta.
- **Descripción:** Cambiar el custodio de una oficina es una transferencia de responsabilidad, no una edición simple.
- **Condiciones:** Debe existir custodio saliente, entrante elegible, fecha y motivo.
- **Operaciones prohibidas:** Reemplazar el nombre del custodio sin cerrar su periodo.
- **Consecuencias históricas:** Se cierra el periodo anterior y se abre el nuevo de forma atómica.
- **Validaciones de servidor:** Comprobar custodia vigente, elegibilidad y ausencia de solapamientos.
- **Casos de prueba:** Éxito: transferir conserva ambos periodos. Rechazo: editar directamente el custodio activo falla.

### RN-018 - Equipo principal y accesorios

- **Estado:** Propuesta.
- **Descripción:** Un equipo puede relacionarse con accesorios inventariables sin perder la identidad ni el expediente individual de cada elemento.
- **Condiciones:** La relación debe indicar principal, accesorio, vigencia y tipo; un accesorio solo puede tener un principal vigente cuando la categoría así lo requiera.
- **Operaciones prohibidas:** Crear relaciones cíclicas, convertir la relación en propiedad irreversible o borrar el historial al separar un accesorio.
- **Consecuencias históricas:** Altas, separaciones y cambios de principal conservan sus periodos y actores.
- **Validaciones de servidor:** Verificar existencia, categorías compatibles, ausencia de ciclos y vigencias no solapadas.
- **Casos de prueba:** Éxito: relacionar un cargador inventariado con una laptop conserva ambos expedientes. Rechazo: relacionar la laptop como accesorio de su propio cargador creando un ciclo falla.

### RN-019 - Kits de equipos

- **Estado:** Propuesta.
- **Descripción:** Un kit agrupa varios equipos para operación conjunta, por ejemplo un kit de grabación, sin fusionar sus expedientes.
- **Condiciones:** Cada integrante conserva código, estado, custodia y trazabilidad; la composición del kit tiene vigencia histórica.
- **Operaciones prohibidas:** Incluir dos veces el mismo equipo en un kit vigente o usar el kit para ocultar estados incompatibles de sus integrantes.
- **Consecuencias históricas:** Incorporaciones y retiros del kit no eliminan composiciones anteriores ni eventos individuales.
- **Validaciones de servidor:** Verificar integrantes únicos, existentes, compatibles y no incluidos en composiciones excluyentes.
- **Casos de prueba:** Éxito: agrupar cámara, lente y grabadora mantiene tres expedientes. Rechazo: agregar dos veces la misma cámara al kit falla.

## 6. Reglas de asignaciones y transferencias

### RN-020 - Una sola custodia activa

- **Estado:** Confirmada por requisitos.
- **Descripción:** Un equipo solo puede tener una custodia activa, personal o compartida.
- **Condiciones:** La custodia anterior debe estar cerrada o cerrarse atómicamente en una transferencia.
- **Operaciones prohibidas:** Periodos activos simultáneos para el mismo equipo.
- **Consecuencias históricas:** Todas las custodias cerradas permanecen en orden cronológico.
- **Validaciones de servidor:** Restricción de unicidad condicional y comprobación transaccional.
- **Casos de prueba:** Éxito: asignar un equipo sin custodia. Rechazo: una segunda asignación paralela no se crea.

### RN-021 - Elegibilidad del equipo para asignación

- **Estado:** Propuesta.
- **Descripción:** Solo un equipo activo, asignable y sin bloqueos puede iniciar custodia.
- **Condiciones:** No debe estar dado de baja, en salida incompatible ni en mantenimiento bloqueante.
- **Operaciones prohibidas:** Asignar un equipo no operativo sin excepción definida o con proceso incompatible.
- **Consecuencias históricas:** Los rechazos no alteran el expediente; los intentos sensibles pueden auditarse.
- **Validaciones de servidor:** Evaluar ciclo de vida, condiciones y procesos abiertos al confirmar.
- **Casos de prueba:** Éxito: un equipo activo y disponible se asigna. Rechazo: un equipo dado de baja no se asigna.

### RN-022 - Transferencia atómica

- **Estado:** Propuesta.
- **Descripción:** Una transferencia cierra la custodia actual y abre la siguiente en una sola transacción.
- **Condiciones:** Deben existir equipo, origen vigente, destino elegible, fecha, condiciones y motivo.
- **Operaciones prohibidas:** Cerrar el origen sin crear el destino o crear el destino sin cerrar el origen.
- **Consecuencias históricas:** Ambos periodos quedan enlazados por la misma operación.
- **Validaciones de servidor:** Control de concurrencia y transacción única con reversión total ante error.
- **Casos de prueba:** Éxito: la transferencia deja exactamente una custodia activa. Rechazo: si el destino falla, el origen permanece activo.

### RN-023 - Conservación del historial de asignaciones

- **Estado:** Confirmada por requisitos.
- **Descripción:** Una nueva asignación nunca reemplaza asignaciones anteriores.
- **Condiciones:** Toda asignación tiene inicio y, al finalizar, cierre explícito.
- **Operaciones prohibidas:** Reutilizar un registro anterior cambiando responsable y fechas.
- **Consecuencias históricas:** Puede responderse quién usó el equipo y durante qué periodo.
- **Validaciones de servidor:** Crear nuevos periodos y restringir cambios sobre periodos cerrados.
- **Casos de prueba:** Éxito: reasignar produce dos periodos. Rechazo: modificar el responsable de un periodo cerrado es impedido.

### RN-024 - Prohibición de solapamientos

- **Estado:** Propuesta.
- **Descripción:** Los periodos de custodia de un equipo no pueden solaparse.
- **Condiciones:** El inicio nuevo debe ser posterior o igual al cierre válido anterior, según precisión temporal aprobada.
- **Operaciones prohibidas:** Fechas retroactivas que creen dos responsables simultáneos.
- **Consecuencias históricas:** Una corrección cronológica conserva auditoría y no rompe la secuencia.
- **Validaciones de servidor:** Consultar intervalos existentes dentro de la transacción.
- **Casos de prueba:** Éxito: iniciar después de la devolución es válido. Rechazo: iniciar dentro de un periodo cerrado se rechaza.

### RN-025 - Orden cronológico

- **Estado:** Propuesta.
- **Descripción:** Las fechas efectivas deben respetar la secuencia causal del expediente.
- **Condiciones:** Entrega precede devolución; salida precede retorno; apertura precede cierre.
- **Operaciones prohibidas:** Cierres anteriores a aperturas o eventos posteriores a una baja sin reactivación.
- **Consecuencias históricas:** Las correcciones registran motivo, actor y valores originales.
- **Validaciones de servidor:** Comparar contra eventos relacionados y usar una zona temporal uniforme.
- **Casos de prueba:** Éxito: devolución posterior a entrega se registra. Rechazo: devolución anterior a entrega falla.

### RN-026 - Datos mínimos de transferencia

- **Estado:** Propuesta.
- **Descripción:** Toda transferencia registra origen, destino, fecha efectiva, actor, motivo y condiciones.
- **Condiciones:** Los participantes deben corresponder con la custodia activa y el destino solicitado.
- **Operaciones prohibidas:** Transferencias anónimas, sin motivo o hacia el mismo responsable sin justificación.
- **Consecuencias históricas:** El evento enlaza entrega del origen y recepción del destino.
- **Validaciones de servidor:** Obligatoriedad, elegibilidad y coincidencia del origen con el estado persistido.
- **Casos de prueba:** Éxito: una transferencia completa se confirma. Rechazo: un origen distinto al vigente provoca conflicto.

### RN-027 - Bloqueos de transferencia

- **Estado:** Propuesta.
- **Descripción:** No se transfiere un equipo dado de baja o con una salida temporal incompatible abierta.
- **Condiciones:** Los procesos bloqueantes deben resolverse antes de transferir.
- **Operaciones prohibidas:** Usar una transferencia para eludir retorno, cierre de mantenimiento o reactivación.
- **Consecuencias históricas:** Resolver el bloqueo genera su propio evento antes de la transferencia.
- **Validaciones de servidor:** Consultar procesos abiertos y ciclo de vida en la misma transacción.
- **Casos de prueba:** Éxito: transferir después del retorno es válido. Rechazo: transferir durante una salida abierta falla.

### RN-028 - Ubicación habitual y ubicación actual

- **Estado:** Propuesta.
- **Descripción:** Cada equipo distingue su ubicación habitual de su ubicación física actual.
- **Condiciones:** La ubicación habitual representa su base administrativa; la actual deriva del último movimiento confirmado o inspección de ubicación.
- **Operaciones prohibidas:** Sobrescribir la ubicación habitual por una salida temporal o presentar como actual una ubicación no respaldada.
- **Consecuencias históricas:** Los cambios de ambas ubicaciones conservan fecha, actor, motivo y origen del dato.
- **Validaciones de servidor:** Verificar ubicaciones vigentes y actualizar cada dimensión solo mediante su operación correspondiente.
- **Casos de prueba:** Éxito: una salida cambia la ubicación actual y mantiene la habitual. Rechazo: editar la habitual como efecto automático de una salida falla.

### RN-029 - Actualización de ubicación por movimientos

- **Estado:** Propuesta.
- **Descripción:** Entregas, transferencias, salidas, retornos y servicios externos actualizan la ubicación física actual de forma trazable.
- **Condiciones:** El cambio de ubicación debe ser parte atómica del movimiento que lo origina.
- **Operaciones prohibidas:** Cerrar un movimiento dejando una ubicación actual contradictoria o editarla sin motivo fuera de un ajuste autorizado.
- **Consecuencias históricas:** Cada ubicación puede reconstruirse cronológicamente desde los movimientos.
- **Validaciones de servidor:** Comprobar origen esperado, destino válido, versión de concurrencia y actualización atómica.
- **Casos de prueba:** Éxito: retornar un equipo actualiza su ubicación a la oficina receptora. Rechazo: retorno concurrente desde una ubicación ya cambiada produce conflicto.

## 7. Reglas de entregas y devoluciones

### RN-030 - Condiciones de entrega

- **Estado:** Confirmada por requisitos.
- **Descripción:** La entrega registra las condiciones física y operativa del equipo.
- **Condiciones:** Debe identificar equipo, receptor, fecha y valores válidos del catálogo.
- **Operaciones prohibidas:** Entregar sin documentar ambas condiciones.
- **Consecuencias históricas:** Las condiciones de entrega no cambian cuando se registra la devolución.
- **Validaciones de servidor:** Obligatoriedad, catálogos vigentes y coherencia con la asignación creada.
- **Casos de prueba:** Éxito: entregar con ambas condiciones abre custodia. Rechazo: omitir condición operativa revierte la operación.

### RN-031 - Aceptación y evidencia de entrega

- **Estado:** Propuesta.
- **Descripción:** La entrega puede exigir aceptación del receptor y evidencia fotográfica según política aprobada.
- **Condiciones:** Debe definirse modalidad de aceptación y mínimo de evidencias por categoría.
- **Operaciones prohibidas:** Marcar aceptada una entrega sin la prueba requerida.
- **Consecuencias históricas:** Aceptación y evidencias quedan vinculadas al evento y no se sustituyen.
- **Validaciones de servidor:** Verificar requisitos configurados antes de confirmar.
- **Casos de prueba:** Éxito: confirmar con aceptación y evidencias requeridas. Rechazo: confirmar sin una evidencia obligatoria falla.

### RN-032 - Cierre por devolución

- **Estado:** Confirmada por requisitos.
- **Descripción:** La devolución cierra la asignación activa correspondiente.
- **Condiciones:** Debe existir exactamente una custodia compatible y vigente.
- **Operaciones prohibidas:** Devolver dos veces o cerrar una custodia distinta.
- **Consecuencias históricas:** Se conserva el periodo completo y el equipo queda en la custodia resultante aprobada.
- **Validaciones de servidor:** Comprobar versión, custodia activa y cierre atómico.
- **Casos de prueba:** Éxito: devolver cierra la única custodia. Rechazo: repetir la devolución no crea otro cierre.

### RN-033 - Datos de devolución

- **Estado:** Confirmada por requisitos.
- **Descripción:** La devolución registra fecha, condiciones, observaciones y evidencias aplicables.
- **Condiciones:** Las condiciones deben describir el estado real al recibir el equipo.
- **Operaciones prohibidas:** Copiar automáticamente la condición de entrega como si fuera una inspección.
- **Consecuencias históricas:** Entrega y devolución permanecen comparables.
- **Validaciones de servidor:** Obligatoriedad, cronología y requisitos de evidencia.
- **Casos de prueba:** Éxito: devolución completa registra diferencias. Rechazo: devolución sin condición física falla.

### RN-034 - Conservación de daños documentados

- **Estado:** Confirmada por requisitos.
- **Descripción:** Un daño detectado al devolver no altera la condición registrada al entregar.
- **Condiciones:** La nueva inspección se registra como evento independiente.
- **Operaciones prohibidas:** Editar retroactivamente la entrega para ocultar una diferencia.
- **Consecuencias históricas:** Se conserva la comparación y toda evidencia de daño.
- **Validaciones de servidor:** Inmutabilidad de eventos cerrados y creación de nueva inspección.
- **Casos de prueba:** Éxito: registrar daño mantiene intacta la entrega. Rechazo: cambiar la condición inicial es impedido.

### RN-035 - Incidencia por daño

- **Estado:** Propuesta.
- **Descripción:** Un daño o falla detectado puede originar una incidencia o mantenimiento correctivo trazable.
- **Condiciones:** Debe indicarse severidad, descripción y relación con la devolución.
- **Operaciones prohibidas:** Marcar operativo un equipo incompatible con el daño sin justificación autorizada.
- **Consecuencias históricas:** Incidencia, evidencia y mantenimiento quedan enlazados.
- **Validaciones de servidor:** Aplicar reglas de severidad y crear relaciones atómicamente.
- **Casos de prueba:** Éxito: daño severo crea mantenimiento relacionado. Rechazo: cerrar devolución dañada como disponible sin resolución falla.

### RN-036 - Devolución con custodia vigente

- **Estado:** Propuesta.
- **Descripción:** Solo puede devolverse un equipo que tenga la custodia activa indicada.
- **Condiciones:** El responsable u oficina debe coincidir con el estado persistido.
- **Operaciones prohibidas:** Devolver un equipo disponible o atribuir la devolución a otro responsable.
- **Consecuencias históricas:** Un conflicto no modifica historial; una corrección usa flujo auditado.
- **Validaciones de servidor:** Comparar identificadores y versión de concurrencia.
- **Casos de prueba:** Éxito: el custodio vigente devuelve el equipo. Rechazo: un custodio anterior no puede cerrarlo.

## 8. Reglas de mantenimientos

### RN-040 - Tipos de mantenimiento

- **Estado:** Confirmada por requisitos.
- **Descripción:** Todo mantenimiento se clasifica como preventivo o correctivo.
- **Condiciones:** El tipo se define al abrir y determina datos y flujo aplicables.
- **Operaciones prohibidas:** Abrir un mantenimiento sin tipo válido.
- **Consecuencias históricas:** El tipo original permanece en el historial; su corrección se audita.
- **Validaciones de servidor:** Catálogo cerrado y requisitos específicos por tipo.
- **Casos de prueba:** Éxito: abrir preventivo con tipo válido. Rechazo: un tipo desconocido no se guarda.

### RN-041 - Contenido del mantenimiento

- **Estado:** Confirmada por requisitos.
- **Descripción:** Se registran fallas, diagnóstico, servicio técnico y resultados aplicables.
- **Condiciones:** Correctivos requieren falla y diagnóstico; preventivos requieren trabajo planificado o realizado.
- **Operaciones prohibidas:** Cerrar sin información mínima del tipo correspondiente.
- **Consecuencias históricas:** Diagnósticos y trabajos permanecen asociados al evento.
- **Validaciones de servidor:** Campos mínimos por tipo y coherencia con el estado de cierre.
- **Casos de prueba:** Éxito: correctivo con falla, diagnóstico y trabajo se cierra. Rechazo: cerrarlo sin diagnóstico falla.

### RN-042 - Flujo del mantenimiento

- **Estado:** Propuesta.
- **Descripción:** El mantenimiento sigue estados permitidos de programación, ejecución, finalización o cancelación.
- **Condiciones:** Cada transición exige los datos definidos en la matriz.
- **Operaciones prohibidas:** Saltar directamente a completado sin apertura o regresar un completado a en curso.
- **Consecuencias históricas:** Cada transición registra actor, fecha y observación.
- **Validaciones de servidor:** Máquina de estados y control de concurrencia.
- **Casos de prueba:** Éxito: programado pasa a en curso. Rechazo: completado no regresa a programado.

### RN-043 - Inmutabilidad del mantenimiento cerrado

- **Estado:** Propuesta.
- **Descripción:** Un mantenimiento completado no se reabre ni se sobrescribe.
- **Condiciones:** Errores posteriores se corrigen mediante un ajuste relacionado.
- **Operaciones prohibidas:** Cambiar diagnóstico, costos o fechas cerradas sin flujo de corrección.
- **Consecuencias históricas:** El original y el ajuste permanecen visibles.
- **Validaciones de servidor:** Bloquear actualizaciones ordinarias cuando el estado sea completado.
- **Casos de prueba:** Éxito: añadir ajuste conserva el cierre. Rechazo: editar directamente el diagnóstico cerrado falla.

### RN-044 - Ajustes de mantenimiento

- **Estado:** Propuesta.
- **Descripción:** Las correcciones posteriores se documentan como ajustes auditados.
- **Condiciones:** Requieren motivo, autor, fecha y referencia al dato corregido.
- **Operaciones prohibidas:** Ajustes sin registro original o sin motivo.
- **Consecuencias históricas:** Las consultas muestran valor original, ajuste y valor efectivo.
- **Validaciones de servidor:** Verificar permisos, referencia y consistencia del ajuste.
- **Casos de prueba:** Éxito: corregir costo con ajuste autorizado. Rechazo: ajuste huérfano se rechaza.

### RN-045 - Mantenimientos simultáneos

- **Estado:** Propuesta.
- **Descripción:** Pueden coexistir mantenimientos compatibles; solo se bloquean los que produzcan conflictos operativos explícitos.
- **Condiciones:** La compatibilidad considera periodo, proveedor, intervención física y si ambos procesos intentan cerrar o cambiar la misma condición.
- **Operaciones prohibidas:** Bloquear de forma general todo segundo mantenimiento o permitir dos intervenciones que se contradigan sobre el mismo equipo.
- **Consecuencias históricas:** Cada mantenimiento conserva identidad propia y, si se relacionan, la referencia entre ambos.
- **Validaciones de servidor:** Evaluar conflictos concretos y concurrencia, sin aplicar una exclusión global por equipo.
- **Casos de prueba:** Éxito: diagnóstico y mantenimiento preventivo compatibles pueden coexistir. Rechazo: dos reparaciones que requieren posesión exclusiva durante el mismo periodo entran en conflicto.

### RN-046 - Condición posterior al mantenimiento

- **Estado:** Propuesta.
- **Descripción:** Completar un mantenimiento registra las condiciones física y operativa resultantes.
- **Condiciones:** Debe existir una inspección final y resultado del servicio.
- **Operaciones prohibidas:** Completar sin declarar si el equipo quedó operativo.
- **Consecuencias históricas:** Se conservan condiciones de entrada y salida.
- **Validaciones de servidor:** Exigir inspección final y validar compatibilidad con el resultado.
- **Casos de prueba:** Éxito: cierre registra equipo operativo. Rechazo: cierre sin condición resultante falla.

### RN-047 - Programación preventiva

- **Estado:** Propuesta.
- **Descripción:** Un preventivo puede tener fecha programada, vencimiento y recurrencia aprobada.
- **Condiciones:** La frecuencia debe corresponder a una política o registrarse manualmente.
- **Operaciones prohibidas:** Marcar realizado un preventivo solo por vencer su fecha.
- **Consecuencias históricas:** Reprogramaciones y cumplimiento quedan registrados.
- **Validaciones de servidor:** Cronología y cálculo de vencimiento sin modificar eventos completados.
- **Casos de prueba:** Éxito: reprogramar conserva fecha anterior. Rechazo: completar automáticamente sin servicio es impedido.

### RN-048 - Baja con mantenimiento abierto

- **Estado:** Propuesta.
- **Descripción:** Un equipo no puede completar su baja mientras tenga mantenimiento abierto.
- **Condiciones:** El mantenimiento debe completarse o cancelarse con justificación.
- **Operaciones prohibidas:** Aprobar baja dejando procesos técnicos inconclusos.
- **Consecuencias históricas:** El cierre o cancelación precede cronológicamente a la baja.
- **Validaciones de servidor:** Consultar mantenimientos abiertos al aprobar, no solo al solicitar.
- **Casos de prueba:** Éxito: dar de baja tras cancelar justificadamente. Rechazo: aprobar con reparación activa falla.

### RN-049 - Mantenimiento sin costos

- **Estado:** Confirmada por decisión.
- **Descripción:** Un mantenimiento puede registrarse y completarse sin repuestos, mano de obra facturada ni otros gastos.
- **Condiciones:** Debe cumplir los datos técnicos y de resultado, aunque su costo total sea cero.
- **Operaciones prohibidas:** Exigir una línea de gasto ficticia para cerrar un mantenimiento o interpretar ausencia de gastos como ausencia del servicio.
- **Consecuencias históricas:** El mantenimiento permanece en el historial con costo total calculado igual a cero.
- **Validaciones de servidor:** Separar la validez técnica del mantenimiento de la existencia de gastos asociados.
- **Casos de prueba:** Éxito: una limpieza interna realizada por TI se cierra sin costos. Rechazo: cerrar sin diagnóstico o resultado sigue fallando aunque no tenga gastos.

## 9. Reglas de repuestos y gastos

### RN-050 - Datos de repuestos

- **Estado:** Confirmada por requisitos.
- **Descripción:** Cada repuesto registra descripción, cantidad y costo.
- **Condiciones:** Debe vincularse al equipo y al mantenimiento o gasto que originó su uso.
- **Operaciones prohibidas:** Registrar cantidades sin unidad o costos sin contexto monetario aprobado.
- **Consecuencias históricas:** Los repuestos consumidos permanecen en el costo histórico del equipo.
- **Validaciones de servidor:** Obligatoriedad, cantidad válida, costo y relación existente.
- **Casos de prueba:** Éxito: registrar repuesto completo aumenta el detalle del mantenimiento. Rechazo: cantidad ausente no se guarda.

### RN-051 - Separación de mano de obra

- **Estado:** Confirmada por requisitos.
- **Descripción:** Los costos de mano de obra se registran separadamente de los repuestos.
- **Condiciones:** Cada concepto debe tener importe, descripción y operación relacionada.
- **Operaciones prohibidas:** Duplicar el mismo importe como repuesto y mano de obra.
- **Consecuencias históricas:** El total puede desglosarse por tipo de costo.
- **Validaciones de servidor:** Clasificación obligatoria y prevención de referencias duplicadas.
- **Casos de prueba:** Éxito: registrar ambos conceptos produce desglose correcto. Rechazo: reutilizar una misma línea como dos conceptos falla.

### RN-052 - Asociación única del gasto

- **Estado:** Confirmada por requisitos.
- **Descripción:** Cada gasto se contabiliza una sola vez para un equipo y operación.
- **Condiciones:** Una referencia documental o externa única no puede aplicarse dos veces al mismo alcance.
- **Operaciones prohibidas:** Duplicar comprobantes o sumar nuevamente una línea ya vinculada.
- **Consecuencias históricas:** Anulaciones y ajustes conservan relación con el gasto original.
- **Validaciones de servidor:** Claves de idempotencia, referencias únicas y comprobación transaccional.
- **Casos de prueba:** Éxito: un gasto nuevo se registra una vez. Rechazo: reenviar la misma operación no duplica el importe.

### RN-053 - Total histórico calculado

- **Estado:** Confirmada por requisitos.
- **Descripción:** El gasto histórico total se calcula desde líneas válidas sin duplicar importes.
- **Condiciones:** Deben incluirse repuestos, mano de obra y otros conceptos aprobados, considerando anulaciones.
- **Operaciones prohibidas:** Capturar manualmente un total independiente que contradiga el detalle.
- **Consecuencias históricas:** El total puede reconstruirse para cualquier fecha de corte.
- **Validaciones de servidor:** Agregación con precisión monetaria y exclusión de líneas anuladas o duplicadas.
- **Casos de prueba:** Éxito: dos líneas distintas se suman una vez. Rechazo: una línea anulada no integra el total vigente.

### RN-054 - Validez de cantidades e importes

- **Estado:** Propuesta.
- **Descripción:** Cantidades deben ser positivas e importes no negativos; las correcciones se realizan mediante anulación auditada.
- **Condiciones:** Escala decimal, unidad y moneda deben seguir la política financiera aprobada.
- **Operaciones prohibidas:** Cantidad cero, negativa o importes negativos usados para corregir o editar silenciosamente.
- **Consecuencias históricas:** La línea original permanece visible cuando se anula y su reemplazo, si existe, se registra por separado.
- **Validaciones de servidor:** Rangos, precisión, moneda y tipo de operación.
- **Casos de prueba:** Éxito: cantidad positiva y costo válido se aceptan. Rechazo: cantidad negativa ordinaria falla.

### RN-055 - Inmutabilidad de gastos contabilizados

- **Estado:** Propuesta.
- **Descripción:** Un gasto confirmado no se elimina ni sobrescribe.
- **Condiciones:** Las correcciones siguen un flujo de anulación auditada y, si corresponde, un nuevo registro correcto.
- **Operaciones prohibidas:** Editar monto, moneda o comprobante de una línea contabilizada.
- **Consecuencias históricas:** Se conserva la línea original y su estado posterior.
- **Validaciones de servidor:** Bloquear actualización ordinaria según estado financiero.
- **Casos de prueba:** Éxito: anular conserva el gasto original. Rechazo: editar directamente su importe falla.

### RN-056 - Anulación auditada de gastos

- **Estado:** Propuesta.
- **Descripción:** Un gasto incorrecto se corrige anulándolo de forma auditada y registrando uno nuevo cuando sea necesario.
- **Condiciones:** La anulación requiere motivo, permiso y referencia al gasto original; el gasto anulado deja de integrar totales vigentes.
- **Operaciones prohibidas:** Editar el importe confirmado, usar asientos compensatorios complejos o eliminar físicamente el gasto.
- **Consecuencias históricas:** Se conservan gasto original, anulación y eventual reemplazo, enlazados entre sí.
- **Validaciones de servidor:** Autorizar, impedir doble anulación, excluir el original de totales y relacionar el reemplazo cuando exista.
- **Casos de prueba:** Éxito: anular un gasto erróneo y registrar el correcto actualiza el total sin borrar historia. Rechazo: anular dos veces el mismo gasto falla.

### RN-057 - Comprobantes de gasto

- **Estado:** Propuesta.
- **Descripción:** Determinados gastos requieren comprobante según monto, categoría o política.
- **Condiciones:** Los umbrales y tipos documentales deben ser aprobados.
- **Operaciones prohibidas:** Confirmar un gasto sujeto a comprobante sin evidencia válida.
- **Consecuencias históricas:** El comprobante queda asociado y no se sustituye silenciosamente.
- **Validaciones de servidor:** Evaluar política vigente y existencia de archivo antes de confirmar.
- **Casos de prueba:** Éxito: gasto sobre umbral con comprobante se confirma. Rechazo: sin comprobante permanece pendiente.

### RN-058 - Política monetaria

- **Estado:** Propuesta.
- **Descripción:** Se propone BOB como moneda principal; monedas adicionales, impuestos, conversión y redondeo requieren política posterior.
- **Condiciones:** Hasta aprobar soporte multimoneda, todos los gastos de la primera versión se registrarían en BOB.
- **Operaciones prohibidas:** Sumar monedas diferentes sin conversión registrada.
- **Consecuencias históricas:** Se conservan moneda original, tipo de cambio aplicado y valor base.
- **Validaciones de servidor:** Código monetario, escala, fecha y fuente del tipo de cambio.
- **Casos de prueba:** Éxito: registrar un gasto en BOB conserva moneda e importe. Rechazo: registrar USD sin política multimoneda aprobada falla.

## 10. Reglas de fotografías y evidencias

### RN-060 - Tipos de evidencia fotográfica

- **Estado:** Confirmada por requisitos.
- **Descripción:** Se conservan fotografías iniciales, de entrega, devolución, daños, reparaciones y estado actual.
- **Condiciones:** Cada fotografía debe clasificarse y relacionarse con equipo y evento cuando corresponda.
- **Operaciones prohibidas:** Guardar evidencia histórica sin contexto identificable.
- **Consecuencias históricas:** La galería permite reconstruir visualmente la evolución del equipo.
- **Validaciones de servidor:** Tipo válido, equipo existente y relación de evento coherente.
- **Casos de prueba:** Éxito: foto de devolución queda vinculada a esa devolución. Rechazo: tipo inexistente no se acepta.

### RN-061 - Evidencias no sobrescribibles

- **Estado:** Confirmada por requisitos.
- **Descripción:** Ninguna evidencia histórica se sobrescribe.
- **Condiciones:** Una nueva versión se almacena como archivo y registro nuevos.
- **Operaciones prohibidas:** Reemplazar bytes o metadatos del archivo original conservando su identidad.
- **Consecuencias históricas:** Original y versión posterior permanecen diferenciados.
- **Validaciones de servidor:** Almacenamiento inmutable o versionado, identificador y huella de integridad.
- **Casos de prueba:** Éxito: corregir una foto agrega otra versión. Rechazo: subir contenido sobre el identificador anterior falla.

### RN-062 - Fotografía principal versionada

- **Estado:** Confirmada por requisitos.
- **Descripción:** Solo una fotografía es principal en el momento actual, pero las anteriores se conservan.
- **Condiciones:** La nueva principal debe pertenecer al mismo equipo y estar disponible.
- **Operaciones prohibidas:** Marcar simultáneamente dos principales o borrar la anterior al cambiarla.
- **Consecuencias históricas:** Se registra el periodo durante el cual cada foto fue principal.
- **Validaciones de servidor:** Cambio atómico y unicidad de principal vigente.
- **Casos de prueba:** Éxito: cambiar principal conserva la anterior en galería. Rechazo: seleccionar foto de otro equipo falla.

### RN-063 - Metadatos de evidencia

- **Estado:** Propuesta.
- **Descripción:** Cada evidencia registra tipo, fecha, autor, origen y operación relacionada.
- **Condiciones:** La fecha de captura puede diferir de la fecha de carga y ambas se conservan cuando estén disponibles.
- **Operaciones prohibidas:** Alterar autor o fecha de carga después de confirmar.
- **Consecuencias históricas:** Los metadatos permiten acreditar cuándo y por quién fue incorporada.
- **Validaciones de servidor:** Generar autor y carga en servidor; validar relaciones y fecha de captura.
- **Casos de prueba:** Éxito: evidencia válida recibe metadatos del servidor. Rechazo: el cliente no puede suplantar al autor.

### RN-064 - Seguridad y formato de archivos

- **Estado:** Propuesta.
- **Descripción:** Los archivos deben cumplir formatos, tamaño y controles de seguridad aprobados.
- **Condiciones:** Se valida contenido real, no solo extensión o tipo declarado por el navegador.
- **Operaciones prohibidas:** Ejecutables, contenido no permitido o archivos que excedan límites.
- **Consecuencias históricas:** Un rechazo registra resultado técnico sin incorporar el archivo al expediente.
- **Validaciones de servidor:** Firma del archivo, tamaño, nombre seguro y análisis antimalware si está disponible.
- **Casos de prueba:** Éxito: imagen válida dentro del límite se almacena. Rechazo: ejecutable renombrado como imagen falla.

### RN-065 - Retiro lógico de evidencia

- **Estado:** Propuesta.
- **Descripción:** Ocultar una evidencia errónea requiere motivo y no elimina el archivo histórico.
- **Condiciones:** Solo usuarios autorizados pueden marcarla anulada o restringida.
- **Operaciones prohibidas:** Borrado físico mediante la aplicación o retiro sin justificación.
- **Consecuencias históricas:** Se conservan evidencia, motivo, autor y fecha de retiro lógico.
- **Validaciones de servidor:** Permiso especial, motivo obligatorio y auditoría.
- **Casos de prueba:** Éxito: anular evidencia duplicada conserva su registro. Rechazo: usuario sin permiso no puede ocultarla.

### RN-066 - Integridad de evidencias

- **Estado:** Propuesta.
- **Descripción:** La aplicación debe detectar sustitución o alteración no autorizada de archivos.
- **Condiciones:** Cada archivo conserva una huella criptográfica y ubicación controlada.
- **Operaciones prohibidas:** Servir como válida una evidencia cuya huella no coincide.
- **Consecuencias históricas:** Una anomalía genera alerta y evento de auditoría.
- **Validaciones de servidor:** Calcular huella al almacenar y verificarla según política de acceso o auditoría.
- **Casos de prueba:** Éxito: archivo intacto valida su huella. Rechazo: archivo alterado se marca inconsistente.

### RN-067 - Acceso a evidencias

- **Estado:** Propuesta.
- **Descripción:** Fotografías y comprobantes solo son accesibles para usuarios autorizados.
- **Condiciones:** La autorización considera tipo de evidencia, operación y ámbito organizacional.
- **Operaciones prohibidas:** Exponer rutas públicas permanentes o confiar solo en ocultar botones.
- **Consecuencias históricas:** Accesos sensibles pueden auditarse según política.
- **Validaciones de servidor:** Autorizar cada descarga y usar referencias no predecibles o temporales.
- **Casos de prueba:** Éxito: usuario autorizado descarga el archivo. Rechazo: una URL directa sin permiso devuelve denegación.

## 11. Reglas de salidas y retornos temporales

### RN-070 - Datos de salida temporal

- **Estado:** Confirmada por requisitos.
- **Descripción:** La salida registra equipo, responsable, fecha y hora, destino y motivo.
- **Condiciones:** El responsable debe corresponder a la custodia o autorización aprobada.
- **Operaciones prohibidas:** Registrar una salida sin responsable, destino o motivo.
- **Consecuencias históricas:** La salida queda como movimiento independiente de la asignación.
- **Validaciones de servidor:** Obligatoriedad, elegibilidad, cronología y estado del equipo.
- **Casos de prueba:** Éxito: salida completa abre el movimiento. Rechazo: salida sin destino no se crea.

### RN-071 - Una sola salida abierta

- **Estado:** Propuesta.
- **Descripción:** Un equipo puede tener como máximo una salida temporal abierta.
- **Condiciones:** La salida anterior debe retornar o cancelarse antes de iniciar otra.
- **Operaciones prohibidas:** Dos ubicaciones externas simultáneas para el mismo equipo.
- **Consecuencias históricas:** Salidas consecutivas se conservan como periodos separados.
- **Validaciones de servidor:** Unicidad condicional y control transaccional.
- **Casos de prueba:** Éxito: abrir salida de equipo en oficina. Rechazo: abrir otra antes del retorno falla.

### RN-072 - Elegibilidad para salida

- **Estado:** Propuesta.
- **Descripción:** Solo equipos activos, localizados y sin bloqueos incompatibles pueden salir.
- **Condiciones:** No deben estar dados de baja, extraviados ni en mantenimiento bloqueante.
- **Operaciones prohibidas:** Abrir salida para un equipo cuya ubicación actual ya es externa o desconocida.
- **Consecuencias históricas:** Los rechazos no modifican ubicación ni custodia.
- **Validaciones de servidor:** Consultar ciclo de vida, ubicación, movimiento y mantenimiento vigentes.
- **Casos de prueba:** Éxito: equipo activo en oficina puede salir. Rechazo: equipo en reparación externa no abre otra salida.

### RN-073 - Datos de retorno

- **Estado:** Confirmada por requisitos.
- **Descripción:** El retorno registra fecha y hora, condición, observaciones y evidencias aplicables.
- **Condiciones:** Debe corresponder a una salida abierta del mismo equipo.
- **Operaciones prohibidas:** Retornar antes de la salida o sin inspección de condición.
- **Consecuencias históricas:** Salida y retorno forman un periodo completo y comparable.
- **Validaciones de servidor:** Relación, cronología, condiciones y evidencias requeridas.
- **Casos de prueba:** Éxito: retorno posterior cierra la salida. Rechazo: retorno sin salida abierta falla.

### RN-074 - Cierre histórico del movimiento

- **Estado:** Confirmada por requisitos.
- **Descripción:** El retorno cierra la salida sin eliminar el movimiento histórico.
- **Condiciones:** Una salida cerrada no vuelve a estado abierto.
- **Operaciones prohibidas:** Borrar salida y retorno para representar que el equipo volvió.
- **Consecuencias históricas:** Fechas, responsable, destino y condiciones permanecen consultables.
- **Validaciones de servidor:** Cierre atómico e inmutabilidad posterior.
- **Casos de prueba:** Éxito: cerrar conserva ambos eventos. Rechazo: reabrir una salida retornada es impedido.

### RN-075 - Salida atrasada

- **Estado:** Propuesta.
- **Descripción:** Una salida abierta posterior a su retorno esperado se considera atrasada de forma calculada.
- **Condiciones:** Se propone que cada ítem de una salida tenga fecha esperada de retorno obligatoria.
- **Operaciones prohibidas:** Cerrar automáticamente una salida por vencimiento.
- **Consecuencias históricas:** El atraso puede generar alertas, pero la salida sigue abierta hasta retorno real.
- **Validaciones de servidor:** Comparar hora vigente con fecha esperada usando zona temporal oficial.
- **Casos de prueba:** Éxito: una salida vencida aparece atrasada. Rechazo: el vencimiento no crea un retorno ficticio.

### RN-076 - Bloqueos durante una salida

- **Estado:** Propuesta.
- **Descripción:** Un equipo con salida abierta no puede devolverse de custodia, transferirse ni darse de baja.
- **Condiciones:** Primero debe registrarse el retorno o una resolución excepcional aprobada.
- **Operaciones prohibidas:** Eludir la ubicación externa mediante otra operación.
- **Consecuencias históricas:** La resolución excepcional conserva motivo y aprobador.
- **Validaciones de servidor:** Revisar salida abierta al ejecutar cada operación bloqueada.
- **Casos de prueba:** Éxito: transferir después del retorno. Rechazo: baja durante salida abierta falla.

### RN-077 - Cambio de responsable durante salida

- **Estado:** Propuesta.
- **Descripción:** Cambiar responsable durante una salida exige un movimiento explícito y trazable.
- **Condiciones:** Debe documentarse entrega entre responsables, lugar, fecha y condiciones.
- **Operaciones prohibidas:** Editar directamente el responsable de la salida abierta.
- **Consecuencias históricas:** Ambos periodos de responsabilidad quedan enlazados.
- **Validaciones de servidor:** Elegibilidad, custodia vigente y transferencia atómica.
- **Casos de prueba:** Éxito: relevo documentado conserva ambos responsables. Rechazo: edición directa del responsable falla.

### RN-078 - Movimiento con múltiples equipos

- **Estado:** Propuesta.
- **Descripción:** Una salida temporal puede agrupar varios equipos o integrantes de un kit bajo un mismo movimiento.
- **Condiciones:** El movimiento tiene cabecera común y una línea individual por equipo con estado, responsable y fechas propias.
- **Operaciones prohibidas:** Incorporar dos veces el mismo equipo, incluir uno no elegible o tratar el kit como sustituto de los expedientes individuales.
- **Consecuencias históricas:** La composición exacta de la salida queda congelada al confirmarse, aunque después cambie el kit.
- **Validaciones de servidor:** Verificar individualmente elegibilidad, salida abierta, concurrencia y pertenencia opcional al kit.
- **Casos de prueba:** Éxito: una salida agrupa cámara, lente y grabadora como tres líneas. Rechazo: incluir una cámara con otra salida abierta revierte la confirmación completa.

### RN-079 - Retorno individual o parcial

- **Estado:** Propuesta.
- **Descripción:** Cada equipo de un movimiento puede retornar por separado; el movimiento general se cierra cuando todas sus líneas quedan resueltas.
- **Condiciones:** Cada retorno registra fecha, condición, observaciones, ubicación y evidencias propias.
- **Operaciones prohibidas:** Marcar retornados equipos aún externos o cerrar el movimiento mientras existan líneas abiertas sin resolución.
- **Consecuencias históricas:** Se conservan retornos parciales, pendientes y fecha final de cierre del movimiento.
- **Validaciones de servidor:** Verificar línea abierta, evitar doble retorno, actualizar ubicación y recalcular estado general atómicamente.
- **Casos de prueba:** Éxito: retornar la cámara deja lente y grabadora abiertas. Rechazo: intentar cerrar todo el movimiento con una línea pendiente falla.

## 12. Reglas de baja y reactivación

### RN-080 - Autorización configurable de baja

- **Estado:** Propuesta.
- **Descripción:** La baja exige motivo y permiso específico; un flujo de aprobación adicional puede habilitarse por configuración.
- **Condiciones:** Sin flujo adicional, un usuario con permiso puede ejecutar la baja; con el flujo habilitado, debe existir una solicitud aprobada conforme a la política.
- **Operaciones prohibidas:** Dar de baja sin permiso, omitir el motivo o eludir un flujo de aprobación que esté habilitado.
- **Consecuencias históricas:** Se conservan ejecutor, motivo y fecha; si hubo solicitud y decisión, también quedan registradas.
- **Validaciones de servidor:** Evaluar permiso, configuración vigente, precondiciones y aprobación solo cuando corresponda.
- **Casos de prueba:** Éxito: un usuario autorizado da de baja directamente cuando la aprobación adicional está deshabilitada. Rechazo: un usuario sin permiso no puede hacerlo.

### RN-081 - Aprobación opcional y segregación

- **Estado:** Propuesta.
- **Descripción:** Cuando la política de aprobación esté habilitada, puede exigir que solicitante y aprobador sean distintos.
- **Condiciones:** La configuración define si hay aprobación, qué permiso decide y si aplica segregación de funciones.
- **Operaciones prohibidas:** Aplicar una doble aprobación fija a todos los casos o eludir una segregación expresamente configurada.
- **Consecuencias históricas:** La política aplicada, solicitante y decisor se conservan cuando exista aprobación.
- **Validaciones de servidor:** Cargar la política vigente, autorizar al decisor y comparar actores solo si la segregación está activa.
- **Casos de prueba:** Éxito: una política sin aprobación permite baja al rol autorizado. Rechazo: con segregación activa, el solicitante no aprueba su caso.

### RN-082 - Precondiciones de baja

- **Estado:** Propuesta.
- **Descripción:** La baja no se aprueba con custodia, salida o mantenimiento abierto.
- **Condiciones:** El equipo debe estar localizado y todos los procesos incompatibles resueltos.
- **Operaciones prohibidas:** Aprobar basándose solo en el estado existente al crear la solicitud.
- **Consecuencias históricas:** Los cierres necesarios preceden a la baja.
- **Validaciones de servidor:** Revalidar todas las precondiciones en el instante de aprobación.
- **Casos de prueba:** Éxito: aprobar equipo sin procesos abiertos. Rechazo: una asignación creada después de solicitar bloquea la aprobación.

### RN-083 - Efectos de la baja

- **Estado:** Propuesta.
- **Descripción:** La baja bloquea nuevas asignaciones, salidas y mantenimientos ordinarios.
- **Condiciones:** Solo operaciones históricas, de consulta, disposición o reactivación autorizada continúan disponibles.
- **Operaciones prohibidas:** Usar un equipo dado de baja como activo disponible.
- **Consecuencias históricas:** El estado previo y la fecha efectiva quedan conservados.
- **Validaciones de servidor:** Invariante de ciclo de vida en todos los comandos operativos.
- **Casos de prueba:** Éxito: consultar el expediente dado de baja. Rechazo: asignarlo a una persona falla.

### RN-084 - Conservación tras la baja

- **Estado:** Confirmada por requisitos.
- **Descripción:** La baja conserva íntegramente expediente, historiales y evidencias.
- **Condiciones:** Ninguna relación histórica se elimina por la transición.
- **Operaciones prohibidas:** Depurar automáticamente fotos, gastos o asignaciones por baja.
- **Consecuencias históricas:** El expediente continúa disponible para auditoría y reportes.
- **Validaciones de servidor:** Transición sin borrados en cascada y comprobaciones de integridad.
- **Casos de prueba:** Éxito: consultar todo el historial después de baja. Rechazo: una operación de baja que elimine relaciones revierte.

### RN-085 - Autorización configurable de reactivación

- **Estado:** Propuesta.
- **Descripción:** Reactivar exige justificación y permiso específico; una aprobación adicional puede habilitarse por configuración.
- **Condiciones:** Debe cumplirse la inspección requerida y la política de autorización vigente.
- **Operaciones prohibidas:** Reactivar sin permiso, sin justificación o eludiendo una aprobación configurada.
- **Consecuencias históricas:** Se conserva el periodo de baja, ejecutor y justificación, además del flujo opcional cuando exista.
- **Validaciones de servidor:** Verificar estado actual, permiso, inspección y política configurable.
- **Casos de prueba:** Éxito: un usuario autorizado reactiva directamente cuando la aprobación está deshabilitada. Rechazo: un equipo activo no puede reactivarse.

### RN-086 - Inspección para reactivar

- **Estado:** Propuesta.
- **Descripción:** Reactivar exige inspección física y operativa vigente.
- **Condiciones:** La inspección debe acreditar que el equipo puede volver al inventario activo.
- **Operaciones prohibidas:** Aprobar sin condiciones resultantes o con resultado incompatible.
- **Consecuencias históricas:** La inspección queda vinculada a la reactivación.
- **Validaciones de servidor:** Existencia, vigencia y resultado permitido de inspección.
- **Casos de prueba:** Éxito: inspección apta permite aprobar. Rechazo: condición inutilizable sin excepción bloquea la reactivación.

### RN-087 - Continuidad del expediente reactivado

- **Estado:** Propuesta.
- **Descripción:** La reactivación conserva código, identidad y todo historial anterior.
- **Condiciones:** Se reabre el mismo expediente; no se crea un equipo nuevo.
- **Operaciones prohibidas:** Duplicar el equipo para simular reactivación o reutilizar su código.
- **Consecuencias históricas:** Los periodos activo, baja y reactivación quedan encadenados.
- **Validaciones de servidor:** Transición sobre el identificador original y unicidad del código.
- **Casos de prueba:** Éxito: reactivar mantiene el identificador. Rechazo: crear otro equipo con su código falla.

### RN-088 - Auditoría de decisiones de ciclo de vida

- **Estado:** Propuesta.
- **Descripción:** Solicitudes, aprobaciones, rechazos y cancelaciones de baja o reactivación se auditan.
- **Condiciones:** Cada decisión requiere actor, fecha, resultado y comentario cuando corresponda.
- **Operaciones prohibidas:** Eliminar solicitudes rechazadas o cambiar su decisor.
- **Consecuencias históricas:** Se conserva incluso el flujo que no produjo una transición.
- **Validaciones de servidor:** Registro append-only dentro de la operación de decisión.
- **Casos de prueba:** Éxito: rechazo conserva solicitud y motivo. Rechazo: intento de eliminarlo es denegado.

### RN-089 - Disposición, pérdida y robo

- **Estado:** Propuesta.
- **Descripción:** Disposición final, venta, donación, pérdida o robo se registran como motivos y procesos diferenciados.
- **Condiciones:** Cada tipo debe tener requisitos documentales y permisos aprobados.
- **Operaciones prohibidas:** Usar una baja genérica para ocultar el destino final conocido.
- **Consecuencias históricas:** El expediente conserva causa, fecha, documentos y destino cuando aplique.
- **Validaciones de servidor:** Catálogo de causas y requisitos específicos.
- **Casos de prueba:** Éxito: baja por donación conserva receptor y documento. Rechazo: causa que exige documento no se aprueba sin él.

## 13. Reglas de auditoría y trazabilidad

### RN-090 - Auditoría de operaciones críticas

- **Estado:** Confirmada por decisión.
- **Descripción:** Toda operación crítica registra actor, instante, entidad, acción y resultado.
- **Condiciones:** Incluye altas, correcciones, custodias, movimientos, mantenimiento, gastos y ciclo de vida.
- **Operaciones prohibidas:** Confirmar la operación si su auditoría obligatoria no puede persistirse.
- **Consecuencias históricas:** Negocio y auditoría se confirman en la misma transacción lógica.
- **Validaciones de servidor:** Contexto de usuario confiable y escritura atómica.
- **Casos de prueba:** Éxito: asignación produce evento de auditoría. Rechazo: fallo de auditoría revierte la asignación.

### RN-091 - Auditoría append-only

- **Estado:** Propuesta.
- **Descripción:** Los registros de auditoría solo se agregan; no se editan ni eliminan funcionalmente.
- **Condiciones:** Cualquier corrección genera otro evento que referencia al anterior.
- **Operaciones prohibidas:** Actualización o borrado desde servicios ordinarios.
- **Consecuencias históricas:** La cadena de acciones permanece completa.
- **Validaciones de servidor:** Repositorio sin operaciones de modificación y permisos restringidos.
- **Casos de prueba:** Éxito: corrección añade evento. Rechazo: intento de editar evento existente falla.

### RN-092 - Valores anteriores y posteriores

- **Estado:** Propuesta.
- **Descripción:** Cambios críticos conservan valores anteriores y posteriores pertinentes.
- **Condiciones:** Deben excluirse secretos y datos innecesarios o sensibles.
- **Operaciones prohibidas:** Auditoría genérica que no permita comprender el cambio crítico.
- **Consecuencias históricas:** Puede reconstruirse qué cambió sin depender de la pantalla.
- **Validaciones de servidor:** Capturar valores desde datos persistidos y comando validado.
- **Casos de prueba:** Éxito: cambio de ubicación registra ambas ubicaciones. Rechazo: evento sin valor anterior no completa la operación configurada.

### RN-093 - Correlación de operaciones

- **Estado:** Propuesta.
- **Descripción:** Una operación compuesta comparte un identificador de correlación.
- **Condiciones:** Transferencias, devoluciones con incidencia y aprobaciones deben agrupar sus eventos.
- **Operaciones prohibidas:** Generar identificadores independientes que impidan reconocer una misma transacción.
- **Consecuencias históricas:** Auditoría y eventos de negocio pueden consultarse como unidad.
- **Validaciones de servidor:** Generar el identificador en servidor y propagarlo internamente.
- **Casos de prueba:** Éxito: transferencia comparte correlación en cierre y apertura. Rechazo: el cliente no impone una correlación ajena.

### RN-094 - Protección del historial de auditoría

- **Estado:** Propuesta.
- **Descripción:** Ningún usuario funcional puede eliminar auditoría.
- **Condiciones:** Retención o depuración técnica solo podrá seguir una política aprobada y controlada fuera de operaciones normales.
- **Operaciones prohibidas:** Permisos de interfaz que permitan borrar o alterar auditoría.
- **Consecuencias históricas:** Cualquier procedimiento excepcional deja constancia externa verificable.
- **Validaciones de servidor:** Autorización denegada por diseño para roles funcionales.
- **Casos de prueba:** Éxito: auditor autorizado consulta. Rechazo: administrador funcional intenta borrar y falla.

### RN-095 - Autoridad de validación en servidor

- **Estado:** Confirmada por decisión.
- **Descripción:** Todas las reglas críticas se validan en servicios de servidor.
- **Condiciones:** Cada canal futuro debe invocar la misma capa de aplicación o dominio.
- **Operaciones prohibidas:** Confiar exclusivamente en campos deshabilitados, validación JavaScript o componentes Razor.
- **Consecuencias históricas:** Solo operaciones válidas generan eventos de negocio.
- **Validaciones de servidor:** Autorización, invariantes, concurrencia y transacción en cada comando.
- **Casos de prueba:** Éxito: comando válido enviado directamente se procesa. Rechazo: petición manipulada que omite la UI es igualmente bloqueada.

### RN-096 - La interfaz no es frontera de seguridad

- **Estado:** Confirmada por decisión.
- **Descripción:** Ocultar controles no concede ni revoca permisos y no garantiza integridad.
- **Condiciones:** El servidor determina identidad, permisos y alcance en cada operación.
- **Operaciones prohibidas:** Aceptar roles, propietario o estado crítico enviados por el cliente como autoridad.
- **Consecuencias históricas:** Los intentos denegados sensibles pueden registrarse sin crear eventos de negocio.
- **Validaciones de servidor:** Políticas de autorización y carga del estado real desde persistencia.
- **Casos de prueba:** Éxito: usuario autorizado ejecuta la acción. Rechazo: usuario modifica el cliente para invocarla y recibe denegación.

### RN-097 - Control de concurrencia

- **Estado:** Confirmada por decisión.
- **Descripción:** Asignaciones, devoluciones y movimientos deben detectar si el equipo cambió desde que el usuario cargó la información.
- **Condiciones:** Esos comandos incluyen o verifican una versión de concurrencia; otras operaciones podrán incorporarla según riesgo.
- **Operaciones prohibidas:** Última escritura gana silenciosamente sobre cambios críticos.
- **Consecuencias históricas:** Un conflicto no crea eventos parciales y obliga a recargar.
- **Validaciones de servidor:** Comparar versión y confirmar cambios dentro de transacción.
- **Casos de prueba:** Éxito: primera actualización con versión vigente se guarda. Rechazo: segunda actualización obsoleta recibe conflicto.

### RN-098 - Intentos rechazados sensibles

- **Estado:** Propuesta.
- **Descripción:** Intentos denegados de operaciones sensibles pueden registrarse para seguridad.
- **Condiciones:** Debe definirse qué acciones se auditan y evitar almacenar datos secretos.
- **Operaciones prohibidas:** Usar este registro como si fuera un evento de negocio exitoso.
- **Consecuencias históricas:** El intento queda separado del expediente funcional y disponible para revisión autorizada.
- **Validaciones de servidor:** Capturar actor, acción, razón y correlación de forma segura.
- **Casos de prueba:** Éxito: intento de baja sin permiso genera evento de seguridad. Rechazo: no crea una solicitud de baja.

### RN-099 - Política temporal uniforme

- **Estado:** Propuesta.
- **Descripción:** Se propone almacenar instantes en UTC y presentarlos con la zona `America/La_Paz`.
- **Condiciones:** Fechas efectivas sin hora deben distinguirse de instantes de auditoría.
- **Operaciones prohibidas:** Mezclar horas locales sin zona o aceptar instantes ambiguos.
- **Consecuencias históricas:** Los eventos mantienen orden reproducible y zona de presentación conocida.
- **Validaciones de servidor:** Normalización, rango y política para fechas futuras o retroactivas.
- **Casos de prueba:** Éxito: instante válido se normaliza y presenta localmente. Rechazo: fecha ambigua sin política se rechaza.

## 14. Modelo de estados

### 14.1 Dimensiones del estado del equipo

| Dimensión | Estados propuestos | Fuente de verdad |
|---|---|---|
| Ciclo de vida | Borrador, Activo, Dado de baja, Anulado | Expediente del equipo y aprobación vigente |
| Custodia | Sin asignar, Asignado a persona, Compartido en oficina | Periodo de custodia activo |
| Condición física | Excelente, Bueno, Regular, Dañado, Inutilizable | Última inspección válida |
| Condición operativa | Operativo, Operativo con limitaciones, No operativo | Última inspección válida |
| Mantenimiento | Sin mantenimiento abierto, Programado, En curso | Proceso de mantenimiento abierto |
| Movimiento por ítem | En ubicación registrada, Salida temporal abierta, Retornado, Resuelto por excepción | Línea individual del movimiento |
| Movimiento general | En preparación, Abierto, Parcialmente retornado, Cerrado, Cancelado | Estado derivado de sus líneas |
| Ubicación | Ubicación habitual y ubicación física actual | Configuración del equipo y último movimiento confirmado |
| Disponibilidad | Disponible, Bajo custodia, Compartido, Fuera temporalmente, En mantenimiento, Bloqueado por baja | Valor derivado; no se edita manualmente |

Los nombres exactos de las condiciones física y operativa son propuestas pendientes de aprobación. Los estados de una solicitud de baja o reactivación pertenecen al flujo de aprobación y no sustituyen el ciclo de vida efectivo del equipo.

### 14.2 Ciclo de vida

| Estado origen | Operación | Estado destino | Condiciones | Reglas |
|---|---|---|---|---|
| Borrador | Activar expediente | Activo | Código único y mínimos completos | RN-001, RN-003, RN-006 |
| Borrador | Anular alta errónea | Anulado | Sin uso operativo; motivo y auditoría | RN-004, RN-090 |
| Activo | Dar de baja | Dado de baja | Permiso, motivo, sin bloqueos y sin aprobación adicional configurada | RN-080, RN-082 a RN-084 |
| Activo | Solicitar baja | Activo | Solo si la política configura aprobación adicional | RN-080, RN-081, RN-088 |
| Activo | Aprobar baja | Dado de baja | Solicitud válida, política cumplida y sin procesos bloqueantes | RN-080 a RN-084 |
| Activo | Rechazar baja | Activo | Solicitud existente y motivo de decisión | RN-081, RN-088 |
| Dado de baja | Reactivar | Activo | Permiso, justificación, inspección apta y sin aprobación adicional configurada | RN-085 a RN-087 |
| Dado de baja | Solicitar reactivación | Dado de baja | Solo si la política configura aprobación adicional | RN-081, RN-085, RN-088 |
| Dado de baja | Aprobar reactivación | Activo | Solicitud válida, política cumplida e inspección apta | RN-081, RN-085 a RN-088 |
| Dado de baja | Rechazar reactivación | Dado de baja | Solicitud existente y motivo de decisión | RN-081, RN-088 |

Transiciones no listadas están prohibidas. `Anulado` es terminal y se reserva para borradores erróneos que nunca tuvieron uso operativo. No existe transición de `Dado de baja` a `Borrador` ni creación de un expediente nuevo para reactivar.

### 14.3 Custodia

| Estado origen | Operación | Estado destino | Condiciones | Reglas |
|---|---|---|---|---|
| Sin asignar | Entregar a persona | Asignado a persona | Persona elegible, equipo asignable y condiciones documentadas | RN-010, RN-013, RN-020, RN-021, RN-030 |
| Sin asignar | Asignar a oficina | Compartido en oficina | Oficina y custodio activos | RN-012, RN-020, RN-021 |
| Asignado a persona | Devolver | Sin asignar | Custodia coincidente y devolución completa | RN-032 a RN-036 |
| Compartido en oficina | Retirar de oficina | Sin asignar | Custodio vigente y recepción documentada | RN-016, RN-032, RN-033 |
| Asignado a persona | Transferir | Asignado a persona | Cierre y apertura atómicos | RN-022 a RN-027 |
| Asignado a persona | Transferir a oficina | Compartido en oficina | Oficina y custodio elegibles | RN-012, RN-022 a RN-027 |
| Compartido en oficina | Transferir a persona | Asignado a persona | Persona elegible | RN-010, RN-022 a RN-027 |
| Compartido en oficina | Cambiar custodio | Compartido en oficina | Nuevo custodio elegible y periodos no solapados | RN-016, RN-024 |

No se permite una transición que produzca más de una custodia activa.

### 14.4 Condición física

| Estado origen | Estado destino permitido | Condición de transición | Reglas |
|---|---|---|---|
| Excelente | Bueno, Regular, Dañado, Inutilizable | Inspección y motivo | RN-030, RN-033, RN-063, RN-090 |
| Bueno | Excelente, Regular, Dañado, Inutilizable | Inspección; mejora exige justificación o mantenimiento | RN-041, RN-046 |
| Regular | Excelente, Bueno, Dañado, Inutilizable | Inspección; mejora exige justificación o mantenimiento | RN-041, RN-046 |
| Dañado | Excelente, Bueno, Regular, Inutilizable | Mejora exige reparación e inspección documentadas | RN-035, RN-041, RN-046 |
| Inutilizable | Excelente, Bueno, Regular, Dañado | Reparación e inspección documentadas | RN-041, RN-046 |

Una transición al mismo estado puede registrar una nueva inspección sin borrar la anterior.

### 14.5 Condición operativa

| Estado origen | Estado destino permitido | Condición de transición | Reglas |
|---|---|---|---|
| Operativo | Operativo con limitaciones, No operativo | Inspección y observación | RN-030, RN-033, RN-035 |
| Operativo con limitaciones | Operativo, No operativo | Diagnóstico o inspección | RN-041, RN-046 |
| No operativo | Operativo, Operativo con limitaciones | Reparación e inspección final | RN-041, RN-046 |

La asignabilidad de `Operativo con limitaciones` permanece como decisión empresarial pendiente.

### 14.6 Mantenimiento

| Estado origen | Operación | Estado destino | Condiciones | Reglas |
|---|---|---|---|---|
| Sin mantenimiento abierto | Programar | Programado | Equipo activo y datos mínimos | RN-040, RN-042, RN-045 |
| Sin mantenimiento abierto | Iniciar correctivo inmediato | En curso | Falla registrada y sin proceso incompatible | RN-040 a RN-045 |
| Programado o En curso | Abrir mantenimiento compatible | Sin cambio en el existente | No existe conflicto operativo explícito | RN-045 |
| Programado | Iniciar | En curso | Fecha y responsable técnico | RN-042 |
| Programado | Cancelar | Cancelado | Motivo obligatorio | RN-042, RN-090 |
| En curso | Completar | Completado | Diagnóstico, trabajo y condición resultante | RN-041, RN-043, RN-046 |
| En curso | Cancelar excepcionalmente | Cancelado | Justificación y permiso | RN-042, RN-090 |

`Completado` y `Cancelado` son terminales. Una corrección posterior se registra como ajuste, no como retroceso de estado.

### 14.7 Salida temporal por ítem

| Estado origen | Operación | Estado destino | Condiciones | Reglas |
|---|---|---|---|---|
| En ubicación registrada | Registrar salida | Salida temporal abierta | Equipo elegible, datos completos y retorno esperado | RN-070 a RN-072, RN-075, RN-078 |
| Salida temporal abierta | Registrar retorno | Retornado | Condición, ubicación y datos de retorno completos | RN-073, RN-074, RN-079 |
| Salida temporal abierta | Marcar atrasada | Sin cambio | Se deriva al superar fecha esperada | RN-075 |
| Salida temporal abierta | Resolver excepción | Resuelto por excepción | Permiso, motivo, ubicación y evidencia según política | RN-076, RN-090 |

No existe cierre automático por vencimiento y no puede abrirse una segunda salida simultánea para el mismo equipo.

### 14.8 Estado del movimiento general

| Estado origen | Operación o condición | Estado destino | Reglas |
|---|---|---|---|
| En preparación | Confirmar salida | Abierto | RN-070 a RN-072, RN-078 |
| Abierto | Retornar parte de los equipos | Parcialmente retornado | RN-073, RN-074, RN-079 |
| Abierto | Resolver todas las líneas | Cerrado | RN-074, RN-079 |
| Parcialmente retornado | Resolver líneas restantes | Cerrado | RN-074, RN-079 |
| En preparación | Cancelar antes de salida | Cancelado | RN-090 |

`Cerrado` se deriva únicamente cuando todas las líneas están retornadas o resueltas por excepción.

### 14.9 Compatibilidad de estados

| Situación | Permitida | Regla aplicable |
|---|---:|---|
| Equipo dado de baja con custodia activa | No | RN-082, RN-083 |
| Equipo dado de baja con salida abierta | No | RN-076, RN-082 |
| Equipo dado de baja con mantenimiento abierto | No | RN-048, RN-082 |
| Dos custodias activas | No | RN-020 |
| Dos salidas temporales abiertas | No | RN-071 |
| Dos mantenimientos correctivos incompatibles abiertos | No | RN-045 |
| Dos mantenimientos compatibles abiertos | Sí | RN-045 |
| Custodia personal con salida temporal | Sí | La salida no reemplaza la custodia |
| Custodia compartida con salida temporal | Sí | Debe identificarse responsable de salida |
| Custodia activa durante mantenimiento | Pendiente | Requiere decisión empresarial |
| Asignación de equipo operativo con limitaciones | Pendiente | Requiere decisión empresarial |
| Cambio de condición sin inspección | No | RN-030, RN-033, RN-046 |

## 15. Matriz de operaciones y reglas aplicables

| Operación | Reglas principales | Validaciones críticas de servidor |
|---|---|---|
| Crear borrador de equipo | RN-001 a RN-003, RN-008 | Código, categoría, posibles duplicados |
| Activar equipo | RN-001, RN-003, RN-006, RN-009, RN-110, RN-111 | Código único, categoría y nombre; datos opcionales si se informan |
| Marcar dato pendiente o no aplicable | RN-108, RN-112, RN-115 | Estado explícito y coherencia con el valor |
| Confirmar verificación | RN-112, RN-113, RN-115, RN-090 | Fecha real, persona y equipo, sin sobrescribir revisiones |
| Añadir observación o fotografía | RN-060, RN-061, RN-104, RN-116 | Preservar fuente original y evidencia histórica |
| Registrar o completar ficha técnica | RN-100 a RN-106, RN-108, RN-109 | Categoría, tipos, unidades, valores presentes y nueva versión |
| Ampliar RAM o reemplazar unidad | RN-102, RN-103, RN-106 a RN-108, RN-049, RN-052 | Versión previa, componentes, mantenimiento/gasto opcional y concurrencia |
| Corregir identificación | RN-005, RN-007, RN-008, RN-090 a RN-093 | Permiso, motivo, duplicados, concurrencia |
| Relacionar accesorio | RN-018, RN-028, RN-029 | Identidades, categorías, ciclos y vigencia |
| Crear o modificar kit | RN-019, RN-090, RN-097 | Integrantes únicos, compatibilidad y concurrencia |
| Consultar expediente | RN-002, RN-067, RN-084 | Autorización y alcance |
| Asignar a persona | RN-010, RN-013, RN-020, RN-021, RN-030, RN-031 | Elegibilidad, estado, custodia única y entrega |
| Asignar a oficina | RN-012, RN-016, RN-020, RN-021, RN-030 | Oficina, custodio, estado y custodia única |
| Transferir custodia | RN-016, RN-022 a RN-027, RN-093, RN-097 | Origen vigente, destino elegible, atomicidad |
| Devolver equipo | RN-032 a RN-036, RN-060, RN-061 | Custodia activa, cronología, condición y evidencia |
| Desactivar persona | RN-013 a RN-016 | Custodias personales y compartidas pendientes |
| Programar preventivo | RN-040, RN-042, RN-045, RN-047 | Equipo activo, compatibilidad y fechas |
| Abrir correctivo | RN-035, RN-040 a RN-045 | Falla, equipo, procesos abiertos y condición |
| Completar mantenimiento | RN-041 a RN-046, RN-049 | Diagnóstico y resultado; los costos son opcionales |
| Ajustar mantenimiento | RN-043, RN-044, RN-090 a RN-093 | Estado cerrado, motivo, permiso y referencia |
| Registrar repuesto | RN-050, RN-052 a RN-054 | Relación, cantidad, costo e idempotencia |
| Registrar mano de obra | RN-051 a RN-054, RN-058 | Clasificación, monto y moneda |
| Corregir gasto | RN-052 a RN-058, RN-090 a RN-093 | Anulación auditada, reemplazo opcional, moneda, comprobante y permiso |
| Subir evidencia | RN-060 a RN-064, RN-066, RN-067 | Tipo, relación, contenido, tamaño y autorización |
| Cambiar foto principal | RN-061 a RN-063, RN-097 | Pertenencia, unicidad vigente y concurrencia |
| Retirar evidencia | RN-061, RN-065 a RN-067, RN-090 | Permiso, motivo e integridad histórica |
| Registrar salida con uno o varios equipos | RN-070 a RN-072, RN-075, RN-077, RN-078 | Elegibilidad individual, retorno esperado y atomicidad |
| Registrar retorno individual o parcial | RN-073 a RN-079, RN-097 | Línea abierta, cronología, ubicación y concurrencia |
| Solicitar baja | RN-048, RN-076, RN-080, RN-082, RN-088 | Procesos abiertos, motivo y permiso |
| Ejecutar o aprobar baja | RN-080 a RN-084, RN-088, RN-089, RN-097 | Permiso, política configurada, precondiciones y versión |
| Solicitar reactivación | RN-085 a RN-088 | Estado dado de baja, justificación y permiso |
| Ejecutar o aprobar reactivación | RN-081, RN-085 a RN-088, RN-097 | Permiso, política configurada, inspección y versión |
| Calcular gasto histórico | RN-050 a RN-058 | No duplicidad, anulaciones, monedas y precisión |
| Ejecutar cualquier comando crítico | RN-090 a RN-099 | Actor, autorización, auditoría, tiempo y concurrencia |

## 16. Decisiones empresariales pendientes

| Código | Decisión requerida | Impacto |
|---|---|---|
| DE-001 | Aprobar código automático secuencial para altas futuras con formato propuesto `CPR-000001`; los códigos heredados como `EQ_1` se conservan | RN-001, RN-005, RN-110 |
| DE-003 | Aprobar estados físicos propuestos: Excelente, Bueno, Regular, Dañado e Inutilizable; y operativos: Operativo, Operativo con limitaciones y No operativo | Matrices 14.4 y 14.5 |
| DE-004 | Si un equipo operativo con limitaciones puede asignarse | RN-021 |
| DE-005 | Si la custodia permanece activa durante mantenimiento | RN-021, RN-045 |
| DE-006 | Requisitos para ser custodio de una oficina | RN-012, RN-016 |
| DE-007 | Plazo y procedimiento cuando una persona deja la empresa | RN-014 |
| DE-008 | Modalidad de aceptación o firma en entregas y devoluciones | RN-031 |
| DE-009 | Operaciones con fotografías obligatorias y cantidad mínima | RN-031, RN-057, RN-060 |
| DE-010 | Formatos, tamaños y retención de archivos | RN-064 a RN-067 |
| DE-011 | Aprobar BOB como moneda principal y definir impuestos y redondeo | RN-054, RN-058 |
| DE-012 | Umbrales de comprobante y aprobación de gastos | RN-057 |
| DE-013 | Frecuencias y alertas de mantenimiento preventivo | RN-047 |
| DE-014 | Aprobar fecha esperada de retorno obligatoria por equipo en salidas temporales | RN-075, RN-078 |
| DE-015 | Requisitos de pérdida, robo, venta, donación y disposición | RN-089 |
| DE-016 | Motivos válidos y documentos para baja y reactivación | RN-080 a RN-089 |
| DE-017 | Permisos y configuración opcional de aprobación para baja y reactivación | RN-080, RN-081, RN-085 |
| DE-018 | Aprobar `America/La_Paz` como zona oficial y definir límites de retroactividad | RN-025, RN-075, RN-099 |
| DE-019 | Política de retención de auditoría | RN-091, RN-094 |
| DE-020 | Aprobar roles preliminares Administrador, Inventario/TI, Gerencia y Consulta, y definir su matriz de permisos | RN-067, RN-080, RN-085, RN-096 |
| DE-021 | Criterios de verificación y formato de serie por categoría, sin exigirla para el alta inicial | RN-009, RN-111 |
| DE-022 | Categorías y restricciones para relaciones principal-accesorio | RN-018 |
| DE-023 | Reglas de composición exclusiva o compartida de kits | RN-019 |
| DE-024 | Catálogo de ubicaciones y autoridad para cambiar ubicación habitual | RN-028, RN-029 |
| DE-025 | Definir atributos técnicos sugeridos y verificaciones futuras por categoría; los mínimos iniciales ya son código, categoría y nombre | RN-003, RN-108, RN-111, RN-112 |
| DE-026 | Catálogo inicial de atributos tipados de monitor, cámara, disco externo, periférico y grabación | RN-105 |
| DE-027 | Política de discrepancias entre RAM total y suma de módulos, y unidad canónica GB/TB para almacenamiento | RN-102, RN-103 |
| DE-028 | Nivel de detalle y autorización requeridos al corregir fichas técnicas antiguas | RN-106, RN-107 |

### Decisiones resueltas en esta revisión

| Código | Resolución | Reglas |
|---|---|---|
| DE-002 | El alta inicial exige únicamente código único, categoría y nombre descriptivo. La falta de datos técnicos, serie, fotografía o ubicación no bloquea el alta. | RN-003, RN-108, RN-111 |

## 17. Criterios para aprobación del documento

- Aprobar, modificar o rechazar cada regla marcada como **Propuesta**.
- Resolver las decisiones pendientes de la tabla `DE-001` a `DE-028` (excepto `DE-002`, ya resuelta) o declarar cuáles pueden aplazarse.
- Confirmar los catálogos y nombres de estados.
- Confirmar la matriz de compatibilidad y las operaciones bloqueantes.
- Confirmar qué eventos exigen evidencia, aceptación o aprobación adicional configurable.
- Una vez aprobado, actualizar `.opencode/MEMORY.md` con una referencia resumida a este documento, sin duplicar todas sus reglas.

Hasta esa aprobación, las reglas confirmadas son requisitos vigentes y las propuestas constituyen una base de análisis, no comportamiento autorizado para producción.
