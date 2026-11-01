--liquibase formatted sql

-- Baseline: Wolverine's message storage as WolverineFx.Postgresql 6.39.0 builds it, taken from
-- a database Wolverine provisioned itself. Production was marked as already having this
-- baseline (changelog-sync); it only runs against a new, empty database.

--changeset jamesmckeon:create-wolverine-agent-restrictions-table
CREATE TABLE wolverine.wolverine_agent_restrictions (
    id uuid NOT NULL,
    uri character varying NOT NULL,
    type character varying NOT NULL,
    node integer NOT NULL DEFAULT 0,
    CONSTRAINT pkey_wolverine_agent_restrictions_id PRIMARY KEY (id)
);
--rollback DROP TABLE wolverine.wolverine_agent_restrictions;

--changeset jamesmckeon:create-wolverine-control-queue-table
CREATE TABLE wolverine.wolverine_control_queue (
    id uuid NOT NULL,
    message_type character varying NOT NULL,
    node_id uuid NOT NULL,
    body bytea NOT NULL,
    posted timestamp with time zone NOT NULL DEFAULT now(),
    expires timestamp with time zone,
    CONSTRAINT pkey_wolverine_control_queue_id PRIMARY KEY (id)
);
--rollback DROP TABLE wolverine.wolverine_control_queue;

--changeset jamesmckeon:create-wolverine-dead-letters-table
CREATE TABLE wolverine.wolverine_dead_letters (
    id uuid NOT NULL,
    execution_time timestamp with time zone,
    body bytea NOT NULL,
    message_type character varying NOT NULL,
    received_at character varying,
    source character varying,
    exception_type character varying,
    exception_message character varying,
    sent_at timestamp with time zone,
    replayable boolean,
    CONSTRAINT pkey_wolverine_dead_letters_id PRIMARY KEY (id)
);
--rollback DROP TABLE wolverine.wolverine_dead_letters;

--changeset jamesmckeon:create-wolverine-incoming-envelopes-table
CREATE TABLE wolverine.wolverine_incoming_envelopes (
    id uuid NOT NULL,
    status character varying NOT NULL,
    owner_id integer NOT NULL,
    execution_time timestamp with time zone,
    attempts integer DEFAULT 0,
    body bytea NOT NULL,
    message_type character varying NOT NULL,
    received_at character varying,
    keep_until timestamp with time zone,
    CONSTRAINT pkey_wolverine_incoming_envelopes_id PRIMARY KEY (id)
);
--rollback DROP TABLE wolverine.wolverine_incoming_envelopes;

--changeset jamesmckeon:create-wolverine-nodes-table
CREATE TABLE wolverine.wolverine_nodes (
    id uuid NOT NULL,
    node_number serial NOT NULL,
    description character varying NOT NULL,
    uri character varying NOT NULL,
    started timestamp with time zone NOT NULL DEFAULT now(),
    health_check timestamp with time zone NOT NULL DEFAULT now(),
    version character varying,
    capabilities text[],
    CONSTRAINT pkey_wolverine_nodes_id PRIMARY KEY (id)
);
--rollback DROP TABLE wolverine.wolverine_nodes;

--changeset jamesmckeon:create-wolverine-node-assignments-table
CREATE TABLE wolverine.wolverine_node_assignments (
    id character varying NOT NULL,
    node_id uuid,
    started timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pkey_wolverine_node_assignments_id PRIMARY KEY (id)
);
--rollback DROP TABLE wolverine.wolverine_node_assignments;

--changeset jamesmckeon:create-wolverine-node-records-table
CREATE TABLE wolverine.wolverine_node_records (
    id serial NOT NULL,
    node_number integer NOT NULL,
    event_name character varying NOT NULL,
    "timestamp" timestamp with time zone NOT NULL DEFAULT now(),
    description character varying,
    CONSTRAINT pkey_wolverine_node_records_id PRIMARY KEY (id)
);
--rollback DROP TABLE wolverine.wolverine_node_records;

--changeset jamesmckeon:create-wolverine-outgoing-envelopes-table
CREATE TABLE wolverine.wolverine_outgoing_envelopes (
    id uuid NOT NULL,
    owner_id integer NOT NULL,
    destination character varying NOT NULL,
    deliver_by timestamp with time zone,
    body bytea NOT NULL,
    attempts integer DEFAULT 0,
    message_type character varying NOT NULL,
    CONSTRAINT pkey_wolverine_outgoing_envelopes_id PRIMARY KEY (id)
);
--rollback DROP TABLE wolverine.wolverine_outgoing_envelopes;

--changeset jamesmckeon:add-wolverine-node-assignments-nodes-fk
ALTER TABLE wolverine.wolverine_node_assignments
    ADD CONSTRAINT fkey_wolverine_node_assignments_node_id FOREIGN KEY (node_id)
        REFERENCES wolverine.wolverine_nodes (id) ON DELETE CASCADE;
--rollback ALTER TABLE wolverine.wolverine_node_assignments DROP CONSTRAINT fkey_wolverine_node_assignments_node_id;

--changeset jamesmckeon:create-wolverine-dead-letters-replayable-index
CREATE INDEX idx_wolverine_dead_letters_replayable
    ON wolverine.wolverine_dead_letters (replayable) WHERE (replayable = true);
--rollback DROP INDEX wolverine.idx_wolverine_dead_letters_replayable;

--changeset jamesmckeon:create-wolverine-incoming-envelopes-keep-until-index
CREATE INDEX idx_wolverine_incoming_envelopes_keep_until
    ON wolverine.wolverine_incoming_envelopes (keep_until) WHERE (status = 'Handled');
--rollback DROP INDEX wolverine.idx_wolverine_incoming_envelopes_keep_until;

--changeset jamesmckeon:create-wolverine-incoming-envelopes-owner-index
CREATE INDEX idx_wolverine_incoming_envelopes_owner
    ON wolverine.wolverine_incoming_envelopes (owner_id) WHERE (owner_id <> 0);
--rollback DROP INDEX wolverine.idx_wolverine_incoming_envelopes_owner;

--changeset jamesmckeon:create-wolverine-incoming-envelopes-recover-index
CREATE INDEX idx_wolverine_incoming_envelopes_recover
    ON wolverine.wolverine_incoming_envelopes (received_at) WHERE (status = 'Incoming' AND owner_id = 0);
--rollback DROP INDEX wolverine.idx_wolverine_incoming_envelopes_recover;

--changeset jamesmckeon:create-wolverine-outgoing-envelopes-owner-index
CREATE INDEX idx_wolverine_outgoing_envelopes_owner
    ON wolverine.wolverine_outgoing_envelopes (owner_id) WHERE (owner_id <> 0);
--rollback DROP INDEX wolverine.idx_wolverine_outgoing_envelopes_owner;

--changeset jamesmckeon:create-wolverine-outgoing-envelopes-recover-index
CREATE INDEX idx_wolverine_outgoing_envelopes_recover
    ON wolverine.wolverine_outgoing_envelopes (destination) WHERE (owner_id = 0);
--rollback DROP INDEX wolverine.idx_wolverine_outgoing_envelopes_recover;
