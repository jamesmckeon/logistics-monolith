--liquibase formatted sql

--changeset jamesmckeon:add-orders-created-by-column
ALTER TABLE orders.orders ADD COLUMN created_by uuid;
