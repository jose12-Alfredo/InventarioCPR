# Propuesta pendiente — autoría Identity de inspecciones y verificaciones

Estado: **solo diseño; sin cambios de entidades, migraciones ni esquema aplicados por esta propuesta**.

## Problema actual

EquipmentInspection.InspectorId y TechnicalVerification.VerifierId identifican StaffMember, no la cuenta AccessUser. La hora ya se genera en el servidor. Usar el ID o alias de una cuenta como si fuera un empleado confundiría a quien registra con quien inspeccionó físicamente el equipo.

## Cambio propuesto para revisión

Añadir en ambos tipos de evento:

- RecordedByAccountId nullable, con el ID Identity validado en servidor.
- RecordedByAccountName nullable, snapshot del identificador visible al registrar.

Mantener intactas las relaciones InspectorId/VerifierId existentes. Los eventos antiguos conservarán NULL en los nuevos datos; no se atribuirán retroactivamente a la cuenta actual. Los campos nuevos serán inmutables una vez confirmado el evento.

La migración propuesta será nueva y aditiva. Si se exige integridad referencial hacia access_users, usar una referencia mínima de solo lectura excluida de migraciones de inventario: AccessDbContext seguirá siendo dueño de las tablas Identity. No introducir una segunda creación de tablas de acceso ni cambios de roles/cuentas.

## Comportamiento previsto

La acción opcional «Registrar verificación técnica ahora» requiere confirmar la persona que realizó la revisión. El servidor obtiene automáticamente el autor de captura de la sesión válida, comprueba cuenta activa/sello/roles y asigna la fecha UTC. No basta estar conectado para considerarse inspector ni se selecciona la última persona que verificó como valor predeterminado.

Un selector de personal real o una vinculación explícita cuenta-persona se incorporará solo cuando el módulo correspondiente esté autorizado. No crear empleados ficticios para completar el dato de autoría. La resolución del actor debe compartir la serialización de consultas Identity del circuito Blazor.

## Validación necesaria después de autorización

- Separación real de autor de captura y persona verificadora.
- Cliente no puede suplantar RecordedByAccountId, nombre ni fecha de carga.
- Verificación desactivada no crea eventos; activada conserva todos los anteriores.
- Lectura de eventos antiguos sin inventar autores.
- FK/restricciones revisadas sobre la rama aislada y sin migraciones al arrancar.

La simplificación actual conserva el mecanismo existente de persona explícita y fecha UTC; no afirma que esta autoría Identity adicional ya se persista. Preparar SQL/EF y aplicarlos requerirá autorización separada.
