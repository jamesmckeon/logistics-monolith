--liquibase formatted sql

-- Baseline: the inventory schema as it stood when schema changes moved from EF Core migrations
-- to Liquibase. Production was marked as already having these changesets (changelog-sync);
-- they only run against a new, empty database.

--changeset jamesmckeon:create-order-allocations-table
CREATE TABLE inventory.order_allocations (
    order_id uuid NOT NULL,
    owner_id integer NOT NULL,
    last_updated timestamp with time zone,
    allocating boolean NOT NULL,
    order_created_on timestamp with time zone NOT NULL,
    CONSTRAINT pk_order_allocations PRIMARY KEY (order_id)
);
--rollback DROP TABLE inventory.order_allocations;

--changeset jamesmckeon:create-owners-table
CREATE TABLE inventory.owners (
    id integer NOT NULL,
    allocation_policy text NOT NULL,
    name character varying(100) NOT NULL DEFAULT '',
    CONSTRAINT pk_owners PRIMARY KEY (id)
);
--rollback DROP TABLE inventory.owners;

--changeset jamesmckeon:create-skus-table
CREATE TABLE inventory.skus (
    sku_id uuid NOT NULL,
    code character varying(50) NOT NULL,
    owner_id integer NOT NULL,
    CONSTRAINT pk_skus PRIMARY KEY (sku_id)
);
--rollback DROP TABLE inventory.skus;

--changeset jamesmckeon:create-orderline-allocations-table
CREATE TABLE inventory.orderline_allocations (
    orderline_allocation_id uuid NOT NULL,
    sku_id uuid NOT NULL,
    quantity_requested integer NOT NULL,
    quantity_allocated integer NOT NULL,
    quantity_short integer NOT NULL,
    last_updated timestamp with time zone,
    order_id uuid NOT NULL,
    CONSTRAINT pk_orderline_allocations PRIMARY KEY (orderline_allocation_id)
);
--rollback DROP TABLE inventory.orderline_allocations;

--changeset jamesmckeon:create-sku-receipts-table
CREATE TABLE inventory.sku_receipts (
    sku_receipt_id uuid NOT NULL,
    sku_id uuid NOT NULL,
    quantity_received integer NOT NULL,
    quantity_allocated integer NOT NULL,
    received_on timestamp with time zone NOT NULL,
    last_updated timestamp with time zone,
    quantity_available integer NOT NULL,
    CONSTRAINT pk_sku_receipts PRIMARY KEY (sku_receipt_id)
);
--rollback DROP TABLE inventory.sku_receipts;

--changeset jamesmckeon:create-receipt-allocations-table
CREATE TABLE inventory.receipt_allocations (
    receipt_allocation_id uuid NOT NULL,
    sku_receipt_id uuid NOT NULL,
    quantity_allocated integer NOT NULL,
    allocated_on timestamp with time zone NOT NULL,
    orderline_allocation_id uuid NOT NULL,
    CONSTRAINT pk_receipt_allocations PRIMARY KEY (receipt_allocation_id)
);
--rollback DROP TABLE inventory.receipt_allocations;

--changeset jamesmckeon:add-orderline-allocations-order-allocations-fk
ALTER TABLE inventory.orderline_allocations
    ADD CONSTRAINT fk_orderline_allocations_order_allocations_order_id FOREIGN KEY (order_id)
        REFERENCES inventory.order_allocations (order_id) ON DELETE CASCADE;
--rollback ALTER TABLE inventory.orderline_allocations DROP CONSTRAINT fk_orderline_allocations_order_allocations_order_id;

--changeset jamesmckeon:add-sku-receipts-skus-fk
ALTER TABLE inventory.sku_receipts
    ADD CONSTRAINT fk_sku_receipts_skus_sku_id FOREIGN KEY (sku_id)
        REFERENCES inventory.skus (sku_id) ON DELETE CASCADE;
--rollback ALTER TABLE inventory.sku_receipts DROP CONSTRAINT fk_sku_receipts_skus_sku_id;

--changeset jamesmckeon:add-receipt-allocations-orderline-allocations-fk
--comment: The constraint name is cut at Postgres's 63-character limit, as EF Core created it
ALTER TABLE inventory.receipt_allocations
    ADD CONSTRAINT fk_receipt_allocations_orderline_allocations_orderline_allocat FOREIGN KEY (orderline_allocation_id)
        REFERENCES inventory.orderline_allocations (orderline_allocation_id) ON DELETE CASCADE;
--rollback ALTER TABLE inventory.receipt_allocations DROP CONSTRAINT fk_receipt_allocations_orderline_allocations_orderline_allocat;

--changeset jamesmckeon:create-order-allocations-owner-id-index
CREATE INDEX ix_order_allocations_owner_id ON inventory.order_allocations (owner_id);
--rollback DROP INDEX inventory.ix_order_allocations_owner_id;

--changeset jamesmckeon:create-order-allocations-worklist-index
CREATE INDEX ix_order_allocations_owner_id_order_created_on_order_id
    ON inventory.order_allocations (owner_id, order_created_on, order_id);
--rollback DROP INDEX inventory.ix_order_allocations_owner_id_order_created_on_order_id;

--changeset jamesmckeon:create-orderline-allocations-order-id-index
CREATE INDEX ix_orderline_allocations_order_id ON inventory.orderline_allocations (order_id);
--rollback DROP INDEX inventory.ix_orderline_allocations_order_id;

--changeset jamesmckeon:create-receipt-allocations-orderline-allocation-id-index
CREATE INDEX ix_receipt_allocations_orderline_allocation_id ON inventory.receipt_allocations (orderline_allocation_id);
--rollback DROP INDEX inventory.ix_receipt_allocations_orderline_allocation_id;

--changeset jamesmckeon:create-receipt-allocations-sku-receipt-id-index
CREATE INDEX ix_receipt_allocations_sku_receipt_id ON inventory.receipt_allocations (sku_receipt_id);
--rollback DROP INDEX inventory.ix_receipt_allocations_sku_receipt_id;

--changeset jamesmckeon:create-sku-receipts-received-on-index
CREATE INDEX ix_sku_receipts_received_on ON inventory.sku_receipts (received_on);
--rollback DROP INDEX inventory.ix_sku_receipts_received_on;

--changeset jamesmckeon:create-sku-receipts-sku-id-index
CREATE INDEX ix_sku_receipts_sku_id ON inventory.sku_receipts (sku_id);
--rollback DROP INDEX inventory.ix_sku_receipts_sku_id;

--changeset jamesmckeon:create-skus-owner-id-code-unique-index
CREATE UNIQUE INDEX ix_skus_owner_id_code ON inventory.skus (owner_id, code);
--rollback DROP INDEX inventory.ix_skus_owner_id_code;

--changeset jamesmckeon:create-sku-reader-role splitStatements:false
--comment: Group role the simulator's login belongs to, to read the SKU catalog. No login or password, so it can live here; roles are server-wide, so it's only created if missing.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sku_reader') THEN
        CREATE ROLE sku_reader NOLOGIN;
    END IF;
END $$;
--rollback DROP ROLE IF EXISTS sku_reader;

--changeset jamesmckeon:grant-sku-reader-inventory-usage
GRANT USAGE ON SCHEMA inventory TO sku_reader;
--rollback REVOKE USAGE ON SCHEMA inventory FROM sku_reader;

--changeset jamesmckeon:grant-sku-reader-skus-select
GRANT SELECT ON inventory.skus TO sku_reader;
--rollback REVOKE SELECT ON inventory.skus FROM sku_reader;
