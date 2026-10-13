-- Seeds Receiving's hold reasons (receiving.hold_reasons). One warehouse-wide list, shared by every owner.
--
-- Safe to re-run: a reason already present takes the active flag given here. Reasons removed from this file
-- are not deleted from the database. Codes must be upper case.
--
-- WATER_DAMAGE is inactive on purpose, so a receipt that uses a retired reason can be tested.

BEGIN;

INSERT INTO receiving.hold_reasons (reason_code, is_active)
VALUES
    ('DAMAGED',            true),
    ('EXPIRED',            true),
    ('OWNER_REVIEW',       true),
    ('QUALITY_INSPECTION', true),
    ('WATER_DAMAGE',       false)
ON CONFLICT (reason_code) DO UPDATE
    SET is_active = EXCLUDED.is_active;

COMMIT;
