# FiberHelp Database Setup Guide

## Problem Summary
Your application is showing these errors:
1. **Reports Page**: "Error: Unable to load cash flow data"
2. **Billing Page**: "Failed to load billing data: Invalid object name 'Expenses'"

## Root Cause
The `Expenses` table is missing from your SQL Server database.

## Solution: Complete Database Setup

### Step 1: Open SQL Server Management Studio (SSMS)
1. Connect to your SQL Server instance
2. Make sure the `FiberhelpDB` database exists (create it if not)

### Step 2: Run the Complete Setup Script
1. Open the file: `FiberHelp/Database/complete_setup.sql`
2. Execute the entire script in SSMS
3. This will create ALL required tables and seed sample data

### Step 3: Verify Tables Were Created
Run this query to check all tables exist:
```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

You should see these 8 tables:
- ? Accounts
- ? Agents
- ? Clients
- ? **Expenses** (this was missing!)
- ? Invoices
- ? OutboxMessages
- ? Tickets
- ? Users

### Step 4: Verify Data Was Seeded
Check each table has data:
```sql
SELECT 'Users' as TableName, COUNT(*) as RowCount FROM Users
UNION ALL
SELECT 'Agents', COUNT(*) FROM Agents
UNION ALL
SELECT 'Accounts', COUNT(*) FROM Accounts
UNION ALL
SELECT 'Clients', COUNT(*) FROM Clients
UNION ALL
SELECT 'Tickets', COUNT(*) FROM Tickets
UNION ALL
SELECT 'Invoices', COUNT(*) FROM Invoices
UNION ALL
SELECT 'Expenses', COUNT(*) FROM Expenses;
```

Expected results:
- Users: 2 rows (admin + agent)
- Agents: 2 rows
- Accounts: 2 rows
- Clients: 2 rows
- Tickets: 3 rows
- Invoices: 4 rows
- **Expenses: 6 rows** (this enables Cash Flow reports!)

### Step 5: Test the Application
1. Stop your application if it's running
2. Set environment variables (see `.env.example`)
3. Restart the application
4. Log in with the admin credentials set in your `.env` file
5. Navigate to **Reports** page - Cash Flow should now load
6. Navigate to **Billing** page - All data should display

## What Each Table Does

### 1. **Users** - Authentication
Stores login credentials for the application

### 2. **Agents** - Staff Management
Stores information about support agents and administrators

### 3. **Accounts** - Account Management
Main account records for business customers

### 4. **Clients** - Client/Customer Data
Individual clients linked to accounts

### 5. **Tickets** - Support Tickets
Support requests and issues from clients

### 6. **Invoices** - Billing Records
Invoices issued to accounts (inflows in cash flow)

### 7. **Expenses** ? - Operating Costs
Business expenses (outflows in cash flow)
- **This table was missing and causing your errors!**

### 8. **OutboxMessages** - Data Sync
Used for synchronizing data between local and server

## Cash Flow Calculation
The Reports page calculates cash flow using:
- **Inflows** = Sum of all PAID invoices (from Invoices table)
- **Outflows** = Sum of all expenses (from Expenses table)
- **Net** = Inflows - Outflows

Without the Expenses table, the cash flow calculation fails!

## Connection String
Make sure your `appsettings.json` has the correct connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=FiberhelpDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Replace `YOUR_SERVER` with your actual SQL Server instance name (e.g., `localhost`, `.\SQLEXPRESS`, or `(localdb)\MSSQLLocalDB`).

## Troubleshooting

### If you still get errors after running the script:

1. **Check if database exists:**
   ```sql
   SELECT name FROM sys.databases WHERE name = 'FiberhelpDB';
   ```

2. **Check connection string** in `appsettings.json`

3. **Verify EF Core can connect:**
   - In Package Manager Console, run:
   ```powershell
   Update-Database
   ```

4. **Check table permissions:**
   - Make sure your SQL login has SELECT, INSERT, UPDATE, DELETE permissions

5. **Clear and restart:**
   - Stop the application
   - Clear browser cache
   - Restart the application

## Alternative: Using EF Core Migrations (Advanced)

If you prefer to use Entity Framework migrations:

1. Open Package Manager Console in Visual Studio
2. Run these commands:
   ```powershell
   Add-Migration InitialCreate
   Update-Database
   ```

This will create tables based on your DbContext, but you'll still need to run the seed data portion of the script.

## Sample Data Summary

After running the script, you'll have:
- 2 user accounts (admin + agent)
- 2 business accounts
- 2 clients
- 3 support tickets
- 4 invoices (2 paid, 2 pending)
- 6 expenses (various categories)

This provides realistic data for testing all features!

## Need More Help?

If you encounter any issues:
1. Check the error messages in the browser console (F12)
2. Check the Output window in Visual Studio for detailed errors
3. Verify all tables exist using the SQL queries above
4. Make sure the connection string is correct
