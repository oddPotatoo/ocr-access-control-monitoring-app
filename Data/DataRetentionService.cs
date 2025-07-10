// Services/DataRetentionService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCR_AccessControl.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OCR_AccessControl.Services
{
    public class DataRetentionService : IHostedService, IDisposable
    {
        private const int RetentionDays = 30;
        private readonly ILogger<DataRetentionService> _logger;
        private readonly IServiceProvider _services;
        private Timer _timer;

        public DataRetentionService(ILogger<DataRetentionService> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DeleteOldRecords, null, TimeSpan.Zero, TimeSpan.FromDays(1)); // Run daily
            return Task.CompletedTask;
        }

        private void DeleteOldRecords(object state)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var phTime = DateTime.UtcNow.AddHours(8);
                var cutoff = phTime.AddDays(-RetentionDays);

                // Parameterized query for safety
                var sql = @"DELETE FROM ""NonResidentLogs"" WHERE entry_time < {0}";
                var rowsAffected = context.Database.ExecuteSqlRaw(sql, cutoff);

                _logger.LogInformation($"Deleted {rowsAffected} records older than {cutoff:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data retention cleanup failed");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();
    }
}