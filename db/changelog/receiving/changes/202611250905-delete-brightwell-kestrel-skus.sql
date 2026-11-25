--liquibase formatted sql

--changeset jamesmckeon:delete-brightwell-kestrel-skus
--preconditions onFail:HALT onError:HALT
--precondition-sql-check expectedResult:0 SELECT (SELECT count(*) FROM orders.orders WHERE owner_id IN (2, 3)) + (SELECT count(*) FROM receiving.delivery_receipts WHERE owner_id IN (2, 3)) + (SELECT count(*) FROM inventory.order_allocations WHERE owner_id IN (2, 3)) + (SELECT count(*) FROM inventory.sku_receipts r JOIN inventory.skus s ON s.sku_id = r.sku_id WHERE s.owner_id IN (2, 3))
DELETE FROM receiving.skus WHERE owner_id IN (2,3);