using FluentAssertions;
using Xunit.Abstractions;

namespace TestProject.Tests;

public class TableSelect : PrepareDataBase, IAsyncLifetime
{
    List<(string, string)> InsertData = new();

    public TableSelect(ITestOutputHelper outputHelper)
        : base(outputHelper,nameof(TableSelect))
    {
        //UserData.CreateTable(Connection);
    }

    public async Task InitializeAsync()
    {
        await UserData.CreateTableAsync(Connection).ConfigureAwait(false);
        foreach (var firstNum in Enumerable.Range(0, 10))
        {
            foreach (var lastNum in Enumerable.Range(0, 10))
            {
                var firstName = $"First{firstNum}";
                var lastName = $"Last{lastNum}";
                InsertData.Add((firstName, lastName));
                await UserData.InsertAsync(Connection,
                    new UserData.IDbParam[]
                    {
                        new UserData.DbParamFirstName(firstName),
                        new UserData.DbParamLastName(lastName),
                    }).ConfigureAwait(false);

            }
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SelectAll()
    {
        var index = 0;
        await foreach (var row in UserData.SelectAsync(Connection,
            UserData.DbQueryType.FirstName | UserData.DbQueryType.LastName,
            order: new UserData.DbOrder(NpgsqlMappingGenerator.DbOrderType.Asc, UserData.DbQueryType.LastUpdate)))
        {
            InsertData[index].Item1.Should().Be(row.FirstName);
            InsertData[index].Item2.Should().Be(row.LastName);
            ++index;
        }
    }
}