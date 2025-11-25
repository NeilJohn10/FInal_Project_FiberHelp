-- Create Agents table (separate table if you prefer explicit agents store)
-- Run this against the FiberhelpDB database (e.g. in SSMS)

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FiberhelpDB')
BEGIN
 CREATE DATABASE [FiberhelpDB];
END
GO

USE [FiberhelpDB];
GO

-- Drop if exists
IF OBJECT_ID('dbo.Agents', 'U') IS NOT NULL
 DROP TABLE dbo.Agents;
GO

CREATE TABLE dbo.Agents (
 Id NVARCHAR(50) NOT NULL PRIMARY KEY,
 Email NVARCHAR(256) NOT NULL UNIQUE,
 PasswordHash NVARCHAR(256) NOT NULL,
 FullName NVARCHAR(256) NULL,
 Department NVARCHAR(100) NULL,
 Phone NVARCHAR(50) NULL,
 IsActive BIT NOT NULL DEFAULT(1),
 IsLocked BIT NOT NULL DEFAULT(0),
 FailedLoginCount INT NOT NULL DEFAULT(0),
 CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 UpdatedAt DATETIME2 NULL,
 LastLoginAt DATETIME2 NULL,
 PasswordChangedAt DATETIME2 NULL
);
GO

-- Optional: insert a sample agent (omit in production)
-- INSERT INTO dbo.Agents (Id, Email, PasswordHash, FullName, Department, Phone, IsActive)
-- VALUES ('agent-1', 'agent@fiberhelp.com', '<SHA256-HASH-HERE>', 'Support Agent', 'Support', '+1-555-0100',1);
-- Replace <SHA256-HASH-HERE> with the SHA256 hex of the desired password.
GO
