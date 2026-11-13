# Propuesta pendiente — sede conocida sin ubicación inventada

Estado: **solo diseño; sin migración ni cambios de esquema**. El formulario simplificado sigue usando la regla actual: sede y ubicación se guardan juntas o permanecen ambas vacías.

## Problema actual

EquipmentLocationObservation exige LocationId y obtiene la sede a través de Location.SiteId. No puede representar una sede conocida sin crear también una ubicación. No se debe fabricar una ubicación llamada «Desconocida» ni almacenar el dato de sede únicamente dentro de una nota que luego no pueda consultarse estructuradamente.

## Cambio de persistencia propuesto

- Añadir SiteId nullable a EquipmentLocationObservation.
- Permitir LocationId nullable y exigir por CHECK que exista SiteId o LocationId.
- FK restrictiva para SiteId.
- Conservar la FK existente de LocationId y añadir una relación compuesta (LocationId, SiteId) a (Location.Id, Location.SiteId), con la clave alternativa necesaria, para rechazar ubicaciones de otra sede cuando ambos datos se proporcionen.

Los registros antiguos mantendrán LocationId y SiteId nuevo NULL; la lectura resolverá su sede por la relación histórica existente. Los registros nuevos con solo sede tendrán SiteId válido y LocationId NULL. No se eliminarán ni reescribirán observaciones anteriores.

## Reglas de lectura y edición

- Mostrar la sede de la observación más reciente y «Ubicación no registrada» cuando esa observación no la conoce; no arrastrar una ubicación antigua de otra sede.
- Una sede observada no equivale a custodia, asignación o movimiento confirmado.
- Mantener la protección de ubicaciones constatadas: dejar un campo vacío en una edición no borra silenciosamente un hecho histórico. Una corrección o nueva observación que deje ubicación sin precisar debe ser explícita y conservar el evento anterior.
- La fecha será del servidor; el origen y la autoría de captura deben coordinarse con la propuesta de auditoría Identity.

## Validación posterior propuesta

Alta con sede sola; alta con sede/ubicación coherentes; rechazo de ubicación de otra sede; lectura de históricos sin SiteId nuevo; edición sin pérdida de ubicaciones; observaciones sucesivas sin atribuir custodia. Revisar duplicados y las claves antes del SQL definitivo.

No se generó código EF pendiente, para no bloquear la Etapa A de fotografías con una migración todavía no autorizada. El SQL y la aplicación en test-migracion-inicial requieren una revisión y autorización independientes.
