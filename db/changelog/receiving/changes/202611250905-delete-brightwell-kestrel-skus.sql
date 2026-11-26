--liquibase formatted sql

--changeset jamesmckeon:delete-brightwell-kestrel-skus
--preconditions onFail:HALT onError:HALT
--precondition-sql-check expectedResult:0 SELECT count(*) FROM receiving.delivery_receipts WHERE owner_id IN (2, 3)
DELETE FROM receiving.skus WHERE owner_id IN (2,3);