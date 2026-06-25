using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Infrastructure.Services.Logger.Slack;
using System.Text.Json;

namespace SoftUmar_Virtuals.Infrastructure.Services.Logger
{
    public class LoggerService : ILoggerService
    {
        private readonly ISlackLogger _slackLogger;
        private readonly IRepository<AuditLogs> _logsRepository;

        public LoggerService(
            ISlackLogger slackLogger,
            IRepository<AuditLogs> logsRepository)
        {
            _slackLogger = slackLogger;
            _logsRepository = logsRepository;
        }

        public async Task LogInfo(string message, string methodName, string? traceId = null)
        {
            await LogToAll("INFO", message, methodName, traceId);
        }

        public async Task LogWarning(string message, string methodName, string? traceId = null)
        {
            await LogToAll("WARNING", message, methodName, traceId);
        }

        public async Task LogError(string message, string methodName, Exception? ex = null, string? traceId = null)
        {
            var fullMessage =
                ex != null
                ? $"{message}, Exception: {ex.Message}"
                : message;

            await LogToAll("ERROR", fullMessage, methodName, traceId);
        }

        public async Task LogRequestResponse(string requestJson, string responseJson, string methodName, bool success, string? traceId = null)
        {
            var currentTraceId =
                traceId ?? Guid.NewGuid().ToString();

            var logMessage = JsonSerializer.Serialize(new
            {
                TraceId = currentTraceId,
                Method = methodName,
                Request = requestJson,
                Response = responseJson,
                Success = success,
                Timestamp = DateTime.UtcNow
            });

            await LogToConsole(logMessage);

            await _slackLogger.Log(logMessage, success);

            await LogToDatabase(
                success ? "INFO" : "ERROR",
                "Request/Response Logged",
                methodName,
                currentTraceId,
                data: logMessage);
        }

        private static async Task LogToConsole(string message)
        {
            await Console.Out.WriteLineAsync("=== === >>> " + message);
        }

        private async Task LogToAll(string level, string message, string methodName, string? traceId)
        {
            var currentTraceId =
                traceId ?? Guid.NewGuid().ToString();

            var logObject = JsonSerializer.Serialize(new
            {
                Level = level,
                TraceId = currentTraceId,
                Method = methodName,
                Message = message,
                Timestamp = DateTime.UtcNow
            });

            await LogToConsole(logObject);

            await _slackLogger.Log(logObject, level == "INFO");

            await LogToDatabase(level, message, methodName, currentTraceId);
        }

        public async Task LogToDatabase(string level, string message, string methodName, string? traceId = null, string? userId = null, string? data = null,
            CancellationToken cancellationToken = default)
        {
            var log = new AuditLogs
            {
                Id = Guid.NewGuid(),
                Level = level,
                Message = message,
                Method = methodName,
                TraceId = traceId,
                UserId = userId,
                Data = data
            };

            await _logsRepository.AddAsync(log, cancellationToken);

            await _logsRepository.SaveChanges(cancellationToken);
        }
    }
}