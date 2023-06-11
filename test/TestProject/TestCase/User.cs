using Npgsql;
using NpgsqlMappingGenerator;

namespace TestProject.TestCase;

[DbTableGenerator("public.userdata")]
public partial class UserData
{
    [DbColumn<DbParamGuid>("id")]
    [DbAutoCreate<DbAutoCreateGuid>(DbAutoCreateType.Insert)]
    public Guid Id { get; set; }

    [DbColumn<DbParamString>("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [DbColumn<DbParamString>("last_name")]
    public string LastName { get; set; } = string.Empty;

    [DbColumn<DbParamDateTime>("last_update")]
    [DbAutoCreate<DbAutoCreateDateTimeNow>(DbAutoCreateType.Insert | DbAutoCreateType.Update)]
    public DateTime LastUpdate { get; set; }

    public static async ValueTask CreateTableAsync(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand("create table public.userdata (id uuid not null ,first_name text not null ,last_name text not null ,last_update timestamp(6) with time zone not null  ,primary key (id) );", connection);
        await cmd.PrepareAsync().ConfigureAwait(false);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}