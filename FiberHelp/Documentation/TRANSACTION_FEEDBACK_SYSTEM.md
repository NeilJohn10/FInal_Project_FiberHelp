# Transaction Validation & Client Feedback System

## Overview

This CRM system now includes a comprehensive transaction validation system with proof of transaction and client feedback capabilities. This ensures clients receive verified confirmation when services are completed and can provide feedback on their experience.

## Features

### 1. Transaction Proof Generation

When a technician resolves a ticket, the system automatically generates a **Transaction Proof** that includes:

- **Unique Reference Number**: Format `TXN-YYYY-XXXXXX` (e.g., TXN-2025-000001)
- **Verification Code**: SHA256-based secure verification code
- **Transaction Details**: Client info, description, completion date, handler info
- **Status Tracking**: Generated ? Sent ? Acknowledged

### 2. Client Feedback System

After receiving transaction proof, clients can submit feedback with:

- **Overall Rating**: 1-5 stars (required)
- **Detailed Ratings** (optional):
  - Service Quality
  - Response Time
  - Staff Professionalism
  - Communication
- **Recommendation Score**: Would you recommend our service?
- **Category**: General, Service Quality, Technical, Billing, Communication
- **Comments**: Free-form text feedback

### 3. Feedback Management (Admin)

Administrators can:

- View all feedback with filtering (Pending, Reviewed, Action Required, Resolved)
- Review and mark feedback
- Flag feedback requiring follow-up
- Complete follow-up actions
- View staff performance metrics based on feedback
- Export feedback data to CSV

## Database Tables

### TransactionProofs Table

```sql
CREATE TABLE TransactionProofs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReferenceNumber NVARCHAR(50) NOT NULL UNIQUE,
    TransactionType NVARCHAR(50) NOT NULL,  -- TicketResolution, PaymentConfirmation
    TicketId INT NULL,
    InvoiceId INT NULL,
    ClientId NVARCHAR(50) NULL,
    ClientName NVARCHAR(200) NULL,
    AccountId INT NULL,
    Description NVARCHAR(1000) NOT NULL,
    Amount DECIMAL(18,2) NULL,
    CompletedById NVARCHAR(50) NULL,
    CompletedByName NVARCHAR(200) NULL,
    CompletedAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Status NVARCHAR(50) NOT NULL DEFAULT 'Generated',
    AcknowledgedAt DATETIME2 NULL,
    Notes NVARCHAR(2000) NULL,
    VerificationCode NVARCHAR(500) NULL
);
```

### ClientFeedbacks Table

```sql
CREATE TABLE ClientFeedbacks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TransactionProofId INT NULL,
    TicketId INT NULL,
    InvoiceId INT NULL,
    ClientId NVARCHAR(50) NOT NULL,
    ClientName NVARCHAR(200) NULL,
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    ServiceQualityRating INT NULL,
    ResponseTimeRating INT NULL,
    ProfessionalismRating INT NULL,
    CommunicationRating INT NULL,
    WouldRecommend BIT NULL,
    Comments NVARCHAR(2000) NULL,
    FeedbackCategory NVARCHAR(50) DEFAULT 'General',
    HandledById NVARCHAR(50) NULL,
    HandledByName NVARCHAR(200) NULL,
    SubmittedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsReviewed BIT NOT NULL DEFAULT 0,
    ReviewedAt DATETIME2 NULL,
    ReviewedById NVARCHAR(50) NULL,
    ReviewNotes NVARCHAR(1000) NULL,
    Status NVARCHAR(50) DEFAULT 'Pending',
    RequiresFollowUp BIT DEFAULT 0,
    FollowUpNotes NVARCHAR(1000) NULL,
    FollowUpCompletedAt DATETIME2 NULL
);
```

## How It Works

### Workflow

1. **Technician Resolves Ticket**
   - Technician clicks "Resolve" on their dashboard
   - Enters resolution notes
   - System creates transaction proof automatically

2. **Transaction Proof Generated**
   - Unique reference number assigned
   - Verification code generated
   - Proof stored in database
   - Synced to cloud via outbox

3. **Client Submits Feedback**
   - Client navigates to `/feedback/{TicketId}`
   - Views transaction proof details
   - Provides ratings and comments
   - Feedback stored and synced

4. **Admin Reviews Feedback**
   - Admin views `/feedback-management`
   - Reviews pending feedback
   - Adds notes and flags for follow-up
   - Completes follow-up actions

## New Pages

| Route | Description |
|-------|-------------|
| `/feedback/{TicketId}` | Client feedback submission form |
| `/feedback-management` | Admin feedback review dashboard |
| `/transaction-proofs` | View all transaction proofs |

## API/Service Methods

### TransactionService

```csharp
// Generate proof when ticket is resolved
Task<TransactionProof?> GenerateTicketResolutionProofAsync(int ticketId, ...)

// Generate proof for payment
Task<TransactionProof?> GeneratePaymentProofAsync(int invoiceId, ...)

// Get proofs for a client
Task<List<TransactionProof>> GetClientTransactionProofsAsync(string clientId)

// Validate proof by reference and code
Task<bool> ValidateProofAsync(string referenceNumber, string verificationCode)

// Submit client feedback
Task<ClientFeedback?> SubmitFeedbackAsync(ClientFeedback feedback)

// Get feedback statistics
Task<FeedbackStatistics> GetFeedbackStatisticsAsync(DateTime? fromDate)

// Get staff performance metrics
Task<List<StaffPerformance>> GetStaffPerformanceAsync()
```

### AdminService

```csharp
// Resolve ticket with automatic proof generation
Task<(bool Success, int? ProofId)> ResolveTicketWithProofAsync(
    int ticketId, 
    string technicianId, 
    string technicianName,
    string resolutionNotes)

// Check if feedback exists for ticket
Task<bool> HasFeedbackForTicketAsync(int ticketId)

// Get proof for specific ticket
Task<TransactionProof?> GetTicketProofAsync(int ticketId)
```

## Setup Instructions

1. **Run Database Migration**
   ```sql
   -- Execute the SQL script
   FiberHelp\Database\create_transaction_feedback_tables.sql
   ```

2. **Verify Service Registration**
   The `TransactionService` is automatically registered in `MauiProgram.cs`:
   ```csharp
   builder.Services.AddScoped<TransactionService>();
   ```

3. **Test the Feature**
   - Create a ticket and assign to a technician
   - Login as technician and resolve the ticket
   - Navigate to `/feedback/{TicketId}` to submit feedback
   - Review feedback at `/feedback-management`

## Statistics & Reports

The Feedback Management page provides:

- **Average Rating**: Overall customer satisfaction score
- **Total Feedback Count**: Number of feedback submissions
- **Pending Review Count**: Feedback awaiting review
- **Requires Follow-up Count**: Feedback flagged for action
- **Would Recommend %**: Net Promoter Score indicator
- **Rating Distribution**: Breakdown by star rating
- **Staff Performance**: Individual technician ratings

## Security

- Verification codes are SHA256-based and unique per transaction
- Duplicate feedback per ticket is prevented
- Only authenticated users can access feedback pages
- Admin-only access to feedback management

## Integration with Existing System

- Transaction proofs sync to cloud via existing outbox pattern
- Feedback syncs to cloud via existing outbox pattern
- Uses existing authentication (AuthService)
- Follows existing error handling patterns (ErrorHandlingService)
- Consistent UI design with other pages
