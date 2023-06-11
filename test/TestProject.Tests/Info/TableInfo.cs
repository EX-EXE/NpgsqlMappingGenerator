using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestProject.TestCase;
using Xunit.Abstractions;

namespace TestProject.Tests.Info;

public class TableInfo : PrepareDataBase, IAsyncLifetime
{
    public TableInfo(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(TableInfo))
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
    public async Task ExistsTable()
    {
        var exists = await UserData.ExistsTableAsync(Connection).ConfigureAwait(false);
        exists.Should().Be(true);
    }

    [Fact]
    public async Task NotExistsTable()
    {
        var exists = await TableTest.ExistsTableAsync(Connection).ConfigureAwait(false);
        exists.Should().Be(false);
    }
}
