-- Create SysLog table for database keep-alive functionality
-- This table is used by the KeepAlive Azure Function to prevent database auto-shutdown

CREATE TABLE SysLog (
    -- Primary key (auto-incrementing)
    LogID BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- User who created the log entry
    LogUser NVARCHAR(255) NOT NULL,

    -- Log message/data
    LogData NVARCHAR(MAX) NULL,

    -- Timestamp when the entry was created (optional but recommended)
    LogDate DATETIME2 DEFAULT GETUTCDATE() NULL

    -- Additional columns can be added here if needed by other parts of the system
);

-- Create index on LogDate for better query performance (if you'll query by date)
CREATE NONCLUSTERED INDEX IX_SysLog_LogDate ON SysLog(LogDate);

-- Create index on LogUser if you'll query by user
CREATE NONCLUSTERED INDEX IX_SysLog_LogUser ON SysLog(LogUser);
