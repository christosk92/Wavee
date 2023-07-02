using Microsoft.EntityFrameworkCore;

namespace Wavee.Sqlite;

public static class LocalDbFactory
{
    private static (string, bool) initialized = (string.Empty, false);
    public static LocalDbContext Create(string cachePath)
    {
        try
        {
            LocalDbContext.DbPath = cachePath;
            if (initialized.Item2 && initialized.Item1 == cachePath)
            {
                return new LocalDbContext();
            }

            using var db = new LocalDbContext();
            db.Database.EnsureCreated();
            initialized = (cachePath, true);
            return new LocalDbContext();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create local database", ex);
        }
    }
}