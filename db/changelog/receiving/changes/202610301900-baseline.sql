--liquibase formatted sql

-- Baseline: the receiving schema as it stood when schema changes moved from EF Core migrations
-- to Liquibase. Production was marked as already having these changesets (changelog-sync);
-- they only run against a new, empty database.

--changeset jamesmckeon:create-carriers-table
CREATE TABLE receiving.carriers (
    carrier_id integer NOT NULL,
    scac_code character varying(4) NOT NULL,
    carrier_name text NOT NULL,
    CONSTRAINT pk_carriers PRIMARY KEY (carrier_id)
);
--rollback DROP TABLE receiving.carriers;

--changeset jamesmckeon:create-delivery-receipts-table
CREATE TABLE receiving.delivery_receipts (
    delivery_receipt_id uuid NOT NULL,
    owner_id integer NOT NULL,
    receipt_number character varying(15) NOT NULL,
    operator_id uuid NOT NULL,
    received_on timestamp with time zone NOT NULL,
    created_on timestamp with time zone NOT NULL,
    bill_of_lading text,
    container_number text,
    shipper_name text,
    shipper_reference text NOT NULL,
    trailer_number text,
    carrier_id integer NOT NULL,
    carrier_name text NOT NULL,
    carrier_scac character varying(4) NOT NULL,
    CONSTRAINT pk_delivery_receipts PRIMARY KEY (delivery_receipt_id),
    CONSTRAINT ck_delivery_receipts_trailer_or_container CHECK (trailer_number IS NOT NULL OR container_number IS NOT NULL)
);
--rollback DROP TABLE receiving.delivery_receipts;

--changeset jamesmckeon:create-delivery-submissions-table
CREATE TABLE receiving.delivery_submissions (
    delivery_id uuid NOT NULL,
    owner_id integer NOT NULL,
    request text NOT NULL,
    result jsonb NOT NULL,
    CONSTRAINT pk_delivery_submissions PRIMARY KEY (owner_id, delivery_id)
);
--rollback DROP TABLE receiving.delivery_submissions;

--changeset jamesmckeon:create-hold-reasons-table
CREATE TABLE receiving.hold_reasons (
    reason_code text NOT NULL,
    is_active boolean NOT NULL,
    CONSTRAINT pk_hold_reasons PRIMARY KEY (reason_code)
);
--rollback DROP TABLE receiving.hold_reasons;

--changeset jamesmckeon:create-receiving-locations-table
CREATE TABLE receiving.receiving_locations (
    location_id text NOT NULL,
    location_type text NOT NULL,
    CONSTRAINT pk_receiving_locations PRIMARY KEY (location_id)
);
--rollback DROP TABLE receiving.receiving_locations;

--changeset jamesmckeon:create-skus-table
CREATE TABLE receiving.skus (
    sku_id uuid NOT NULL,
    owner_id integer NOT NULL,
    sku_code text NOT NULL,
    is_lot_tracked boolean NOT NULL,
    is_expiration_tracked boolean NOT NULL,
    CONSTRAINT pk_skus PRIMARY KEY (sku_id)
);
--rollback DROP TABLE receiving.skus;

--changeset jamesmckeon:create-invalid-pallets-table
CREATE TABLE receiving.invalid_pallets (
    invalid_pallet_id uuid NOT NULL,
    owner_id integer NOT NULL,
    sku_code text NOT NULL,
    license_plate_number text NOT NULL,
    quantity integer NOT NULL,
    lot_number text,
    expires_on timestamp with time zone,
    exceptions text[] NOT NULL,
    location_id text NOT NULL,
    request_location_id text NOT NULL,
    delivery_receipt_id uuid NOT NULL,
    CONSTRAINT pk_invalid_pallets PRIMARY KEY (invalid_pallet_id),
    CONSTRAINT ck_invalid_pallets_exceptions CHECK (cardinality(exceptions) > 0),
    CONSTRAINT ck_invalid_pallets_quantity CHECK (quantity > 0)
);
--rollback DROP TABLE receiving.invalid_pallets;

--changeset jamesmckeon:create-received-pallets-table
CREATE TABLE receiving.received_pallets (
    received_pallet_id uuid NOT NULL,
    license_plate_number text NOT NULL,
    quantity integer NOT NULL,
    location_id text NOT NULL,
    hold_reason_code text,
    expires_on timestamp with time zone,
    lot_number text,
    delivery_receipt_id uuid NOT NULL,
    sku_id uuid NOT NULL,
    sku_is_expiration_tracked boolean NOT NULL,
    sku_is_lot_tracked boolean NOT NULL,
    owner_id integer NOT NULL,
    sku_code text NOT NULL,
    CONSTRAINT pk_received_pallets PRIMARY KEY (received_pallet_id),
    CONSTRAINT ck_received_pallets_quantity CHECK (quantity > 0)
);
--rollback DROP TABLE receiving.received_pallets;

--changeset jamesmckeon:create-receipt-number-counters-table
CREATE TABLE receiving.receipt_number_counters (
    owner_id integer NOT NULL,
    last_number bigint NOT NULL,
    CONSTRAINT pk_receipt_number_counters PRIMARY KEY (owner_id),
    CONSTRAINT ck_receipt_number_counters_last_number CHECK (last_number > 0)
);
--rollback DROP TABLE receiving.receipt_number_counters;

--changeset jamesmckeon:add-invalid-pallets-delivery-receipts-fk
ALTER TABLE receiving.invalid_pallets
    ADD CONSTRAINT fk_invalid_pallets_delivery_receipts_delivery_receipt_id FOREIGN KEY (delivery_receipt_id)
        REFERENCES receiving.delivery_receipts (delivery_receipt_id) ON DELETE CASCADE;
--rollback ALTER TABLE receiving.invalid_pallets DROP CONSTRAINT fk_invalid_pallets_delivery_receipts_delivery_receipt_id;

--changeset jamesmckeon:add-invalid-pallets-receiving-locations-fk
ALTER TABLE receiving.invalid_pallets
    ADD CONSTRAINT fk_invalid_pallets_receiving_locations_location_id FOREIGN KEY (location_id)
        REFERENCES receiving.receiving_locations (location_id) ON DELETE RESTRICT;
--rollback ALTER TABLE receiving.invalid_pallets DROP CONSTRAINT fk_invalid_pallets_receiving_locations_location_id;

--changeset jamesmckeon:add-received-pallets-delivery-receipts-fk
ALTER TABLE receiving.received_pallets
    ADD CONSTRAINT fk_received_pallets_delivery_receipts_delivery_receipt_id FOREIGN KEY (delivery_receipt_id)
        REFERENCES receiving.delivery_receipts (delivery_receipt_id) ON DELETE CASCADE;
--rollback ALTER TABLE receiving.received_pallets DROP CONSTRAINT fk_received_pallets_delivery_receipts_delivery_receipt_id;

--changeset jamesmckeon:add-received-pallets-hold-reasons-fk
ALTER TABLE receiving.received_pallets
    ADD CONSTRAINT fk_received_pallets_hold_reasons_hold_reason FOREIGN KEY (hold_reason_code)
        REFERENCES receiving.hold_reasons (reason_code) ON DELETE RESTRICT;
--rollback ALTER TABLE receiving.received_pallets DROP CONSTRAINT fk_received_pallets_hold_reasons_hold_reason;

--changeset jamesmckeon:add-received-pallets-receiving-locations-fk
ALTER TABLE receiving.received_pallets
    ADD CONSTRAINT fk_received_pallets_receiving_locations_location_id FOREIGN KEY (location_id)
        REFERENCES receiving.receiving_locations (location_id) ON DELETE RESTRICT;
--rollback ALTER TABLE receiving.received_pallets DROP CONSTRAINT fk_received_pallets_receiving_locations_location_id;

--changeset jamesmckeon:create-carriers-scac-code-unique-index
CREATE UNIQUE INDEX ix_carriers_scac_code ON receiving.carriers (scac_code);
--rollback DROP INDEX receiving.ix_carriers_scac_code;

--changeset jamesmckeon:create-delivery-receipts-owner-receipt-number-unique-index
CREATE UNIQUE INDEX ix_delivery_receipts_owner_id_receipt_number ON receiving.delivery_receipts (owner_id, receipt_number);
--rollback DROP INDEX receiving.ix_delivery_receipts_owner_id_receipt_number;

--changeset jamesmckeon:create-invalid-pallets-delivery-receipt-id-index
CREATE INDEX ix_invalid_pallets_delivery_receipt_id ON receiving.invalid_pallets (delivery_receipt_id);
--rollback DROP INDEX receiving.ix_invalid_pallets_delivery_receipt_id;

--changeset jamesmckeon:create-invalid-pallets-location-id-index
CREATE INDEX ix_invalid_pallets_location_id ON receiving.invalid_pallets (location_id);
--rollback DROP INDEX receiving.ix_invalid_pallets_location_id;

--changeset jamesmckeon:create-received-pallets-delivery-receipt-id-index
CREATE INDEX ix_received_pallets_delivery_receipt_id ON receiving.received_pallets (delivery_receipt_id);
--rollback DROP INDEX receiving.ix_received_pallets_delivery_receipt_id;

--changeset jamesmckeon:create-received-pallets-hold-reason-code-index
CREATE INDEX ix_received_pallets_hold_reason_code ON receiving.received_pallets (hold_reason_code);
--rollback DROP INDEX receiving.ix_received_pallets_hold_reason_code;

--changeset jamesmckeon:create-received-pallets-location-id-index
CREATE INDEX ix_received_pallets_location_id ON receiving.received_pallets (location_id);
--rollback DROP INDEX receiving.ix_received_pallets_location_id;

--changeset jamesmckeon:create-skus-owner-sku-code-unique-index
CREATE UNIQUE INDEX ix_skus_owner_id_sku_code ON receiving.skus (owner_id, sku_code);
--rollback DROP INDEX receiving.ix_skus_owner_id_sku_code;
