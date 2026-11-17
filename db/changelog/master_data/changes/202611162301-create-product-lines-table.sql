--liquibase formatted sql

-- Each owner's product lines, such as outerwear or footwear. A product line belongs to its owner, so
-- two owners can use the same code for different lines.

--changeset jamesmckeon:create-master-data-product-lines-table
CREATE TABLE master_data.product_lines (
    owner_id integer NOT NULL,
    code character varying(20) NOT NULL,
    name character varying(100) NOT NULL,
    CONSTRAINT pk_product_lines PRIMARY KEY (owner_id, code),
    CONSTRAINT fk_product_lines_owners_owner_id FOREIGN KEY (owner_id)
        REFERENCES master_data.owners (id)
);
--rollback DROP TABLE master_data.product_lines;
