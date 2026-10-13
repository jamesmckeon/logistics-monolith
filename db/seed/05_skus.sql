-- Seeds the SKU catalog of each owner in 04_owners.sql into both modules that keep one: receiving.skus and
-- inventory.skus. A SKU missing from Inventory makes every order or receipt that names it retry and then
-- dead-letter, so both tables are filled from the one catalog below, in one statement.
--
-- Safe to re-run: a SKU already present, matched on owner and code, keeps its id and takes the tracking flags
-- given here. SKUs removed from this file are not deleted from the database. Codes must be upper case.
--
-- sku_id is derived from the owner and code, so a SKU has the same id in both tables and in every environment.
--
-- Each owner has ten named SKUs for manual requests, and a numbered block to give the catalog a realistic size:
--
--   1  HFO-000001 .. HFO-006000   no tracking
--   2  BWN-000001 .. BWN-000400   lot and expiry tracked, except every 20th (untracked accessories)
--   3  KIS-000001 .. KIS-001200   every 4th lot tracked; every 20th expiry tracked as well

WITH named (owner_id, sku_code, is_lot_tracked, is_expiration_tracked) AS (
    VALUES
        (1, 'HFO-JKT-ALPINE-BLK-M',     false, false),
        (1, 'HFO-JKT-ALPINE-BLK-L',     false, false),
        (1, 'HFO-JKT-ALPINE-NVY-M',     false, false),
        (1, 'HFO-FLC-RIDGE-GRY-M',      false, false),
        (1, 'HFO-FLC-RIDGE-GRY-L',      false, false),
        (1, 'HFO-BOOT-TRAIL-BRN-10',    false, false),
        (1, 'HFO-BOOT-TRAIL-BRN-11',    false, false),
        (1, 'HFO-PANT-TREK-KHK-32',     false, false),
        (1, 'HFO-SOCK-MERINO-GRY-L',    false, false),
        (1, 'HFO-HAT-BEANIE-RED-OS',    false, false),

        (2, 'BWN-WHEY-VAN-2LB',         true,  true),
        (2, 'BWN-WHEY-CHOC-2LB',        true,  true),
        (2, 'BWN-WHEY-CHOC-5LB',        true,  true),
        (2, 'BWN-CREATINE-300G',        true,  true),
        (2, 'BWN-PREWORKOUT-BRY-30SV',  true,  true),
        (2, 'BWN-MULTIVIT-90CT',        true,  true),
        (2, 'BWN-OMEGA3-120CT',         true,  true),
        (2, 'BWN-BAR-PB-12PK',          true,  true),
        (2, 'BWN-SHAKER-24OZ',          false, false),
        (2, 'BWN-TEE-LOGO-L',           false, false),

        (3, 'KIS-BOLT-M8X40-ZN',        false, false),
        (3, 'KIS-BOLT-M10X60-ZN',       false, false),
        (3, 'KIS-NUT-M8-ZN',            false, false),
        (3, 'KIS-WASHER-M8-SS',         false, false),
        (3, 'KIS-BEARING-6204-2RS',     false, false),
        (3, 'KIS-VBELT-A42',            false, false),
        (3, 'KIS-GREASE-EP2-400G',      true,  false),
        (3, 'KIS-OIL-HYD46-20L',        true,  false),
        (3, 'KIS-SEALANT-RTV-300ML',    true,  true),
        (3, 'KIS-ADHESIVE-EPX-50ML',    true,  true)
),
numbered (owner_id, sku_code, is_lot_tracked, is_expiration_tracked) AS (
    SELECT 1, 'HFO-' || lpad(n::text, 6, '0'), false, false
    FROM generate_series(1, 6000) AS n
    UNION ALL
    SELECT 2, 'BWN-' || lpad(n::text, 6, '0'), n % 20 <> 0, n % 20 <> 0
    FROM generate_series(1, 400) AS n
    UNION ALL
    SELECT 3, 'KIS-' || lpad(n::text, 6, '0'), n % 4 = 0, n % 20 = 0
    FROM generate_series(1, 1200) AS n
),
catalog AS (
    SELECT md5(owner_id::text || ':' || sku_code)::uuid AS sku_id,
           owner_id,
           sku_code,
           is_lot_tracked,
           is_expiration_tracked
    FROM (SELECT * FROM named UNION ALL SELECT * FROM numbered) AS skus
),
receiving_skus AS (
    INSERT INTO receiving.skus (sku_id, owner_id, sku_code, is_lot_tracked, is_expiration_tracked)
    SELECT sku_id, owner_id, sku_code, is_lot_tracked, is_expiration_tracked
    FROM catalog
    ON CONFLICT (owner_id, sku_code) DO UPDATE
        SET is_lot_tracked = EXCLUDED.is_lot_tracked,
            is_expiration_tracked = EXCLUDED.is_expiration_tracked
)
INSERT INTO inventory.skus (sku_id, owner_id, code)
SELECT sku_id, owner_id, sku_code
FROM catalog
ON CONFLICT (owner_id, code) DO NOTHING;
