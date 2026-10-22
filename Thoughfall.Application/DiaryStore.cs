namespace Thoughfall.Application;

using System.Globalization;
using Microsoft.Data.Sqlite;

/// <summary>Owns the diary database. All database work runs off the caller's thread.</summary>
public sealed class DiaryStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DiaryStore(string folder)
    {
        Folder = DataPaths.ResolveDirectory(folder);
    }

    public string Folder { get; }

    public string FilePath => Path.Combine(Folder, "data.db3");

    /// <summary>Commits the exact text as one entry. Blank input is ignored.</summary>
    public async Task<bool> SaveAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var entry = new DiaryEntry(Guid.NewGuid(), DateTimeOffset.Now, text);
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            // Microsoft.Data.Sqlite's async APIs execute synchronously; move the whole operation.
            return await Task.Run(() =>
                {
                    using var connection = OpenDatabase();
                    using var command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO entries (id, timestamp, text) VALUES ($id, $timestamp, $text)";
                    command.Parameters.AddWithValue("$id", entry.Id.ToString("D"));
                    command.Parameters.AddWithValue("$timestamp", entry.Timestamp.ToString("O", CultureInfo.InvariantCulture));
                    command.Parameters.AddWithValue("$text", entry.Text);
                    command.ExecuteNonQuery();
                    return true;
                })
                .ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            throw new IOException("The diary database could not save this entry. Check the data folder and retry.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Materializes a snapshot before releasing database access.</summary>
    public async Task<IReadOnlyList<DiaryEntry>> ReadRangeAsync(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException("The From date must be on or before the To date.");
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
                {
                    using var connection = OpenDatabase();
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT id, timestamp, text FROM entries ORDER BY rowid";
                    using var reader = command.ExecuteReader();
                    return DiaryEntries.SelectRange(ReadEntries(reader), from, to);
                })
                .ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            throw new IOException("The diary database could not be read. Check the data folder and retry.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task ExportAsync(string destination, IReadOnlyList<DiaryEntry> entries, ExportFormat format)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return Task.Run(() =>
        {
            var path = Path.GetFullPath(destination);
            var parent = DataPaths.ResolveDirectory(Path.GetDirectoryName(path)!);
            path = Path.Combine(parent, Path.GetFileName(path));
            var folder = Path.TrimEndingDirectorySeparator(DataPaths.ResolveDirectory(Folder));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (path.Equals(folder, comparison) || path.StartsWith(folder + Path.DirectorySeparatorChar, comparison))
            {
                throw new IOException("Choose a file outside the diary data folder.");
            }

            ExportFile.Write(path, entries, format);
        });
    }

    private SqliteConnection OpenDatabase()
    {
        Directory.CreateDirectory(Folder);
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = FilePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,
            }.ToString()
        );
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA synchronous = FULL";
            command.ExecuteNonQuery();
            using var transaction = connection.BeginTransaction();
            command.Transaction = transaction;
            command.CommandText = "PRAGMA user_version";
            var version = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (version == 0)
            {
                command.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE name NOT LIKE 'sqlite_%'";
                if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 0)
                {
                    throw new InvalidDataException("The database has an unrecognized schema. It has not been replaced.");
                }

                command.CommandText = """
                    CREATE TABLE entries (
                        id TEXT NOT NULL PRIMARY KEY,
                        timestamp TEXT NOT NULL,
                        text TEXT NOT NULL
                    );
                    PRAGMA user_version = 1;
                    """;
                command.ExecuteNonQuery();
            }
            else if (version != 1)
            {
                throw new InvalidDataException($"Diary database version {version} is not supported by this application.");
            }

            transaction.Commit();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static IEnumerable<DiaryEntry> ReadEntries(SqliteDataReader reader)
    {
        while (reader.Read())
        {
            if (
                reader.IsDBNull(0)
                || reader.IsDBNull(1)
                || reader.IsDBNull(2)
                || !Guid.TryParse(reader.GetString(0), out var id)
                || id == Guid.Empty
                || !DateTimeOffset.TryParseExact(reader.GetString(1), "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp)
            )
            {
                throw new InvalidDataException("The diary contains an invalid entry. Back up data.db3 before repairing it.");
            }

            yield return new DiaryEntry(id, timestamp, reader.GetString(2));
        }
    }
}
