--liquibase formatted sql

--changeset jamesmckeon:grant-sku-reader-master-data-usage
GRANT USAGE ON SCHEMA master_data TO sku_reader;
--rollback REVOKE USAGE ON SCHEMA master_data FROM sku_reader;

--changeset jamesmckeon:grant-sku-reader-master-data-skus-select
GRANT SELECT ON master_data.skus TO sku_reader;
--rollback REVOKE SELECT ON master_data.skus FROM sku_reader;