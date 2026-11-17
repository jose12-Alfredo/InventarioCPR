# Revisión sencilla de los 46 registros anteriores

Preparación de solo lectura: **2026-11-17**. No se guardaron empresas, responsables ni confirmaciones físicas en los equipos existentes.

## Hoja preparada

Abrir en Excel/LibreOffice, como CSV UTF-8 con separador **punto y coma**:

`C:\Users\jose1\AppData\Local\Temp\opencode\assignments-neon-validation\conciliacion-46-equipos.csv`

Contiene exactamente **46 filas**, seleccionadas por los IDs del baseline anterior a la validación de asignaciones. No son «los primeros 46 equipos» ni una selección por paginación. ID, código original, nombre y categoría ya están informados. Las columnas de propietario, usuaria, área administrativa, custodio, confirmación física, fecha y observaciones se entregan **en blanco** para su revisión. Es un apoyo de trabajo; **no existe importador automático de este CSV** y abrirlo/editarlo no cambia Neon.

### Separar pruebas anteriores de activos reales

**41 de las 46 filas tienen códigos `PRUEBA_*`, `REG_SIMPLE_*` o `VALIDA_*` de fases anteriores**, señalados como «Revisar prueba anterior». Los otros cinco códigos son `EQ_1`, `EQ_2`, `EQ_3`, `EQ_4` y `EQ-5`; tampoco se certifican como empresariales solo por su código. Confirmar físicamente/administrativamente cuáles son reales antes de asignar empresas o custodios. No borrar filas del sistema ni inventar una conciliación de activos que eran pruebas.

Los nueve fixtures de la última fase están excluidos de esta hoja. Su eventual retiro se trata en `TEST_DATA_RETIREMENT.md`; la revisión de estas 41 pruebas anteriores no amplía ese alcance.

## 1. Preparar empresas desde la interfaz existente

Ruta: **Asignaciones → Empresas, personal y áreas → Catálogo: Empresas → Nueva entrada**.

- El nombre visible es texto libre obligatorio (máximo 160 caracteres); no hay una lista codificada de empresas.
- Puede registrar **CPRSA, DMA y MAA** cuando confirme el nombre que debe mostrarse. No requiere cambiar código, migraciones ni desplegar otra versión.
- Razón social e identificador fiscal son **opcionales**. Dejarlos vacíos si no están confirmados; no derivarlos de siglas, ubicación o departamento.
- Se impiden nombres duplicados normalizados por espacios extremos y mayúsculas. Reutilizar la entrada existente por ID, no crear variantes para el mismo nombre.
- Administrador/Sistemas guardan; Consulta solo lee. Actividad y cambios quedan auditados.

Comprobado en `DirectoryEditor.razor`, `DirectoryService.SaveDirectoryAsync` y el mapping `companies`; el formulario/catálogo ya pasó la validación web previa. **No se guardaron esas tres denominaciones ni identificadores en esta preparación.**

## 2. Preparar áreas y trabajadores confirmados

En el mismo catálogo: revisar Departamento → Área; luego Trabajadores. Reutilizar registros existentes por ID y confirmar homónimos/código de personal. Para recibir equipos, la persona debe estar activa, tener área/departamento activos y «Habilitar para recibir equipos». No necesita usuario Identity.

**Inspector/verificador anterior no equivale a responsable actual.** La hoja no precarga al inspector como custodio. No habilitar todas las personas antiguas de forma masiva. El área del trabajador y el área administrativa del activo pueden ser diferentes.

## 3. Revisar un equipo, sin mezclar dimensiones

1. Buscar por código en **Inventario**. Abrir el expediente amplio, comprobar ID/código/foto y acceder a **Asignaciones → Asignar / conciliar**. También puede abrir `/asignaciones?equipo=<IdOriginal>`.
2. En «Clasificar empresa propietaria, empresa usuaria y área del activo», guardar solo lo confirmado. Propietaria y usuaria se eligen de forma independiente; ninguna cambia automáticamente responsable o ubicación. Una relación desconocida puede quedar sin registrar, con observación de seguimiento; no adivinarla.
3. Si se comprobó que ya tiene custodio: usar **Conciliar custodia existente**, elegir la persona verificada y modalidad individual o compartida. En compartida, seleccionar oficina y **un solo custodio principal**. Anotar su ID/nombre verificados en la hoja y `SinCustodiaConfirmadoFisicamente_SI_NO=NO`; dejarlo en blanco si aún no se ha comprobado la situación.
4. La confirmación digital usa la hora actual del servidor. Marcar «Conozco la fecha original de entrega» solamente con evidencia; si se desconoce, dejarla sin informar. No registrar una entrega nueva ni una inspección pasada ficticia para completar campos.
5. Si se comprobó físicamente que no tiene custodia: usar **Confirmar libre físicamente**, con observación de revisión. Completar `SinCustodiaConfirmadoFisicamente_SI_NO=SI` en la hoja solamente después de esa comprobación.
6. Si no se conoce la situación: no ejecutar ninguna confirmación. Mantener conciliación pendiente y registrar qué falta en la hoja. No crear un responsable genérico ni un tercer estado de disponibilidad.
7. Anotar el ID de operación, quién revisó y fecha UTC en la hoja. Reabrir el equipo y comprobar propietario/usuaria/área, historia y responsable. Si otra sesión cambió el equipo, actualizar y revisar, no forzar una revisión antigua.

La ruta mostrada es la implementación actual; no se añadió otro formulario ni flujo paralelo. La hoja no aplica cambios ni sustituye los servicios auditados.

## 4. Significado de Libre/Ocupado

- **Libre:** no hay custodia principal abierta registrada.
- **Ocupado:** hay custodia principal abierta, individual o compartida.
- «Conciliación pendiente» es un aviso administrativo separado, **no disponibilidad**.

En la auditoría, los 46 registros no tenían custodia y seguían marcados pendientes. Su Libre digital **no acredita** disponibilidad física. Un responsable desconocido no se convierte en Libre físicamente confirmado. Condición operativa y disponibilidad también son independientes.

## 5. Comprobación de cierre

Por cada activo real revisado: relaciones confirmadas o pendientes documentadas, custodia comprobada o ausencia comprobada, operación conservada y fotografía/historia técnica anteriores intactas. En cada ámbito: **Libres + Ocupados = Total de definitivos**, antes de paginar. Los borradores quedan excluidos.

No es necesario «conciliar» las pruebas para que desaparezca un contador: primero decidir qué datos representan activos empresariales. La finalización de la revisión física depende del usuario y no se afirma completada por exportar esta hoja.
