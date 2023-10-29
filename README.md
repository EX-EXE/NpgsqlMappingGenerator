[![NuGet version](https://badge.fury.io/nu/NpgsqlMappingGenerator.svg)](https://badge.fury.io/nu/NpgsqlMappingGenerator)
# NpgsqlMappingGenerator
Generate a function that uses Npgsql.  
For example Select/Insert/Update/Delete functions.

## Install
### Install Npgsql
PM> Install-Package [Npgsql](https://www.nuget.org/packages/Npgsql/)

### Install NpgsqlMappingGenerator
PM> Install-Package [NpgsqlMappingGenerator](https://www.nuget.org/packages/NpgsqlMappingGenerator/)

## Example

### DbTable
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
#### Usage
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


### DbView
```csharp
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
```

#### Usage
```csharp
// Insert TestData
foreach (var type in new[] { "TypeOne", "TypeTwo", "TypeThree" })
{
    var typeGuid = Guid.NewGuid();
    await UserType.InsertAsync(conn,
        new UserType.IDbParam[]
        {
            new UserType.DbParamId(typeGuid),
            new UserType.DbParamName(type),
        }).ConfigureAwait(false);
    foreach (var user in new[] { "UserOne", "UserTwo", "UserThree" })
    {
        await User.InsertAsync(conn,
            new User.IDbParam[]
            {
                new User.DbParamUserTypeId(typeGuid),
                new User.DbParamFirstName("Test"),
                new User.DbParamLastName(user)
            }).ConfigureAwait(false);
    }
}

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
```
## DbCommand
```csharp
[DbCommand(
    "SELECT public.userdata.id as userid, public.userdata.first_name as first_name, public.userdata.last_name as last_name, public.authority_type.name as authority_name , public.authority_type.id as authorityid " +
    "FROM public.user_authority " +
    "JOIN public.userdata ON public.userdata.id = public.user_authority.user_data_id AND public.userdata.id = @param_userid " +
    "JOIN public.authority_type ON public.authority_type.id = public.user_authority.authority_type_id ")]
[DbCommandParam<DbParamGuid>("@param_userid")]
public partial class ViewUserAuthority
{
    [DbColumn<DbParamGuid>("userid")]
    public Guid UserId { get; set; }

    [DbColumn<DbParamGuid>("authorityid")]
    public Guid AuthorityId { get; set; }

    [DbColumn<DbParamString>("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [DbColumn<DbParamString>("last_name")]
    public string LastName { get; set; } = string.Empty;

    [DbColumn<DbParamString>("authority_name")]
    public string AuthorityName { get; set; } = string.Empty;
}
```
#### Usage
```csharp
await foreach (var row in ViewUserAuthority.ExecuteAsync(
    Connection, 
    ViewUserAuthority.DbQueryType.All, 
    param_userid: user.Id))
{
    // row.UserId
    // row.AuthorityId
    // row.FirstName
    // row.LastName
    // row.AuthorityName
}
```


## DbTable Attribute
| Attribute | Description |
|---|---|
|[DbTableGenerator(*TableName*)]|DbTable Hook Attribute|
|[DbColumn<*DbParamType*>(<ColumnName>)]|Specifies the column name.|

## DbView Attribute
| Attribute | Description |
|---|---|
|[DbViewGenerator(*TableName*)]|DbView Hook Attribute|
|[DbViewTable<*BaseTableType*>]|Specifying the base table.|
|[DbViewInnerJoin<*JoinTableType*,*CompTableType*>(*JoinColumnName*,*CompColumnName*)]|Specifies a join between tables.|
|[DbViewLeftOuterJoin<*JoinTableType*,*CompTableType*>(*JoinColumnName*,*CompColumnName*)]|Specifies a join between tables.|
|[DbViewRightOuterJoin<*JoinTableType*,*CompTableType*>(*JoinColumnName*,*CompColumnName*)]|Specifies a join between tables.|
|[DbViewFullOuterJoin<*JoinTableType*,*CompTableType*>(*JoinColumnName*,*CompColumnName*)]|Specifies a join between tables.|
|[DbViewCrossJoin<*JoinTableType*,*CompTableType*>(*JoinColumnName*,*CompColumnName*)]|Specifies a join between tables.|
|[DbViewColumn<*DbTable*>(*ColumnName*)]|Specifies the columns to use.|

# License in use
This generator use npgsql.
[npgsql LICENSE](https://github.com/npgsql/npgsql/blob/main/LICENSE)
```
Copyright (c) 2002-2021, Npgsql

Permission to use, copy, modify, and distribute this software and its
documentation for any purpose, without fee, and without a written agreement
is hereby granted, provided that the above copyright notice and this
paragraph and the following two paragraphs appear in all copies.

IN NO EVENT SHALL NPGSQL BE LIABLE TO ANY PARTY FOR DIRECT, INDIRECT,
SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES, INCLUDING LOST PROFITS,
ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS DOCUMENTATION, EVEN IF
Npgsql HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

NPGSQL SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING, BUT NOT LIMITED
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
PURPOSE. THE SOFTWARE PROVIDED HEREUNDER IS ON AN "AS IS" BASIS, AND Npgsql
HAS NO OBLIGATIONS TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS,
OR MODIFICATIONS.
```
