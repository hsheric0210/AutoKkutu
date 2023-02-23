// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031")]
[assembly: SuppressMessage("Reliability", "CA2007")]
[assembly: SuppressMessage("Roslynator", "RCS1123")]
[assembly: SuppressMessage("Minor Code Smell", "S3220")]
[assembly: SuppressMessage("Major Securiy Hotspot", "S2077")]

[assembly: SuppressMessage("Major Code Smell", "S107", Scope = "member", Target = "~M:AutoKkutuLib.Database.SQLite.SqliteDatabase.RegisterMissionWordPriorityFunc(Microsoft.Data.Sqlite.SqliteConnection)")]
[assembly: SuppressMessage("Major Code Smell", "S1168", Scope = "member", Target = "~M:AutoKkutuLib.Database.CommonDatabaseCommand.TryExecuteReader(System.String)~System.Data.Common.DbDataReader")]

// SQL injection
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutuLib.Database.SQLite.SQLiteDatabaseCommand.#ctor(Microsoft.Data.Sqlite.SqliteConnection,System.String,System.Boolean)")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutuLib.Database.PostgreSQL.PostgreSQLDatabaseCommand.#ctor(Npgsql.NpgsqlConnection,System.String,System.Boolean)")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutuLib.Database.MySQL.MySQLDatabaseCommand.#ctor(MySqlConnector.MySqlConnection,System.String,System.Boolean)")]

// Dispose pattern
[assembly: SuppressMessage("Reliability", "CA2000", Scope = "member", Target = "~M:AutoKkutuLib.Database.MySQL.MySqlDatabase.#ctor(System.String)")]
[assembly: SuppressMessage("Reliability", "CA2000", Scope = "member", Target = "~M:AutoKkutuLib.Database.PostgreSQL.PostgreSqlDatabase.#ctor(System.String)")]
[assembly: SuppressMessage("Reliability", "CA2000", Scope = "member", Target = "~M:AutoKkutuLib.Database.SQLite.SqliteDatabase.#ctor(System.String)")]
[assembly: SuppressMessage("Reliability", "CA2000", Scope = "member", Target = "~M:AutoKkutuLib.Database.SQLite.SqliteDatabaseHelper.ExecuteReader(Microsoft.Data.Sqlite.SqliteConnection,System.String,Microsoft.Data.Sqlite.SqliteParameter[])~Microsoft.Data.Sqlite.SqliteDataReader")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutuLib.Database.SQLite.SqliteDatabaseHelper.ExecuteNonQuery(Microsoft.Data.Sqlite.SqliteConnection,System.String,Microsoft.Data.Sqlite.SqliteParameter[])~System.Int32")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutuLib.Database.SQLite.SqliteDatabaseHelper.ExecuteReader(Microsoft.Data.Sqlite.SqliteConnection,System.String,Microsoft.Data.Sqlite.SqliteParameter[])~Microsoft.Data.Sqlite.SqliteDataReader")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutuLib.Database.SQLite.SqliteDatabaseHelper.ExecuteScalar(Microsoft.Data.Sqlite.SqliteConnection,System.String,Microsoft.Data.Sqlite.SqliteParameter[])~System.Object")]

// SecureRandom
[assembly: SuppressMessage("Security", "CA5394", Scope = "member", Target = "~M:AutoKkutuLib.Utils.RandomUtils.GenerateRandomString(System.Int32,System.Boolean,System.Random)~System.String")]
[assembly: SuppressMessage("Major Code Smell", "S1121:Assignments should not be made from within sub-expressions", Justification = "<Pending>", Scope = "member", Target = "~M:AutoKkutuLib.Node.NodeCount.Increment(AutoKkutuLib.NodeTypes,System.Int32)")]
[assembly: SuppressMessage("Major Code Smell", "S1121:Assignments should not be made from within sub-expressions", Justification = "<Pending>", Scope = "member", Target = "~M:AutoKkutuLib.Node.WordCount.Increment(AutoKkutuLib.WordFlags,System.Int32)")]
