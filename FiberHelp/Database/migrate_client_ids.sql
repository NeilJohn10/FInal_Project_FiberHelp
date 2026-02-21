-- ============================================
-- FiberHelp - Migrate Client IDs to Friendly Format
-- Run this in SQL Server Management Studio
-- against your LOCAL FiberhelpDB database
-- ============================================
-- This migrates GUID-based Client IDs (like 62facbba-e027-4f95-a184-62c8ac9bed32)
-- to user-friendly format (like CLT-0001)
-- ============================================

USE FiberhelpDB;
GO

PRINT 'Starting Client ID migration...';
PRINT '';

-- Step 1: Show current clients with GUID IDs
PRINT 'Current clients with GUID IDs:';
SELECT Id, Name, Email, JoinDate 
FROM Clients 
WHERE LEN(Id) > 10 
  AND Id LIKE '%-%-%-%-%'
ORDER BY JoinDate;

PRINT '';

-- Step 2: Create a temp table to store the mapping
IF OBJECT_ID('tempdb..#ClientIdMapping') IS NOT NULL
    DROP TABLE #ClientIdMapping;

CREATE TABLE #ClientIdMapping (
    OldId NVARCHAR(50),
    NewId NVARCHAR(20),
    ClientName NVARCHAR(200)
);

-- Step 3: Generate new IDs for clients with GUID format
DECLARE @Counter INT = 1;
DECLARE @OldId NVARCHAR(50);
DECLARE @ClientName NVARCHAR(200);

-- Find the max existing CLT- sequence number
SELECT @Counter = ISNULL(MAX(CAST(REPLACE(Id, 'CLT-', '') AS INT)), 0) + 1
FROM Clients
WHERE Id LIKE 'CLT-%' AND TRY_CAST(REPLACE(Id, 'CLT-', '') AS INT) IS NOT NULL;

PRINT 'Starting sequence number: ' + CAST(@Counter AS VARCHAR(10));

DECLARE client_cursor CURSOR FOR
    SELECT Id, Name
    FROM Clients
    WHERE LEN(Id) > 10 
      AND Id LIKE '%-%-%-%-%'
    ORDER BY JoinDate;

OPEN client_cursor;
FETCH NEXT FROM client_cursor INTO @OldId, @ClientName;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO #ClientIdMapping (OldId, NewId, ClientName)
    VALUES (@OldId, 'CLT-' + RIGHT('0000' + CAST(@Counter AS VARCHAR(4)), 4), @ClientName);
    
    SET @Counter = @Counter + 1;
    
    FETCH NEXT FROM client_cursor INTO @OldId, @ClientName;
END

CLOSE client_cursor;
DEALLOCATE client_cursor;

-- Step 4: Show the mapping
PRINT '';
PRINT 'ID Mapping (Old -> New):';
SELECT OldId, NewId, ClientName FROM #ClientIdMapping;

-- Step 5: Update Tickets table first (foreign key reference)
PRINT '';
PRINT 'Updating Tickets table references...';
UPDATE t
SET t.ClientId = m.NewId
FROM Tickets t
INNER JOIN #ClientIdMapping m ON t.ClientId = m.OldId;
PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' ticket(s)';

-- Step 6: Update the Clients table
PRINT '';
PRINT 'Updating Clients table IDs...';
UPDATE c
SET c.Id = m.NewId
FROM Clients c
INNER JOIN #ClientIdMapping m ON c.Id = m.OldId;
PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' client(s)';

-- Step 7: Show the result
PRINT '';
PRINT '============================================';
PRINT 'Migration complete! New Client IDs:';
PRINT '============================================';
SELECT Id, Name, Email, JoinDate
FROM Clients
ORDER BY 
    CASE WHEN Id LIKE 'CLT-%' THEN 0 ELSE 1 END,
    Id;

-- Clean up
DROP TABLE #ClientIdMapping;

PRINT '';
PRINT 'Done! New clients will now use the CLT-XXXX format.';
GO
