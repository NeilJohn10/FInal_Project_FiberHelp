-- ============================================
-- FiberHelp - Fix Agents Table Missing Columns
-- Run this in SQL Server Management Studio
-- against your LOCAL FiberhelpDB database
-- ============================================
-- This fixes the error:
-- "Invalid column name 'Role'. Invalid column name 'ServiceArea'."
-- ============================================

USE FiberhelpDB;
GO

PRINT 'Checking and adding missing columns to Agents table...';
PRINT '';

-- Add Role column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Agents') AND name = 'Role')
BEGIN
    ALTER TABLE dbo.Agents ADD [Role] NVARCHAR(50) NOT NULL CONSTRAINT DF_Agents_Role DEFAULT 'Support Agent';
    PRINT '? Added Role column with default value "Support Agent"';
END
ELSE
BEGIN
    PRINT '? Role column already exists';
END
GO

-- Add ServiceArea column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Agents') AND name = 'ServiceArea')
BEGIN
    ALTER TABLE dbo.Agents ADD [ServiceArea] NVARCHAR(200) NULL;
    PRINT '? Added ServiceArea column';
END
ELSE
BEGIN
    PRINT '? ServiceArea column already exists';
END
GO

PRINT '';
PRINT '============================================';
PRINT 'Verifying table structure...';
PRINT '============================================';

-- Show current table structure
SELECT 
    c.name AS [Column],
    t.name AS [Type],
    CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length AS VARCHAR(10)) END AS [MaxLength],
    CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS [Nullable]
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.Agents')
ORDER BY c.column_id;

PRINT '';
PRINT '============================================';
PRINT 'Current agents in database:';
PRINT '============================================';

-- Show all agents
SELECT Id, Email, FullName, Role, Department, ServiceArea, IsActive, IsLocked
FROM dbo.Agents
ORDER BY Email;

PRINT '';
PRINT '============================================';
PRINT 'DONE! Restart the FiberHelp application now.';
PRINT '============================================';
GO
