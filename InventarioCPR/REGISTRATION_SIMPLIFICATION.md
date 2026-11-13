# Registro de equipos simplificado — implementación local

Nota de actualización (2026-11-13): este documento conserva los resultados de la fase de simplificación anterior. El formulario actual añade Borrador → Definitiva con fotografía principal obligatoria; la migración fue aplicada con autorización únicamente a `test-migracion-inicial` y se validó el flujo real con Neon/Cloudinary. Las referencias inferiores a guardado sin foto, ausencia de migraciones o limpieza de notas describen aquella fase; comportamiento vigente en `MANDATORY_PHOTO_REGISTRATION.md` y evidencia real en `MANDATORY_PHOTO_REAL_VALIDATION.md`. No hubo publicación de esta versión.

## Alcance

Se modificó el modal existente `TechnicalFormPreview`, con los mismos servicios de inventario, Identity, Neon y fotografías de la Etapa A. No se creó un formulario paralelo, no se cambiaron entidades/snapshot/migraciones ni se aplicaron cambios de esquema. No se hicieron commit, push o despliegue; la versión publicada de Render no incorpora todavía esta simplificación.

## Qué cambió en pantalla

- Registro rápido: exactamente código, categoría y nombre. Ambos datos técnicos y controles futuros ausentes.
- Registro completo: Identificación, Características técnicas y Control y fotografías. Dos columnas cuando hay espacio y una en móvil; pasos y navegación persistentes en el mismo modal.
- Identificación conserva marca, modelo y serie opcionales. Estado de serie detrás de «Opciones del número de serie»; el resumen señala Pendiente/No aplica cuando corresponde.
- Procesador: un campo principal. Fabricante y modelo permanecen accesibles en «Detalles avanzados del procesador» y se conservan al editar aunque esa sección siga cerrada.
- RAM total manual sin módulos conocidos. Al marcar «He registrado todos los módulos», la suma de capacidades positivas pasa a ser el total calculado. Los registros antiguos parciales no se convierten automáticamente en completos. El total manual se mantiene en el borrador al alternar la distribución completa/parcial.
- Añadir módulo/Añadir disco crea filas solo cuando se requieren. Filas de borrador enteramente vacías no se guardan como componentes o capacidades cero. Una distribución marcada completa con filas sin completar debe corregirse o marcarse parcial.
- Tecnología SSD/HDD, interfaz y capacidad por disco. Se pueden guardar los discos conocidos sin exigir conocer todos; valores históricos de tecnología/interfaz fuera de las opciones actuales siguen siendo visibles como registrados.
- Estados especiales de RAM/discos/SO y control están en desplegables cerrados inicialmente. No registrado, Pendiente y No aplica siguen siendo distintos. Los pendientes explícitos se conservan incluso si hay un valor conocido pendiente de verificar; se resumen en una línea cuando existen.
- Condición física y operativa permanecen separadas, sin precargar un catálogo empresarial no aprobado. Solo se solicita inspector cuando las condiciones informadas cambian.
- Verificación técnica opcional y desactivada inicialmente. Al activarla se pide la persona que realmente verificó; no se preselecciona el verificador anterior. La fecha continúa generándose en el servidor.
- Retiradas las vistas previas de responsable, área, compra e importación Excel y los atributos por categoría sin persistencia implementada. No se eliminaron entidades, columnas o hechos históricos.
- Una sola explicación general y un único mensaje de error en el cuerpo; acciones fijas y un solo cuerpo desplazable.

## Guardar y continuar con fotografías

En el tercer paso, «Guardar equipo y añadir fotos» valida y guarda antes de abrir `EquipmentPhotos` de la Etapa A dentro del mismo modal. El ID devuelto se conserva inmediatamente, antes de intentar refrescar la lista. Las operaciones posteriores actualizan ese ID en lugar de crear otra alta.

El cambio de paso conserva montada la cola de fotografías para ese equipo, aunque no esté visible. Durante la subida se bloquea cerrar/guardar. Al intentar cerrar o registrar otro con archivos aún pendientes se pide confirmación; las imágenes confirmadas y el equipo no se eliminan. Un fallo de fotografía permite reintentar sobre la misma reserva y expediente.

Después de cada commit se limpian la nota nueva y la marca «Verificar ahora» para no repetir eventos al volver a guardar después de las fotos. Se actualiza el listado sin recargar la aplicación completa.

## Preservación técnica y validación de servidor

El DTO añade IDs de procedencia de módulos/discos, revisión técnica esperada y flags de edición/completitud RAM; no añade columnas. El servicio comprueba que esos IDs pertenecen a la configuración vigente y rechaza duplicados o referencias de otra revisión.

Al crear una revisión nueva se mantienen ranuras, tecnología y serie de RAM; posición y serie de discos; y atributos técnicos tipados que el formulario no edita. Los componentes nuevos reciben posiciones libres. No se modifican las revisiones antiguas.

La edición de identificación/control sin tocar la parte técnica no la reemplaza. Si el usuario editó la ficha técnica y la revisión cambió en paralelo, se rechaza la instantánea desactualizada. El servidor valida la suma cuando la distribución se declara completa; un total desconocido sigue siendo NULL, no cero.

Las notas de procedencia Excel quedan intactas. Responsable y compra pasan al contexto de los flujos históricos/administrativos futuros, no a campos de texto libre de este alta. La galería continúa disponible también desde Evidencias del expediente posterior.

## Reglas actuales mantenidas y propuestas separadas

- Modelo informado requiere marca.
- Sede y ubicación se guardan juntas o ambas quedan vacías; una ayuda aparece cuando solo se ha completado una.
- Condiciones nuevas/cambiadas requieren identificar la persona inspectora.
- No se inventan estados de condición aprobados.
- No se atribuye inspección/verificación automáticamente a la cuenta conectada.

Propuestas no ejecutadas:

1. `Proposals/RegistrationIdentityAuthorship.md`: autor de captura Identity separado de la persona real de StaffMember; fecha y usuario del servidor. No existe aún ese campo adicional en PostgreSQL.
2. `Proposals/IndependentSiteRegistration.md`: sede conocida sin crear ubicación ficticia, con relaciones coherentes y preservación de observaciones antiguas.

No se generó ninguna migración pendiente con esas propuestas. EF `has-pending-model-changes` confirmó que el esquema/modelo no cambió. Las dos requieren autorización separada para preparar/aplicar sus cambios de base.

## Pruebas realizadas

Suite final Release: **50 superadas, 0 fallidas, 1 omitida**. La integración persistente antigua quedó omitida; la nueva prueba de preservación y el navegador verificaron esta tarea con fixtures autorizados en `test-migracion-inicial` y sin modificar production.

La prueba PostgreSQL nueva conserva un equipo `REG_SIMPLE_META_…` ficticio y comprueba:

- RAM parcial de 32 GB con módulos conocidos de 8+8 sin recalcularla silenciosamente.
- Edición no técnica sin crear otra revisión.
- Edición técnica preservando fabricante/modelo avanzado, ranuras, seriales y atributos tipados.
- Completar módulos y preservar tres revisiones; solo una vigente.
- Rechazo de una instantánea técnica desactualizada.
- Nota de procedencia Excel ficticia y marcador RAM pendiente intactos.

Pruebas del modal con sesión administrativa manual por HTTPS localhost:

| Caso | Resultado |
|---|---|
| Registro rápido con tres controles | Guardado y listado correctos |
| Laptop completa | Procesador, marca/modelo, serie pendiente, dos módulos y dos discos guardados |
| RAM calculada y alternancia parcial | 8+8=16; total manual de 32 conservado al desmarcar completitud |
| Cambio de categoría durante el alta | Monitor no muestra RAM ni vistas previas; volver a laptop conserva su borrador |
| Condiciones independientes | Valores físicos/operativos ficticios distintos y persona identificada |
| Verificación desactivada | No crea una verificación artificial |
| Equipo guardado y fotografía | Mismo ID; fallo HTTP de foto simulado, equipo conservado y reintento real a Cloudinary confirmado |
| Guardar/cerrar tras las fotos | No duplica alta, nota, revisión técnica ni verificación |
| Edición RAM a 32 GB | Revisión 1 y revisión 2 con notas anteriores conservadas |
| Total manual de 24 GB sin módulos | Persiste y vuelve a mostrarse |
| Alta sin datos opcionales | No crea ficha técnica ficticia |
| Guardar y registrar otro | Reinicia el formulario y conserva el alta anterior |
| Código duplicado con minúsculas/espacios | Rechazado por servidor |
| Cierre con cola pendiente | Advertencia comprobada; cancelar conserva selección |

Fixture principal de navegador: `REG_SIMPLE_COMPLETO_MWDUGD35`, ID `e8a2969b-2d2c-4b23-b7c8-5d9adfb51e6b`; foto ficticia en la carpeta `inventariocpr-pruebas/registro-simplificado`. Otros fixtures son `REG_SIMPLE_RAPIDO_…`, `REG_SIMPLE_MANUAL_…` y `REG_SIMPLE_INCOMPLETO_…`, incluidos registros rápidos de reintentos del verificador. Se conservan identificados como ficticios; no se borraron registros existentes.

## Responsive, accesibilidad y evidencias

Impeccable cargado y consultados DESIGN.md/AGENTS.md. Revisión real en Chromium de tres pasos × seis tamaños: 360×800, 390×844, 768×1024, 1024×768, 1440×900 y 1920×1080.

- 0 violaciones Axe A/AA en las 18 combinaciones, también con verificación y opciones avanzadas expandidas.
- 0 overflow horizontal del modal/cuerpo.
- Acciones visibles y 0 objetivos activos menores de 44 px (checkbox mediante etiqueta táctil).
- Prueba adicional de viewport reducido a 390×430 para simular espacio de teclado: acciones dentro del viewport.
- Impeccable en fuentes modificadas y formulario autenticado renderizado: `[]`.
- Capturas revisadas visualmente, con datos explícitamente ficticios. Las imágenes finales de revisión no guardan el ejemplo mostrado.

Capturas locales en `.impeccable/review/`:

- `registration-final-step-{1,2,3}-{ancho}x{alto}.png`.
- `registration-final-control-photos.png`, zona inferior del tercer paso.
- `registration-disks-{ancho}x{alto}.png`, segunda parte de la ficha técnica.
- `registration-quick-1440x900.png` y `registration-photos-after-save.png`.

Resultados: `registration-simplified-results.json` y `registration-final-visual.json`. Algunos primeros fallos pertenecían al harness (esperas de render y temporizador de un navegador previo); se corrigieron y se reanudó desde el expediente existente, sin reemplazarlo ni duplicar sus fotos.

## Limitaciones explícitas

- Android/iPhone físicos y teclado nativo no ejecutados en esta sesión; se probó Chromium y viewport móvil/táctil/reducido. No se certifica Safari por emulación de tamaño.
- La autoría adicional Identity y sede independiente están propuestas, no implementadas en base.
- Los catálogos definitivos de condición siguen pendientes de aprobación; no se promovieron estados propuestos a reglas vigentes.
- La función Cloudinary mantiene los límites y seguridad de la Etapa A. Un fallo del proveedor no revierte un equipo ya guardado.
- Cambios únicamente locales. No hay commit, push, publicación ni cambios a production.
