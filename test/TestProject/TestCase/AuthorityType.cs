using Npgsql;
using NpgsqlMappingGenerator;

namespace TestProject.TestCase;

[DbTableGenerator("public.authority_type")]
public partial class AuthorityType
{
    [DbColumn<DbParamGuid>("id")]
    [DbAutoCreate<DbAutoCreateGuid>(DbAutoCreateType.Insert)]
    public Guid Id { get; set; }

    [DbColumn<DbParamString>("name")]
    public string Name { get; set; } = string.Empty;

    [DbColumn<DbParamDateTime>("last_update")]
    [DbAutoCreate<DbAutoCreateDateTimeNow>(DbAutoCreateType.Insert | DbAutoCreateType.Update)]
    public DateTime LastUpdate { get; set; }

    public static async ValueTask CreateTableAsync(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand("create table public.authority_type (id uuid not null ,name text not null ,last_update timestamp(6) with time zone not null  ,primary key (id) );", connection);
        await cmd.PrepareAsync().ConfigureAwait(false);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}