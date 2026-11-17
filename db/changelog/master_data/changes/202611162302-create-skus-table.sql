--liquibase formatted sql

-- The item master: the source of truth for each SKU and its attributes. Modules keep their own copies,
-- such as receiving.skus and inventory.skus, with the fields they need. sku_id matches those copies.
--
-- Quantities are in units (eaches). Either can be null: some SKUs only ever arrive as cartons, and a
-- few aren't packed in cartons. When both are set, a pallet holds a whole number of cartons.

--changeset jamesmckeon:create-master-data-skus-table
CREATE TABLE master_data.skus (
    sku_id uuid NOT NULL,
    owner_id integer NOT NULL,
    code character varying(50) NOT NULL,
    product_line character varying(20),
    is_lot_tracked boolean NOT NULL,
    is_expiration_tracked boolean NOT NULL,
    carton_quantity integer,
    pallet_quantity integer,
    CONSTRAINT pk_skus PRIMARY KEY (sku_id),
    CONSTRAINT uq_skus_owner_id_code UNIQUE (owner_id, code),
    CONSTRAINT fk_skus_owners_owner_id FOREIGN KEY (owner_id)
        REFERENCES master_data.owners (id),
    CONSTRAINT fk_skus_product_lines FOREIGN KEY (owner_id, product_line)
        REFERENCES master_data.product_lines (owner_id, code),
    CONSTRAINT ck_skus_carton_quantity_positive CHECK (carton_quantity > 0),
    CONSTRAINT ck_skus_pallet_quantity_positive CHECK (pallet_quantity > 0),
    CONSTRAINT ck_skus_pallet_holds_whole_cartons CHECK (
        carton_quantity IS NULL
        OR pallet_quantity IS NULL
        OR (pallet_quantity > carton_quantity AND pallet_quantity % carton_quantity = 0))
);
--rollback DROP TABLE master_data.skus;
