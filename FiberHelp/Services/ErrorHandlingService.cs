using System;
using System.Collections.Generic;
using System.Linq;

namespace FiberHelp.Services
{
    /// <summary>
    /// Centralized error handling service with user-friendly messages
    /// </summary>
    public class ErrorHandlingService
    {
        private readonly List<ErrorLog> _errorHistory = new();
        private const int MaxHistorySize = 100;

        /// <summary>
        /// Get user-friendly error message from exception
        /// </summary>
        public string GetUserFriendlyMessage(Exception ex)
        {
            LogError(ex);

            return ex switch
            {
                UnauthorizedAccessException => "? Access Denied: You don't have permission to perform this action.",
                TimeoutException => "?? Request Timeout: The operation took too long. Please try again.",
                InvalidOperationException => "?? Invalid Operation: This action cannot be completed right now.",
                ArgumentNullException => "?? Missing Information: Required data is missing.",
                ArgumentException => "?? Invalid Data: The provided information is not valid.",
                KeyNotFoundException => "?? Not Found: The requested item could not be found.",
                FormatException => "?? Format Error: The data format is incorrect.",
                OperationCanceledException => "?? Cancelled: The operation was cancelled.",
                System.Net.Http.HttpRequestException httpEx => $"?? Network Error: {GetNetworkErrorMessage(httpEx)}",
                Microsoft.EntityFrameworkCore.DbUpdateException dbEx => $"?? Database Error: {GetDatabaseErrorMessage(dbEx)}",
                Microsoft.Data.SqlClient.SqlException sqlEx => $"??? SQL Error: {GetSqlErrorMessage(sqlEx)}",
                System.IO.IOException ioEx => $"?? File Error: {GetFileErrorMessage(ioEx)}",
                _ => $"? Unexpected Error: {GetGenericErrorMessage(ex)}"
            };
        }

        /// <summary>
        /// Get user-friendly message for specific operation context
        /// </summary>
        public string GetContextualMessage(Exception ex, string operation, string entityType)
        {
            LogError(ex, operation, entityType);

            var action = operation.ToLower() switch
            {
                "create" => "creating",
                "update" => "updating",
                "delete" => "deleting",
                "load" => "loading",
                "save" => "saving",
                "fetch" => "fetching",
                _ => "processing"
            };

            var entity = entityType.ToLower();

            // Specific database errors
            if (ex is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                if (dbEx.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                    dbEx.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return $"?? Duplicate Entry: This {entity} already exists in the system.";
                }

                if (dbEx.InnerException?.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) == true ||
                    dbEx.InnerException?.Message.Contains("reference", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return $"?? Dependency Error: Cannot delete this {entity} because it's linked to other records.";
                }

                if (dbEx.InnerException?.Message.Contains("delete", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return $"??? Delete Error: Unable to delete {entity}. It may be referenced by other records.";
                }

                return $"?? Database Error: Error {action} {entity}. Please try again.";
            }

            // Network errors
            if (ex is System.Net.Http.HttpRequestException ||
                ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                return $"?? Connection Error: Unable to {action} {entity}. Check your internet connection.";
            }

            // Validation errors
            if (ex is ArgumentException || ex is ArgumentNullException)
            {
                return $"?? Validation Error: Invalid data provided for {entity}. Please check your input.";
            }

            // Permission errors
            if (ex is UnauthorizedAccessException)
            {
                return $"? Permission Denied: You don't have permission to {action} {entity}.";
            }

            // Timeout errors
            if (ex is TimeoutException)
            {
                return $"?? Timeout: {action} {entity} took too long. Please try again.";
            }

            // Generic contextual error
            return $"? Error: Failed to {action} {entity}. {GetShortErrorMessage(ex)}";
        }

        /// <summary>
        /// Check if error is critical (requires immediate attention)
        /// </summary>
        public bool IsCriticalError(Exception ex)
        {
            return ex is OutOfMemoryException ||
                   ex is StackOverflowException ||
                   ex is System.AccessViolationException ||
                   ex is System.Threading.ThreadAbortException ||
                   ex.Message.Contains("corrupt", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("fatal", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Log error to history
        /// </summary>
        private void LogError(Exception ex, string? operation = null, string? entityType = null)
        {
            var log = new ErrorLog
            {
                Timestamp = DateTime.UtcNow,
                Exception = ex,
                Operation = operation,
                EntityType = entityType,
                Message = ex.Message,
                StackTrace = ex.StackTrace
            };

            _errorHistory.Insert(0, log);

            // Keep only last 100 errors
            while (_errorHistory.Count > MaxHistorySize)
            {
                _errorHistory.RemoveAt(_errorHistory.Count - 1);
            }

            // Also log to debug output
            System.Diagnostics.Debug.WriteLine($"[ERROR] {operation ?? "General"} - {entityType ?? "Unknown"}: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"  [INNER] {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Get recent error history
        /// </summary>
        public IReadOnlyList<ErrorLog> GetRecentErrors(int count = 10)
        {
            return _errorHistory.Take(count).ToList();
        }

        /// <summary>
        /// Clear error history
        /// </summary>
        public void ClearHistory()
        {
            _errorHistory.Clear();
        }

        // Helper methods for specific error types
        private string GetNetworkErrorMessage(System.Net.Http.HttpRequestException ex)
        {
            if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return "The connection timed out. Check your internet connection.";
            if (ex.Message.Contains("name resolution", StringComparison.OrdinalIgnoreCase))
                return "Cannot reach the server. Check your network settings.";
            if (ex.Message.Contains("refused", StringComparison.OrdinalIgnoreCase))
                return "Server refused the connection. Try again later.";
            return "Network connection failed. Check your internet connection.";
        }

        private string GetDatabaseErrorMessage(Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;

            if (inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                return "This record already exists.";
            if (inner.Contains("foreign key", StringComparison.OrdinalIgnoreCase))
                return "Cannot complete this action due to related records.";
            if (inner.Contains("null", StringComparison.OrdinalIgnoreCase))
                return "Required field is missing.";
            if (inner.Contains("constraint", StringComparison.OrdinalIgnoreCase))
                return "Data validation rule violated.";

            return "Database operation failed. Please try again.";
        }

        private string GetSqlErrorMessage(Microsoft.Data.SqlClient.SqlException ex)
        {
            return ex.Number switch
            {
                -1 or -2 => "Database connection timeout. Check your connection.",
                2 or 53 => "Cannot connect to database server. Check network.",
                2601 or 2627 => "Duplicate entry. Record already exists.",
                547 => "Cannot delete - record is referenced by other data.",
                1205 => "Database deadlock detected. Please try again.",
                _ => "Database error occurred. Please try again."
            };
        }

        private string GetFileErrorMessage(System.IO.IOException ex)
        {
            if (ex.Message.Contains("access", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("permission", StringComparison.OrdinalIgnoreCase))
                return "File access denied. Check permissions.";
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return "File not found.";
            if (ex.Message.Contains("in use", StringComparison.OrdinalIgnoreCase))
                return "File is being used by another process.";

            return "File operation failed.";
        }

        private string GetGenericErrorMessage(Exception ex)
        {
            var msg = ex.Message;

            // Truncate long messages
            if (msg.Length > 200)
                msg = msg.Substring(0, 197) + "...";

            // Remove technical jargon
            msg = msg.Replace("System.", "")
                     .Replace("Exception", "")
                     .Replace("Microsoft.", "");

            return msg;
        }

        private string GetShortErrorMessage(Exception ex)
        {
            var msg = ex.Message;
            if (msg.Length > 100)
                msg = msg.Substring(0, 97) + "...";
            return msg;
        }

        /// <summary>
        /// Error log entry
        /// </summary>
        public class ErrorLog
        {
            public DateTime Timestamp { get; set; }
            public Exception? Exception { get; set; }
            public string? Operation { get; set; }
            public string? EntityType { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? StackTrace { get; set; }
        }
    }
}
