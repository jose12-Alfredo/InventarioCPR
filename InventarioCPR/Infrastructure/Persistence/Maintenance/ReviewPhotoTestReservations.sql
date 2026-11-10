-- PROPUESTA DE MANTENIMIENTO. NO EJECUTADA.
-- Solo test-migracion-inicial / br-flat-smoke-b4kdmosq.
-- Endpoint aprobado: ep-raspy-dream-b476uukh-pooler.c-6.us-east-2.aws.neon.tech.
-- No es una migración; no ejecutar al iniciar ni en production.
-- Precondiciones externas: verificar el HOST real con el factory de pruebas,
-- detener las fuentes de subida a esta rama y repetir el GET authenticated
-- de los dos PublicId derivados: ambos deben seguir devolviendo 404.
-- El nombre neondb y el marcador siguiente NO identifican por sí solos una rama.
-- El marcador queda comentado y la transacción termina en ROLLBACK por defecto.

BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';

-- Activar únicamente después de autorización y validación externa del destino:
-- SET LOCAL inventariocpr.maintenance_branch = 'br-flat-smoke-b4kdmosq';

DO $review$
DECLARE
    target_equipment constant uuid := '337728c5-15e4-48bc-83ba-526f69266fdd';
    eligible integer;
    removed integer;
    photos_before jsonb;
    periods_before jsonb;
    photos_after jsonb;
    periods_after jsonb;
BEGIN
    IF current_database() <> 'neondb'
       OR current_setting('inventariocpr.maintenance_branch', true) IS DISTINCT FROM 'br-flat-smoke-b4kdmosq' THEN
        RAISE EXCEPTION 'Mantenimiento no autorizado o destino sin confirmar; no se elimina ninguna reserva.';
    END IF;

    PERFORM 1 FROM public.equipment
    WHERE "Id" = target_equipment AND "Code" = 'VALIDA_FOTOS_A_MW9RWNV6'
    FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'No coincide el equipo ficticio autorizado.'; END IF;

    PERFORM 1 FROM public.photo_uploads
    WHERE "Id" IN ('0b0aa24b-8459-40ca-856d-58e9d8ad53e1'::uuid,
                   '474a5b57-f27d-471a-9a6c-70a619dcc7b8'::uuid)
    FOR UPDATE;

    SELECT count(*) INTO eligible
    FROM public.photo_uploads u
    WHERE u."EquipmentId" = target_equipment
      AND ((u."Id" = '0b0aa24b-8459-40ca-856d-58e9d8ad53e1'::uuid
            AND u."FileName" = 'PRUEBA_invalida.jpg'
            AND u."CreatedAtUtc" = '2026-11-09T21:29:57.939500Z'::timestamptz)
        OR (u."Id" = '474a5b57-f27d-471a-9a6c-70a619dcc7b8'::uuid
            AND u."FileName" = 'PRUEBA_excesiva.png'
            AND u."CreatedAtUtc" = '2026-11-09T21:30:00.909010Z'::timestamptz))
      AND u."Sha256" IS NULL AND u."Format" IS NULL
      AND u."Width" IS NULL AND u."Height" IS NULL AND u."ByteLength" IS NULL
      AND u."EvidenceId" IS NULL AND u."CompletedAtUtc" IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM public.photo_evidence e
          WHERE e."Id" = u."Id" OR e."CloudinaryPublicId" =
            'inventario/equipos/' || replace(u."EquipmentId"::text, '-', '') || '/' || replace(u."Id"::text, '-', '')
      );
    IF eligible <> 2 THEN
        RAISE EXCEPTION 'Las dos reservas ya no coinciden exactamente con el estado revisado.';
    END IF;

    SELECT jsonb_agg(to_jsonb(e) ORDER BY e."Id") INTO photos_before
      FROM public.photo_evidence e WHERE e."EquipmentId" = target_equipment;
    SELECT jsonb_agg(to_jsonb(p) ORDER BY p."Id") INTO periods_before
      FROM public.photo_principal_periods p WHERE p."EquipmentId" = target_equipment;
    IF coalesce(jsonb_array_length(photos_before), 0) < 6 THEN
        RAISE EXCEPTION 'No están presentes las seis evidencias confirmadas; detener revisión.';
    END IF;

    DELETE FROM public.photo_uploads
    WHERE "EquipmentId" = target_equipment
      AND "Id" IN ('0b0aa24b-8459-40ca-856d-58e9d8ad53e1'::uuid,
                   '474a5b57-f27d-471a-9a6c-70a619dcc7b8'::uuid)
      AND "Sha256" IS NULL AND "EvidenceId" IS NULL AND "CompletedAtUtc" IS NULL;
    GET DIAGNOSTICS removed = ROW_COUNT;
    IF removed <> 2 THEN RAISE EXCEPTION 'Cantidad inesperada; se revierte toda la operación.'; END IF;

    SELECT jsonb_agg(to_jsonb(e) ORDER BY e."Id") INTO photos_after
      FROM public.photo_evidence e WHERE e."EquipmentId" = target_equipment;
    SELECT jsonb_agg(to_jsonb(p) ORDER BY p."Id") INTO periods_after
      FROM public.photo_principal_periods p WHERE p."EquipmentId" = target_equipment;
    IF photos_before IS DISTINCT FROM photos_after OR periods_before IS DISTINCT FROM periods_after THEN
        RAISE EXCEPTION 'Cambió una evidencia o su historial; se revierte la limpieza.';
    END IF;
    RAISE NOTICE 'Dos reservas de prueba retiradas dentro de la transacción; evidencias e historial intactos. Revisar ROLLBACK/COMMIT autorizado.';
END
$review$;

ROLLBACK;
-- No sustituir por COMMIT sin autorización explícita y sin repetir los preflights.
-- Este script NO borra recursos de Cloudinary ni modifica photo_evidence o photo_principal_periods.
