using Npgsql;
using NpgsqlMappingGenerator;

namespace TestProject;


[DbTableGenerator("public.datetime_table")]
public partial class DateTimeTable
{
    [DbColumn<DbParamGuid>("id")]
    [DbAutoCreate<DbAutoCreateGuid>(DbAutoCreateType.Insert)]
    public Guid Id { get; set; }

    [DbColumn<DbParamDateTime>("datetime")]
    public DateTime DateTime { get; set; } = DateTime.Now;

    [DbColumn<DbParamDateTimeUtc>("datetime_utc")]
    public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;

    [DbColumn<DbParamDateTimeOffset>("datetimeoffset")]
    public DateTimeOffset DateTimeOffset { get; set; } = DateTime.UtcNow;

    public static async ValueTask CreateTableAsync(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand("create table public.datetime_table (id uuid not null ,datetime timestamp without time zone not null ,datetime_utc timestamp with time zone not null ,datetimeoffset timestamp with time zone not null  ,primary key (id) );", connection);
        await cmd.PrepareAsync().ConfigureAwait(false);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}