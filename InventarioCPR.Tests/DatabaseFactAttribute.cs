using Xunit;

namespace InventarioCPR.Tests;

public sealed class DatabaseFactAttribute : FactAttribute
{
    public DatabaseFactAttribute(bool persistsData = false)
    {
        var variable = persistsData
            ? "INVENTARIOCPR_RUN_PERSISTENT_DATABASE_TESTS"
            : "INVENTARIOCPR_RUN_DATABASE_TESTS";
        if (Environment.GetEnvironmentVariable(variable) != "test-migracion-inicial")
            Skip = $"Requiere opt-in explícito mediante {variable}.";
    }
}
