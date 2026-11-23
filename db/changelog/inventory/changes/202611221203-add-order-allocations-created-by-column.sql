--liquibase formatted sql

--changeset jamesmckeon:add-order-allocations-created-by-column
ALTER TABLE inventory.order_allocations ADD COLUMN created_by uuid;
