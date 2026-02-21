-- =============================================
-- Transaction Proof and Client Feedback Tables
-- For CRM transaction validation and client feedback
-- =============================================

-- Create TransactionProofs table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TransactionProofs')
BEGIN
    CREATE TABLE [dbo].[TransactionProofs] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ReferenceNumber] NVARCHAR(50) NOT NULL,
        [TransactionType] NVARCHAR(50) NOT NULL,
        [TicketId] INT NULL,
        [InvoiceId] INT NULL,
        [ClientId] NVARCHAR(50) NULL,
        [ClientName] NVARCHAR(200) NULL,
        [AccountId] INT NULL,
        [Description] NVARCHAR(1000) NOT NULL,
        [Amount] DECIMAL(18,2) NULL,
        [CompletedById] NVARCHAR(50) NULL,
        [CompletedByName] NVARCHAR(200) NULL,
        [CompletedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Generated',
        [AcknowledgedAt] DATETIME2 NULL,
        [Notes] NVARCHAR(2000) NULL,
        [VerificationCode] NVARCHAR(500) NULL
    );

    -- Create unique index on ReferenceNumber
    CREATE UNIQUE INDEX [IX_TransactionProofs_ReferenceNumber] 
    ON [dbo].[TransactionProofs] ([ReferenceNumber]);

    -- Create indexes for common queries
    CREATE INDEX [IX_TransactionProofs_ClientId] 
    ON [dbo].[TransactionProofs] ([ClientId]);

    CREATE INDEX [IX_TransactionProofs_TicketId] 
    ON [dbo].[TransactionProofs] ([TicketId]);

    CREATE INDEX [IX_TransactionProofs_InvoiceId] 
    ON [dbo].[TransactionProofs] ([InvoiceId]);

    CREATE INDEX [IX_TransactionProofs_TransactionType] 
    ON [dbo].[TransactionProofs] ([TransactionType]);

    CREATE INDEX [IX_TransactionProofs_CompletedAt] 
    ON [dbo].[TransactionProofs] ([CompletedAt] DESC);

    PRINT 'TransactionProofs table created successfully';
END
ELSE
BEGIN
    PRINT 'TransactionProofs table already exists';
END
GO

-- Create ClientFeedbacks table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ClientFeedbacks')
BEGIN
    CREATE TABLE [dbo].[ClientFeedbacks] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TransactionProofId] INT NULL,
        [TicketId] INT NULL,
        [InvoiceId] INT NULL,
        [ClientId] NVARCHAR(50) NOT NULL,
        [ClientName] NVARCHAR(200) NULL,
        [Rating] INT NOT NULL CHECK ([Rating] >= 1 AND [Rating] <= 5),
        [ServiceQualityRating] INT NULL CHECK ([ServiceQualityRating] IS NULL OR ([ServiceQualityRating] >= 1 AND [ServiceQualityRating] <= 5)),
        [ResponseTimeRating] INT NULL CHECK ([ResponseTimeRating] IS NULL OR ([ResponseTimeRating] >= 1 AND [ResponseTimeRating] <= 5)),
        [ProfessionalismRating] INT NULL CHECK ([ProfessionalismRating] IS NULL OR ([ProfessionalismRating] >= 1 AND [ProfessionalismRating] <= 5)),
        [CommunicationRating] INT NULL CHECK ([CommunicationRating] IS NULL OR ([CommunicationRating] >= 1 AND [CommunicationRating] <= 5)),
        [WouldRecommend] BIT NULL,
        [Comments] NVARCHAR(2000) NULL,
        [FeedbackCategory] NVARCHAR(50) NOT NULL DEFAULT 'General',
        [HandledById] NVARCHAR(50) NULL,
        [HandledByName] NVARCHAR(200) NULL,
        [SubmittedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsReviewed] BIT NOT NULL DEFAULT 0,
        [ReviewedAt] DATETIME2 NULL,
        [ReviewedById] NVARCHAR(50) NULL,
        [ReviewNotes] NVARCHAR(1000) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [RequiresFollowUp] BIT NOT NULL DEFAULT 0,
        [FollowUpNotes] NVARCHAR(1000) NULL,
        [FollowUpCompletedAt] DATETIME2 NULL
    );

    -- Create indexes for common queries
    CREATE INDEX [IX_ClientFeedbacks_ClientId] 
    ON [dbo].[ClientFeedbacks] ([ClientId]);

    CREATE INDEX [IX_ClientFeedbacks_TicketId] 
    ON [dbo].[ClientFeedbacks] ([TicketId]);

    CREATE INDEX [IX_ClientFeedbacks_TransactionProofId] 
    ON [dbo].[ClientFeedbacks] ([TransactionProofId]);

    CREATE INDEX [IX_ClientFeedbacks_Status] 
    ON [dbo].[ClientFeedbacks] ([Status]);

    CREATE INDEX [IX_ClientFeedbacks_SubmittedAt] 
    ON [dbo].[ClientFeedbacks] ([SubmittedAt] DESC);

    CREATE INDEX [IX_ClientFeedbacks_Rating] 
    ON [dbo].[ClientFeedbacks] ([Rating]);

    CREATE INDEX [IX_ClientFeedbacks_HandledById] 
    ON [dbo].[ClientFeedbacks] ([HandledById]);

    -- Create unique constraint to prevent duplicate feedback per ticket/client
    CREATE UNIQUE INDEX [IX_ClientFeedbacks_TicketId_ClientId] 
    ON [dbo].[ClientFeedbacks] ([TicketId], [ClientId]) 
    WHERE [TicketId] IS NOT NULL;

    PRINT 'ClientFeedbacks table created successfully';
END
ELSE
BEGIN
    PRINT 'ClientFeedbacks table already exists';
END
GO

-- Add foreign key relationships (optional - if you want referential integrity)
-- Note: These are optional as the system handles orphan records gracefully

-- FK to TransactionProofs from ClientFeedbacks
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClientFeedbacks_TransactionProofs')
BEGIN
    ALTER TABLE [dbo].[ClientFeedbacks]
    ADD CONSTRAINT [FK_ClientFeedbacks_TransactionProofs]
    FOREIGN KEY ([TransactionProofId]) REFERENCES [dbo].[TransactionProofs]([Id])
    ON DELETE SET NULL;
    
    PRINT 'FK_ClientFeedbacks_TransactionProofs created';
END
GO

PRINT 'Transaction validation and feedback tables setup complete!';
GO
