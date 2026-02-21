-- ============================================
-- FiberHelp - Add Missing Ticket Columns
-- Run this in SQL Server Management Studio
-- ============================================

USE FiberhelpDB;
GO

-- Add AssignedAgentId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'AssignedAgentId')
BEGIN
    ALTER TABLE Tickets ADD AssignedAgentId NVARCHAR(50) NULL;
    PRINT 'Added AssignedAgentId column to Tickets table.';
END
ELSE
BEGIN
    PRINT 'AssignedAgentId column already exists.';
END
GO

-- Add AssignedTechnicianId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'AssignedTechnicianId')
BEGIN
    ALTER TABLE Tickets ADD AssignedTechnicianId NVARCHAR(50) NULL;
    PRINT 'Added AssignedTechnicianId column to Tickets table.';
END
GO

-- Add ResolvedByTechnicianId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ResolvedByTechnicianId')
BEGIN
    ALTER TABLE Tickets ADD ResolvedByTechnicianId NVARCHAR(50) NULL;
    PRINT 'Added ResolvedByTechnicianId column to Tickets table.';
END
GO

-- Add AssignedAt column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'AssignedAt')
BEGIN
    ALTER TABLE Tickets ADD AssignedAt DATETIME2 NULL;
    PRINT 'Added AssignedAt column to Tickets table.';
END
GO

-- Add ResolvedAt column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ResolvedAt')
BEGIN
    ALTER TABLE Tickets ADD ResolvedAt DATETIME2 NULL;
    PRINT 'Added ResolvedAt column to Tickets table.';
END
GO

-- Add ResolutionNotes column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ResolutionNotes')
BEGIN
    ALTER TABLE Tickets ADD ResolutionNotes NVARCHAR(1000) NULL;
    PRINT 'Added ResolutionNotes column to Tickets table.';
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
AND c.name IN ('AssignedAgentId', 'AssignedTechnicianId', 'ResolvedByTechnicianId', 'AssignedAt', 'ResolvedAt', 'ResolutionNotes')
ORDER BY c.name;

PRINT '';
PRINT '============================================';
PRINT 'Ticket columns migration complete!';
PRINT '============================================';
GO
