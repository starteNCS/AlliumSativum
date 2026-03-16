ALTER TABLE Catalog.Attributes
    ADD COLUMN Min double precision NULL;
ALTER TABLE Catalog.Attributes
    ADD COLUMN Max double precision NULL;
ALTER TABLE Catalog.Attributes
    ADD COLUMN DataType varchar(50) NOT NULL DEFAULT 'unknown';
