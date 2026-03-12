-- ============================================================
-- Apply Data Masking to Email and Phone columns
-- Run this script in SSMS against FiberhelpDB
--
-- APPROACH: Rename real tables to _Data suffix, create
-- masking functions, then create VIEWS with the original
-- table names that always show masked Email/Phone.
--
-- INSTEAD OF triggers are added so INSERT/UPDATE/DELETE
-- through the views still work (e.g. from SSMS or other tools).
--
-- Your .NET MAUI app reads/writes the _Data tables directly
-- (configured in devLocalContext.cs) so login and all
-- features continue to work with real unmasked data.
--
-- After running this script:
--   Right-click dbo.Agents > Select Top 1000 Rows
--   -> Email and Phone will be MASKED for everyone.
--
-- TO UNDO: Run the ROLLBACK section at the bottom.
-- ============================================================

USE [FiberhelpDB];
GO

-- ============================================================
-- MASKING FUNCTIONS
-- ============================================================
IF OBJECT_ID('dbo.fn_MaskEmail', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_MaskEmail;
GO
CREATE FUNCTION dbo.fn_MaskEmail(@email NVARCHAR(256))
RETURNS NVARCHAR(256)
AS
BEGIN
    IF @email IS NULL OR LEN(LTRIM(RTRIM(@email))) = 0 RETURN NULL;
    DECLARE @at INT = CHARINDEX('@', @email);
    IF @at <= 1 RETURN '***@***';
    DECLARE @local NVARCHAR(256) = LEFT(@email, @at - 1);
    DECLARE @domain NVARCHAR(256) = SUBSTRING(@email, @at, LEN(@email));
    DECLARE @show INT = CASE WHEN LEN(@local) < 3 THEN LEN(@local) ELSE 3 END;
    RETURN LEFT(@local, @show) + REPLICATE('*', LEN(@local) - @show) + @domain;
END
GO

IF OBJECT_ID('dbo.fn_MaskPhone', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_MaskPhone;
GO
CREATE FUNCTION dbo.fn_MaskPhone(@phone NVARCHAR(256))
RETURNS NVARCHAR(256)
AS
BEGIN
    IF @phone IS NULL OR LEN(LTRIM(RTRIM(@phone))) = 0 RETURN NULL;
    DECLARE @prefix NVARCHAR(10) = '';
    DECLARE @digits NVARCHAR(256) = '';
    DECLARE @i INT = 1;
    IF LEFT(@phone, 1) = '+' SET @prefix = '+';
    WHILE @i <= LEN(@phone)
    BEGIN
        IF SUBSTRING(@phone, @i, 1) LIKE '[0-9]'
            SET @digits = @digits + SUBSTRING(@phone, @i, 1);
        SET @i = @i + 1;
    END
    IF LEN(@digits) <= 4 RETURN REPLICATE('*', LEN(@digits));
    RETURN @prefix + REPLICATE('*', LEN(@digits) - 4) + RIGHT(@digits, 4);
END
GO

PRINT 'Created fn_MaskEmail and fn_MaskPhone';
GO

-- ============================================================
-- 1. AGENTS
-- ============================================================
IF OBJECT_ID('dbo.trg_Agents_Insert', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Agents_Insert;
IF OBJECT_ID('dbo.trg_Agents_Update', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Agents_Update;
IF OBJECT_ID('dbo.trg_Agents_Delete', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Agents_Delete;
GO
IF OBJECT_ID('dbo.Agents', 'V') IS NOT NULL DROP VIEW dbo.Agents;
GO

IF OBJECT_ID('dbo.Agents', 'U') IS NOT NULL AND OBJECT_ID('dbo.Agents_Data', 'U') IS NULL
    EXEC sp_rename 'dbo.Agents', 'Agents_Data';
GO

CREATE VIEW dbo.Agents AS
SELECT
    [Id],
    dbo.fn_MaskEmail([Email]) AS [Email],
    [PasswordHash], [FullName], [Department],
    dbo.fn_MaskPhone([Phone]) AS [Phone],
    [IsActive], [IsLocked], [FailedLoginCount],
    [CreatedAt], [UpdatedAt], [LastLoginAt], [PasswordChangedAt],
    [Role], [ServiceArea],
    [IsArchived], [ArchivedAt], [ArchivedBy],
    [LockedUntil], [LockoutCount]
FROM dbo.Agents_Data;
GO

CREATE TRIGGER dbo.trg_Agents_Insert ON dbo.Agents INSTEAD OF INSERT AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Agents_Data (
        [Id],[Email],[PasswordHash],[FullName],[Department],[Phone],
        [IsActive],[IsLocked],[FailedLoginCount],
        [CreatedAt],[UpdatedAt],[LastLoginAt],[PasswordChangedAt],
        [Role],[ServiceArea],[IsArchived],[ArchivedAt],[ArchivedBy],
        [LockedUntil],[LockoutCount]
    )
    SELECT
        [Id],[Email],[PasswordHash],[FullName],[Department],[Phone],
        [IsActive],[IsLocked],[FailedLoginCount],
        [CreatedAt],[UpdatedAt],[LastLoginAt],[PasswordChangedAt],
        [Role],[ServiceArea],[IsArchived],[ArchivedAt],[ArchivedBy],
        [LockedUntil],[LockoutCount]
    FROM inserted;
END
GO

CREATE TRIGGER dbo.trg_Agents_Update ON dbo.Agents INSTEAD OF UPDATE AS
BEGIN
    SET NOCOUNT ON;
    UPDATE d SET
        d.[Email]=i.[Email], d.[PasswordHash]=i.[PasswordHash],
        d.[FullName]=i.[FullName], d.[Department]=i.[Department],
        d.[Phone]=i.[Phone], d.[IsActive]=i.[IsActive],
        d.[IsLocked]=i.[IsLocked], d.[FailedLoginCount]=i.[FailedLoginCount],
        d.[CreatedAt]=i.[CreatedAt], d.[UpdatedAt]=i.[UpdatedAt],
        d.[LastLoginAt]=i.[LastLoginAt], d.[PasswordChangedAt]=i.[PasswordChangedAt],
        d.[Role]=i.[Role], d.[ServiceArea]=i.[ServiceArea],
        d.[IsArchived]=i.[IsArchived], d.[ArchivedAt]=i.[ArchivedAt],
        d.[ArchivedBy]=i.[ArchivedBy],
        d.[LockedUntil]=i.[LockedUntil], d.[LockoutCount]=i.[LockoutCount]
    FROM dbo.Agents_Data d INNER JOIN inserted i ON d.[Id] = i.[Id];
END
GO

CREATE TRIGGER dbo.trg_Agents_Delete ON dbo.Agents INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    DELETE d FROM dbo.Agents_Data d INNER JOIN deleted del ON d.[Id] = del.[Id];
END
GO

PRINT 'Agents -> masked (table renamed to Agents_Data)';
GO

-- ============================================================
-- 2. TECHNICIANS
-- ============================================================
IF OBJECT_ID('dbo.trg_Technicians_Insert', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Technicians_Insert;
IF OBJECT_ID('dbo.trg_Technicians_Update', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Technicians_Update;
IF OBJECT_ID('dbo.trg_Technicians_Delete', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Technicians_Delete;
GO
IF OBJECT_ID('dbo.Technicians', 'V') IS NOT NULL DROP VIEW dbo.Technicians;
GO

IF OBJECT_ID('dbo.Technicians', 'U') IS NOT NULL AND OBJECT_ID('dbo.Technicians_Data', 'U') IS NULL
    EXEC sp_rename 'dbo.Technicians', 'Technicians_Data';
GO

CREATE VIEW dbo.Technicians AS
SELECT
    [Id],
    dbo.fn_MaskEmail([Email]) AS [Email],
    [PasswordHash], [FullName],
    dbo.fn_MaskPhone([Phone]) AS [Phone],
    [ServiceArea], [Department], [Specialization],
    [IsActive], [IsLocked], [FailedLoginCount],
    [LastLoginAt], [CreatedAt], [UpdatedAt], [PasswordChangedAt],
    [Notes], [IsArchived], [ArchivedAt], [ArchivedBy],
    [LockedUntil], [LockoutCount]
FROM dbo.Technicians_Data;
GO

CREATE TRIGGER dbo.trg_Technicians_Insert ON dbo.Technicians INSTEAD OF INSERT AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Technicians_Data (
        [Id],[Email],[PasswordHash],[FullName],[Phone],
        [ServiceArea],[Department],[Specialization],
        [IsActive],[IsLocked],[FailedLoginCount],
        [LastLoginAt],[CreatedAt],[UpdatedAt],[PasswordChangedAt],
        [Notes],[IsArchived],[ArchivedAt],[ArchivedBy],
        [LockedUntil],[LockoutCount]
    )
    SELECT
        [Id],[Email],[PasswordHash],[FullName],[Phone],
        [ServiceArea],[Department],[Specialization],
        [IsActive],[IsLocked],[FailedLoginCount],
        [LastLoginAt],[CreatedAt],[UpdatedAt],[PasswordChangedAt],
        [Notes],[IsArchived],[ArchivedAt],[ArchivedBy],
        [LockedUntil],[LockoutCount]
    FROM inserted;
END
GO

CREATE TRIGGER dbo.trg_Technicians_Update ON dbo.Technicians INSTEAD OF UPDATE AS
BEGIN
    SET NOCOUNT ON;
    UPDATE d SET
        d.[Email]=i.[Email], d.[PasswordHash]=i.[PasswordHash],
        d.[FullName]=i.[FullName], d.[Phone]=i.[Phone],
        d.[ServiceArea]=i.[ServiceArea], d.[Department]=i.[Department],
        d.[Specialization]=i.[Specialization],
        d.[IsActive]=i.[IsActive], d.[IsLocked]=i.[IsLocked],
        d.[FailedLoginCount]=i.[FailedLoginCount],
        d.[LastLoginAt]=i.[LastLoginAt], d.[CreatedAt]=i.[CreatedAt],
        d.[UpdatedAt]=i.[UpdatedAt], d.[PasswordChangedAt]=i.[PasswordChangedAt],
        d.[Notes]=i.[Notes],
        d.[IsArchived]=i.[IsArchived], d.[ArchivedAt]=i.[ArchivedAt],
        d.[ArchivedBy]=i.[ArchivedBy],
        d.[LockedUntil]=i.[LockedUntil], d.[LockoutCount]=i.[LockoutCount]
    FROM dbo.Technicians_Data d INNER JOIN inserted i ON d.[Id] = i.[Id];
END
GO

CREATE TRIGGER dbo.trg_Technicians_Delete ON dbo.Technicians INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    DELETE d FROM dbo.Technicians_Data d INNER JOIN deleted del ON d.[Id] = del.[Id];
END
GO

PRINT 'Technicians -> masked (table renamed to Technicians_Data)';
GO

-- ============================================================
-- 3. USERS
-- ============================================================
IF OBJECT_ID('dbo.trg_Users_Insert', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Users_Insert;
IF OBJECT_ID('dbo.trg_Users_Update', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Users_Update;
IF OBJECT_ID('dbo.trg_Users_Delete', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Users_Delete;
GO
IF OBJECT_ID('dbo.Users', 'V') IS NOT NULL DROP VIEW dbo.Users;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL AND OBJECT_ID('dbo.Users_Data', 'U') IS NULL
    EXEC sp_rename 'dbo.Users', 'Users_Data';
GO

CREATE VIEW dbo.Users AS
SELECT
    [Id],
    dbo.fn_MaskEmail([Email]) AS [Email],
    [PasswordHash], [Role], [FullName], [IsActive]
FROM dbo.Users_Data;
GO

CREATE TRIGGER dbo.trg_Users_Insert ON dbo.Users INSTEAD OF INSERT AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Users_Data ([Id],[Email],[PasswordHash],[Role],[FullName],[IsActive])
    SELECT [Id],[Email],[PasswordHash],[Role],[FullName],[IsActive] FROM inserted;
END
GO

CREATE TRIGGER dbo.trg_Users_Update ON dbo.Users INSTEAD OF UPDATE AS
BEGIN
    SET NOCOUNT ON;
    UPDATE d SET
        d.[Email]=i.[Email], d.[PasswordHash]=i.[PasswordHash],
        d.[Role]=i.[Role], d.[FullName]=i.[FullName], d.[IsActive]=i.[IsActive]
    FROM dbo.Users_Data d INNER JOIN inserted i ON d.[Id] = i.[Id];
END
GO

CREATE TRIGGER dbo.trg_Users_Delete ON dbo.Users INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    DELETE d FROM dbo.Users_Data d INNER JOIN deleted del ON d.[Id] = del.[Id];
END
GO

PRINT 'Users -> masked (table renamed to Users_Data)';
GO

-- ============================================================
-- 4. ACCOUNTS
-- ============================================================
IF OBJECT_ID('dbo.trg_Accounts_Insert', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Accounts_Insert;
IF OBJECT_ID('dbo.trg_Accounts_Update', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Accounts_Update;
IF OBJECT_ID('dbo.trg_Accounts_Delete', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Accounts_Delete;
GO
IF OBJECT_ID('dbo.Accounts', 'V') IS NOT NULL DROP VIEW dbo.Accounts;
GO

IF OBJECT_ID('dbo.Accounts', 'U') IS NOT NULL AND OBJECT_ID('dbo.Accounts_Data', 'U') IS NULL
    EXEC sp_rename 'dbo.Accounts', 'Accounts_Data';
GO

CREATE VIEW dbo.Accounts AS
SELECT
    [AccountId],
    [AccountNumber], [Name], [Type],
    [ServicePlan], [Status], [IsActive],
    [ContactName],
    dbo.fn_MaskEmail([ContactEmail]) AS [ContactEmail],
    dbo.fn_MaskPhone([ContactPhone]) AS [ContactPhone],
    [BillingAddress], [CreatedAt],
    [IsArchived], [ArchivedAt], [ArchivedBy]
FROM dbo.Accounts_Data;
GO

CREATE TRIGGER dbo.trg_Accounts_Insert ON dbo.Accounts INSTEAD OF INSERT AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Accounts_Data (
        [AccountNumber],[Name],[Type],[ServicePlan],[Status],[IsActive],
        [ContactName],[ContactEmail],[ContactPhone],[BillingAddress],[CreatedAt],
        [IsArchived],[ArchivedAt],[ArchivedBy]
    )
    SELECT
        [AccountNumber],[Name],[Type],[ServicePlan],[Status],[IsActive],
        [ContactName],[ContactEmail],[ContactPhone],[BillingAddress],[CreatedAt],
        [IsArchived],[ArchivedAt],[ArchivedBy]
    FROM inserted;
END
GO

CREATE TRIGGER dbo.trg_Accounts_Update ON dbo.Accounts INSTEAD OF UPDATE AS
BEGIN
    SET NOCOUNT ON;
    UPDATE d SET
        d.[AccountNumber]=i.[AccountNumber], d.[Name]=i.[Name], d.[Type]=i.[Type],
        d.[ServicePlan]=i.[ServicePlan], d.[Status]=i.[Status], d.[IsActive]=i.[IsActive],
        d.[ContactName]=i.[ContactName], d.[ContactEmail]=i.[ContactEmail],
        d.[ContactPhone]=i.[ContactPhone], d.[BillingAddress]=i.[BillingAddress],
        d.[CreatedAt]=i.[CreatedAt],
        d.[IsArchived]=i.[IsArchived], d.[ArchivedAt]=i.[ArchivedAt], d.[ArchivedBy]=i.[ArchivedBy]
    FROM dbo.Accounts_Data d INNER JOIN inserted i ON d.[AccountId] = i.[AccountId];
END
GO

CREATE TRIGGER dbo.trg_Accounts_Delete ON dbo.Accounts INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    DELETE d FROM dbo.Accounts_Data d INNER JOIN deleted del ON d.[AccountId] = del.[AccountId];
END
GO

PRINT 'Accounts -> masked (table renamed to Accounts_Data)';
GO

-- ============================================================
-- 5. CLIENTS
-- ============================================================
IF OBJECT_ID('dbo.trg_Clients_Insert', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Clients_Insert;
IF OBJECT_ID('dbo.trg_Clients_Update', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Clients_Update;
IF OBJECT_ID('dbo.trg_Clients_Delete', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Clients_Delete;
GO
IF OBJECT_ID('dbo.Clients', 'V') IS NOT NULL DROP VIEW dbo.Clients;
GO

IF OBJECT_ID('dbo.Clients', 'U') IS NOT NULL AND OBJECT_ID('dbo.Clients_Data', 'U') IS NULL
    EXEC sp_rename 'dbo.Clients', 'Clients_Data';
GO

CREATE VIEW dbo.Clients AS
SELECT
    [Id], [Name],
    dbo.fn_MaskEmail([Email]) AS [Email],
    [Plan], [Status], [JoinDate], [AccountId],
    [IsArchived], [ArchivedAt], [ArchivedByUserId]
FROM dbo.Clients_Data;
GO

CREATE TRIGGER dbo.trg_Clients_Insert ON dbo.Clients INSTEAD OF INSERT AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Clients_Data (
        [Id],[Name],[Email],[Plan],[Status],[JoinDate],[AccountId],
        [IsArchived],[ArchivedAt],[ArchivedByUserId]
    )
    SELECT
        [Id],[Name],[Email],[Plan],[Status],[JoinDate],[AccountId],
        [IsArchived],[ArchivedAt],[ArchivedByUserId]
    FROM inserted;
END
GO

CREATE TRIGGER dbo.trg_Clients_Update ON dbo.Clients INSTEAD OF UPDATE AS
BEGIN
    SET NOCOUNT ON;
    UPDATE d SET
        d.[Name]=i.[Name], d.[Email]=i.[Email],
        d.[Plan]=i.[Plan], d.[Status]=i.[Status],
        d.[JoinDate]=i.[JoinDate], d.[AccountId]=i.[AccountId],
        d.[IsArchived]=i.[IsArchived], d.[ArchivedAt]=i.[ArchivedAt],
        d.[ArchivedByUserId]=i.[ArchivedByUserId]
    FROM dbo.Clients_Data d INNER JOIN inserted i ON d.[Id] = i.[Id];
END
GO

CREATE TRIGGER dbo.trg_Clients_Delete ON dbo.Clients INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    DELETE d FROM dbo.Clients_Data d INNER JOIN deleted del ON d.[Id] = del.[Id];
END
GO

PRINT 'Clients -> masked (table renamed to Clients_Data)';
GO

-- ============================================================
-- VERIFICATION
-- ============================================================
PRINT '';
PRINT '=== VERIFICATION ===';
SELECT 'Agents' AS [Table], Email, Phone FROM dbo.Agents;
SELECT 'Technicians' AS [Table], Email, Phone FROM dbo.Technicians;
SELECT 'Accounts' AS [Table], ContactEmail, ContactPhone FROM dbo.Accounts;
SELECT 'Clients' AS [Table], Email FROM dbo.Clients;
SELECT 'Users' AS [Table], Email FROM dbo.Users;
GO

PRINT '';
PRINT '=============================================';
PRINT '  DATA MASKING COMPLETE';
PRINT '=============================================';
PRINT '';
PRINT '  Right-click any table > Select Top 1000';
PRINT '  Email and Phone are now ALWAYS MASKED.';
PRINT '';
PRINT '  Your app reads/writes the _Data tables';
PRINT '  directly (via devLocalContext.cs) so login';
PRINT '  and all features work with real data.';
PRINT '';
PRINT '  To see real data, query _Data tables:';
PRINT '    SELECT * FROM dbo.Agents_Data;';
PRINT '    SELECT * FROM dbo.Technicians_Data;';
PRINT '    SELECT * FROM dbo.Users_Data;';
PRINT '    SELECT * FROM dbo.Accounts_Data;';
PRINT '    SELECT * FROM dbo.Clients_Data;';
PRINT '';
PRINT '=============================================';
PRINT '  TO UNDO (rollback to original tables):';
PRINT '=============================================';
PRINT '  DROP VIEW dbo.Agents;';
PRINT '  EXEC sp_rename ''Agents_Data'', ''Agents'';';
PRINT '  DROP VIEW dbo.Technicians;';
PRINT '  EXEC sp_rename ''Technicians_Data'', ''Technicians'';';
PRINT '  DROP VIEW dbo.Users;';
PRINT '  EXEC sp_rename ''Users_Data'', ''Users'';';
PRINT '  DROP VIEW dbo.Accounts;';
PRINT '  EXEC sp_rename ''Accounts_Data'', ''Accounts'';';
PRINT '  DROP VIEW dbo.Clients;';
PRINT '  EXEC sp_rename ''Clients_Data'', ''Clients'';';
PRINT '  DROP FUNCTION dbo.fn_MaskEmail;';
PRINT '  DROP FUNCTION dbo.fn_MaskPhone;';
GO
