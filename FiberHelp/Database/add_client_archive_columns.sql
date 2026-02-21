-- ============================================
-- Add archive columns to Clients/Customers tables if missing
-- Run this on your database (e.g. in SSMS) where the app points
-- This handles both table name variations used in scripts: Clients and Customers
-- ============================================

USE FiberhelpDB;
GO

-- Helper to add columns to a specified table
IF OBJECT_ID('dbo.Clients','U') IS NOT NULL
BEGIN
    -- IsArchived
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clients' AND COLUMN_NAME = 'IsArchived')
    BEGIN
        ALTER TABLE dbo.Clients ADD IsArchived BIT NOT NULL CONSTRAINT DF_Clients_IsArchived DEFAULT (0);
        PRINT 'Added IsArchived to Clients';
    END
    ELSE
    BEGIN
        PRINT 'Clients.IsArchived already exists';
    END

    -- ArchivedAt
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clients' AND COLUMN_NAME = 'ArchivedAt')
    BEGIN
        ALTER TABLE dbo.Clients ADD ArchivedAt DATETIME2 NULL;
        PRINT 'Added ArchivedAt to Clients';
    END

    -- ArchivedByUserId
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clients' AND COLUMN_NAME = 'ArchivedByUserId')
    BEGIN
        ALTER TABLE dbo.Clients ADD ArchivedByUserId NVARCHAR(50) NULL;
        PRINT 'Added ArchivedByUserId to Clients';
    END
END
ELSE
BEGIN
    PRINT 'Table dbo.Clients does not exist (skipping Clients modifications)';
END
GO

-- Also handle legacy/alternate table name Customers
IF OBJECT_ID('dbo.Customers','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Customers' AND COLUMN_NAME = 'IsArchived')
    BEGIN
        ALTER TABLE dbo.Customers ADD IsArchived BIT NOT NULL CONSTRAINT DF_Customers_IsArchived DEFAULT (0);
        PRINT 'Added IsArchived to Customers';
    END
    ELSE
    BEGIN
        PRINT 'Customers.IsArchived already exists';
    END

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Customers' AND COLUMN_NAME = 'ArchivedAt')
    BEGIN
        ALTER TABLE dbo.Customers ADD ArchivedAt DATETIME2 NULL;
        PRINT 'Added ArchivedAt to Customers';
    END

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Customers' AND COLUMN_NAME = 'ArchivedByUserId')
    BEGIN
        ALTER TABLE dbo.Customers ADD ArchivedByUserId NVARCHAR(50) NULL;
        PRINT 'Added ArchivedByUserId to Customers';
    END
END
ELSE
BEGIN
    PRINT 'Table dbo.Customers does not exist (skipping Customers modifications)';
END
GO

-- Optional: create indices to speed up archive queries
IF OBJECT_ID('IX_Clients_IsArchived', 'I') IS NULL AND OBJECT_ID('dbo.Clients','U') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_Clients_IsArchived ON dbo.Clients (IsArchived) INCLUDE (Id, Name, Email);
    PRINT 'Created index IX_Clients_IsArchived';
END
GO

IF OBJECT_ID('IX_Customers_IsArchived', 'I') IS NULL AND OBJECT_ID('dbo.Customers','U') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_Customers_IsArchived ON dbo.Customers (IsArchived) INCLUDE (Id, Name, Email);
    PRINT 'Created index IX_Customers_IsArchived';
END
GO

PRINT 'Client archive columns migration complete.';
GO
