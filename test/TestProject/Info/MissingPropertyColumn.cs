﻿using Npgsql;
using NpgsqlMappingGenerator;
namespace TestProject;

[DbTableGenerator("public.missing_propertycolumn")]
public partial class MissingPropertyColumn
{
    [DbColumn<DbParamGuid>("id")]
    [DbAutoCreate<DbAutoCreateGuid>(DbAutoCreateType.Insert)]
    public Guid Id { get; set; }

    [DbColumn<DbParamString>("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [DbColumn<DbParamString>("last_name")]
    public string LastName { get; set; } = string.Empty;

    public static async ValueTask CreateTableAsync(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand($"create table {DbTableQuery} (id uuid not null ,first_name text not null ,last_name text not null ,last_update timestamp(6) with time zone not null  ,primary key (id) );", connection);
        await cmd.PrepareAsync().ConfigureAwait(false);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}