namespace SampleProject;

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlMappingGenerator;

[DbViewGenerator]
[DbViewTable<Category>]
[DbViewLeftOuterJoinAttributeName<FilmCategory, Category>(nameof(FilmCategory.CategoryId), nameof(Category.CategoryId))]
[DbViewLeftOuterJoinAttributeName<Film, FilmCategory>(nameof(Film.FilmId), nameof(FilmCategory.FilmId))]
[DbViewInnerJoin<FilmActor, Film>(nameof(FilmActor.FilmId), nameof(Film.FilmId))]
[DbViewInnerJoin<Actor, FilmActor>(nameof(Actor.ActorId), nameof(FilmActor.ActorId))]
[DbViewColumn<Film>(nameof(Film.FilmId))]
[DbViewColumn<Film>(nameof(Film.Title))]
[DbViewColumn<Film>(nameof(Film.Description))]
[DbViewColumn<Category>(nameof(Category.Name))]
public partial class FilmList
{
}

[DbTableGenerator("public.actor")]
public partial class Actor
{
    [DbColumn<DbParamInteger>("actor_id")]
    [DbAggregate(DbAggregateType.Count | DbAggregateType.Min | DbAggregateType.Max)]
    public int ActorId { get; set; }

    [DbColumn<DbParamString>("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [DbColumn<DbParamString>("last_name")]
    public string LastName { get; set; } = string.Empty;

    [DbColumn<DbParamDateTime>("last_update")]
    public DateTime LastUpdate { get; set; }
}

[DbTableGenerator("public.category")]
public partial class Category
{
    [DbColumn<DbParamInteger>("category_id")]
    [DbAggregate(DbAggregateType.Count)]
    public int CategoryId { get; set; }

    [DbColumn<DbParamString>("name")]
    public string Name { get; set; } = string.Empty;

    [DbColumn<DbParamDateTime>("last_update")]
    public DateTime LastUpdate { get; set; }
}

[DbTableGenerator("public.film_category")]
public partial class FilmCategory
{
    [DbColumn<DbParamInteger>("film_id")]
    public int FilmId { get; set; }

    [DbColumn<DbParamInteger>("category_id")]
    public int CategoryId { get; set; }

    [DbColumn<DbParamDateTime>("last_update")]
    public DateTime LastUpdate { get; set; }
}

[DbTableGenerator("public.film")]
public partial class Film
{
    [DbColumn<DbParamInteger>("film_id")]
    public int FilmId { get; set; }

    [DbColumn<DbParamString>("title")]
    public string Title { get; set; } = string.Empty;

    [DbColumn<DbParamString>("description")]
    public string Description { get; set; } = string.Empty;

    [DbColumn<DbParamInteger>("release_year")]
    public int ReleaseYear { get; set; }

    [DbColumn<DbParamInteger>("language_id")]
    public int LanguageId { get; set; }

    [DbColumn<DbParamInteger>("rental_duration")]
    public int RentalDuration { get; set; }

    [DbColumn<DbParamDouble>("rental_rate")]
    public double RentalRate { get; set; }

    [DbColumn<DbParamInteger>("length")]
    public int Length { get; set; }

    [DbColumn<DbParamDouble>("replacement_cost")]
    public double ReplacementCost { get; set; }

    [DbColumn<DbParamDateTime>("last_update")]
    public DateTime LastUpdate { get; set; }

}

[DbTableGenerator("public.film_actor")]
public partial class FilmActor
{
    [DbColumn<DbParamInteger>("actor_id")]
    public int ActorId { get; set; }

    [DbColumn<DbParamInteger>("film_id")]
    public int FilmId { get; set; }

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

        await foreach (var row in FilmList.SelectAsync(
            conn,
            FilmList.DbColumnQueryType.All))
        {
            Console.WriteLine($" {row.Title} {row.Description} {row.Name}");
        }
        await Actor.InsertAsync(conn, new Actor.IDbParam[]{
            new Actor.DbParamFirstName("A"),
            new Actor.DbParamLastName("B"),
        });


        await foreach (var row in Film.SelectAsync(
            conn,
            Film.DbColumnQueryType.All))
        {
            Console.WriteLine($" {row.RentalRate} {row.Title} {row.ReleaseYear}");
        }


        await foreach (var row in Actor.SelectAsync(
            conn,
            Actor.DbColumnQueryType.ActorIdCount | Actor.DbColumnQueryType.ActorIdMin | Actor.DbColumnQueryType.ActorIdMax))
        {
            Console.WriteLine($"{row.ActorIdCount} {row.ActorIdMax} {row.ActorIdMin}");
        }


        await foreach (var row in Actor.SelectAsync(
            conn,
            Actor.DbColumnQueryType.AllColumn,
            Actor.DbConditions.Create(
                DbLogicOperator.And,
                Actor.DbCondition.Create(DbCompareOperator.GreaterThanOrEqual, new Actor.DbParamActorId(100)),
                Actor.DbCondition.Create(DbCompareOperator.LessThan, new Actor.DbParamActorId(150)))))
        {
            Console.WriteLine($"{row.ActorId} {row.FirstName} {row.LastName} {row.LastUpdate}");
        }
    }
}