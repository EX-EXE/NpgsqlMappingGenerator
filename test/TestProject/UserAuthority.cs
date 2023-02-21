using Npgsql;
using NpgsqlMappingGenerator;
namespace TestProject;

[DbTableGenerator("public.user_authority")]
public partial class UserAuthority
{
    [DbColumn<DbParamGuid>("user_data_id")]
    public Guid UserDataId { get; set; }

    [DbColumn<DbParamGuid>("authority_type_id")]
    public Guid AuthorityTypeId { get; set; }

    [DbColumn<DbParamDateTime>("last_update")]
    [DbAutoCreate<DbAutoCreateDateTimeNow>(DbAutoCreateType.Insert | DbAutoCreateType.Update)]
    public DateTime LastUpdate { get; set; }

    public static async ValueTask CreateTableAsync(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand("create table public.user_authority (user_data_id uuid not null ,authority_type_id uuid not null ,last_update timestamp(6) with time zone not null  ,primary key (user_data_id,authority_type_id) );", connection);
        await cmd.PrepareAsync().ConfigureAwait(false);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}