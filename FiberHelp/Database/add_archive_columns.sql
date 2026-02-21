-- ============================================
-- FiberHelp - Add Archive Columns to Tickets
-- Run this in SQL Server Management Studio
-- ============================================

USE FiberhelpDB;
GO

-- Add IsArchived column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'IsArchived')
BEGIN
    ALTER TABLE Tickets ADD IsArchived BIT NOT NULL DEFAULT 0;
    PRINT 'Added IsArchived column to Tickets table.';
END
ELSE
BEGIN
    PRINT 'IsArchived column already exists.';
END
GO

-- Add ArchivedAt column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ArchivedAt')
BEGIN
    ALTER TABLE Tickets ADD ArchivedAt DATETIME2 NULL;
    PRINT 'Added ArchivedAt column to Tickets table.';
END
GO

-- Add ArchivedByUserId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ArchivedByUserId')
BEGIN
    ALTER TABLE Tickets ADD ArchivedByUserId NVARCHAR(50) NULL;
    PRINT 'Added ArchivedByUserId column to Tickets table.';
END
GO

-- Add ClosedAt column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ClosedAt')
BEGIN
    ALTER TABLE Tickets ADD ClosedAt DATETIME2 NULL;
    PRINT 'Added ClosedAt column to Tickets table.';
END
GO

-- Verify the columns were added
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Tickets')
AND c.name IN ('IsArchived', 'ArchivedAt', 'ArchivedByUserId', 'ClosedAt')
ORDER BY c.name;

PRINT '';
PRINT '============================================';
PRINT 'Ticket archive columns migration complete!';
PRINT '============================================';
GO

-- ============================================
-- OPTIONAL: Create index for faster archive queries
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tickets_IsArchived')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tickets_IsArchived 
    ON Tickets (IsArchived) 
    INCLUDE (TicketId, Subject, Status, CreatedAt);
    PRINT 'Created index IX_Tickets_IsArchived for faster archive queries.';
END
GO
