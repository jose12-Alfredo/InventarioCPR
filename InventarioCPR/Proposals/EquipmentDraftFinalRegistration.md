# Requisito aprobado: Borrador y Definitiva con fotografía real

Estado actualizado (2026-11-13): **implementado y validado con Neon/Cloudinary reales**. Migración `20261112200209_EquipmentDraftFinalization` aplicada con autorización únicamente a `test-migracion-inicial`. La descripción siguiente conserva el origen del diseño; estado efectivo/esquema en `../MANDATORY_PHOTO_REGISTRATION.md` y evidencia real en `../MANDATORY_PHOTO_REAL_VALIDATION.md`. Render sigue sin actualizarse con esta versión.

Cada equipo nuevo debe tener al menos una fotografía real confirmada para finalizar su registro. Una imagen de ejemplo, miniatura placeholder, selección local, reserva de subida o transferencia fallida no cumple la regla. Los equipos existentes se conservan; no se inventan fotos ni se reinterpretan automáticamente como borradores.

## Estados y transición propuesta

- **Borrador**: equipo nuevo con identidad estable para permitir la subida privada. Puede guardar los datos mínimos ya validados y continuar después. No equivale a un alta definitiva ni debe contabilizarse como tal en indicadores/exportaciones de registros definitivos.
- **Definitiva**: datos mínimos válidos y al menos una evidencia confirmada del mismo equipo, con almacenamiento remoto validado por el servicio existente. Especificar en la etapa de implementación los campos de auditoría de finalización y el tratamiento explícito de registros legacy.
- `Finalizar registro`: comando de servidor autorizado, idempotente, con control de concurrencia. Comprueba evidencia persistida y elegible en la misma transacción que cambia el estado; nunca confiar en un contador enviado por el navegador. La subida remota no se integra en una transacción distribuida: se aprovecha la confirmación persistida y recuperación de Etapa A.

## Flujo propuesto

Guardar datos → crear/actualizar borrador → subir imagen real con el servicio privado actual → recibir confirmación de evidencia → habilitar Finalizar → comprobar otra vez en servidor → cerrar como definitiva. Conservar ID, datos y cola ante errores. Reanudar un borrador no debe duplicar el equipo ni el historial. Las futuras pantallas deberán ser responsive y pasar Impeccable antes de implementarse.

## Reglas a implementar en una etapa posterior

- Bloquear finalizar sin evidencia confirmada, incluso por llamada directa al servicio, bajo concurrencia o tras recargar.
- Evitar que un registro definitivo pierda su última foto elegible mediante una operación posterior; reemplazar/confirmar primero. No borrar evidencia histórica para cumplir esa regla.
- Mantener autorización actual y validación de acceso al equipo/foto. Separar estado del registro de condición física/operativa y de disponibilidad de datos técnicos.
- Fijar la estrategia de backfill legacy, índices, eventos y consultas antes de generar una migración específica. No mezclar su historial con la migración de `security`.
- Probar fallo remoto, fallo de confirmación DB, retry, finalización duplicada, foto de otro equipo, evidencia inválida, carreras entre quitar foto/finalizar y preservación de equipos anteriores.

El formulario actualizado guarda sin foto únicamente como Borrador; no permite finalizar sin principal confirmada. El flujo ya está implementado y validado realmente en la rama autorizada. La versión previamente publicada sigue sin este cambio hasta autorizar y realizar su publicación; no se presenta esa versión antigua como cumplimiento de la nueva regla.
