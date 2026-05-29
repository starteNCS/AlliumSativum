CREATE SCHEMA IF NOT EXISTS Catalog;

CREATE TABLE IF NOT EXISTS Catalog.DataSources
(
    Id               uuid PRIMARY KEY,
    Name             varchar(25)   NOT NULL,
    Connector        int           NOT NULL,
    -- really bad, but okay for development purposes. Not every connector needs some connection string
    ConnectionString varchar(1000) NULL
);

CREATE TABLE IF NOT EXISTS Catalog.Relations
(
    Id               uuid PRIMARY KEY,
    DataSourceId     uuid REFERENCES Catalog.DataSources (Id),
    Name             varchar(125) NOT NULL,
    Cardinality      bigint       NOT NULL,
    ConnectionOpenMs bigint       NOT NULL,
    Transfer100Ms    bigint       NOT NULL,
    MetricsDate      timestamp    NOT NULL
);

CREATE TABLE IF NOT EXISTS Catalog.Attributes
(
    Id                  uuid PRIMARY KEY,
    RelationId          uuid REFERENCES Catalog.Relations (Id),
    Name                varchar(125) NOT NULL,
    DistinctCardinality bigint       NOT NULL,
    MetricsDate         timestamp    NOT NULL
);