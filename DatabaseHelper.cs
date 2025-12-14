using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace SecureNotes
{
    public class DatabaseHelper
    {
        private readonly string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notes.db");
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            _connectionString = $"Data Source={_dbPath};Version=3;";
            EnsureDatabase();
        }

        private void EnsureDatabase()
        {
            if (!File.Exists(_dbPath))
                SQLiteConnection.CreateFile(_dbPath);

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var sql = @"CREATE TABLE IF NOT EXISTS Notes (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Title TEXT NOT NULL,
                                Content TEXT NOT NULL,
                                Type TEXT,
                                Color TEXT,
                                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                                SharedWith TEXT
                            );";
                using (var cmd = new SQLiteCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
        }

        public int AddNote(Note note)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var sql = @"INSERT INTO Notes (Title, Content, Type, Color, SharedWith) 
                            VALUES (@title, @content, @type, @color, @sharedWith);
                            SELECT last_insert_rowid();";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@title", note.Title);
                    cmd.Parameters.AddWithValue("@content", note.Content);
                    cmd.Parameters.AddWithValue("@type", note.Type);
                    cmd.Parameters.AddWithValue("@color", note.Color);
                    cmd.Parameters.AddWithValue("@sharedWith", (object)note.SharedWith ?? DBNull.Value);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public List<Note> GetNotes()
        {
            var list = new List<Note>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var sql = "SELECT Id, Title, Content, Type, Color, CreatedAt, SharedWith FROM Notes ORDER BY CreatedAt DESC";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Note
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Content = reader.GetString(2),
                            Type = reader.IsDBNull(3) ? "note" : reader.GetString(3),
                            Color = reader.IsDBNull(4) ? "#FFFFFF" : reader.GetString(4),
                            CreatedAt = reader.IsDBNull(5) ? DateTime.Now : DateTime.Parse(reader.GetString(5)),
                            SharedWith = reader.IsDBNull(6) ? null : reader.GetString(6)
                        });
                    }
                }
            }
            return list;
        }

        public void DeleteNote(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM Notes WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}