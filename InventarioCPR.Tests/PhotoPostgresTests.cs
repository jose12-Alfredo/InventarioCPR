using InventarioCPR.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class PhotoDatabaseFactAttribute : FactAttribute
{
    public PhotoDatabaseFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("INVENTARIOCPR_RUN_PHOTO_DATABASE_TESTS") != "test-migracion-inicial")
            Skip = "Requiere autorización explícita de la etapa A en test-migracion-inicial.";
    }
}

public sealed class PhotoPostgresTests
{
    private static async Task<NpgsqlConnection> OpenAsync()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets("InventarioCPR-local").Build();
        var raw = NeonConnection.DevelopmentApp(configuration);
        if (configuration["NeonDevelopmentApp:BranchConfirmed"] != "test-migracion-inicial" ||
            new NpgsqlConnectionStringBuilder(raw).Host?.Replace("-pooler.", ".") != "ep-raspy-dream-b476uukh.c-6.us-east-2.aws.neon.tech")
            throw new InvalidOperationException("El destino no coincide con el autorizado para las fotografías.");
        var connection = new NpgsqlConnection(raw);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<object?> ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string sql, params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, tx);
        command.Parameters.AddRange(parameters);
        return await command.ExecuteScalarAsync();
    }

    [PhotoDatabaseFact]
    public async Task PhotoMigrationIsAppliedWithSameEquipmentForeignKeysAndLivePrincipalIndex()
    {
        await using var connection = await OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        Assert.Equal(1L, await ExecuteAsync(connection, tx, "SELECT count(*) FROM public.\"__EFMigrationsHistory\" WHERE \"MigrationId\"='20261109161923_EquipmentPhotoHistory'"));
        Assert.Equal(1L, await ExecuteAsync(connection, tx, "SELECT count(*) FROM pg_indexes WHERE indexname='IX_photo_principal_periods_EquipmentId' AND indexdef LIKE '%UNIQUE%' AND indexdef LIKE '%WHERE%ToUtc%IS NULL%'"));
        Assert.Equal(2L, await ExecuteAsync(connection, tx, "SELECT count(*) FROM pg_constraint WHERE conname IN ('FK_photo_principal_periods_photo_evidence_PhotoId_EquipmentId','FK_photo_uploads_photo_evidence_EvidenceId_EquipmentId') AND array_length(conkey,1)=2 AND confdeltype='r'"));
        await tx.RollbackAsync();
    }

    [PhotoDatabaseFact]
    public async Task DatabaseRejectsTwoCurrentPrincipalsAndEvidenceOfAnotherEquipment()
    {
        await using var connection = await OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        var category = Guid.NewGuid(); var first = Guid.NewGuid(); var second = Guid.NewGuid(); var photo = Guid.NewGuid();
        await ExecuteAsync(connection, tx, "INSERT INTO equipment_categories (\"Id\",\"Name\") VALUES (@id,@name)", new NpgsqlParameter("id", category), new("name", "FOTOS_FICTICIAS_" + category.ToString("N")));
        foreach (var id in new[] { first, second })
            await ExecuteAsync(connection, tx, "INSERT INTO equipment (\"Id\",\"Code\",\"Name\",\"CategoryId\") VALUES (@id,@code,'Equipo ficticio de pruebas de restricciones',@category)", new NpgsqlParameter("id", id), new("code", "VALIDA_FOTOS_SQL_" + id.ToString("N")), new("category", category));
        await ExecuteAsync(connection, tx, "INSERT INTO photo_evidence (\"Id\",\"EquipmentId\",\"CloudinaryPublicId\",\"EvidenceType\",\"UploadedAtUtc\") VALUES (@id,@eq,@public,'General',now())", new NpgsqlParameter("id", photo), new("eq", first), new("public", "prueba-sin-archivo-" + photo.ToString("N")));
        const string principalSql = "INSERT INTO photo_principal_periods (\"Id\",\"EquipmentId\",\"PhotoId\",\"FromUtc\",\"ActorId\",\"ActorName\") VALUES (@id,@eq,@photo,now(),@actor,'Actor ficticio de restricciones')";
        await ExecuteAsync(connection, tx, principalSql, new NpgsqlParameter("id", Guid.NewGuid()), new("eq", first), new("photo", photo), new("actor", Guid.NewGuid()));
        await tx.SaveAsync("duplicate_principal");
        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, tx, principalSql, new NpgsqlParameter("id", Guid.NewGuid()), new("eq", first), new("photo", photo), new("actor", Guid.NewGuid())));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        await tx.RollbackAsync("duplicate_principal");
        await tx.SaveAsync("foreign_photo");
        var foreign = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, tx, principalSql, new NpgsqlParameter("id", Guid.NewGuid()), new("eq", second), new("photo", photo), new("actor", Guid.NewGuid())));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, foreign.SqlState);
        await tx.RollbackAsync("foreign_photo");
        await tx.RollbackAsync();
    }
}
