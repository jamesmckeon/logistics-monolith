--liquibase formatted sql

--changeset jamesmckeon:make-skus-product-line-required
ALTER TABLE master_data.skus ALTER COLUMN product_line SET NOT NULL;