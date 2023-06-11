using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestProject.TestCase;
using Xunit.Abstractions;

namespace TestProject.Tests.Info;

public class ColumnInfo : PrepareDataBase, IAsyncLifetime
{
    public ColumnInfo(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(ColumnInfo))
    {
    }

    public async Task InitializeAsync()
    {
        await TableTest.CreateTableAsync(Connection).ConfigureAwait(false);
        await MissingDbColumn.CreateTableAsync(Connection).ConfigureAwait(false);
        await MissingPropertyColumn.CreateTableAsync(Connection).ConfigureAwait(false);
    }


    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task NoMissingTest()
    {
        Func<Task> func = async () => await TableTest.CheckTableColumnsAsync(Connection);
        await func.Should().NotThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MissingDbColumnTest()
    {
        Func<Task> func = async () => await MissingDbColumn.CheckTableColumnsAsync(Connection);
        await func.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MissingPropertyColumnTest()
    {
        Func<Task> func = async () => await MissingPropertyColumn.CheckTableColumnsAsync(Connection);
        await func.Should().ThrowAsync<InvalidOperationException>();
    }
}
