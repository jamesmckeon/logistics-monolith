--liquibase formatted sql

--changeset jamesmckeon:add-delivery-receipts-created-by-column
ALTER TABLE receiving.delivery_receipts ADD COLUMN created_by uuid;
