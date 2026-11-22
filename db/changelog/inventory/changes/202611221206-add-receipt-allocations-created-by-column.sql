--liquibase formatted sql

--changeset jamesmckeon:add-receipt-allocations-created-by-column
ALTER TABLE inventory.receipt_allocations ADD COLUMN created_by uuid;
