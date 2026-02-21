-- ============================================
-- FiberHelp - Create Technicians Table
-- SQL Server Script for FiberHelpDB
-- Run this in SQL Server Management Studio
-- ============================================

USE FiberhelpDB;
GO

-- ============================================
-- 1. CREATE TECHNICIANS TABLE
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Technicians')
BEGIN
    CREATE TABLE Technicians (
        Id NVARCHAR(50) NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Email NVARCHAR(255) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(50) NULL,
        ServiceArea NVARCHAR(500) NULL,
        Department NVARCHAR(100) NULL DEFAULT 'Field Operations',
        Specialization NVARCHAR(200) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        IsLocked BIT NOT NULL DEFAULT 0,
        FailedLoginCount INT NOT NULL DEFAULT 0,
        LastLoginAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        PasswordChangedAt DATETIME2 NULL,
        Notes NVARCHAR(1000) NULL
    );
    
    PRINT 'Technicians table created successfully.';
END
ELSE
BEGIN
    PRINT 'Technicians table already exists.';
END
GO

-- ============================================
-- 2. CREATE UNIQUE INDEX ON EMAIL
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Technicians_Email' AND object_id = OBJECT_ID('Technicians'))
BEGIN
    CREATE UNIQUE INDEX IX_Technicians_Email ON Technicians(Email);
    PRINT 'Created unique index on Email.';
END
GO

-- ============================================
-- 3. CREATE INDEX FOR ACTIVE TECHNICIANS
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Technicians_IsActive' AND object_id = OBJECT_ID('Technicians'))
BEGIN
    CREATE INDEX IX_Technicians_IsActive ON Technicians(IsActive) WHERE IsActive = 1;
    PRINT 'Created index on IsActive.';
END
GO

-- ============================================
-- 4. CREATE INDEX FOR SERVICE AREA
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Technicians_ServiceArea' AND object_id = OBJECT_ID('Technicians'))
BEGIN
    CREATE INDEX IX_Technicians_ServiceArea ON Technicians(ServiceArea);
    PRINT 'Created index on ServiceArea.';
END
GO

-- ============================================
-- 5. ADD TECHNICIAN COLUMNS TO TICKETS TABLE
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'AssignedTechnicianId')
BEGIN
    ALTER TABLE Tickets ADD AssignedTechnicianId NVARCHAR(50) NULL;
    PRINT 'Added AssignedTechnicianId to Tickets table.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'AssignedAt')
BEGIN
    ALTER TABLE Tickets ADD AssignedAt DATETIME2 NULL;
    PRINT 'Added AssignedAt to Tickets table.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ResolvedByTechnicianId')
BEGIN
    ALTER TABLE Tickets ADD ResolvedByTechnicianId NVARCHAR(50) NULL;
    PRINT 'Added ResolvedByTechnicianId to Tickets table.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ResolvedAt')
BEGIN
    ALTER TABLE Tickets ADD ResolvedAt DATETIME2 NULL;
    PRINT 'Added ResolvedAt to Tickets table.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tickets') AND name = 'ResolutionNotes')
BEGIN
    ALTER TABLE Tickets ADD ResolutionNotes NVARCHAR(1000) NULL;
    PRINT 'Added ResolutionNotes to Tickets table.';
END
GO

-- ============================================
-- 6. ADD FOREIGN KEY CONSTRAINTS (OPTIONAL)
-- ============================================
-- Uncomment below if you want to enforce referential integrity

/*
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Tickets_AssignedTechnician')
BEGIN
    ALTER TABLE Tickets 
    ADD CONSTRAINT FK_Tickets_AssignedTechnician 
    FOREIGN KEY (AssignedTechnicianId) REFERENCES Technicians(Id);
    PRINT 'Added FK_Tickets_AssignedTechnician constraint.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Tickets_ResolvedByTechnician')
BEGIN
    ALTER TABLE Tickets 
    ADD CONSTRAINT FK_Tickets_ResolvedByTechnician 
    FOREIGN KEY (ResolvedByTechnicianId) REFERENCES Technicians(Id);
    PRINT 'Added FK_Tickets_ResolvedByTechnician constraint.';
END
GO
*/

-- ============================================
-- 7. INSERT SAMPLE TECHNICIAN (OPTIONAL)
-- ============================================
-- Uncomment to add a sample technician for testing
-- Password: Technician123@

/*
IF NOT EXISTS (SELECT * FROM Technicians WHERE Email = 'tech@fiberhelp.com')
BEGIN
    INSERT INTO Technicians (Id, Email, PasswordHash, FullName, Phone, ServiceArea, Department, IsActive)
    VALUES (
        NEWID(),
        'tech@fiberhelp.com',
        '8C6976E5B5410415BDE908BD4DEE15DFB167A9C873FC4BB8A81F6F2AB448A918', -- SHA256 of 'Technician123@'
        'Sample Technician',
        '+639123456789',
        'Metro Manila',
        'Field Operations',
        1
    );
    PRINT 'Sample technician created.';
END
GO
*/

-- ============================================
-- 8. VERIFY TABLE CREATION
-- ============================================
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    ISNULL(dc.definition, '') AS DefaultValue
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
WHERE c.object_id = OBJECT_ID('Technicians')
ORDER BY c.column_id;

PRINT '';
PRINT '============================================';
PRINT 'Technicians table setup complete!';
PRINT '============================================';
GO
