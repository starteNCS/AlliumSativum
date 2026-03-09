CREATE TABLE Catalog.AttributePeaks (
    Id uuid PRIMARY KEY,
    AttributeId uuid REFERENCES Catalog.Attributes(Id),
    Position int NOT NULL,
    Height int NOT NULL
);

ALTER TABLE Catalog.Attributes DROP COLUMN KellySkewness;