--liquibase formatted sql

--changeset jamesmckeon:add-orderline-allocations-updated-by-column
ALTER TABLE inventory.orderline_allocations ADD COLUMN updated_by uuid;
