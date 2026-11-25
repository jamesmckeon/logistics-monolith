--liquibase formatted sql

--changeset jamesmckeon:make-skus-pallet-quantity-required
ALTER TABLE master_data.skus ALTER COLUMN pallet_quantity SET NOT NULL;