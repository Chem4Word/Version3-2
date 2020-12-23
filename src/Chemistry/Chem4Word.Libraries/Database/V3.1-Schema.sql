--
-- File generated with SQLiteStudio v3.2.1 on Fri Nov 13 15:17:18 2020
--
-- Text encoding used: System
--
PRAGMA foreign_keys = off;
BEGIN TRANSACTION;

-- Table: Gallery
CREATE TABLE Gallery (
    Id        INTEGER NOT NULL
                      PRIMARY KEY AUTOINCREMENT
                      UNIQUE,
    Chemistry BLOB    NOT NULL,
    Name      TEXT    NOT NULL,
    Formula   TEXT
);


-- Table: ChemicalNames
CREATE TABLE ChemicalNames (
    ChemicalNameId INTEGER NOT NULL,
    Name           TEXT    NOT NULL,
    Namespace      TEXT    NOT NULL,
    Tag            TEXT    NOT NULL,
    ChemistryID    INTEGER NOT NULL,
    FOREIGN KEY (
        ChemistryID
    )
    REFERENCES Gallery (Id),
    CONSTRAINT PK_ChemicalNames PRIMARY KEY (
        ChemicalNameId
    )
);


-- Table: ChemistryByTags
CREATE TABLE ChemistryByTags (
    Id        INTEGER NOT NULL
                      PRIMARY KEY AUTOINCREMENT
                      UNIQUE,
    GalleryId INTEGER NOT NULL,
    TagId     INTEGER NOT NULL,
    FOREIGN KEY (
        GalleryId
    )
    REFERENCES Gallery (Id),
    FOREIGN KEY (
        TagId
    )
    REFERENCES UserTags (Id) 
);


-- Table: UserTags
CREATE TABLE UserTags (
    Id      INTEGER NOT NULL
                    PRIMARY KEY AUTOINCREMENT
                    UNIQUE,
    UserTag TEXT    NOT NULL
                    UNIQUE,
    Lock    INTEGER NOT NULL
                    DEFAULT 0
);


-- View: GetAllChemistryWithTags
CREATE VIEW GetAllChemistryWithTags AS
    SELECT Gallery.*,
           UserTags.UserTag
      FROM UserTags
           INNER JOIN
           (
               Gallery
               INNER JOIN
               ChemistryByTags ON Gallery.Id = ChemistryByTags.GalleryId
           )
           ON UserTags.ID = ChemistryByTags.TagId;


-- View: GetUserTags
CREATE VIEW GetUserTags AS
    SELECT UserTags.UserTag
      FROM UserTags;


-- View: UserTag
CREATE VIEW UserTag AS
    SELECT UserTags.UserTag
      FROM UserTags
     ORDER BY UserTags.Id;


COMMIT TRANSACTION;
PRAGMA foreign_keys = on;
