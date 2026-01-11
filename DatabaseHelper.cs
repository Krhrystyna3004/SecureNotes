using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace SecureNotes
{
    public class DatabaseHelper
    {
        private readonly string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notes.db");
        private readonly string _cs;

        public DatabaseHelper()
        {
            _cs = $"Data Source={_dbPath};Version=3;";
            EnsureDatabase();
        }

        private void EnsureDatabase()
        {
            if (!File.Exists(_dbPath)) SQLiteConnection.CreateFile(_dbPath);

            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();

                string users = @"CREATE TABLE IF NOT EXISTS Users (
Id INTEGER PRIMARY KEY AUTOINCREMENT,
Username TEXT UNIQUE NOT NULL,
PasswordHash TEXT NOT NULL,
PasswordSalt TEXT NOT NULL,
PinHash TEXT,
PinSalt TEXT,
PreferredTheme TEXT DEFAULT 'Light',
CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);";

                string notes = @"CREATE TABLE IF NOT EXISTS Notes (
Id INTEGER PRIMARY KEY AUTOINCREMENT,
OwnerId INTEGER NOT NULL,
GroupId INTEGER,
Title TEXT NOT NULL,
Content TEXT NOT NULL,
Type TEXT NOT NULL,
Color TEXT,
Tags TEXT,
IvBase64 TEXT,
CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);";

                string groups = @"CREATE TABLE IF NOT EXISTS Groups (
Id INTEGER PRIMARY KEY AUTOINCREMENT,
OwnerId INTEGER NOT NULL,
InviteCode TEXT UNIQUE NOT NULL,
Name TEXT,
CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);";

                string members = @"CREATE TABLE IF NOT EXISTS GroupMembers (
Id INTEGER PRIMARY KEY AUTOINCREMENT,
GroupId INTEGER NOT NULL,
UserId INTEGER NOT NULL,
Permission TEXT NOT NULL
);";

                using (var cmd = new SQLiteCommand(users, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(notes, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(groups, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(members, conn)) cmd.ExecuteNonQuery();
            }
        }

        // Users
        public User GetUserByUsername(string username)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Id, Username, PasswordHash, PasswordSalt, PinHash, PinSalt, PreferredTheme, CreatedAt FROM Users WHERE Username=@u", conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        return new User
                        {
                            Id = r.GetInt32(0),
                            Username = r.GetString(1),
                            PasswordHash = r.GetString(2),
                            PasswordSalt = r.GetString(3),
                            PinHash = r.IsDBNull(4) ? "" : r.GetString(4),
                            PinSalt = r.IsDBNull(5) ? "" : r.GetString(5),
                            PreferredTheme = r.IsDBNull(6) ? "Light" : r.GetString(6),
                            CreatedAt = DateTime.Parse(r.GetString(7))
                        };
                    }
                }
            }
        }

        public int CreateUser(User u)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"INSERT INTO Users (Username, PasswordHash, PasswordSalt, PinHash, PinSalt, PreferredTheme)
VALUES (@un, @ph, @ps, @pinh, @pins, @theme); SELECT last_insert_rowid();", conn))
                {
                    cmd.Parameters.AddWithValue("@un", u.Username);
                    cmd.Parameters.AddWithValue("@ph", u.PasswordHash);
                    cmd.Parameters.AddWithValue("@ps", u.PasswordSalt);
                    cmd.Parameters.AddWithValue("@pinh", string.IsNullOrEmpty(u.PinHash) ? (object)DBNull.Value : u.PinHash);
                    cmd.Parameters.AddWithValue("@pins", string.IsNullOrEmpty(u.PinSalt) ? (object)DBNull.Value : u.PinSalt);
                    cmd.Parameters.AddWithValue("@theme", u.PreferredTheme ?? "Light");
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void UpdateUserPin(int userId, string oldPinHashCheck, string newPinHash, string newPinSalt)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();

                using (var cmdGet = new SQLiteCommand("SELECT PinHash FROM Users WHERE Id=@id", conn))
                {
                    cmdGet.Parameters.AddWithValue("@id", userId);
                    var currentHash = cmdGet.ExecuteScalar() as string;
                    if (!string.IsNullOrEmpty(currentHash) && currentHash != oldPinHashCheck)
                        throw new Exception("Невірний старий PIN.");
                }

                using (var cmd = new SQLiteCommand("UPDATE Users SET PinHash=@h, PinSalt=@s WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@h", newPinHash);
                    cmd.Parameters.AddWithValue("@s", newPinSalt);
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateUserTheme(int userId, string theme)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE Users SET PreferredTheme=@t WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@t", theme);
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteUser(int userId)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();

                using (var cmdNotes = new SQLiteCommand("DELETE FROM Notes WHERE OwnerId=@id", conn))
                {
                    cmdNotes.Parameters.AddWithValue("@id", userId);
                    cmdNotes.ExecuteNonQuery();
                }

                using (var cmdMembers = new SQLiteCommand("DELETE FROM GroupMembers WHERE UserId=@id", conn))
                {
                    cmdMembers.Parameters.AddWithValue("@id", userId);
                    cmdMembers.ExecuteNonQuery();
                }

                using (var cmdUser = new SQLiteCommand("DELETE FROM Users WHERE Id=@id", conn))
                {
                    cmdUser.Parameters.AddWithValue("@id", userId);
                    cmdUser.ExecuteNonQuery();
                }
            }
        }

        // Notes
        public int AddNote(Note n)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"INSERT INTO Notes (OwnerId, GroupId, Title, Content, Type, Color, Tags, IvBase64, CreatedAt, UpdatedAt)
VALUES (@o, @g, @t, @c, @ty, @col, @tags, @iv, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
SELECT last_insert_rowid();", conn))
                {
                    cmd.Parameters.AddWithValue("@o", n.OwnerId);
                    cmd.Parameters.AddWithValue("@g", (object)n.GroupId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@t", n.Title);
                    cmd.Parameters.AddWithValue("@c", n.Content);
                    cmd.Parameters.AddWithValue("@ty", n.Type);
                    cmd.Parameters.AddWithValue("@col", n.Color);
                    cmd.Parameters.AddWithValue("@tags", string.IsNullOrEmpty(n.Tags) ? (object)DBNull.Value : n.Tags);
                    cmd.Parameters.AddWithValue("@iv", string.IsNullOrEmpty(n.IvBase64) ? (object)DBNull.Value : n.IvBase64);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void UpdateNote(Note n)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"UPDATE Notes SET
GroupId=@g, Title=@t, Content=@c, Type=@ty, Color=@col, Tags=@tags, IvBase64=@iv,
UpdatedAt=CURRENT_TIMESTAMP
WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@g", (object)n.GroupId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@t", n.Title);
                    cmd.Parameters.AddWithValue("@c", n.Content);
                    cmd.Parameters.AddWithValue("@ty", n.Type);
                    cmd.Parameters.AddWithValue("@col", n.Color);
                    cmd.Parameters.AddWithValue("@tags", string.IsNullOrEmpty(n.Tags) ? (object)DBNull.Value : n.Tags);
                    cmd.Parameters.AddWithValue("@iv", string.IsNullOrEmpty(n.IvBase64) ? (object)DBNull.Value : n.IvBase64);
                    cmd.Parameters.AddWithValue("@id", n.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Note GetNoteById(int id)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"SELECT Id, OwnerId, GroupId, Title, Content, Type, Color, Tags, IvBase64, CreatedAt, UpdatedAt
FROM Notes WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        return new Note
                        {
                            Id = r.GetInt32(0),
                            OwnerId = r.GetInt32(1),
                            GroupId = r.IsDBNull(2) ? (int?)null : r.GetInt32(2),
                            Title = r.GetString(3),
                            Content = r.GetString(4),
                            Type = r.GetString(5),
                            Color = r.IsDBNull(6) ? "#FFFFFF" : r.GetString(6),
                            Tags = r.IsDBNull(7) ? "" : r.GetString(7),
                            IvBase64 = r.IsDBNull(8) ? null : r.GetString(8),
                            CreatedAt = DateTime.Parse(r.GetString(9)),
                            UpdatedAt = DateTime.Parse(r.GetString(10))
                        };
                    }
                }
            }
        }

        public List<Note> GetNotesForUser(int userId)
        {
            var list = new List<Note>();
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                string sql = @"SELECT Id, OwnerId, GroupId, Title, Content, Type, Color, Tags, IvBase64, CreatedAt, UpdatedAt
FROM Notes
WHERE OwnerId=@u
OR GroupId IN (SELECT GroupId FROM GroupMembers WHERE UserId=@u)
ORDER BY UpdatedAt DESC";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new Note
                            {
                                Id = r.GetInt32(0),
                                OwnerId = r.GetInt32(1),
                                GroupId = r.IsDBNull(2) ? (int?)null : r.GetInt32(2),
                                Title = r.GetString(3),
                                Content = r.GetString(4),
                                Type = r.GetString(5),
                                Color = r.IsDBNull(6) ? "#FFFFFF" : r.GetString(6),
                                Tags = r.IsDBNull(7) ? "" : r.GetString(7),
                                IvBase64 = r.IsDBNull(8) ? null : r.GetString(8),
                                CreatedAt = DateTime.Parse(r.GetString(9)),
                                UpdatedAt = DateTime.Parse(r.GetString(10))
                            });
                        }
                    }
                }
            }
            return list;
        }

        public void DeleteNote(int id)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM Notes WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Groups
        public Group CreateGroup(int ownerId, string name = "Моя група")
        {
            var invite = Guid.NewGuid().ToString("N").Substring(0, 8);

            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand(@"INSERT INTO Groups (OwnerId, InviteCode, Name)
VALUES (@o, @code, @name); SELECT last_insert_rowid();", conn))
                {
                    cmd.Parameters.AddWithValue("@o", ownerId);
                    cmd.Parameters.AddWithValue("@code", invite);
                    cmd.Parameters.AddWithValue("@name", name);
                    var id = Convert.ToInt32(cmd.ExecuteScalar());

                    // Автоматично додаємо власника як учасника з правами edit (без дублювання)
                    EnsureMember(conn, id, ownerId, "edit");

                    return new Group { Id = id, OwnerId = ownerId, InviteCode = invite, Name = name };
                }
            }
        }

        public Group GetGroupByInvite(string code)
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Id, OwnerId, InviteCode, Name, CreatedAt FROM Groups WHERE InviteCode=@c", conn))
                {
                    cmd.Parameters.AddWithValue("@c", code);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        return new Group
                        {
                            Id = r.GetInt32(0),
                            OwnerId = r.GetInt32(1),
                            InviteCode = r.GetString(2),
                            Name = r.IsDBNull(3) ? "Група" : r.GetString(3),
                            CreatedAt = DateTime.Parse(r.GetString(4))
                        };
                    }
                }
            }
        }

        public List<Group> GetGroupsForUser(int userId)
        {
            var list = new List<Group>();
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                string sql = @"SELECT g.Id, g.OwnerId, g.InviteCode, g.Name, g.CreatedAt
FROM Groups g
JOIN GroupMembers m ON m.GroupId = g.Id
WHERE m.UserId=@u
ORDER BY g.CreatedAt DESC";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new Group
                            {
                                Id = r.GetInt32(0),
                                OwnerId = r.GetInt32(1),
                                InviteCode = r.GetString(2),
                                Name = r.IsDBNull(3) ? "Група" : r.GetString(3),
                                CreatedAt = DateTime.Parse(r.GetString(4))
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<Note> GetNotesForGroup(int groupId)
        {
            var list = new List<Note>();
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                string sql = @"SELECT Id, OwnerId, GroupId, Title, Content, Type, Color, Tags, IvBase64, CreatedAt, UpdatedAt
FROM Notes WHERE GroupId=@g ORDER BY UpdatedAt DESC";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@g", groupId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new Note
                            {
                                Id = r.GetInt32(0),
                                OwnerId = r.GetInt32(1),
                                GroupId = r.IsDBNull(2) ? (int?)null : r.GetInt32(2),
                                Title = r.GetString(3),
                                Content = r.GetString(4),
                                Type = r.GetString(5),
                                Color = r.IsDBNull(6) ? "#FFFFFF" : r.GetString(6),
                                Tags = r.IsDBNull(7) ? "" : r.GetString(7),
                                IvBase64 = r.IsDBNull(8) ? null : r.GetString(8),
                                CreatedAt = DateTime.Parse(r.GetString(9)),
                                UpdatedAt = DateTime.Parse(r.GetString(10))
                            });
                        }
                    }
                }
            }
            return list;
        }

        public void AddMember(int groupId, int userId, string permission = "edit")
        {
            using (var conn = new SQLiteConnection(_cs))
            {
                conn.Open();
                EnsureMember(conn, groupId, userId, permission);
            }
        }

        // Helper to avoid duplicate membership
        private void EnsureMember(SQLiteConnection conn, int groupId, int userId, string permission)
        {
            using (var check = new SQLiteCommand("SELECT COUNT(1) FROM GroupMembers WHERE GroupId=@g AND UserId=@u", conn))
            {
                check.Parameters.AddWithValue("@g", groupId);
                check.Parameters.AddWithValue("@u", userId);
                var exists = Convert.ToInt32(check.ExecuteScalar()) > 0;
                if (exists) return;
            }

            using (var insert = new SQLiteCommand(@"INSERT INTO GroupMembers (GroupId, UserId, Permission)
VALUES (@g, @u, @p)", conn))
            {
                insert.Parameters.AddWithValue("@g", groupId);
                insert.Parameters.AddWithValue("@u", userId);
                insert.Parameters.AddWithValue("@p", permission);
                insert.ExecuteNonQuery();
            }
        }
    }
}