--liquibase formatted sql

-- Every SKU now has a carton and a pallet quantity, in units (eaches), and a pallet holds a whole number
-- of cartons. This corrects the table's original description, which said either quantity could be null.

--changeset jamesmckeon:make-skus-carton-quantity-required
ALTER TABLE master_data.skus ALTER COLUMN carton_quantity SET NOT NULL;