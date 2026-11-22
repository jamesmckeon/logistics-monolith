--liquibase formatted sql

--changeset jamesmckeon:add-order-allocations-updated-by-column
ALTER TABLE inventory.order_allocations ADD COLUMN updated_by uuid;
