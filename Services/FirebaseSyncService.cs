using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OCR_AccessControl.Models;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class FirebaseSyncService : BackgroundService
{
    private readonly string _connectionString;
    private readonly string _firebaseUrl;
    private readonly string _serviceAccountKeyPath;
    private readonly ILogger<FirebaseSyncService> _logger;

    public FirebaseSyncService(IConfiguration configuration, ILogger<FirebaseSyncService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _firebaseUrl = configuration["Firebase:DatabaseUrl"];
        _serviceAccountKeyPath = configuration["Firebase:ServiceAccountKeyPath"];
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncData();
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); // Sync every 20 seconds
        }
    }

    private async Task SyncData()
    {
        try
        {
            // Fetch data from Firebase
            var firebaseClient = new FirebaseClient(
                _firebaseUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult("your-firebase-database-secret")
                });

            var firebaseData = await firebaseClient
                .Child("NonResidentLogs")
                .OnceAsJsonAsync();

            var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(firebaseData);

            if (data == null)
            {
                _logger.LogWarning("No data received from Firebase or deserialization failed.");
                return;
            }

            // Push data to PostgreSQL
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            int successCount = 0;
            int errorCount = 0;

            foreach (var kvp in data)
            {
                var id = kvp.Key;
                var row = kvp.Value;

                try
                {
                    // Ensure the ID can be parsed into an integer
                    if (!int.TryParse(id, out int intId))
                    {
                        _logger.LogWarning("Skipping row with non-integer ID: {RowId}", id);
                        errorCount++;
                        continue;
                    }

                    var query = @"
                INSERT INTO ""NonResidentLogs"" (id, full_name, id_type, id_number, qr_code, entry_time, exit_time)
                VALUES (@id, @full_name, @id_type, @id_number, @qr_code, @entry_time, @exit_time)
                ON CONFLICT (id) DO UPDATE
                SET full_name = EXCLUDED.full_name,
                    id_type = EXCLUDED.id_type,
                    id_number = EXCLUDED.id_number,
                    qr_code = EXCLUDED.qr_code,
                    entry_time = EXCLUDED.entry_time,
                    exit_time = EXCLUDED.exit_time";

                    using var cmd = new NpgsqlCommand(query, connection);

                    cmd.Parameters.AddWithValue("@id", intId);
                    cmd.Parameters.AddWithValue("@full_name", row.ContainsKey("full_name") ? row["full_name"]?.ToString() ?? "" : DBNull.Value);
                    cmd.Parameters.AddWithValue("@id_type", row.ContainsKey("id_type") ? row["id_type"]?.ToString() ?? "" : DBNull.Value);
                    cmd.Parameters.AddWithValue("@id_number", row.ContainsKey("id_number") ? row["id_number"]?.ToString() ?? "" : DBNull.Value);
                    cmd.Parameters.AddWithValue("@qr_code", row.ContainsKey("qr_code") ? row["qr_code"]?.ToString() ?? "" : DBNull.Value);

                    // Optional datetime parsing for entry/exit times
                    if (row.ContainsKey("entry_time") && DateTime.TryParse(row["entry_time"]?.ToString(), out var entryTime))
                        cmd.Parameters.AddWithValue("@entry_time", entryTime);
                    else
                        cmd.Parameters.AddWithValue("@entry_time", DBNull.Value);

                    if (row.ContainsKey("exit_time") && DateTime.TryParse(row["exit_time"]?.ToString(), out var exitTime))
                        cmd.Parameters.AddWithValue("@exit_time", exitTime);
                    else
                        cmd.Parameters.AddWithValue("@exit_time", DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing row {RowId}: {ErrorMessage}", id, ex.Message);
                    errorCount++;
                }
            }

            _logger.LogInformation("Data sync completed. {SuccessCount} succeeded, {ErrorCount} failed.", successCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in sync process: {ErrorMessage}", ex.Message);
        }
    }
}