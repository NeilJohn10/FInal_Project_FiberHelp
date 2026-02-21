-- ============================================
-- FiberHelp - Add Role Column to Agents Table
-- Run this in SQL Server Management Studio
-- against your LOCAL FiberhelpDB database
-- ============================================

USE FiberhelpDB;
GO

-- Check if Role column exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Agents') AND name = 'Role')
BEGIN
    -- Add Role column with default value
    ALTER TABLE Agents ADD [Role] NVARCHAR(50) NOT NULL DEFAULT 'Support Agent';
    PRINT 'Added Role column to Agents table with default value "Support Agent".';
END
ELSE
BEGIN
    PRINT 'Role column already exists in Agents table.';
END
GO

-- Check if ServiceArea column exists (needed for Agent model)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Agents') AND name = 'ServiceArea')
BEGIN
    ALTER TABLE Agents ADD [ServiceArea] NVARCHAR(200) NULL;
    PRINT 'Added ServiceArea column to Agents table.';
END
GO

-- Verify the table structure
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Agents')
ORDER BY c.column_id;

-- Show all agents with the new column
SELECT Id, Email, FullName, Role, Department, Phone, IsActive, IsLocked
FROM Agents
ORDER BY Email;

PRINT '';
PRINT '============================================';
PRINT 'Agents table structure updated successfully!';
PRINT 'The application should now be able to read agents.';
PRINT '============================================';
GO
