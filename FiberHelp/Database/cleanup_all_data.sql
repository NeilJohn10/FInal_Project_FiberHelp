-- =================================================================
-- REMOVE ALL DATA FROM FIBERHELP DATABASE
-- Run this in SSMS to clean all seed data
-- =================================================================

USE [FiberhelpDB];
GO

PRINT 'Starting complete database cleanup...';
PRINT '=================================================================';

-- Disable foreign key constraints temporarily
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
PRINT 'Foreign key constraints disabled';

-- ==========================================
-- DELETE ALL DATA (Order matters for FK!)
-- ==========================================

-- 1. Delete from child tables first
PRINT 'Deleting Tickets...';
DELETE FROM Tickets;

PRINT 'Deleting Invoices...';
DELETE FROM Invoices;

PRINT 'Deleting Clients...';
DELETE FROM Clients;

PRINT 'Deleting OutboxMessages...';
DELETE FROM OutboxMessages;

PRINT 'Deleting Expenses...';
DELETE FROM Expenses;

-- 2. Delete from user tables
PRINT 'Deleting Technicians...';
DELETE FROM Technicians;

PRINT 'Deleting Agents...';
DELETE FROM Agents;

PRINT 'Deleting Users...';
DELETE FROM Users;

-- 3. Delete from parent tables last
PRINT 'Deleting Accounts...';
DELETE FROM Accounts;

-- Re-enable foreign key constraints
EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL';
PRINT 'Foreign key constraints re-enabled';

-- ==========================================
-- RESET IDENTITY COLUMNS TO START FROM 1
-- ==========================================
PRINT '';
PRINT 'Resetting identity columns...';

DBCC CHECKIDENT ('Tickets', RESEED, 0);
DBCC CHECKIDENT ('Invoices', RESEED, 0);
DBCC CHECKIDENT ('Accounts', RESEED, 0);
DBCC CHECKIDENT ('Expenses', RESEED, 0);
DBCC CHECKIDENT ('OutboxMessages', RESEED, 0);

PRINT 'Identity columns reset to 0';

-- ==========================================
-- CREATE DEFAULT ADMIN USER
-- ==========================================
PRINT '';
PRINT 'Creating default admin user...';

-- Password hash for 'Adminlogin123@'
INSERT INTO Users (Id, Email, PasswordHash, Role, FullName, IsActive)
VALUES (NEWID(), 'admin@fiberhelp.com', '9AF15B336E6A9619928537DF30B2E6A2376569FCF9D7E773ECCEDE65606529A0', 'Administrator', 'System Admin', 1);

PRINT 'Admin user created: admin@fiberhelp.com / Adminlogin123@';

-- ==========================================
-- VERIFICATION
-- ==========================================
PRINT '';
PRINT '=================================================================';
PRINT 'VERIFICATION - All tables should show 0 records (except Users=1):';
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
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'OutboxMessages', COUNT(*) FROM OutboxMessages;

PRINT '';
PRINT '=================================================================';
PRINT 'DATABASE CLEANUP COMPLETED!';
PRINT 'Login with: admin@fiberhelp.com / Adminlogin123@';
PRINT '=================================================================';
GO
