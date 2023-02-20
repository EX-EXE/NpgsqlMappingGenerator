using Npgsql;
using NpgsqlMappingGenerator;
namespace TestProject;

[DbTableGenerator("public.product")]
public partial class Product
{
    [DbColumn<DbParamGuid>("id")]
    [DbAutoCreate<DbAutoCreateGuid>(DbAutoCreateType.Insert)]
    public Guid Id { get; set; }

    [DbColumn<DbParamString>("name")]
    public string Name { get; set; } = string.Empty;

    [DbColumn<DbParamInteger>("price")]
    [DbAggregate(DbAggregateType.Avg | DbAggregateType.Count | DbAggregateType.Max | DbAggregateType.Min)]
    public int Price { get; set; } = 0;

    [DbColumn<DbParamDateTime>("last_update")]
    [DbAutoCreate<DbAutoCreateDateTimeNow>(DbAutoCreateType.Insert | DbAutoCreateType.Update)]
    public DateTime LastUpdate { get; set; }

    public static async ValueTask CreateTableAsync(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand("create table public.product (id uuid not null ,name text not null ,price integer not null ,last_update timestamp(6) with time zone not null  ,primary key (id) );", connection);
        await cmd.PrepareAsync().ConfigureAwait(false);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}