--liquibase formatted sql

--changeset jamesmckeon:drop-orders-ef-migrations

drop table if exists orders."__EFMigrationsHistory";

--rollback create table orders."__EFMigrationsHistory"
--rollback (
--rollback migration_id    varchar(150) not null
--rollback constraint pk___ef_migrations_history
--rollback primary key,
--rollback product_version varchar(32)  not null
--rollback );


