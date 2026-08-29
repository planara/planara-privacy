using Microsoft.EntityFrameworkCore;
using Planara.Privacy.Data;

namespace Planara.Privacy.Tests;

public static class DbTestUtils
{
    public static async Task ResetPrivacyDbAsync(DataContext db, CancellationToken cancellationToken = default)
    {
        db.ChangeTracker.Clear();

        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE "UserConsents", "ConsentVersions", "OutboxMessages" RESTART IDENTITY CASCADE;
            """,
            cancellationToken);
    }
}