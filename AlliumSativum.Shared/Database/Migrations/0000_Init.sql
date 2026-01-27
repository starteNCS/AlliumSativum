CREATE SCHEMA Catalog;

CREATE TABLE Catalog.DataSources (
    Id uuid PRIMARY KEY,
    Name varchar(25) NOT NULL,
    Connector int NOT NULL,
    -- really bad, but okay for development purposes. Not every connector needs some connection string
    ConnectionString varchar(1000) NULL 
);

CREATE TABLE Catalog.Relations (
    Id uuid PRIMARY KEY,
    DataSourceId uuid REFERENCES Catalog.DataSources(Id),
    Name varchar(125) NOT NULL,
    Cardinality bigint NOT NULL,
    MetricsDate timestamp NOT NULL
);  

CREATE TABLE Catalog.Attributes (
    Id uuid PRIMARY KEY,
    RelationId uuid REFERENCES Catalog.Relations(Id),
    Name varchar(125) NOT NULL,
    DistinctCardinality bigint NOT NULL,
    MetricsDate timestamp NOT NULL
);