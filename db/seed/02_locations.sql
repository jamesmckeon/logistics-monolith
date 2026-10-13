-- Seeds Receiving's locations (receiving.receiving_locations). Locations are shared by every owner.
--
-- Safe to re-run: a location already present takes the type given here. Locations removed from this file are
-- not deleted from the database.
--
-- HOLD-01, EXC-01 and BULK-01 are the defaults named in Receiving:DefaultLocations (appsettings.json); a
-- receipt can't be processed unless all three exist with these types. Ids must be upper case.

BEGIN;

INSERT INTO receiving.receiving_locations (location_id, location_type)
SELECT 'BULK-' || lpad(n::text, 2, '0'), 'Bulk'
FROM generate_series(1, 40) AS n
UNION ALL
SELECT 'HOLD-' || lpad(n::text, 2, '0'), 'Hold'
FROM generate_series(1, 4) AS n
UNION ALL
SELECT 'EXC-' || lpad(n::text, 2, '0'), 'ReceivingException'
FROM generate_series(1, 2) AS n
ON CONFLICT (location_id) DO UPDATE
    SET location_type = EXCLUDED.location_type;

COMMIT;
