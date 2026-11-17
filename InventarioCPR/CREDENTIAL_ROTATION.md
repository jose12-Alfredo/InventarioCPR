# Rotación de credenciales — procedimiento pendiente de autorización

Esta preparación no cambió contraseñas ni claves reales; el reset Identity histórico se distingue en el estado siguiente. No introducir secretos en chat, comandos que queden en historial, commits, capturas o logs. Preparar acceso al dashboard de Neon, Render y Cloudinary y un gestor de contraseñas; las rotaciones pendientes se acuerdan antes de actualizar públicamente Render y no se necesitan nuevas credenciales para pruebas automatizadas locales.

## Estado actualizado — 2026-11-17

- **Neon:** existe antecedente de contraseña expuesta y no hay evidencia documentada de su rotación posterior. Sigue pendiente identificar el rol/consumidores y autorizar la transición. Si el rol se comparte con production, detener el cambio hasta acordar un alcance que no la afecte; esta tarea no autoriza ese impacto.
- **Identity:** la contraseña expuesta antigua **ya se sustituyó** mediante UserManager y se verificó login por usuario/correo; ver SECURITY.md. La auditoría actual no cambia hash/stamp y confirma ausencia local de secretos de bootstrap/reset. Confirmar custodia de la vigente; solo repetir el procedimiento siguiente si se detecta nueva exposición o se autoriza otro reset.
- **Cloudinary:** validar inventario de consumidores y si hubo exposición de las claves vigentes. No se afirma una rotación ya completada ni una exposición nueva demostrada. Si se requiere rotar, coordinar la coexistencia/revocación y probar lectura privada sin cambiar imágenes.
- **Data Protection:** C definitivo ya existe y está vigente hasta 2028-11-13; A/B históricos son necesarios para descifrar las dos claves actuales. No rotar ni retirar certificados como sustituto de rotar la contraseña PostgreSQL. No volver a generar C por rutina.
- Ninguna variable, contraseña, API key, certificado o credencial de Render/Neon/Cloudinary se modificó en esta preparación. La actualización segura de secretos exige intervención del titular en los dashboards y autorización de alcance.

## 1. Administrador Identity

1. Confirmar la cuenta y el destino de Identity, tener respaldo y una sesión administrativa de recuperación. No confundir `StaffMember` del inventario con una cuenta Identity.
2. La interfaz actual no incluye cambio de contraseña. En una operación local de mantenimiento autorizada, usar el `UserManager<AccessUser>` de la aplicación contra el contexto Identity correcto: `GeneratePasswordResetTokenAsync`, `ResetPasswordAsync`, comprobar `IdentityResult.Succeeded` y `UpdateSecurityStampAsync`. Recibir la contraseña mediante entrada local oculta/gestor seguro; no guardar tokens ni contraseña. No reemplazar el hash con SQL manual ni crear un segundo administrador como atajo.
3. Generar contraseña única que cumpla la política vigente (mínimo 14 caracteres, mayúsculas/minúsculas, número y símbolo). Guardar en el gestor; no modificar roles ni `IsActive`.
4. Probar login con la contraseña nueva y rechazo de la anterior evitando provocar bloqueo por intentos repetidos. Confirmar que las sesiones previas dejan de ser válidas al validar el stamp (intervalo actual: cinco minutos). No afirmar invalidación instantánea de todas las cookies.
5. La utilidad de mantenimiento y su ejecución deben revisarse/autorizarse antes de operar datos reales; esta etapa deja el procedimiento, no ejecuta el reseteo.

## 2. PostgreSQL / Neon

1. Inventariar **sin imprimir valores** los consumidores: `NeonDevelopmentApp`, `NeonMigrationTest`, `NeonDirect`, `Database` y, si se configura, `DataProtection`. Confirmar rama, rol y si el mismo rol se comparte con otro entorno. No rotar un rol compartido con producción sin autorización para ese impacto.
2. Preferir crear un rol de sustitución con mínimos permisos y transición solapada cuando Neon y la política permitan hacerlo. Si se cambia la contraseña del rol existente desde Neon, coordinar la ventana: las conexiones nuevas con el secreto anterior fallarán.
3. Guardar la credencial nueva directamente en el gestor, User Secrets local y variables seguras Render correspondientes. No editar `appsettings.json` ni pegar URI autenticadas en Git. Mantener TLS `VerifyFull` y los controles de rama actuales.
4. Verificar conexión de solo lectura primero; luego login, lectura de inventario, llavero Data Protection y, únicamente si está autorizado, alta/foto ficticia en la rama aislada. Reciclar pools/procesos para no confundir conexiones antiguas aún abiertas con validación de la contraseña nueva.
5. Revocar el rol/secreto anterior cuando todos los consumidores funcionen. Si hay que volver atrás, actualizar todos los consumidores de manera coordinada; no dejar el secreto expuesto como respaldo permanente.

## 3. Cloudinary

1. Identificar la cuenta, API key y consumidores reales. No borrar carpetas, imágenes ni cambiar `public_id`, tipo `authenticated` o configuración de entrega.
2. Crear una nueva API key/secret desde el dashboard si permite coexistencia; custodiarla en el gestor.
3. Actualizar `Cloudinary:ApiKey` / `Cloudinary:ApiSecret` en User Secrets y `Cloudinary__ApiKey` / `Cloudinary__ApiSecret` en Render. Mantener el mismo `CloudName` y carpeta autorizada. No solicitar ni transmitir los valores por chat.
4. Probar primero API de lectura y una foto privada existente; después, con autorización, subida ficticia y recuperación de su entrega privada. No registrar URLs firmadas ni secretos.
5. Revocar la API key antigua solo después de validar todos los consumidores. Confirmar rechazo de la antigua mediante una comprobación controlada sin registrar valores. No cambiar ni eliminar evidencias históricas.

## Orden antes de la actualización pública

Respaldos y accesos de recuperación → confirmación del destino y rol/consumidores → rotación PostgreSQL expuesta → Cloudinary si corresponde → confirmar contraseña Identity vigente (reset solo si procede) → verificar C+A/B y llavero existentes, sin repetir migraciones → actualización autorizada de consumidores → publicación autorizada y comprobación Render. Registrar fecha, entorno, responsable y resultado; nunca valores secretos. Para revertir, conservar credenciales seguras vigentes o generar otra sustitución autorizada; no volver a habilitar las expuestas.
