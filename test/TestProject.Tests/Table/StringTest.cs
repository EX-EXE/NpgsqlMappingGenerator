using FluentAssertions;
using System;
using Xunit.Abstractions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestProject.Tests.Table;

public class StringTest : PrepareDataBase, IAsyncLifetime
{
    public StringTest(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(StringTest))
    {
    }

    public async Task InitializeAsync()
    {
        await StringTable.CreateTableAsync(Connection).ConfigureAwait(false);
        await StringTable.InsertAsync(Connection,
            new StringTable.IDbParam[]
            {
                new StringTable.DbParamData("TestData"),
            }).ConfigureAwait(false);
    }


    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task LikeTest()
    {
        bool hit = false;
        await foreach (var row in StringTable.SelectAsync(
            Connection,
            StringTable.DbQueryType.All,
            where: new StringTable.DbCondition(NpgsqlMappingGenerator.DbCompareOperator.Like, new StringTable.DbParamData("TestData"))))
        {
            hit |= true;
        }
        hit.Should().BeTrue();
    }

    [Fact]
    public async Task LikeTest2()
    {
        bool hit = false;
        await foreach (var row in StringTable.SelectAsync(
            Connection,
            StringTable.DbQueryType.All,
            where: new StringTable.DbCondition(NpgsqlMappingGenerator.DbCompareOperator.Like, new StringTable.DbParamData("testData"))))
        {
            hit |= true;
        }
        hit.Should().BeFalse();
    }
    [Fact]
    public async Task ILikeTest()
    {
        bool hit = false;
        await foreach (var row in StringTable.SelectAsync(
            Connection,
            StringTable.DbQueryType.All,
            where: new StringTable.DbCondition(NpgsqlMappingGenerator.DbCompareOperator.ILike, new StringTable.DbParamData("TestData"))))
        {
            hit |= true;
        }
        hit.Should().BeTrue();
    }

    [Fact]
    public async Task ILikeTest2()
    {
        bool hit = false;
        await foreach (var row in StringTable.SelectAsync(
            Connection,
            StringTable.DbQueryType.All,
            where: new StringTable.DbCondition(NpgsqlMappingGenerator.DbCompareOperator.ILike, new StringTable.DbParamData("testData"))))
        {
            hit |= true;
        }
        hit.Should().BeTrue();
    }
}