-- Seeds Inventory's owners (inventory.owners).
--
-- Safe to re-run: an owner already present takes the name and allocation policy given here. Owners removed
-- from this file are not deleted from the database.
--
--   1  Harrow & Finch Outfitters   direct-to-consumer outdoor apparel   SKU prefix HFO
--   2  Brightwell Nutrition        supplements sold to retailers        SKU prefix BWN
--   3  Kestrel Industrial Supply   maintenance parts on consignment     SKU prefix KIS
--
-- A caller needs the Owner.{id} role for an owner before any request naming it is allowed.

BEGIN;

INSERT INTO inventory.owners (id, name, allocation_policy)
VALUES
    (1, 'Harrow & Finch Outfitters', 'Partial'),
    (2, 'Brightwell Nutrition',      'ShipComplete'),
    (3, 'Kestrel Industrial Supply', 'Partial')
ON CONFLICT (id) DO UPDATE
    SET name = EXCLUDED.name,
        allocation_policy = EXCLUDED.allocation_policy;

COMMIT;
