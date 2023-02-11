namespace SampleProject;

using System;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlMappingGenerator;


[DbTableGenerator("public.actor")]
public partial class Actor
{
    [DbColumn<DbParamInteger>("actor_id")]
    public int ActorId { get; set; }

    [DbColumn<DbParamString>("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [DbColumn<DbParamString>("last_name")]
    public string LastName { get; set; } = string.Empty;

    [DbColumn<DbParamDateTime>("last_update")]
    public DateTime LastUpdate { get; set; }
}

internal class Program
{
    static async Task Main(string[] args)
    {
        await using var conn = new NpgsqlConnection("Host=localhost;Username=exe;Password=exe;Database=dvdrental");
        await conn.OpenAsync();

        //await Actor.UpdateAsync(conn, new Actor.IDbParam[]{
        //    Actor.DbParamFirstName.Create("Up"),
        //    Actor.DbParamLastName.Create("fB")}, 
        //    Actor.DbCondition.Create(DbCompareOperator.GreaterThan, Actor.DbParamActorId.Create(200)));

        await Actor.InsertAsync(conn, new Actor.IDbParam[]{
            new Actor.DbParamFirstName("A"),
            new Actor.DbParamLastName("B"),
        });

        await foreach (var row in Actor.SelectAsync(
            conn,
            Actor.DbColumnQueryType.All, 
            Actor.DbConditions.Create(
                DbLogicOperator.And,
                Actor.DbCondition.Create(DbCompareOperator.GreaterThanOrEqual, new Actor.DbParamActorId(100)),
                Actor.DbCondition.Create(DbCompareOperator.LessThan, new Actor.DbParamActorId(150)))))
        {
            Console.WriteLine($"{row.ActorId} {row.FirstName} {row.LastName} {row.LastUpdate}");
        }
    }
}