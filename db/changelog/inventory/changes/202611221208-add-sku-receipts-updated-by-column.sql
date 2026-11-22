--liquibase formatted sql

--changeset jamesmckeon:add-sku-receipts-updated-by-column
ALTER TABLE inventory.sku_receipts ADD COLUMN updated_by uuid;
