using FluentAssertions;
using Xunit.Abstractions;

namespace TestProject.Tests;

public class TableUpdate : PrepareDataBase, IAsyncLifetime
{
    Dictionary<string, string> InsertUserData = new();

    public TableUpdate(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(TableUpdate))
    {
    }

    public async Task InitializeAsync()
    {
        await UserData.CreateTableAsync(Connection).ConfigureAwait(false);
        foreach (var firstNum in Enumerable.Range(0, 10))
        {
            var firstName = $"First{firstNum}";
            var lastName = $"Last";
            InsertUserData.Add(firstName, lastName);
            await UserData.InsertAsync(Connection,
                new UserData.IDbParam[]
                {
                        new UserData.DbParamFirstName(firstName),
                        new UserData.DbParamLastName(lastName),
                }).ConfigureAwait(false);
        }
    }


    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UpdateAll()
    {
        foreach (var (key, value) in InsertUserData.ToArray())
        {
            var newFirst = $"Update{key}";
            var newLast = $"Update{value}";
            InsertUserData[newFirst] = newLast;
            var num = await UserData.UpdateAsync(
                Connection, 
                new UserData.IDbParam[]
                {
                    new UserData.DbParamFirstName(newFirst),
                    new UserData.DbParamLastName(newLast),
                },
                UserData.DbParamFirstName.CreateCondition(NpgsqlMappingGenerator.DbCompareOperator.Equals, key)
                ).ConfigureAwait(false);
            num.Should().Be(1);
        }

        await foreach (var row in UserData.SelectAsync(Connection,
            UserData.DbQueryType.FirstName | UserData.DbQueryType.LastName))
        {
            InsertUserData[row.FirstName].Should().Be(row.LastName);
        }
    }
}