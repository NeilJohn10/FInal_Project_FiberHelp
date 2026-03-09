-- Create the AuditLogs table for persistent logging of login attempts, errors, and user activities
-- Run this against your FiberhelpDB (LocalDB) if the table doesn't auto-create

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        EventType       NVARCHAR(50)   NOT NULL,          -- Login, Logout, Error, Create, Update, Delete, Archive, Restore, Access
        Category        NVARCHAR(50)   NOT NULL,          -- Authentication, UserActivity, SystemError
        Message         NVARCHAR(1000) NOT NULL,          -- User-friendly description (safe to display)
        TechnicalDetails NVARCHAR(4000) NULL,             -- Stack traces, inner exceptions (admin-only, NEVER shown to users)
        UserId          NVARCHAR(50)   NULL,              -- Who performed the action
        UserEmail       NVARCHAR(256)  NULL,              -- Email of the user
        UserRole        NVARCHAR(50)   NULL,              -- Role at time of event
        EntityType      NVARCHAR(100)  NULL,              -- Ticket, Client, Account, etc.
        EntityId        NVARCHAR(50)   NULL,              -- ID of affected entity
        IsSuccess       BIT            NOT NULL DEFAULT 1,
        Severity        NVARCHAR(20)   NOT NULL DEFAULT 'Info',  -- Info, Warning, Error, Critical
        Timestamp       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
    );

    -- Indexes for fast querying
    CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs (Timestamp DESC);
    CREATE INDEX IX_AuditLogs_Category  ON AuditLogs (Category);
    CREATE INDEX IX_AuditLogs_UserEmail ON AuditLogs (UserEmail);

    PRINT 'AuditLogs table created successfully.';
END
ELSE
BEGIN
    PRINT 'AuditLogs table already exists.';
END
GO
