-- ============================================
-- FiberHelp - Add Agent to Local Database
-- Run this in SQL Server Management Studio
-- against your LOCAL FiberhelpDB database
-- ============================================

USE FiberhelpDB;
GO

-- First, generate the password hash using the .NET app
-- Agents should be created through the app UI (Admin > Agents > Register Agent)
-- which uses PBKDF2 hashing via PasswordHasher.Hash().
-- Alternatively, set FIBERHELP_ADMIN_EMAIL/PASSWORD env vars for app-based seeding.
DECLARE @Email NVARCHAR(255) = 'enjay@gmail.com';
DECLARE @FullName NVARCHAR(200) = 'Enjay Agent';

-- Option 1: Insert a new agent with a known password
-- First, let's check if the agent already exists
IF NOT EXISTS (SELECT 1 FROM Agents WHERE Email = @Email)
BEGIN
    -- Generate a GUID for the ID
    DECLARE @AgentId NVARCHAR(50) = NEWID();
    
    -- Insert with a placeholder hash — create the agent through the app UI instead
    -- for proper PBKDF2 hashing. This placeholder will NOT work for login.

    INSERT INTO Agents (
        Id, Email, PasswordHash, FullName, Role, Department, 
        Phone, IsActive, IsLocked, FailedLoginCount, CreatedAt
    )
    VALUES (
        @AgentId,
        @Email,
        -- Placeholder: create agents via the app for secure PBKDF2 hashing
        'PLACEHOLDER_USE_APP_TO_CREATE_AGENT',
        @FullName,
        'Support Agent',
        'Support',
        '',
        1,  -- IsActive
        0,  -- IsLocked
        0,  -- FailedLoginCount
        GETUTCDATE()
    );

    PRINT 'Agent record created. Reset password through the app for proper hashing.';
END
ELSE
BEGIN
    PRINT 'Agent already exists with email: ' + @Email;
    
    -- Show current agent info
    SELECT Id, Email, FullName, Role, IsActive, IsLocked, FailedLoginCount
    FROM Agents 
    WHERE Email = @Email;
END
GO

-- ============================================
-- OPTION 2: Copy agents from cloud to local
-- Run this if you have linked server to cloud
-- ============================================

/*
-- List all agents in local database
SELECT 'LOCAL AGENTS:' AS [Source];
SELECT Id, Email, FullName, Role, IsActive, IsLocked FROM Agents;
*/

-- ============================================
-- OPTION 3: Reset an agent's password
-- ============================================

/*
-- To reset a password, create the agent through the app UI
-- which uses secure PBKDF2 hashing. Do NOT hardcode password hashes in SQL.

UPDATE Agents 
SET IsLocked = 0,
    FailedLoginCount = 0
WHERE Email = 'enjay@gmail.com';
-- Then reset password through the app's agent management page.
*/

-- ============================================
-- VERIFY: Check all agents in local database
-- ============================================
SELECT 
    Id, 
    Email, 
    FullName, 
    Role, 
    IsActive, 
    IsLocked, 
    FailedLoginCount,
    LEFT(PasswordHash, 20) + '...' AS PasswordHashPreview
FROM Agents
ORDER BY Email;

PRINT '';
PRINT 'If enjay@gmail.com is not listed above, the agent does not exist in local database.';
PRINT 'The updated AuthService will now also check the cloud database for login.';
GO
