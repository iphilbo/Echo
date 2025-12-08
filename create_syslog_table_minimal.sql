-- Minimal SysLog table (only columns required by KeepAlive function)
-- This is the minimum structure needed for the keep-alive functionality

CREATE TABLE SysLog (
    LogUser NVARCHAR(255),
    LogData NVARCHAR(MAX)
);
