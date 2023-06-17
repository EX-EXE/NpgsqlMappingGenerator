using FluentAssertions;
using TestProject.TestCase;
using Xunit.Abstractions;

namespace TestProject.Tests.TestCase;

public class TableUpsert : PrepareDataBase, IAsyncLifetime
{
    Dictionary<string, string> InsertUserData = new();

    public TableUpsert(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(TableUpsert))
    {
    }

    public async Task InitializeAsync()
    {
        await UserData.CreateTableAsync(Connection).ConfigureAwait(false);
    }


    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UpsertTest()
    {
        var count = 10;
        var insertNames = new List<string>();
        var updateNames = new List<string>();
        var firstName = nameof(UpsertTest);
        // Insert
        foreach (var num in Enumerable.Range(0, count))
        {
            var insertName = $"Insert{num}";
            insertNames.Add(insertName);
            await UserData.UpsertAsync(Connection,
                UserData.DbColumnType.FirstName | UserData.DbColumnType.LastName,
                new UserData.IDbParam[]
                {
                    new UserData.DbParamFirstName(firstName),
                    new UserData.DbParamLastName(insertName),
                }).ConfigureAwait(false);
        }
        // Update
        foreach (var num in Enumerable.Range(0, count))
        {
            var updateName = $"Update{num}";
            updateNames.Add(updateName);
            await UserData.UpsertAsync(Connection,
            UserData.DbColumnType.FirstName | UserData.DbColumnType.LastName,
            new UserData.IDbParam[]
            {
                new UserData.DbParamFirstName(firstName),
                new UserData.DbParamLastName(insertNames[num]),
            },
            new UserData.IDbParam[]
            {
                new UserData.DbParamFirstName(firstName),
                new UserData.DbParamLastName(updateName),
            });
        }

        // Check
        await foreach (var row in UserData.SelectAsync(
            Connection,
            UserData.DbQueryType.LastName,
            UserData.DbParamFirstName.CreateCondition(NpgsqlMappingGenerator.DbCompareOperator.Equals, nameof(UpsertTest))))
        {
            updateNames.Contains(row.LastName).Should().BeTrue();
        }
    }
}