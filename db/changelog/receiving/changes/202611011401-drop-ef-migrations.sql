--liquibase formatted sql

--changeset jamesmckeon:drop-receiving-ef-migrations

drop table if exists receiving."__EFMigrationsHistory";

--rollback create table receiving."__EFMigrationsHistory"
--rollback (
--rollback migration_id    varchar(150) not null
--rollback constraint pk___ef_migrations_history
--rollback primary key,
--rollback product_version varchar(32)  not null
--rollback );


