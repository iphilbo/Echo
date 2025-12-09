using System.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Net.Http;

namespace Prometheus.KeepAlive
{
    public class KeepAliveFunction
    {
        private readonly ILogger<KeepAliveFunction> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        public KeepAliveFunction(ILogger<KeepAliveFunction> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Timer-triggered function that keeps the Azure SQL Database warm during business hours.
        /// Prevents auto-shutdown by performing periodic write operations.
        /// Runs every 5 minutes, but only executes database operations during configured work window.
        /// </summary>
        /// <param name="timer">Timer trigger information</param>
        [Function("DbKeepAlive")]
        public async Task Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timer)
        {
            try
            {
                // Only keep database warm during business hours to save costs
                // Azure SQL Database can auto-shutdown after periods of inactivity
                if (!IsWithinWorkWindow())
                {
                    var tzName = Environment.GetEnvironmentVariable("TIME_ZONE") ?? "Eastern Standard Time";
                    var workDays = (Environment.GetEnvironmentVariable("WORK_DAYS") ?? "Mon-Fri").ToLowerInvariant();
                    var start = TimeSpan.Parse(Environment.GetEnvironmentVariable("WORK_START") ?? "07:00");
                    var end = TimeSpan.Parse(Environment.GetEnvironmentVariable("WORK_END") ?? "19:00");
                    TimeZoneInfo tz;
                    try { tz = TimeZoneInfo.FindSystemTimeZoneById(tzName); }
                    catch { tz = TimeZoneInfo.Local; }
                    var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
                    _logger.LogInformation("Skipping keepalive (outside business hours): nowLocal={NowLocal} tz={TimeZone} window={Start}-{End} days={WorkDays}", nowLocal, tz.Id, start, end, workDays);
                    return;
                }

                // Get list of databases to keep alive
                // Format: comma-separated list (e.g., "Default,Secondary,Third")
                // If not specified, defaults to "Default" for backward compatibility
                var databasesConfig = Environment.GetEnvironmentVariable("KEEPALIVE_DATABASES") ?? "Default";
                var databaseNames = databasesConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (databaseNames.Length == 0)
                {
                    _logger.LogWarning("No databases configured for keep-alive. Set KEEPALIVE_DATABASES environment variable.");
                    return;
                }

                _logger.LogInformation("Processing keep-alive for {Count} database(s): {Databases}", databaseNames.Length, string.Join(", ", databaseNames));

                // Process all databases and heartbeat endpoint (in parallel for efficiency)
                var tasks = new List<Task>();
                tasks.AddRange(databaseNames.Select(dbName => KeepDatabaseAliveAsync(dbName.Trim())));
                tasks.Add(KeepHeartbeatAliveAsync());
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keep-alive process failed");
            }
        }

        /// <summary>
        /// Performs keep-alive operation on a single database.
        /// </summary>
        /// <param name="databaseName">Name of the database (used to look up connection string)</param>
        private async Task KeepDatabaseAliveAsync(string databaseName)
        {
            try
            {
                // Look up connection string by database name
                // Supports: ConnectionStrings:{DatabaseName} or ConnectionStrings:Default for "Default"
                // Special case: IRIS uses IRISConn environment variable
                string connStr;

                if (databaseName.Equals("IRIS", StringComparison.OrdinalIgnoreCase))
                {
                    // IRIS uses IRISConn environment variable
                    connStr = Environment.GetEnvironmentVariable("IRISConn") ?? string.Empty;
                }
                else if (databaseName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                {
                    // Default database: try ConnectionStrings:Default, then SQLConn for backward compatibility
                    connStr = Environment.GetEnvironmentVariable("ConnectionStrings:Default")
                              ?? Environment.GetEnvironmentVariable("SQLConn")
                              ?? string.Empty;
                }
                else
                {
                    // Other databases: use ConnectionStrings:{DatabaseName}
                    var connStrKey = $"ConnectionStrings:{databaseName}";
                    connStr = Environment.GetEnvironmentVariable(connStrKey) ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(connStr))
                {
                    var expectedVar = databaseName.Equals("IRIS", StringComparison.OrdinalIgnoreCase)
                        ? "IRISConn"
                        : databaseName.Equals("Default", StringComparison.OrdinalIgnoreCase)
                            ? "ConnectionStrings:Default or SQLConn"
                            : $"ConnectionStrings:{databaseName}";
                    _logger.LogWarning("No connection string found for database '{DatabaseName}' (expected: {ExpectedVariable})", databaseName, expectedVar);
                    return;
                }

                // Perform a simple write operation to keep the database active
                // This prevents Azure SQL Database from auto-shutting down during business hours
                using var conn = new SqlConnection(connStr);
                using var cmd = new SqlCommand("INSERT INTO SysLog (LogUser, LogData) VALUES (@u, @d)", conn);
                cmd.Parameters.AddWithValue("@u", "ChronJob");
                cmd.Parameters.AddWithValue("@d", "Keeping Alive");
                await conn.OpenAsync();
                var rows = await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Database '{DatabaseName}' keep-alive successful. RowsAffected={Rows} at {Time}", databaseName, rows, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keep-alive failed for database '{DatabaseName}'", databaseName);
                // Don't throw - continue processing other databases even if one fails
            }
        }

        /// <summary>
        /// Performs keep-alive operation on the application heartbeat endpoint.
        /// Keeps the app layer warm during business hours to prevent cold starts.
        /// </summary>
        private async Task KeepHeartbeatAliveAsync()
        {
            try
            {
                // Get heartbeat URL from environment variable, with default
                var heartbeatUrl = Environment.GetEnvironmentVariable("HEARTBEAT_URL") 
                    ?? "https://iris.intralogichealth.com/api/heartbeat";

                if (string.IsNullOrWhiteSpace(heartbeatUrl))
                {
                    _logger.LogWarning("No heartbeat URL configured. Set HEARTBEAT_URL environment variable to enable app layer keep-alive.");
                    return;
                }

                // Make HTTP GET request to heartbeat endpoint
                var response = await _httpClient.GetAsync(heartbeatUrl);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Heartbeat endpoint keep-alive successful. StatusCode={StatusCode} at {Time}", response.StatusCode, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heartbeat endpoint keep-alive failed");
                // Don't throw - continue processing other keep-alive operations even if heartbeat fails
            }
        }

        /// <summary>
        /// Determines if the current time is within the configured business hours window.
        /// Only keeps database warm during business hours to optimize costs.
        /// </summary>
        /// <returns>True if within work window, false otherwise</returns>
        private static bool IsWithinWorkWindow()
        {
            // Load configuration from environment variables with defaults
            var tzName = Environment.GetEnvironmentVariable("TIME_ZONE") ?? "Eastern Standard Time";
            var workDays = (Environment.GetEnvironmentVariable("WORK_DAYS") ?? "Mon-Fri").ToLowerInvariant();
            var start = TimeSpan.Parse(Environment.GetEnvironmentVariable("WORK_START") ?? "07:00");
            var end = TimeSpan.Parse(Environment.GetEnvironmentVariable("WORK_END") ?? "19:00");

            TimeZoneInfo tz;
            try { tz = TimeZoneInfo.FindSystemTimeZoneById(tzName); }
            catch { tz = TimeZoneInfo.Local; }

            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            // Check if current day is within configured work days
            var isWeekday = nowLocal.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
            if (workDays.Contains("mon-fri") && !isWeekday) return false;
            if (workDays.Contains("sat") && nowLocal.DayOfWeek == DayOfWeek.Saturday) return true;
            if (workDays.Contains("sun") && nowLocal.DayOfWeek == DayOfWeek.Sunday) return true;
            if (!workDays.Contains("mon-fri") && !workDays.Contains(nowLocal.DayOfWeek.ToString().ToLowerInvariant().Substring(0,3))) return false;

            var nowTod = nowLocal.TimeOfDay;
            return nowTod >= start && nowTod <= end;
        }
    }
}
