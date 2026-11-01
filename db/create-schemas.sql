-- Creates each module's schema, which Liquibase needs before it can run. Liquibase keeps its tracking
-- tables inside each module's schema (--liquibase-schema-name), so it can't create the schema itself.
--
-- Run once against a new database, before the first liquibase update. Production already has these
-- schemas. Everything inside them is owned by the changelogs in db/changelog.
--
-- Safe to re-run: a schema that already exists is left as it is.

CREATE SCHEMA IF NOT EXISTS orders;
CREATE SCHEMA IF NOT EXISTS inventory;
CREATE SCHEMA IF NOT EXISTS receiving;
CREATE SCHEMA IF NOT EXISTS wolverine;
