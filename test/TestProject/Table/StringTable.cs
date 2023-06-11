using Npgsql;
using NpgsqlMappingGenerator;

namespace TestProject;


[DbTableGenerator("public.string_table")]
public partial class StringTable
{
    [DbColumn<DbParamGuid>("id")]
    [DbAutoCreate<DbAutoCreateGuid>(DbAutoCreateType.Insert)]
    public Guid Id { get; set; }

    [DbColumn<DbParamString>("data")]
    public string Data { get; set; } = string.Empty;

    public static async ValueTask CreateTableAsync(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand($"create table {DbTableQuery} (id uuid not null ,data text not null ,primary key (id) );", connection);
        await cmd.PrepareAsync().ConfigureAwait(false);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}