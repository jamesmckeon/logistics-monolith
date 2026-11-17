--liquibase formatted sql

-- The owner (client) master: the source of truth for each owner and its settings. Modules keep their
-- own copies, such as inventory.owners, with the fields they need.

--changeset jamesmckeon:create-master-data-owners-table
CREATE TABLE master_data.owners (
    id integer NOT NULL,
    name character varying(100) NOT NULL,
    allocation_policy text NOT NULL,
    CONSTRAINT pk_owners PRIMARY KEY (id)
);
--rollback DROP TABLE master_data.owners;
