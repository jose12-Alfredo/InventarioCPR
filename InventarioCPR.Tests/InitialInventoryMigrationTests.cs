using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace InventarioCPR.Tests;

// Run ONLY after confirming ExpectedHost against the endpoint of test-migracion-inicial in Neon.
public sealed class InitialInventoryMigrationTests
{
    private static async Task<NpgsqlConnection> OpenTestBranchAsync()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets("InventarioCPR-local").Build();
        var raw = configuration.GetConnectionString("NeonMigrationTest");
        var expectedHost = configuration["NeonMigrationTest:ExpectedHost"];
        if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(expectedHost))
            throw new InvalidOperationException("Configure NeonMigrationTest y ExpectedHost con el endpoint confirmado de test-migracion-inicial en User Secrets.");

        var test = new NpgsqlConnectionStringBuilder(raw) { SslMode = SslMode.VerifyFull };
        var normalHost = (test.Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        var expected = expectedHost.Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(normalHost, expected, StringComparison.OrdinalIgnoreCase) || test.Database != "neondb")
            throw new InvalidOperationException("La conexión de prueba no coincide con el endpoint y base autorizados.");

        var defaultRaw = configuration.GetConnectionString("NeonDirect");
        if (!string.IsNullOrWhiteSpace(defaultRaw))
        {
            var mainHost = (new NpgsqlConnectionStringBuilder(defaultRaw).Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(mainHost, normalHost, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La conexión de prueba apunta al mismo endpoint que NeonDirect.");
        }

        var connection = new NpgsqlConnection(test.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql, params NpgsqlParameter[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.AddRange(parameters);
        return (T)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Resultado nulo inesperado."));
    }

    [DatabaseFact]
    public async Task MigrationHasTablesHistoryIndexesAndRestrictiveForeignKeys()
    {
        await using var connection = await OpenTestBranchAsync();
        var tableCount = await ScalarAsync<long>(connection, null,
            "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname = 'public' AND tablename = ANY(ARRAY['departments','equipment_brands','equipment_categories','sites','areas','category_attribute_definitions','commercial_models','locations','staff_members','equipment','equipment_data_markers','equipment_inspections','equipment_location_observations','equipment_observations','technical_configurations','technical_verifications','photo_evidence','ram_modules','storage_units','technical_attribute_values'])");
        Assert.Equal(20, tableCount);
        Assert.Equal(1L, await ScalarAsync<long>(connection, null,
            "SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%_InitialInventorySchema'"));
        Assert.Equal(0L, await ScalarAsync<long>(connection, null,
            "SELECT count(*) FROM pg_constraint c JOIN pg_class t ON t.oid = c.conrelid " +
            "JOIN pg_namespace n ON n.oid = t.relnamespace WHERE n.nspname = 'public' " +
            "AND c.contype = 'f' AND c.confdeltype = 'c' AND t.relname NOT LIKE 'access_%'"));
        Assert.Equal(2L, await ScalarAsync<long>(connection, null,
            "SELECT count(*) FROM pg_indexes WHERE schemaname = 'public' AND indexname IN ('IX_equipment_CodeNormalized','IX_technical_configurations_EquipmentId') AND indexdef LIKE '%UNIQUE%'"));
        Assert.Equal(1L, await ScalarAsync<long>(connection, null,
            "SELECT count(*) FROM pg_constraint WHERE conname LIKE 'FK_photo_evidence_equipment_inspections_%' AND array_length(conkey, 1) = 2"));
    }

    [DatabaseFact]
    public async Task IncompleteEquipmentKeepsOriginalCodeAndRejectsNormalizedDuplicate()
    {
        await using var connection = await OpenTestBranchAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var categoryId = Guid.NewGuid();
        var code = "EQ_" + Guid.NewGuid().ToString("N")[..12];
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO equipment_categories (\"Id\", \"Name\") VALUES (@id, @name) RETURNING 1",
            new("id", categoryId), new("name", "Categoría ficticia " + code));
        var equipmentId = Guid.NewGuid();
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO equipment (\"Id\", \"Code\", \"CategoryId\", \"Name\") VALUES (@id, @code, @category, 'Equipo ficticio') RETURNING 1",
            new("id", equipmentId), new("code", code), new("category", categoryId));
        Assert.Equal(code, await ScalarAsync<string>(connection, transaction,
            "SELECT \"Code\" FROM equipment WHERE \"Id\" = @id", new NpgsqlParameter("id", equipmentId)));
        Assert.Equal(code.ToUpperInvariant(), await ScalarAsync<string>(connection, transaction,
            "SELECT \"CodeNormalized\" FROM equipment WHERE \"Id\" = @id", new NpgsqlParameter("id", equipmentId)));

        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => ScalarAsync<int>(connection, transaction,
            "INSERT INTO equipment (\"Id\", \"Code\", \"CategoryId\", \"Name\") VALUES (@id, @code, @category, 'Duplicado ficticio') RETURNING 1",
            new("id", Guid.NewGuid()), new("code", " " + code.ToLowerInvariant() + " "), new("category", categoryId)));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        await transaction.RollbackAsync();
    }

    [DatabaseFact]
    public async Task MultipleDrivesAndTechnicalRevisionsKeepTheirHistory()
    {
        await using var connection = await OpenTestBranchAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var categoryId = Guid.NewGuid();
        var equipmentId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO equipment_categories (\"Id\", \"Name\") VALUES (@id, @name) RETURNING 1",
            new("id", categoryId), new("name", "Categoría ficticia " + categoryId.ToString("N")));
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO equipment (\"Id\", \"Code\", \"CategoryId\", \"Name\") VALUES (@id, @code, @category, 'Equipo ficticio') RETURNING 1",
            new("id", equipmentId), new("code", "EQ_" + equipmentId.ToString("N")), new("category", categoryId));
        var start = DateTimeOffset.UtcNow;
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO technical_configurations (\"Id\", \"EquipmentId\", \"CategoryId\", \"Revision\", \"EffectiveFromUtc\", \"RamModulesComplete\", \"TotalRamGb\") VALUES (@id, @equipment, @category, 1, @start, false, 16) RETURNING 1",
            new("id", firstId), new("equipment", equipmentId), new("category", categoryId), new("start", start));
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO storage_units (\"Id\", \"TechnicalConfigurationId\", \"Position\", \"Technology\", \"Interface\", \"Capacity\", \"CapacityUnit\") VALUES (@id, @config, 1, 'SSD', 'NVMe', 512, 'GB') RETURNING 1",
            new("id", Guid.NewGuid()), new("config", firstId));
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO storage_units (\"Id\", \"TechnicalConfigurationId\", \"Position\", \"Technology\", \"Interface\", \"Capacity\", \"CapacityUnit\") VALUES (@id, @config, 2, 'HDD', 'SATA', 1, 'TB') RETURNING 1",
            new("id", Guid.NewGuid()), new("config", firstId));
        Assert.Equal(2L, await ScalarAsync<long>(connection, transaction,
            "SELECT count(*) FROM storage_units WHERE \"TechnicalConfigurationId\" = @id", new NpgsqlParameter("id", firstId)));

        // A savepoint allows asserting the index without aborting the transaction containing the fictitious records.
        await transaction.SaveAsync("before_duplicate");
        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => ScalarAsync<int>(connection, transaction,
            "INSERT INTO technical_configurations (\"Id\", \"EquipmentId\", \"CategoryId\", \"Revision\", \"EffectiveFromUtc\", \"RamModulesComplete\") VALUES (@id, @equipment, @category, 2, @start, false) RETURNING 1",
            new("id", Guid.NewGuid()), new("equipment", equipmentId), new("category", categoryId), new("start", start.AddHours(1))));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        await transaction.RollbackAsync("before_duplicate");

        await ScalarAsync<int>(connection, transaction,
            "UPDATE technical_configurations SET \"EffectiveToUtc\" = @end WHERE \"Id\" = @id RETURNING 1",
            new("end", start.AddHours(1)), new("id", firstId));
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO technical_configurations (\"Id\", \"EquipmentId\", \"CategoryId\", \"Revision\", \"EffectiveFromUtc\", \"RamModulesComplete\", \"TotalRamGb\") VALUES (@id, @equipment, @category, 2, @start, false, 32) RETURNING 1",
            new("id", secondId), new("equipment", equipmentId), new("category", categoryId), new("start", start.AddHours(1)));
        Assert.Equal(2L, await ScalarAsync<long>(connection, transaction,
            "SELECT count(*) FROM technical_configurations WHERE \"EquipmentId\" = @id", new NpgsqlParameter("id", equipmentId)));
        Assert.Equal(1L, await ScalarAsync<long>(connection, transaction,
            "SELECT count(*) FROM technical_configurations WHERE \"EquipmentId\" = @id AND \"EffectiveToUtc\" IS NULL", new NpgsqlParameter("id", equipmentId)));
        await transaction.RollbackAsync();
    }

    [DatabaseFact]
    public async Task PhotographCannotReferenceInspectionOfAnotherEquipment()
    {
        await using var connection = await OpenTestBranchAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var categoryId = Guid.NewGuid();
        var inspectorId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var inspectionId = Guid.NewGuid();
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO equipment_categories (\"Id\", \"Name\") VALUES (@id, @name) RETURNING 1",
            new("id", categoryId), new("name", "Categoría ficticia " + categoryId.ToString("N")));
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO staff_members (\"Id\", \"Name\") VALUES (@id, 'Inspector ficticio') RETURNING 1",
            new NpgsqlParameter("id", inspectorId));
        foreach (var id in new[] { firstId, secondId })
            await ScalarAsync<int>(connection, transaction,
                "INSERT INTO equipment (\"Id\", \"Code\", \"CategoryId\", \"Name\") VALUES (@id, @code, @category, 'Equipo ficticio') RETURNING 1",
                new("id", id), new("code", "EQ_" + id.ToString("N")), new("category", categoryId));
        await ScalarAsync<int>(connection, transaction,
            "INSERT INTO equipment_inspections (\"Id\", \"EquipmentId\", \"InspectorId\", \"InspectedAtUtc\", \"PhysicalCondition\") VALUES (@id, @equipment, @inspector, @at, 'Observación ficticia') RETURNING 1",
            new("id", inspectionId), new("equipment", firstId), new("inspector", inspectorId), new("at", DateTimeOffset.UtcNow));

        var invalid = await Assert.ThrowsAsync<PostgresException>(() => ScalarAsync<int>(connection, transaction,
            "INSERT INTO photo_evidence (\"Id\", \"EquipmentId\", \"InspectionId\", \"CloudinaryPublicId\", \"EvidenceType\", \"UploadedAtUtc\") VALUES (@id, @equipment, @inspection, @publicId, 'ficticia', @at) RETURNING 1",
            new("id", Guid.NewGuid()), new("equipment", secondId), new("inspection", inspectionId),
            new("publicId", "prueba-no-subida-" + Guid.NewGuid().ToString("N")), new("at", DateTimeOffset.UtcNow)));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, invalid.SqlState);
        await transaction.RollbackAsync();
    }
}
