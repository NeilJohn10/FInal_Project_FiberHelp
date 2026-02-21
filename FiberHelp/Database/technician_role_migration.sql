-- ============================================
-- FiberHelp Technician Role Migration Script
-- Run this script to add Technician support
-- ============================================

-- Add Role and ServiceArea columns to Agents table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Agents') AND name = 'Role')
BEGIN
    ALTER TABLE Agents ADD Role NVARCHAR(50) NOT NULL DEFAULT 'Support Agent';
    PRINT 'Added Role column to Agents table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Agents') AND name = 'ServiceArea')
BEGIN
    ALTER TABLE Agents ADD ServiceArea NVARCHAR(200) NULL;
    PRINT 'Added ServiceArea column to Agents table';
END

-- Add Technician assignment columns to Tickets table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'AssignedAgentId')
BEGIN
    ALTER TABLE Tickets ADD AssignedAgentId NVARCHAR(50) NULL;
    PRINT 'Added AssignedAgentId column to Tickets table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'AssignedTechnicianId')
BEGIN
    ALTER TABLE Tickets ADD AssignedTechnicianId NVARCHAR(50) NULL;
    PRINT 'Added AssignedTechnicianId column to Tickets table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ResolvedByTechnicianId')
BEGIN
    ALTER TABLE Tickets ADD ResolvedByTechnicianId NVARCHAR(50) NULL;
    PRINT 'Added ResolvedByTechnicianId column to Tickets table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'AssignedAt')
BEGIN
    ALTER TABLE Tickets ADD AssignedAt DATETIME2 NULL;
    PRINT 'Added AssignedAt column to Tickets table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ResolvedAt')
BEGIN
    ALTER TABLE Tickets ADD ResolvedAt DATETIME2 NULL;
    PRINT 'Added ResolvedAt column to Tickets table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ResolutionNotes')
BEGIN
    ALTER TABLE Tickets ADD ResolutionNotes NVARCHAR(1000) NULL;
    PRINT 'Added ResolutionNotes column to Tickets table';
END

-- Update existing agents to have default role
UPDATE Agents SET Role = 'Support Agent' WHERE Role IS NULL OR Role = '';

PRINT 'Migration complete! Technician role support is now enabled.';
GO
