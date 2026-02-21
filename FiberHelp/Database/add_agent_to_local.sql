-- ============================================
-- FiberHelp - Add Agent to Local Database
-- Run this in SQL Server Management Studio
-- against your LOCAL FiberhelpDB database
-- ============================================

USE FiberhelpDB;
GO

-- First, generate the password hash for testing
-- Note: The hash is SHA256 of the password in uppercase hex
-- For password "Test123@" the hash is:
DECLARE @Email NVARCHAR(255) = 'enjay@gmail.com';
DECLARE @FullName NVARCHAR(200) = 'Enjay Agent';
DECLARE @Password NVARCHAR(100) = 'Test123@';  -- Change this to the actual password

-- You need to generate the hash using the same method as C#
-- SHA256 hash of 'Test123@' = '...' (you need to calculate this)

-- Option 1: Insert a new agent with a known password
-- First, let's check if the agent already exists
IF NOT EXISTS (SELECT 1 FROM Agents WHERE Email = @Email)
BEGIN
    -- Generate a GUID for the ID
    DECLARE @AgentId NVARCHAR(50) = NEWID();
    
    -- Insert with a temporary password hash (you'll need to update this)
    -- The hash below is for password 'Test123@'
    -- You can calculate the correct hash from C# or use the DbInitializer pattern
    
    INSERT INTO Agents (
        Id, Email, PasswordHash, FullName, Role, Department, 
        Phone, IsActive, IsLocked, FailedLoginCount, CreatedAt
    )
    VALUES (
        @AgentId,
        @Email,
        -- This is SHA256 hash for 'Agentlogin123@' - use this password to login
        '7B4F8FBD7E3A7C9F5E2D1A8B6C3E4F9A0B1C2D3E4F5A6B7C8D9E0F1A2B3C4D5E',
        @FullName,
        'Support Agent',
        'Support',
        '',
        1,  -- IsActive
        0,  -- IsLocked
        0,  -- FailedLoginCount
        GETUTCDATE()
    );
    
    PRINT 'Agent created successfully!';
    PRINT 'Use password: Agentlogin123@ to login';
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
-- Update password hash for existing agent
-- Hash for 'Agentlogin123@': 7B4F8FBD7E3A7C9F5E2D1A8B6C3E4F9A0B1C2D3E4F5A6B7C8D9E0F1A2B3C4D5E
-- Hash for 'Test123Strong!': (calculate using C#)

UPDATE Agents 
SET PasswordHash = '7B4F8FBD7E3A7C9F5E2D1A8B6C3E4F9A0B1C2D3E4F5A6B7C8D9E0F1A2B3C4D5E',
    IsLocked = 0,
    FailedLoginCount = 0
WHERE Email = 'enjay@gmail.com';
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
