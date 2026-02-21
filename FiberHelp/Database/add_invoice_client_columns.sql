-- =============================================
-- Add Client and Invoice Type columns to Invoices table
-- Run this on both local and cloud databases
-- =============================================

-- Add ClientId column (links to Clients table)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Invoices' AND COLUMN_NAME = 'ClientId')
BEGIN
    ALTER TABLE [dbo].[Invoices] ADD [ClientId] NVARCHAR(50) NULL;
    PRINT 'Added ClientId column to Invoices table';
END
GO

-- Add ClientName column (denormalized for display)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Invoices' AND COLUMN_NAME = 'ClientName')
BEGIN
    ALTER TABLE [dbo].[Invoices] ADD [ClientName] NVARCHAR(200) NULL;
    PRINT 'Added ClientName column to Invoices table';
END
GO

-- Add RelatedTicketId column (for service invoices linked to tickets)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Invoices' AND COLUMN_NAME = 'RelatedTicketId')
BEGIN
    ALTER TABLE [dbo].[Invoices] ADD [RelatedTicketId] INT NULL;
    PRINT 'Added RelatedTicketId column to Invoices table';
END
GO

-- Add InvoiceType column (Subscription, Service, OneTime)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Invoices' AND COLUMN_NAME = 'InvoiceType')
BEGIN
    ALTER TABLE [dbo].[Invoices] ADD [InvoiceType] NVARCHAR(50) NOT NULL DEFAULT 'Subscription';
    PRINT 'Added InvoiceType column to Invoices table';
END
GO

-- Update existing invoices to link to clients based on AccountId
UPDATE i
SET i.ClientId = c.Id,
    i.ClientName = c.Name
FROM [dbo].[Invoices] i
INNER JOIN [dbo].[Clients] c ON c.AccountId = i.AccountId
WHERE i.ClientId IS NULL AND i.AccountId IS NOT NULL;
GO

PRINT 'Invoice table updated with client integration columns';
