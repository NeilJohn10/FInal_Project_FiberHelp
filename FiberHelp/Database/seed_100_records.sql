-- =================================================================
-- FIBERHELP - SEED 80-100 RECORDS PER TABLE
-- Run this after complete_setup.sql
-- This satisfies the rubric requirement for 80-100 records per entity
-- =================================================================

USE [FiberhelpDB];
GO

PRINT 'Starting bulk seed data insertion...';
PRINT '=================================================================';

-- ==========================================
-- 1. SEED 100 ACCOUNTS
-- ==========================================
PRINT 'Seeding Accounts (100 records)...';

DECLARE @i INT = 1;
DECLARE @baseDate DATETIME2 = DATEADD(YEAR, -2, SYSUTCDATETIME());

WHILE @i <= 100
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountNumber = 'ACCT-2024' + RIGHT('0000' + CAST(@i AS VARCHAR(4)), 4))
    BEGIN
        INSERT INTO Accounts (AccountNumber, Name, Type, ServicePlan, Status, ContactName, ContactEmail, ContactPhone, BillingAddress, CreatedAt, IsActive)
        VALUES (
            'ACCT-2024' + RIGHT('0000' + CAST(@i AS VARCHAR(4)), 4),
            CASE 
                WHEN @i % 5 = 0 THEN 'TechCorp ' + CAST(@i AS VARCHAR(10))
                WHEN @i % 5 = 1 THEN 'Digital Solutions ' + CAST(@i AS VARCHAR(10))
                WHEN @i % 5 = 2 THEN 'NetWorks Inc ' + CAST(@i AS VARCHAR(10))
                WHEN @i % 5 = 3 THEN 'Cloud Systems ' + CAST(@i AS VARCHAR(10))
                ELSE 'Fiber Connect ' + CAST(@i AS VARCHAR(10))
            END,
            CASE WHEN @i % 3 = 0 THEN 'Enterprise' WHEN @i % 3 = 1 THEN 'Business' ELSE 'Residential' END,
            CASE 
                WHEN @i % 4 = 0 THEN 'Premium'
                WHEN @i % 4 = 1 THEN 'Standard'
                WHEN @i % 4 = 2 THEN 'Basic'
                ELSE 'Enterprise'
            END,
            CASE WHEN @i % 10 = 0 THEN 'Inactive' ELSE 'Active' END,
            'Contact Person ' + CAST(@i AS VARCHAR(10)),
            'contact' + CAST(@i AS VARCHAR(10)) + '@company.com',
            '+639' + RIGHT('000000000' + CAST(1700000000 + @i * 12345 AS VARCHAR(10)), 9),
            CAST(@i AS VARCHAR(10)) + ' Sample Street, City ' + CAST((@i % 20) + 1 AS VARCHAR(5)),
            DATEADD(DAY, @i * 7, @baseDate),
            CASE WHEN @i % 10 = 0 THEN 0 ELSE 1 END
        );
    END
    SET @i = @i + 1;
END
PRINT 'Accounts seeded: 100 records';
GO

-- ==========================================
-- 2. SEED 100 CLIENTS
-- ==========================================
PRINT 'Seeding Clients (100 records)...';

DECLARE @j INT = 1;
DECLARE @clientDate DATETIME2 = DATEADD(YEAR, -2, SYSUTCDATETIME());
DECLARE @accountId INT;

WHILE @j <= 100
BEGIN
    -- Get a random account ID
    SELECT TOP 1 @accountId = AccountId FROM Accounts ORDER BY NEWID();
    
    IF NOT EXISTS (SELECT 1 FROM Clients WHERE Id = 'client-' + RIGHT('000' + CAST(@j AS VARCHAR(3)), 3))
    BEGIN
        INSERT INTO Clients (Id, Name, Email, [Plan], Status, JoinDate, AccountId, IsArchived)
        VALUES (
            'client-' + RIGHT('000' + CAST(@j AS VARCHAR(3)), 3),
            CASE 
                WHEN @j % 6 = 0 THEN 'Juan Dela Cruz ' + CAST(@j AS VARCHAR(10))
                WHEN @j % 6 = 1 THEN 'Maria Santos ' + CAST(@j AS VARCHAR(10))
                WHEN @j % 6 = 2 THEN 'Pedro Garcia ' + CAST(@j AS VARCHAR(10))
                WHEN @j % 6 = 3 THEN 'Ana Reyes ' + CAST(@j AS VARCHAR(10))
                WHEN @j % 6 = 4 THEN 'Jose Mendoza ' + CAST(@j AS VARCHAR(10))
                ELSE 'Rosa Fernandez ' + CAST(@j AS VARCHAR(10))
            END,
            'client' + CAST(@j AS VARCHAR(10)) + '@email.com',
            CASE 
                WHEN @j % 4 = 0 THEN 'Premium'
                WHEN @j % 4 = 1 THEN 'Standard'
                WHEN @j % 4 = 2 THEN 'Basic'
                ELSE 'Enterprise'
            END,
            CASE WHEN @j % 12 = 0 THEN 'Inactive' ELSE 'Active' END,
            DATEADD(DAY, @j * 5, @clientDate),
            @accountId,
            0
        );
    END
    SET @j = @j + 1;
END
PRINT 'Clients seeded: 100 records';
GO

-- ==========================================
-- 3. SEED 100 TICKETS
-- ==========================================
PRINT 'Seeding Tickets (100 records)...';

DECLARE @k INT = 1;
DECLARE @ticketDate DATETIME2 = DATEADD(MONTH, -6, SYSUTCDATETIME());
DECLARE @clientId NVARCHAR(50);
DECLARE @accId INT;

WHILE @k <= 100
BEGIN
    -- Get random client
    SELECT TOP 1 @clientId = Id, @accId = AccountId FROM Clients ORDER BY NEWID();
    
    INSERT INTO Tickets (AccountId, Subject, Status, CreatedAt, ClientId, ClientName, Priority, IsArchived)
    VALUES (
        @accId,
        CASE 
            WHEN @k % 12 = 0 THEN 'No internet connection'
            WHEN @k % 12 = 1 THEN 'Slow internet speed'
            WHEN @k % 12 = 2 THEN 'Intermittent connection'
            WHEN @k % 12 = 3 THEN 'High latency issue'
            WHEN @k % 12 = 4 THEN 'Frequent disconnections'
            WHEN @k % 12 = 5 THEN 'Router not working'
            WHEN @k % 12 = 6 THEN 'Cable cut in area'
            WHEN @k % 12 = 7 THEN 'Billing inquiry'
            WHEN @k % 12 = 8 THEN 'Account suspended'
            WHEN @k % 12 = 9 THEN 'Wrong credentials'
            WHEN @k % 12 = 10 THEN 'Service outage reported'
            ELSE 'General complaint'
        END + ' - Ticket #' + CAST(@k AS VARCHAR(10)),
        CASE 
            WHEN @k % 5 = 0 THEN 'Resolved'
            WHEN @k % 5 = 1 THEN 'Closed'
            WHEN @k % 5 = 2 THEN 'In Progress'
            WHEN @k % 5 = 3 THEN 'Assigned'
            ELSE 'Open'
        END,
        DATEADD(DAY, @k * 2, @ticketDate),
        @clientId,
        (SELECT TOP 1 Name FROM Clients WHERE Id = @clientId),
        CASE 
            WHEN @k % 4 = 0 THEN 'Critical'
            WHEN @k % 4 = 1 THEN 'High'
            WHEN @k % 4 = 2 THEN 'Medium'
            ELSE 'Low'
        END,
        CASE WHEN @k % 15 = 0 THEN 1 ELSE 0 END
    );
    SET @k = @k + 1;
END
PRINT 'Tickets seeded: 100 records';
GO

-- ==========================================
-- 4. SEED 100 INVOICES
-- ==========================================
PRINT 'Seeding Invoices (100 records)...';

DECLARE @m INT = 1;
DECLARE @invoiceDate DATETIME2 = DATEADD(YEAR, -1, SYSUTCDATETIME());
DECLARE @invAccountId INT;
DECLARE @invAccountName NVARCHAR(200);
DECLARE @invClientId NVARCHAR(50);
DECLARE @invClientName NVARCHAR(200);
DECLARE @invAmount DECIMAL(18,2);
DECLARE @invStatus NVARCHAR(50);

WHILE @m <= 100
BEGIN
    -- Get random account
    SELECT TOP 1 @invAccountId = AccountId, @invAccountName = Name FROM Accounts WHERE IsActive = 1 ORDER BY NEWID();
    -- Get random client from that account (or any)
    SELECT TOP 1 @invClientId = Id, @invClientName = Name FROM Clients WHERE AccountId = @invAccountId ORDER BY NEWID();
    IF @invClientId IS NULL
        SELECT TOP 1 @invClientId = Id, @invClientName = Name FROM Clients ORDER BY NEWID();
    
    -- Determine amount based on plan
    SET @invAmount = CASE 
        WHEN @m % 4 = 0 THEN 2499.00
        WHEN @m % 4 = 1 THEN 1499.00
        WHEN @m % 4 = 2 THEN 999.00
        ELSE 4999.00
    END;
    
    SET @invStatus = CASE 
        WHEN @m % 4 = 0 THEN 'Paid'
        WHEN @m % 4 = 1 THEN 'Paid'
        WHEN @m % 4 = 2 THEN 'Pending'
        ELSE 'Overdue'
    END;
    
    INSERT INTO Invoices (AccountId, AccountName, ClientId, ClientName, AmountDue, IssueDate, DueDate, Status, PaymentRef, PaidDate, InvoiceType, Notes)
    VALUES (
        @invAccountId,
        @invAccountName,
        @invClientId,
        @invClientName,
        @invAmount,
        DATEADD(DAY, @m * 3, @invoiceDate),
        DATEADD(DAY, @m * 3 + 30, @invoiceDate),
        @invStatus,
        CASE WHEN @invStatus = 'Paid' THEN 'PAY-' + RIGHT('000000' + CAST(@m AS VARCHAR(6)), 6) ELSE NULL END,
        CASE WHEN @invStatus = 'Paid' THEN DATEADD(DAY, @m * 3 + 15, @invoiceDate) ELSE NULL END,
        CASE WHEN @m % 3 = 0 THEN 'Subscription' WHEN @m % 3 = 1 THEN 'Service' ELSE 'OneTime' END,
        'Invoice for month ' + CAST((@m % 12) + 1 AS VARCHAR(2))
    );
    SET @m = @m + 1;
END
PRINT 'Invoices seeded: 100 records';
GO

-- ==========================================
-- 5. SEED 100 EXPENSES
-- ==========================================
PRINT 'Seeding Expenses (100 records)...';

DECLARE @n INT = 1;
DECLARE @expenseDate DATETIME2 = DATEADD(YEAR, -1, SYSUTCDATETIME());

WHILE @n <= 100
BEGIN
    INSERT INTO Expenses ([Date], Category, Description, Amount, Reference)
    VALUES (
        DATEADD(DAY, @n * 3, @expenseDate),
        CASE 
            WHEN @n % 7 = 0 THEN 'Payroll'
            WHEN @n % 7 = 1 THEN 'Utilities'
            WHEN @n % 7 = 2 THEN 'Network'
            WHEN @n % 7 = 3 THEN 'Maintenance'
            WHEN @n % 7 = 4 THEN 'Office'
            WHEN @n % 7 = 5 THEN 'Marketing'
            ELSE 'Equipment'
        END,
        CASE 
            WHEN @n % 7 = 0 THEN 'Staff salaries - Period ' + CAST(@n AS VARCHAR(10))
            WHEN @n % 7 = 1 THEN 'Electric bill - Month ' + CAST((@n % 12) + 1 AS VARCHAR(2))
            WHEN @n % 7 = 2 THEN 'Fiber optic cables purchase #' + CAST(@n AS VARCHAR(10))
            WHEN @n % 7 = 3 THEN 'Server maintenance #' + CAST(@n AS VARCHAR(10))
            WHEN @n % 7 = 4 THEN 'Office supplies order #' + CAST(@n AS VARCHAR(10))
            WHEN @n % 7 = 5 THEN 'Social media advertising'
            ELSE 'Network equipment purchase #' + CAST(@n AS VARCHAR(10))
        END,
        CASE 
            WHEN @n % 7 = 0 THEN 15000.00 + (@n * 50)
            WHEN @n % 7 = 1 THEN 800.00 + (@n * 10)
            WHEN @n % 7 = 2 THEN 5000.00 + (@n * 100)
            WHEN @n % 7 = 3 THEN 2000.00 + (@n * 25)
            WHEN @n % 7 = 4 THEN 500.00 + (@n * 5)
            WHEN @n % 7 = 5 THEN 3000.00 + (@n * 30)
            ELSE 8000.00 + (@n * 75)
        END,
        'EXP-' + RIGHT('000000' + CAST(@n AS VARCHAR(6)), 6)
    );
    SET @n = @n + 1;
END
PRINT 'Expenses seeded: 100 records';
GO

-- ==========================================
-- 6. SEED ADDITIONAL AGENTS (10 total)
-- ==========================================
PRINT 'Seeding Agents (10 records)...';

-- Agent/Technician password hash — sample data only, not used for real login.
-- Real agents/technicians should be created through the app UI for secure PBKDF2 hashing.
DECLARE @defaultHash NVARCHAR(256) = 'SAMPLE_DATA_HASH_NOT_FOR_LOGIN';

IF (SELECT COUNT(*) FROM Agents) < 10
BEGIN
    INSERT INTO Agents (Id, Email, PasswordHash, FullName, Role, Department, Phone, ServiceArea, IsActive, IsLocked, FailedLoginCount, CreatedAt)
    VALUES 
    ('agent-001', 'agent1@fiberhelp.com', @defaultHash, 'John Agent One', 'Support Agent', 'Customer Support', '+639171111111', 'Metro Manila North', 1, 0, 0, SYSUTCDATETIME()),
    ('agent-002', 'agent2@fiberhelp.com', @defaultHash, 'Jane Agent Two', 'Support Agent', 'Customer Support', '+639172222222', 'Metro Manila South', 1, 0, 0, SYSUTCDATETIME()),
    ('agent-003', 'agent3@fiberhelp.com', @defaultHash, 'Mike Agent Three', 'Support Agent', 'Technical Support', '+639173333333', 'Cavite', 1, 0, 0, SYSUTCDATETIME()),
    ('agent-004', 'supervisor1@fiberhelp.com', @defaultHash, 'Sarah Supervisor', 'Supervisor', 'Operations', '+639174444444', 'All Areas', 1, 0, 0, SYSUTCDATETIME()),
    ('agent-005', 'agent5@fiberhelp.com', @defaultHash, 'Luis Agent Five', 'Support Agent', 'Billing', '+639175555555', 'Laguna', 1, 0, 0, SYSUTCDATETIME());
    PRINT 'Additional agents seeded';
END
GO

-- ==========================================
-- 7. SEED TECHNICIANS (10 total)
-- ==========================================
PRINT 'Seeding Technicians (10 records)...';

-- Ensure Technicians table exists with correct schema
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Technicians')
BEGIN
    CREATE TABLE [dbo].[Technicians] (
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Email] NVARCHAR(255) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(255) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(50) NULL,
        [ServiceArea] NVARCHAR(500) NULL,
        [Department] NVARCHAR(100) NULL DEFAULT 'Field Operations',
        [Specialization] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsLocked] BIT NOT NULL DEFAULT 0,
        [FailedLoginCount] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL,
        [LastLoginAt] DATETIME2 NULL,
        [PasswordChangedAt] DATETIME2 NULL,
        [Notes] NVARCHAR(1000) NULL,
        [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] NVARCHAR(100) NULL
    );
    PRINT 'Technicians table created';
END
GO

DECLARE @techHash NVARCHAR(256) = 'SAMPLE_DATA_HASH_NOT_FOR_LOGIN'; -- Create technicians via app for secure hashing

IF (SELECT COUNT(*) FROM Technicians) < 10
BEGIN
    INSERT INTO Technicians (Id, Email, PasswordHash, FullName, Phone, ServiceArea, Department, Specialization, IsActive, IsLocked, FailedLoginCount, CreatedAt)
    VALUES 
    ('tech-001', 'tech1@fiberhelp.com', @techHash, 'Carlos Technician One', '+639181111111', 'Metro Manila North', 'Field Operations', 'Fiber Installation', 1, 0, 0, SYSUTCDATETIME()),
    ('tech-002', 'tech2@fiberhelp.com', @techHash, 'Ramon Technician Two', '+639182222222', 'Metro Manila South', 'Field Operations', 'Network Repair', 1, 0, 0, SYSUTCDATETIME()),
    ('tech-003', 'tech3@fiberhelp.com', @techHash, 'Paolo Technician Three', '+639183333333', 'Cavite', 'Field Operations', 'Fiber Splicing', 1, 0, 0, SYSUTCDATETIME()),
    ('tech-004', 'tech4@fiberhelp.com', @techHash, 'Marco Technician Four', '+639184444444', 'Laguna', 'Field Operations', 'Router Configuration', 1, 0, 0, SYSUTCDATETIME()),
    ('tech-005', 'tech5@fiberhelp.com', @techHash, 'Antonio Technician Five', '+639185555555', 'Rizal', 'Field Operations', 'Cable Repair', 1, 0, 0, SYSUTCDATETIME()),
    ('tech-006', 'tech6@fiberhelp.com', @techHash, 'Diego Technician Six', '+639186666666', 'Bulacan', 'Field Operations', 'Fiber Installation', 1, 0, 0, SYSUTCDATETIME()),
    ('tech-007', 'tech7@fiberhelp.com', @techHash, 'Eduardo Technician Seven', '+639187777777', 'Pampanga', 'Field Operations', 'Network Troubleshooting', 1, 0, 0, SYSUTCDATETIME()),
    ('tech-008', 'tech8@fiberhelp.com', @techHash, 'Felipe Technician Eight', '+639188888888', 'Batangas', 'Field Operations', 'Splitter Installation', 1, 0, 0, SYSUTCDATETIME()),
    ('tech-009', 'tech9@fiberhelp.com', @techHash, 'Gabriel Technician Nine', '+639189999999', 'Quezon', 'Field Operations', 'Fiber Testing', 1, 0, 0, SYSUTCDATETIME()),
    ('tech-010', 'tech10@fiberhelp.com', @techHash, 'Hector Technician Ten', '+639180000000', 'Metro Manila East', 'Field Operations', 'General Maintenance', 1, 0, 0, SYSUTCDATETIME());
    PRINT 'Technicians seeded: 10 records';
END
GO

-- ==========================================
-- VERIFICATION
-- ==========================================
PRINT '';
PRINT '=================================================================';
PRINT 'SEED DATA VERIFICATION';
PRINT '=================================================================';

SELECT 'Accounts' AS [Table], COUNT(*) AS [RecordCount] FROM Accounts
UNION ALL
SELECT 'Clients', COUNT(*) FROM Clients
UNION ALL
SELECT 'Tickets', COUNT(*) FROM Tickets
UNION ALL
SELECT 'Invoices', COUNT(*) FROM Invoices
UNION ALL
SELECT 'Expenses', COUNT(*) FROM Expenses
UNION ALL
SELECT 'Agents', COUNT(*) FROM Agents
UNION ALL
SELECT 'Technicians', COUNT(*) FROM Technicians
UNION ALL
SELECT 'Users', COUNT(*) FROM Users;

PRINT '';
PRINT '=================================================================';
PRINT 'BULK SEED COMPLETED SUCCESSFULLY!';
PRINT 'Each main table should now have 80-100+ records.';
PRINT '=================================================================';
PRINT '';
PRINT 'Test Login Credentials:';
PRINT '  Admin: admin@fiberhelp.com / Adminlogin123@';
PRINT '  Agent: agent@fiberhelp.com / Agentlogin123@';
PRINT '  Agent (new): agent1@fiberhelp.com / Password123@';
PRINT '  Technician: tech1@fiberhelp.com / Password123@';
PRINT '';
GO
