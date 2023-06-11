using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace TestProject.Tests;

public class TableInfo : PrepareDataBase, IAsyncLifetime
{
    public TableInfo(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(TableInfo))
    {
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
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
        var exists = await NotCreateTable.ExistsTableAsync(Connection).ConfigureAwait(false);
        exists.Should().Be(false);
    }
}
