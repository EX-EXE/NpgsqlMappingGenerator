using FluentAssertions;
using Xunit.Abstractions;

namespace TestProject.Tests;

public class TableDelete : PrepareDataBase, IAsyncLifetime
{
    Dictionary<string, string> InsertUserData = new();

    public TableDelete(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(TableDelete))
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
    public async Task DeleteAll()
    {
        var num = await UserData.DeleteAsync(Connection).ConfigureAwait(false);
        num.Should().Be(InsertUserData.Count);
    }


    [Fact]
    public async Task DeleteAll2()
    {
        foreach (var (key, value) in InsertUserData)
        {
            var num = await UserData.DeleteAsync(Connection, UserData.DbParamFirstName.CreateCondition(NpgsqlMappingGenerator.DbCompareOperator.Equals, key)).ConfigureAwait(false);
            num.Should().Be(1);
        }
    }
}