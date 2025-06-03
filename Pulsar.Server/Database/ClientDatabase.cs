using System;
using System.Data.SQLite;
using System.IO;

namespace Pulsar.Server.Database
{
    public class ClientDatabase
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public ClientDatabase(string dbPath = "clients.db")
        {
            _dbPath = dbPath;
            _connectionString = $"Data Source={_dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Clients (
                    Id TEXT PRIMARY KEY,
                    IP TEXT,
                    Hostname TEXT,
                    FirstSeen DATETIME,
                    LastSeen DATETIME,
                    Status TEXT
                );";
                cmd.ExecuteNonQuery();
            }
        }

        public void AddOrUpdateClient(string id, string ip, string hostname, bool online)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO Clients (Id, IP, Hostname, FirstSeen, LastSeen, Status)
                    VALUES (@id, @ip, @hostname, @now, @now, @status)
                    ON CONFLICT(Id) DO UPDATE SET
                        IP = excluded.IP,
                        Hostname = excluded.Hostname,
                        LastSeen = excluded.LastSeen,
                        Status = excluded.Status;";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@ip", ip);
                cmd.Parameters.AddWithValue("@hostname", hostname);
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@status", online ? "Online" : "Offline");
                cmd.ExecuteNonQuery();
            }
        }

        public void SetClientOffline(string id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE Clients SET Status = 'Offline', LastSeen = @now WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
