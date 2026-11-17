# Incidente histórico de configuración Render — 13 de noviembre de 2026

**Actualización 2026-11-17:** C definitivo ya existe, A/B están custodiados y la URL pública responde healthz204/login200/bootBlazor200. El usuario realizó la importación de configuración en una fase anterior; esta auditoría no accedió al dashboard para recertificar sus valores efectivos. Las instrucciones originales siguientes documentan el incidente, **no una orden pendiente de volver a importar o desplegar**. La actualización actual y la comprobación previa de variables están en `DEPLOYMENT.md` y `RELEASE_READINESS.md`. No generar otro certificado por rutina.

El código fue subido a `main` (publicación inicial `6705a08`), pero el nuevo despliegue rechazó el arranque con `InvalidOperationException: Render requiere DataProtection:UseDatabase=true y certificado de protección`. Render mantiene sirviendo la versión anterior. Un push no configura los secretos del servicio.

## Corrección de configuración preparada

Se preparó **fuera del repositorio**, en una carpeta con ACL restringida al usuario local:

`C:\Users\jose1\AppData\Local\Temp\opencode\render-config\InventarioCPR.Render.env`

Contiene siete variables: habilitación de Data Protection PostgreSQL, certificado activo definitivo y contraseña, más los dos certificados históricos y sus contraseñas. No contiene la contraseña PostgreSQL ni credenciales Cloudinary y no las cambia. No subir ese archivo a GitHub ni compartir su contenido por chat.

El certificado definitivo nuevo es RSA 3072, exclusivo de protección de claves de Render, válido hasta el 13 de noviembre de 2028. No es el certificado HTTPS de desarrollo ni un certificado de pruebas. Tiene respaldo local en User Secrets `InventarioCPR-local`, bajo `RenderDeployment:DataProtection:Certificate:Base64` y `RenderDeployment:DataProtection:Certificate:Password`. Su creación no activa automáticamente el proveedor de la aplicación ni configura Render.

Validación realizada: carga del certificado con privada, cifrado/descifrado entre proveedores nuevos sobre un llavero temporal local y lectura/descifrado de las **dos claves históricas de Neon** con los certificados de lectura. Conexión de prueba confirmada por ID/endpoint; no hay migraciones Data Protection pendientes. **Cero escrituras remotas de claves**, ninguna contraseña existente cambiada. No se retiró el rechazo de arranque ante configuración incompleta.

## Acción manual original del incidente (solo si la comparación actual detecta faltantes)

1. Abrir Render en el navegador normal, servicio **InventarioCPR → Environment → Add from .env**.
2. Importar el archivo privado indicado. Si el diálogo pide texto, abrirlo localmente y pegar su contenido solo en ese diálogo de Render. No copiarlo al repositorio ni al chat.
3. Confirmar las siete variables y pulsar **Save, rebuild, and deploy**. Usar Base64 como fuente: no combinar esas variables con `DataProtection__Certificate__Path` o rutas `PreviousCertificates__*__Path` preexistentes.
4. Comprobar que el despliegue pasa a **Live** y luego recargar `/inventario`. Hasta completar este paso no se afirma que la versión nueva esté sirviéndose en Render.

No hacen falta cambios de código ni contraseñas nuevas de Neon/Cloudinary para resolver el error mostrado. La conexión `ConnectionStrings__Database` existente debe seguir en la rama autorizada y las opciones de fotografías/escritura previamente habilitadas se conservan.

## Borrador creado por el binario anterior

La revisión solo de lectura detectó un registro `Draft` con `DraftPayload` nulo, creado por el binario anterior después de migrar el esquema. No tenía historia técnica. La pantalla antigua no distingue el nuevo estado y por eso puede mostrarlo entre equipos guardados.

Se reforzó la lectura de esos borradores: se recuperan categoría, serie, datos técnicos e inspector desde sus hechos originales, sin reemplazarlos por valores genéricos. Se conserva su revisión técnica al finalizar. Prueba local en PostgreSQL desechable con el INSERT del runtime anterior, módulos de RAM, inspección y nota: pasa al estado definitivo tras subir foto sin duplicar ni perder hechos. **10 pruebas de registro obligatorio aprobadas, 0 fallidas**. No se convirtió ni modificó el registro real para realizar esa prueba; tampoco se necesita otra migración.

## Cómo subir fotografías

Versión anterior todavía visible: **Ver expediente → Evidencias → Seleccionar varias fotos / Tomar foto → Subir fotografías / reintentar**; esperar confirmación y usar **Usar como principal** cuando corresponda. Guardar la ficha por sí solo no sube archivos.

Versión nueva: abrir/retomar **Borradores**, completar datos, abrir fotografías, subir y confirmar principal; después **Finalizar registro**. Guardar un borrador sin foto es válido; finalizarlo sin foto no lo es.
