-- =================================================================
-- COMPLETE FIBERHELP DATABASE SETUP SCRIPT
-- Run this script in SQL Server Management Studio (SSMS)
-- =================================================================

USE [FiberhelpDB];
GO

-- ==========================================
-- 1. USERS TABLE
-- ==========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Email] NVARCHAR(256) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(256) NOT NULL,
        [Role] NVARCHAR(50) NOT NULL,
        [FullName] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    PRINT 'Users table created successfully';
END
ELSE
    PRINT 'Users table already exists';
GO

-- ==========================================
-- 2. AGENTS TABLE
-- ==========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Agents')
BEGIN
    CREATE TABLE [dbo].[Agents] (
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Email] NVARCHAR(256) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(256) NOT NULL,
        [FullName] NVARCHAR(256) NULL,
        [Department] NVARCHAR(100) NULL,
        [Phone] NVARCHAR(50) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsLocked] BIT NOT NULL DEFAULT 0,
        [FailedLoginCount] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL,
        [LastLoginAt] DATETIME2 NULL,
        [PasswordChangedAt] DATETIME2 NULL
    );
    PRINT 'Agents table created successfully';
END
ELSE
    PRINT 'Agents table already exists';
GO

-- ==========================================
-- 3. ACCOUNTS TABLE
-- ==========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Accounts')
BEGIN
    CREATE TABLE [dbo].[Accounts] (
        [AccountId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [AccountNumber] NVARCHAR(100) NOT NULL UNIQUE,
        [Name] NVARCHAR(200) NULL,
        [Type] NVARCHAR(100) NULL,
        [ServicePlan] NVARCHAR(100) NULL,
        [Status] NVARCHAR(50) NULL DEFAULT 'Active',
        [ContactName] NVARCHAR(200) NULL,
        [ContactEmail] NVARCHAR(256) NULL,
        [ContactPhone] NVARCHAR(50) NULL,
        [BillingAddress] NVARCHAR(400) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL
    );
    PRINT 'Accounts table created successfully';
END
ELSE
    PRINT 'Accounts table already exists';
GO

-- ==========================================
-- 4. CLIENTS TABLE
-- ==========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Clients')
BEGIN
    CREATE TABLE [dbo].[Clients] (
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Email] NVARCHAR(200) NULL,
        [Plan] NVARCHAR(100) NULL,
        [Status] NVARCHAR(50) NULL,
        [JoinDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [AccountId] INT NULL,
        CONSTRAINT FK_Clients_Accounts FOREIGN KEY ([AccountId]) REFERENCES [Accounts]([AccountId])
    );
    PRINT 'Clients table created successfully';
END
ELSE
    PRINT 'Clients table already exists';
GO

-- ==========================================
-- 5. TICKETS TABLE
-- ==========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tickets')
BEGIN
    CREATE TABLE [dbo].[Tickets] (
        [TicketId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [AccountId] INT NULL,
        [Subject] NVARCHAR(500) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Open',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ClientId] NVARCHAR(50) NULL,
        [ClientName] NVARCHAR(256) NULL,
        [Priority] NVARCHAR(50) NULL DEFAULT 'Medium',
        CONSTRAINT FK_Tickets_Clients FOREIGN KEY ([ClientId]) REFERENCES [Clients]([Id])
    );
    PRINT 'Tickets table created successfully';
END
ELSE
    PRINT 'Tickets table already exists';
GO

-- ==========================================
-- 6. INVOICES TABLE
-- ==========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
    CREATE TABLE [dbo].[Invoices] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [AccountId] INT NULL,
        [AccountName] NVARCHAR(256) NULL,
        [AmountDue] DECIMAL(18,2) NOT NULL,
        [IssueDate] DATETIME2 NOT NULL,
        [DueDate] DATETIME2 NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [PaymentRef] NVARCHAR(100) NULL,
        [PaidDate] DATETIME2 NULL,
        [Notes] NVARCHAR(MAX) NULL,
        CONSTRAINT FK_Invoices_Accounts FOREIGN KEY ([AccountId]) REFERENCES [Accounts]([AccountId])
    );
    PRINT 'Invoices table created successfully';
END
ELSE
    PRINT 'Invoices table already exists';
GO

-- ==========================================
-- 7. EXPENSES TABLE (MISSING - THIS IS THE PROBLEM!)
-- ==========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expenses')
BEGIN
    CREATE TABLE [dbo].[Expenses] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Date] DATETIME2 NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(400) NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Reference] NVARCHAR(100) NULL
    );
    PRINT 'Expenses table created successfully';
END
ELSE
    PRINT 'Expenses table already exists';
GO

-- ==========================================
-- 8. OUTBOX MESSAGES TABLE (For sync)
-- ==========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OutboxMessages')
BEGIN
    CREATE TABLE [dbo].[OutboxMessages] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EntityName] NVARCHAR(100) NOT NULL,
        [Operation] NVARCHAR(20) NOT NULL,
        [Key] NVARCHAR(100) NOT NULL,
        [Payload] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ProcessedAt] DATETIME2 NULL
    );
    PRINT 'OutboxMessages table created successfully';
END
ELSE
    PRINT 'OutboxMessages table already exists';
GO

-- ==========================================
-- SEED DATA
-- ==========================================
PRINT 'Starting seed data insertion...';

-- Seed Users / Agents
-- Admin and agent credentials are seeded by the .NET app via environment variables
-- (FIBERHELP_ADMIN_EMAIL, FIBERHELP_ADMIN_PASSWORD). See .env.example.
-- No hardcoded password hashes in SQL scripts.
PRINT 'User/Agent seeding is handled by the app via environment variables.';

-- Seed Accounts
IF NOT EXISTS (SELECT * FROM Accounts)
BEGIN
    INSERT INTO Accounts (AccountNumber, Name, Type, ServicePlan, Status, ContactName, ContactEmail, CreatedAt)
    VALUES 
    ('ACCT-20250001', 'TechCorp Inc.', 'Business', 'Premium', 'Active', 'John Doe', 'john@techcorp.com', SYSUTCDATETIME()),
    ('ACCT-20250002', 'Digital Solutions', 'Business', 'Standard', 'Active', 'Jane Smith', 'jane@digitalsol.com', SYSUTCDATETIME());
    PRINT 'Sample accounts seeded';
END

-- Seed Clients
IF NOT EXISTS (SELECT * FROM Clients)
BEGIN
    DECLARE @AccountId1 INT = (SELECT TOP 1 AccountId FROM Accounts WHERE AccountNumber = 'ACCT-20250001');
    DECLARE @AccountId2 INT = (SELECT TOP 1 AccountId FROM Accounts WHERE AccountNumber = 'ACCT-20250002');

    INSERT INTO Clients (Id, Name, Email, [Plan], Status, JoinDate, AccountId)
    VALUES 
    ('client-001', 'Contoso Ltd', 'contact@contoso.com', 'Premium', 'Active', SYSUTCDATETIME(), @AccountId1),
    ('client-002', 'Fabrikam', 'info@fabrikam.com', 'Standard', 'Active', SYSUTCDATETIME(), @AccountId2);
    PRINT 'Sample clients seeded';
END

-- Seed Tickets
IF NOT EXISTS (SELECT * FROM Tickets)
BEGIN
    INSERT INTO Tickets (AccountId, Subject, Status, CreatedAt, ClientId, ClientName, Priority)
    VALUES 
    (NULL, 'Sample ticket 1', 'Open', SYSUTCDATETIME(), 'client-001', 'Contoso Ltd', 'High'),
    (NULL, 'Sample ticket 2', 'In Progress', SYSUTCDATETIME(), 'client-002', 'Fabrikam', 'Medium'),
    (NULL, 'Resolved issue', 'Resolved', DATEADD(DAY, -5, SYSUTCDATETIME()), 'client-001', 'Contoso Ltd', 'Low');
    PRINT 'Sample tickets seeded';
END

-- Seed Invoices
IF NOT EXISTS (SELECT * FROM Invoices)
BEGIN
    DECLARE @Account1 INT = (SELECT TOP 1 AccountId FROM Accounts WHERE AccountNumber = 'ACCT-20250001');
    DECLARE @Account2 INT = (SELECT TOP 1 AccountId FROM Accounts WHERE AccountNumber = 'ACCT-20250002');

    INSERT INTO Invoices (AccountId, AccountName, AmountDue, IssueDate, DueDate, Status, PaymentRef, PaidDate)
    VALUES 
    (@Account1, 'TechCorp Inc.', 1500.00, DATEADD(DAY, -30, SYSUTCDATETIME()), SYSUTCDATETIME(), 'Paid', 'PAY-000001', DATEADD(DAY, -25, SYSUTCDATETIME())),
    (@Account1, 'TechCorp Inc.', 1500.00, DATEADD(DAY, -15, SYSUTCDATETIME()), DATEADD(DAY, 15, SYSUTCDATETIME()), 'Pending', NULL, NULL),
    (@Account2, 'Digital Solutions', 750.00, DATEADD(DAY, -20, SYSUTCDATETIME()), DATEADD(DAY, 10, SYSUTCDATETIME()), 'Pending', NULL, NULL),
    (@Account2, 'Digital Solutions', 750.00, DATEADD(DAY, -60, SYSUTCDATETIME()), DATEADD(DAY, -30, SYSUTCDATETIME()), 'Paid', 'PAY-000002', DATEADD(DAY, -28, SYSUTCDATETIME()));
    PRINT 'Sample invoices seeded';
END

-- Seed Expenses (THIS WAS MISSING!)
IF NOT EXISTS (SELECT * FROM Expenses)
BEGIN
    INSERT INTO Expenses (Date, Category, Description, Amount, Reference)
    VALUES 
    (DATEADD(DAY, -25, SYSUTCDATETIME()), 'Network', 'Fiber optic cables purchase', 1500.00, 'EXP-001'),
    (DATEADD(DAY, -20, SYSUTCDATETIME()), 'Utilities', 'Electric bill - November', 320.00, 'EXP-002'),
    (DATEADD(DAY, -15, SYSUTCDATETIME()), 'Payroll', 'Support staff salaries', 4200.00, 'EXP-003'),
    (DATEADD(DAY, -10, SYSUTCDATETIME()), 'Equipment', 'Network switches', 850.00, 'EXP-004'),
    (DATEADD(DAY, -5, SYSUTCDATETIME()), 'Maintenance', 'Server maintenance', 500.00, 'EXP-005'),
    (SYSUTCDATETIME(), 'Office', 'Office supplies', 150.00, 'EXP-006');
    PRINT 'Sample expenses seeded';
END

PRINT '=================================================================';
PRINT 'Database setup completed successfully!';
PRINT '=================================================================';
PRINT '';
PRINT 'Admin credentials are set via environment variables:';
PRINT '  FIBERHELP_ADMIN_EMAIL and FIBERHELP_ADMIN_PASSWORD';
PRINT '  See .env.example for details.';
PRINT '';
PRINT 'All tables and seed data have been created.';
GO
