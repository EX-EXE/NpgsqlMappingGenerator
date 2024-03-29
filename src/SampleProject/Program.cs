﻿namespace SampleProject;

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlMappingGenerator;

[DbViewGenerator]
[DbViewTable<Category>]
[DbViewLeftOuterJoinAttributeName<FilmCategory, Category>(nameof(FilmCategory.CategoryId), nameof(Category.CategoryId))]
[DbViewLeftOuterJoinAttributeName<Film, FilmCategory>(nameof(Film.FilmId), nameof(FilmCategory.FilmId))]
[DbViewInnerJoin<FilmActor, Film>(nameof(FilmActor.FilmId), nameof(Film.FilmId))]
[DbViewInnerJoin<Actor, FilmActor>(nameof(Actor.ActorId), nameof(FilmActor.ActorId))]
public partial class FilmList
{
}

[DbViewGenerator]
[DbViewTable<Category>]
[DbViewLeftOuterJoinAttributeName<FilmCategory, Category>(nameof(FilmCategory.CategoryId), nameof(Category.CategoryId))]
[DbViewLeftOuterJoinAttributeName<Film, FilmCategory>(nameof(Film.FilmId), nameof(FilmCategory.FilmId))]
[DbViewInnerJoin<FilmActor, Film>(nameof(FilmActor.FilmId), nameof(Film.FilmId))]
[DbViewInnerJoin<Actor, FilmActor>(nameof(Actor.ActorId), nameof(FilmActor.ActorId))]
[DbViewColumn<Film>(nameof(Film.FilmId), DbAggregateType.Min | DbAggregateType.Max)]
[DbViewColumn<Film>(nameof(Film.Title))]
[DbViewColumn<Film>(nameof(Film.Description))]
[DbViewColumn<Category>(nameof(Category.Name))]
public partial class FilmList2
{
}

[DbViewGenerator]
[DbViewTable<User>]
[DbViewInnerJoin<UserType, User>(nameof(UserType.Id), nameof(User.UserTypeId))]
[DbViewColumn<User>(nameof(User.FirstName))]
[DbViewColumn<User>(nameof(User.LastName))]
[DbViewColumn<UserType>(nameof(UserType.Name))]
public partial class UserView
{

}

[DbTableGenerator("public.user")]
public partial class User
{
    [DbColumn<DbParamGuid>("id")]
    [DbAutoCreate<DbAutoCreateGuid>(DbAutoCreateType.Insert)]
    public Guid Id { get; set; }

    [DbColumn<DbParamGuid>("user_type_id")]
    public Guid UserTypeId { get; set; }

    [DbColumn<DbParamString>("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [DbColumn<DbParamString>("last_name")]
    public string LastName { get; set; } = string.Empty;

    [DbColumn<DbParamDateTime>("last_update")]
    [DbAutoCreate<DbAutoCreateDateTimeNow>(DbAutoCreateType.Insert | DbAutoCreateType.Update)]
    public DateTime LastUpdate { get; set; }
}

[DbTableGenerator("public.user_type")]
public partial class UserType
{
    [DbColumn<DbParamGuid>("id")]
    [DbAutoCreate<DbAutoCreateGuid>(DbAutoCreateType.Insert)]
    public Guid Id { get; set; }

    [DbColumn<DbParamString>("name")]
    public string Name { get; set; } = string.Empty;

    [DbColumn<DbParamDateTime>("last_update")]
    [DbAutoCreate<DbAutoCreateDateTimeNow>(DbAutoCreateType.Insert | DbAutoCreateType.Update)]
    public DateTime LastUpdate { get; set; }
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

        //foreach (var type in new[] { "TypeOne", "TypeTwo", "TypeThree" })
        //{
        //    var typeGuid = Guid.NewGuid();
        //    await UserType.InsertAsync(conn,
        //        new UserType.IDbParam[]
        //        {
        //            new UserType.DbParamId(typeGuid),
        //            new UserType.DbParamName(type),
        //        }).ConfigureAwait(false);
        //    foreach (var user in new[] { "UserOne", "UserTwo", "UserThree" })
        //    {
        //        await User.InsertAsync(conn,
        //            new User.IDbParam[]
        //            {
        //                new User.DbParamUserTypeId(typeGuid),
        //                new User.DbParamFirstName("Test"),
        //                new User.DbParamLastName(user)
        //            }).ConfigureAwait(false);
        //    }
        //}

        // Select
        // [Result]
        // Test UserThree TypeOne
        // Test UserTwo TypeOne
        // Test UserOne TypeOne
        // Test UserThree TypeTwo
        // Test UserTwo TypeTwo
        // Test UserOne TypeTwo
        // Test UserThree TypeThree
        // Test UserTwo TypeThree
        // Test UserOne TypeThree
        await foreach (var userViewRow in UserView.SelectAsync(
            conn,
            UserView.DbQueryType.All))
        {
            Console.WriteLine($"{userViewRow.UserFirstName} {userViewRow.UserLastName} {userViewRow.UserTypeName}");
        }


        //await Actor.UpdateAsync(conn, new Actor.IDbParam[]{
        //    Actor.DbParamFirstName.Create("Up"),
        //    Actor.DbParamLastName.Create("fB")}, 
        //    Actor.DbCondition.Create(DbCompareOperator.GreaterThan, Actor.DbParamActorId.Create(200)));

        //await foreach (var row in FilmList.SelectAsync(
        //    conn,
        //    FilmList.DbQueryType.All,
        //    //FilmList.DbCondition.Create(DbCompareOperator.Like,new FilmList.DbParamFilmDescription("%Brilliant%")),
        //   new  FilmList.DbConditions(
        //        DbLogicOperator.Or,
        //        new FilmList.DbCondition(DbCompareOperator.Equals, new FilmList.DbParamActorFirstName("Bette")),
        //        new FilmList.DbCondition(DbCompareOperator.Equals, new FilmList.DbParamActorFirstName("Cuba")))
        //    ))
        //{
        //    Console.WriteLine($"{row.FilmFilmId} {row.FilmTitle} {row.FilmDescription} {row.CategoryName}");
        //}

        var cancellationToken = CancellationToken.None;

        // Insert
        //foreach(var lastName in new[] { "One","Two","Three"})
        //{
        //    await User.InsertAsync(conn,
        //        new User.IDbParam[]
        //        {
        //        new User.DbParamFirstName("Test"),
        //        new User.DbParamLastName(lastName)
        //        }, cancellationToken).ConfigureAwait(false);
        //}

        //// Select
        //// [Result]
        //// 386fc06a-a9ba-40fa-b8f5-b188696f9495 Test One 2023/02/18 土 7:45:53
        //// 469136e3-6793-4102-a84d-9af826cf25ca Test Three 2023/02/18 土 7:45:53
        //await foreach (var userRow in User.SelectAsync(
        //    conn,
        //    User.DbQueryType.Id | User.DbQueryType.FirstName | User.DbQueryType.LastName | User.DbQueryType.LastUpdate, // User.DbQueryType.All
        //   new User.DbConditions(
        //        DbLogicOperator.Or,
        //        User.DbParamLastName.CreateCondition(DbCompareOperator.Equals, "One"),
        //        User.DbParamLastName.CreateCondition(DbCompareOperator.Equals, "Three")),
        //   cancellationToken:cancellationToken))
        //{
        //    Console.WriteLine($"{userRow.Id} {userRow.FirstName} {userRow.LastName} {userRow.LastUpdate}");
        //}

        // Update
        await User.UpdateAsync(conn,
            new User.IDbParam[]
            {
                new User.DbParamFirstName("TestUpdate"),
                new User.DbParamLastName("Four")
            },
            User.DbParamLastName.CreateCondition(DbCompareOperator.Equals, "Three"),
            cancellationToken).ConfigureAwait(false);

        // Delete
        await User.DeleteAsync(conn,
            User.DbParamLastName.CreateCondition(DbCompareOperator.Equals, "Two"),
            cancellationToken).ConfigureAwait(false);







        await Actor.UpdateAppendTextAsync(conn,
            new Actor.IDbParam[]
            {
                new Actor.DbParamFirstName("Append1"),
                new Actor.DbParamLastName("Append2"),
            },
            Actor.DbParamActorId.CreateCondition(DbCompareOperator.Equals, 215));
        await Actor.UpdateAppendTextAsync(conn,
            new Actor.IDbParam[]
            {
                new Actor.DbParamFirstName("Append1"),
                new Actor.DbParamLastName("Append2"),
            },
            Actor.DbParamActorId.CreateCondition(DbCompareOperator.Equals, 215),
            DbAppendType.Prepend);


        await Actor.UpsertAsync(conn, Actor.DbColumnType.ActorId,
            new Actor.IDbParam[]
            {
                new Actor.DbParamActorId(225),
                new Actor.DbParamFirstName("1"),
                new Actor.DbParamLastName("22"),
            });

        await Actor.UpsertAsync(conn, Actor.DbColumnType.ActorId,
            new Actor.IDbParam[]
            {
                new Actor.DbParamFirstName("Upsert"),
                new Actor.DbParamLastName("Insert"),
            },
            new Actor.IDbParam[]
            {
                new Actor.DbParamFirstName("Upsert"),
                new Actor.DbParamLastName("Update"),
            });

        await foreach (var row in Film.SelectAsync(
            conn,
            Film.DbQueryType.All))
        {
            Console.WriteLine($" {row.RentalRate} {row.Title} {row.ReleaseYear}");
        }


        await foreach (var row in Actor.SelectAsync(
            conn,
            Actor.DbQueryType.ActorIdCount | Actor.DbQueryType.ActorIdMin | Actor.DbQueryType.ActorIdMax))
        {
            Console.WriteLine($"{row.ActorIdCount} {row.ActorIdMax} {row.ActorIdMin}");
        }


        await foreach (var row in Actor.SelectAsync(
            conn,
            Actor.DbQueryType.AllColumns,
            new Actor.DbConditions(
                DbLogicOperator.And,
                Actor.DbParamActorId.CreateCondition(DbCompareOperator.Equals, 100),
                new Actor.DbCondition(DbCompareOperator.GreaterThanEqual, new Actor.DbParamActorId(100)),
                new Actor.DbCondition(DbCompareOperator.LessThan, new Actor.DbParamActorId(150)))))
        {
            Console.WriteLine($"{row.ActorId} {row.FirstName} {row.LastName} {row.LastUpdate}");
        }
    }
}