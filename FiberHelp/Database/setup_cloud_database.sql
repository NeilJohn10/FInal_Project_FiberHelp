-- =====================================================
-- CLOUD DATABASE SETUP SCRIPT
-- Run this on your CLOUD database (db33569 on db33569.public.databaseasp.net)
-- This creates all required tables for FiberHelp sync
-- =====================================================

-- 1. Create Accounts table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Accounts')
BEGIN
    CREATE TABLE [dbo].[Accounts](
        [AccountId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [AccountNumber] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(200) NULL,
        [Type] NVARCHAR(100) NULL,
        [ServicePlan] NVARCHAR(100) NULL,
        [ContactName] NVARCHAR(200) NULL,
        [ContactEmail] NVARCHAR(256) NULL,
        [ContactPhone] NVARCHAR(50) NULL,
        [BillingAddress] NVARCHAR(400) NULL,
        [Status] NVARCHAR(50) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] NVARCHAR(100) NULL
    );
    CREATE UNIQUE INDEX IX_Accounts_AccountNumber ON [dbo].[Accounts](AccountNumber);
    PRINT 'Created Accounts table';
END
ELSE
    PRINT 'Accounts table already exists';

-- 2. Create Clients table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Clients')
BEGIN
    CREATE TABLE [dbo].[Clients](
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Email] NVARCHAR(200) NULL,
        [Plan] NVARCHAR(100) NULL,
        [Status] NVARCHAR(50) NULL,
        [JoinDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [AccountId] INT NULL,
        [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedByUserId] NVARCHAR(50) NULL,
        CONSTRAINT FK_Clients_Accounts FOREIGN KEY (AccountId) REFERENCES [dbo].[Accounts](AccountId)
    );
    PRINT 'Created Clients table';
END
ELSE
    PRINT 'Clients table already exists';

-- 3. Create Agents table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Agents')
BEGIN
    CREATE TABLE [dbo].[Agents](
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Email] NVARCHAR(256) NOT NULL,
        [PasswordHash] NVARCHAR(255) NOT NULL,
        [FullName] NVARCHAR(256) NULL,
        [Role] NVARCHAR(50) NULL DEFAULT 'Support Agent',
        [Department] NVARCHAR(100) NULL,
        [Phone] NVARCHAR(50) NULL,
        [ServiceArea] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsLocked] BIT NOT NULL DEFAULT 0,
        [FailedLoginCount] INT NOT NULL DEFAULT 0,
        [LastLoginAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        [PasswordChangedAt] DATETIME2 NULL,
        [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] NVARCHAR(100) NULL
    );
    CREATE UNIQUE INDEX IX_Agents_Email ON [dbo].[Agents](Email);
    PRINT 'Created Agents table';
END
ELSE
    PRINT 'Agents table already exists';

-- 4. Create Technicians table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Technicians')
BEGIN
    CREATE TABLE [dbo].[Technicians](
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Email] NVARCHAR(255) NOT NULL,
        [PasswordHash] NVARCHAR(255) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(50) NULL,
        [ServiceArea] NVARCHAR(500) NULL,
        [Department] NVARCHAR(100) NULL DEFAULT 'Field Operations',
        [Specialization] NVARCHAR(200) NULL,
        [Notes] NVARCHAR(1000) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsLocked] BIT NOT NULL DEFAULT 0,
        [FailedLoginCount] INT NOT NULL DEFAULT 0,
        [LastLoginAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        [PasswordChangedAt] DATETIME2 NULL,
        [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] NVARCHAR(100) NULL
    );
    CREATE UNIQUE INDEX IX_Technicians_Email ON [dbo].[Technicians](Email);
    PRINT 'Created Technicians table';
END
ELSE
    PRINT 'Technicians table already exists';

-- 5. Create Tickets table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Tickets')
BEGIN
    CREATE TABLE [dbo].[Tickets](
        [TicketId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Subject] NVARCHAR(500) NULL,
        [Status] NVARCHAR(50) NULL,
        [Priority] NVARCHAR(50) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ClientId] NVARCHAR(50) NULL,
        [ClientName] NVARCHAR(256) NULL,
        [AccountId] INT NULL,
        [AssignedAgentId] NVARCHAR(50) NULL,
        [AssignedTechnicianId] NVARCHAR(50) NULL,
        [AssignedAt] DATETIME2 NULL,
        [ResolvedByTechnicianId] NVARCHAR(50) NULL,
        [ResolvedAt] DATETIME2 NULL,
        [ResolutionNotes] NVARCHAR(1000) NULL,
        [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedByUserId] NVARCHAR(50) NULL,
        [ClosedAt] DATETIME2 NULL
    );
    PRINT 'Created Tickets table';
END
ELSE
    PRINT 'Tickets table already exists';

-- 6. Create Invoices table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Invoices')
BEGIN
    CREATE TABLE [dbo].[Invoices](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [AccountId] INT NULL,
        [AccountName] NVARCHAR(200) NULL,
        [ClientId] NVARCHAR(50) NULL,
        [ClientName] NVARCHAR(200) NULL,
        [AmountDue] DECIMAL(18,2) NOT NULL,
        [IssueDate] DATETIME2 NOT NULL,
        [DueDate] DATETIME2 NULL,
        [Status] NVARCHAR(50) NULL,
        [PaymentRef] NVARCHAR(100) NULL,
        [PaidDate] DATETIME2 NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [InvoiceType] NVARCHAR(50) NULL DEFAULT 'Subscription',
        [RelatedTicketId] INT NULL
    );
    PRINT 'Created Invoices table';
END
ELSE
    PRINT 'Invoices table already exists';

-- 7. Create Expenses table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Expenses')
BEGIN
    CREATE TABLE [dbo].[Expenses](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Date] DATETIME2 NOT NULL,
        [Category] NVARCHAR(100) NULL,
        [Description] NVARCHAR(400) NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Reference] NVARCHAR(100) NULL
    );
    PRINT 'Created Expenses table';
END
ELSE
    PRINT 'Expenses table already exists';

-- 8. Create Users table (if needed)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users](
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Email] NVARCHAR(256) NOT NULL,
        [PasswordHash] NVARCHAR(255) NULL,
        [FullName] NVARCHAR(200) NULL,
        [Role] NVARCHAR(50) NULL
    );
    CREATE UNIQUE INDEX IX_Users_Email ON [dbo].[Users](Email);
    PRINT 'Created Users table';
END
ELSE
    PRINT 'Users table already exists';

-- 9. Create OutboxMessages table (for tracking sync - optional on cloud)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OutboxMessages')
BEGIN
    CREATE TABLE [dbo].[OutboxMessages](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EntityName] NVARCHAR(100) NOT NULL,
        [Operation] NVARCHAR(20) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [Key] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [ProcessedAt] DATETIME2 NULL
    );
    PRINT 'Created OutboxMessages table';
END
ELSE
    PRINT 'OutboxMessages table already exists';

-- =====================================================
-- VERIFICATION: Show all tables in the database
-- =====================================================
PRINT '';
PRINT '=== VERIFICATION: All tables in database ===';
SELECT TABLE_NAME, 
       (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) as ColumnCount
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

PRINT '';
PRINT '=== Cloud database setup complete! ===';
PRINT 'Now reset your local OutboxMessages and restart the app to sync data.';
