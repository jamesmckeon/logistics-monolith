--liquibase formatted sql

--changeset jamesmckeon:add-delivery-submissions-created-by-column
ALTER TABLE receiving.delivery_submissions ADD COLUMN created_by uuid;
