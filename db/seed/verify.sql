-- Checks the seeded reference data. Read-only; run it after the numbered files.

-- 1. What is there: row counts, per owner where the table has one.
SELECT 'receiving.carriers' AS seeded_table, NULL::integer AS owner_id, count(*) AS row_count
FROM receiving.carriers
UNION ALL
SELECT 'receiving.receiving_locations (' || location_type || ')', NULL, count(*)
FROM receiving.receiving_locations
GROUP BY location_type
UNION ALL
SELECT 'receiving.hold_reasons', NULL, count(*)
FROM receiving.hold_reasons
UNION ALL
SELECT 'inventory.owners', NULL, count(*)
FROM inventory.owners
UNION ALL
SELECT 'receiving.skus', owner_id, count(*)
FROM receiving.skus
GROUP BY owner_id
UNION ALL
SELECT 'inventory.skus', owner_id, count(*)
FROM inventory.skus
GROUP BY owner_id
ORDER BY seeded_table, owner_id;

-- 2. What must hold: every row here should report 0 problems.
SELECT 'default locations missing or of the wrong type' AS problem, count(*) AS problems
FROM (VALUES ('HOLD-01', 'Hold'), ('EXC-01', 'ReceivingException'), ('BULK-01', 'Bulk'))
         AS expected (location_id, location_type)
WHERE NOT EXISTS (SELECT 1
                  FROM receiving.receiving_locations AS l
                  WHERE l.location_id = expected.location_id
                    AND l.location_type = expected.location_type)
UNION ALL
SELECT 'Receiving SKUs with no matching Inventory SKU', count(*)
FROM receiving.skus AS r
WHERE NOT EXISTS (SELECT 1
                  FROM inventory.skus AS i
                  WHERE i.sku_id = r.sku_id AND i.owner_id = r.owner_id AND i.code = r.sku_code)
UNION ALL
SELECT 'Inventory SKUs with no matching Receiving SKU', count(*)
FROM inventory.skus AS i
WHERE NOT EXISTS (SELECT 1
                  FROM receiving.skus AS r
                  WHERE r.sku_id = i.sku_id AND r.owner_id = i.owner_id AND r.sku_code = i.code)
UNION ALL
SELECT 'SKUs whose owner is not in inventory.owners', count(*)
FROM inventory.skus AS i
WHERE NOT EXISTS (SELECT 1 FROM inventory.owners AS o WHERE o.id = i.owner_id)
UNION ALL
SELECT 'codes or ids that are not upper case', count(*)
FROM (SELECT sku_code AS value FROM receiving.skus
      UNION ALL
      SELECT code FROM inventory.skus
      UNION ALL
      SELECT location_id FROM receiving.receiving_locations
      UNION ALL
      SELECT reason_code FROM receiving.hold_reasons
      UNION ALL
      SELECT scac_code FROM receiving.carriers) AS codes
WHERE value <> upper(value);
