--liquibase formatted sql

--changeset jamesmckeon:add-sku-receipts-created-by-column
ALTER TABLE inventory.sku_receipts ADD COLUMN created_by uuid;
