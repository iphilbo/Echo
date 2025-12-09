using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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
        /// Timer-triggered function that keeps application heartbeat endpoints warm during business hours.
        /// Prevents cold starts by making periodic HTTP requests to configured endpoints.
        /// Runs every 5 minutes, but only executes during configured work window.
        /// </summary>
        /// <param name="timer">Timer trigger information</param>
        [Function("HeartbeatKeepAlive")]
        public async Task Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timer)
        {
            try
            {
                // Only keep applications warm during business hours to save costs
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

                // Get list of heartbeat URLs to keep alive
                // Format: comma-separated list (e.g., "https://app1.com/api/heartbeat,https://app2.com/api/heartbeat")
                // Default includes both IRIS and Dev endpoints
                var heartbeatUrlsConfig = Environment.GetEnvironmentVariable("HEARTBEAT_URL")
                    ?? "https://iris.intralogichealth.com/api/heartbeat,https://dev.intralogichealth.com/api/heartbeat";
                _logger.LogInformation("Heartbeat URLs config (raw): '{Config}'", heartbeatUrlsConfig);
                var heartbeatUrls = heartbeatUrlsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (heartbeatUrls.Length == 0)
                {
                    _logger.LogWarning("No heartbeat URLs configured. Set HEARTBEAT_URL environment variable.");
                    return;
                }

                _logger.LogInformation("Processing keep-alive for {Count} heartbeat endpoint(s): {Urls}", heartbeatUrls.Length, string.Join(", ", heartbeatUrls));
                // Log each URL individually for debugging
                for (int i = 0; i < heartbeatUrls.Length; i++)
                {
                    _logger.LogInformation("Heartbeat URL [{Index}]: '{Url}' (length: {Length})", i, heartbeatUrls[i], heartbeatUrls[i].Length);
                }

                // Process all heartbeat endpoints (in parallel for efficiency)
                var tasks = heartbeatUrls.Select(url => KeepHeartbeatAliveAsync(url.Trim()));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keep-alive process failed");
            }
        }

        /// <summary>
        /// Performs keep-alive operation on a single application heartbeat endpoint.
        /// Keeps the app layer warm during business hours to prevent cold starts.
        /// </summary>
        /// <param name="heartbeatUrl">The heartbeat endpoint URL to call</param>
        private async Task KeepHeartbeatAliveAsync(string heartbeatUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(heartbeatUrl))
                {
                    _logger.LogWarning("Empty heartbeat URL provided, skipping.");
                    return;
                }

                // Trim and validate URL
                heartbeatUrl = heartbeatUrl.Trim();
                _logger.LogInformation("Attempting heartbeat call to: '{Url}'", heartbeatUrl);

                // Make HTTP GET request to heartbeat endpoint
                var response = await _httpClient.GetAsync(heartbeatUrl);

                // For keep-alive purposes, any response (including 401) indicates the server is alive
                // 401 Unauthorized means the server is responding but rejecting the request
                // This is acceptable for keep-alive - the goal is to prevent cold starts
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Heartbeat endpoint '{Url}' keep-alive successful. StatusCode={StatusCode} at {Time}", heartbeatUrl, response.StatusCode, DateTime.UtcNow);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // 401 means server is alive but requires authentication - still counts as "warm"
                    _logger.LogWarning("Heartbeat endpoint '{Url}' returned 401 Unauthorized. Server is responding but may require authentication. StatusCode={StatusCode} at {Time}", heartbeatUrl, response.StatusCode, DateTime.UtcNow);
                }
                else
                {
                    // Other non-success status codes - log as error
                    response.EnsureSuccessStatusCode(); // This will throw for other error codes
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request failed for heartbeat endpoint '{Url}'. Message: {Message}", heartbeatUrl, httpEx.Message);
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger.LogError(timeoutEx, "Timeout calling heartbeat endpoint '{Url}'. Message: {Message}", heartbeatUrl, timeoutEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heartbeat endpoint keep-alive failed for '{Url}'. Exception: {ExceptionType}, Message: {Message}", heartbeatUrl, ex.GetType().Name, ex.Message);
                // Don't throw - continue processing other keep-alive operations even if heartbeat fails
            }
        }

        /// <summary>
        /// Determines if the current time is within the configured business hours window.
        /// Only keeps applications warm during business hours to optimize costs.
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
