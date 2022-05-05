﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031")]
[assembly: SuppressMessage("Reliability", "CA2007")]
[assembly: SuppressMessage("Roslynator", "RCS1123")]

[assembly: SuppressMessage("Minor Code Smell", "S101")]
[assembly: SuppressMessage("Major Code Smell", "S107", Scope = "member", Target = "~M:AutoKkutu.Databases.SQLite.SQLiteDatabase.RegisterRearrangeMissionFunc(Microsoft.Data.Sqlite.SqliteConnection)")]
[assembly: SuppressMessage("Major Code Smell", "S1168", Scope = "member", Target = "~M:AutoKkutu.Databases.CommonDatabaseConnection.TryExecuteReader(System.String,System.String,AutoKkutu.Databases.CommonDatabaseParameter[])~System.Data.Common.DbDataReader")]

// Dispose pattern
[assembly: SuppressMessage("Reliability", "CA2000", Scope = "member", Target = "~M:AutoKkutu.Databases.MySQL.MySQLDatabase.#ctor(System.String)")]
[assembly: SuppressMessage("Reliability", "CA2000", Scope = "member", Target = "~M:AutoKkutu.Databases.PostgreSQL.PostgreSQLDatabase.#ctor(System.String)")]
[assembly: SuppressMessage("Reliability", "CA2000", Scope = "member", Target = "~M:AutoKkutu.Databases.SQLite.SQLiteDatabase.#ctor(System.String)")]
[assembly: SuppressMessage("Reliability", "CA2000", Scope = "member", Target = "~M:AutoKkutu.Databases.SQLite.SQLiteDatabaseHelper.ExecuteReader(Microsoft.Data.Sqlite.SqliteConnection,System.String,Microsoft.Data.Sqlite.SqliteParameter[])~Microsoft.Data.Sqlite.SqliteDataReader")]

// SQL injection
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutu.Databases.MySQL.MySQLDatabaseConnection.ExecuteNonQuery(System.String,AutoKkutu.Databases.CommonDatabaseParameter[])~System.Int32")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutu.Databases.MySQL.MySQLDatabaseConnection.ExecuteReader(System.String,AutoKkutu.Databases.CommonDatabaseParameter[])~System.Data.Common.DbDataReader")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutu.Databases.MySQL.MySQLDatabaseConnection.ExecuteScalar(System.String,AutoKkutu.Databases.CommonDatabaseParameter[])~System.Object")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutu.Databases.PostgreSQL.PostgreSQLDatabaseConnection.ExecuteNonQuery(System.String,AutoKkutu.Databases.CommonDatabaseParameter[])~System.Int32")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutu.Databases.PostgreSQL.PostgreSQLDatabaseConnection.ExecuteReader(System.String,AutoKkutu.Databases.CommonDatabaseParameter[])~System.Data.Common.DbDataReader")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutu.Databases.PostgreSQL.PostgreSQLDatabaseConnection.ExecuteScalar(System.String,AutoKkutu.Databases.CommonDatabaseParameter[])~System.Object")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutu.Databases.SQLite.SQLiteDatabaseHelper.ExecuteNonQuery(Microsoft.Data.Sqlite.SqliteConnection,System.String,Microsoft.Data.Sqlite.SqliteParameter[])~System.Int32")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutu.Databases.SQLite.SQLiteDatabaseHelper.ExecuteReader(Microsoft.Data.Sqlite.SqliteConnection,System.String,Microsoft.Data.Sqlite.SqliteParameter[])~Microsoft.Data.Sqlite.SqliteDataReader")]
[assembly: SuppressMessage("Security", "CA2100", Scope = "member", Target = "~M:AutoKkutu.Databases.SQLite.SQLiteDatabaseHelper.ExecuteScalar(Microsoft.Data.Sqlite.SqliteConnection,System.String,Microsoft.Data.Sqlite.SqliteParameter[])~System.Object")]

// SecureRandom
[assembly: SuppressMessage("Security", "CA5394", Scope = "member", Target = "~M:AutoKkutu.Utils.RandomUtils.GenerateRandomString(System.Int32,System.Boolean,System.Random)~System.String")]
