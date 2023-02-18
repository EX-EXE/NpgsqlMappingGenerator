# NpgsqlMappingGenerator
Generate a function that uses Npgsql.  
For example Select/Insert/Update/Delete functions.

## Quick Start
### Install Npgsql
PM> Install-Package [Npgsql](https://www.nuget.org/packages/Npgsql/)

### Install NpgsqlMappingGenerator
PM> Install-Package [NpgsqlMappingGenerator](https://www.nuget.org/packages/NpgsqlMappingGenerator/)

### Define DBTable
```csharp
[DbTableGenerator("public.user")]
public partial class User
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
}
```
### Usage
```csharp
// Insert
foreach(var lastName in new[] { "One","Two","Three"})
{
    await User.InsertAsync(conn,
        new User.IDbParam[]
        {
            new User.DbParamFirstName("Test"),
            new User.DbParamLastName(lastName)
        });
}

// Select
// [Result]
// 386fc06a-a9ba-40fa-b8f5-b188696f9495 Test One 2023/02/18 土 7:45:53
// 469136e3-6793-4102-a84d-9af826cf25ca Test Three 2023/02/18 土 7:45:53
await foreach (var userRow in User.SelectAsync(
    conn,
    User.DbQueryType.Id | User.DbQueryType.FirstName | User.DbQueryType.LastName | User.DbQueryType.LastUpdate, // User.DbQueryType.All
   new User.DbConditions(
        DbLogicOperator.Or,
        User.DbParamLastName.CreateCondition(DbCompareOperator.Equals, "One"),
        User.DbParamLastName.CreateCondition(DbCompareOperator.Equals, "Three")),
   ))
{
    Console.WriteLine($"{userRow.Id} {userRow.FirstName} {userRow.LastName} {userRow.LastUpdate}");
}

// Update
await User.UpdateAsync(conn,
    new User.IDbParam[]
    {
        new User.DbParamFirstName("TestUpdate"),
        new User.DbParamLastName("Four")
    },
    User.DbParamLastName.CreateCondition(DbCompareOperator.Equals, "Three"));
            
// Delete
await User.DeleteAsync(conn,
    User.DbParamLastName.CreateCondition(DbCompareOperator.Equals, "Two"));
```


