-- ============================================
-- FiberHelp - Sync Technicians to Cloud
-- Run this on your CLOUD database (db33569)
-- ============================================

-- This script copies technician data from local to cloud
-- Replace the values with your actual technician data from the local database

-- First, check if the Technicians table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Technicians')
BEGIN
    CREATE TABLE [dbo].[Technicians](
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Email] NVARCHAR(255) NOT NULL,
        [PasswordHash] NVARCHAR(255) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(50) NULL,
        [ServiceArea] NVARCHAR(500) NULL,
        [Department] NVARCHAR(100) DEFAULT 'Field Operations',
        [Specialization] NVARCHAR(200) NULL,
        [IsActive] BIT DEFAULT 1,
        [IsLocked] BIT DEFAULT 0,
        [FailedLoginCount] INT DEFAULT 0,
        [LastLoginAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        [PasswordChangedAt] DATETIME2 NULL,
        [Notes] NVARCHAR(1000) NULL
    );
    
    CREATE UNIQUE INDEX IX_Technicians_Email ON [dbo].[Technicians]([Email]);
    
    PRINT 'Technicians table created successfully.';
END
ELSE
BEGIN
    PRINT 'Technicians table already exists.';
END
GO

-- ============================================
-- MANUAL INSERT EXAMPLE
-- Copy technician data from your local database and insert here
-- ============================================

-- Example: Insert the technician you created locally
-- Run this query on your LOCAL database first to get the values:
-- SELECT * FROM Technicians;

-- Then insert into cloud (replace values):
/*
INSERT INTO [dbo].[Technicians] (
    [Id], [Email], [PasswordHash], [FullName], [Phone], 
    [ServiceArea], [Department], [Specialization], 
    [IsActive], [IsLocked], [FailedLoginCount], 
    [CreatedAt], [Notes]
)
VALUES (
    '1dfde9f-423d-4f7e-9d4c-b72899dd9467a',  -- Replace with actual Id
    'luca@gmail.com',                         -- Replace with actual Email
    'EE90391E089F117B22E7F11DCE28E577E1F258B5262DEDFE...', -- Replace with actual PasswordHash
    'Joshua Gubantes',                        -- Replace with actual FullName
    '099214214425',                           -- Replace with actual Phone
    'Davao City',                             -- Replace with actual ServiceArea
    'Field Operations',                       -- Department
    'Fiber Specialist',                       -- Replace with actual Specialization
    1,                                        -- IsActive
    0,                                        -- IsLocked
    0,                                        -- FailedLoginCount
    GETUTCDATE(),                             -- CreatedAt
    NULL                                      -- Notes
);
*/

-- ============================================
-- VERIFY DATA
-- ============================================
SELECT 
    Id, Email, FullName, Phone, ServiceArea, 
    Department, Specialization, IsActive, CreatedAt
FROM [dbo].[Technicians]
ORDER BY CreatedAt DESC;

PRINT '';
PRINT '============================================';
PRINT 'Check the Technicians table above for synced data.';
PRINT '============================================';
GO
