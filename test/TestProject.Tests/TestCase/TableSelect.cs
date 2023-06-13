using FluentAssertions;
using TestProject.TestCase;
using Xunit.Abstractions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestProject.Tests.TestCase;

public class TableSelect : PrepareDataBase, IAsyncLifetime
{
    List<(string, string)> InsertUserData = new();
    List<(string, int)> InsertProductData = new();

    public TableSelect(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(TableSelect))
    {
    }

    public async Task InitializeAsync()
    {
        await UserData.CreateTableAsync(Connection).ConfigureAwait(false);
        await Product.CreateTableAsync(Connection).ConfigureAwait(false);
        foreach (var firstNum in Enumerable.Range(0, 10))
        {
            foreach (var lastNum in Enumerable.Range(0, 10))
            {
                var firstName = $"First{firstNum}";
                var lastName = $"Last{lastNum}";
                InsertUserData.Add((firstName, lastName));
                await UserData.InsertAsync(Connection,
                    new UserData.IDbParam[]
                    {
                        new UserData.DbParamFirstName(firstName),
                        new UserData.DbParamLastName(lastName),
                    }).ConfigureAwait(false);

            }
        }
        foreach (var price in new[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 })
        {
            var name = $"Product{price}";
            InsertProductData.Add((name, price));
            await Product.InsertAsync(Connection,
                new Product.IDbParam[]
                {
                        new Product.DbParamName(name),
                        new Product.DbParamPrice(price),
                }).ConfigureAwait(false);
        }
    }


    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SelectAllOrderByAsc()
    {
        var index = 0;
        await foreach (var row in UserData.SelectAsync(Connection,
            UserData.DbQueryType.FirstName | UserData.DbQueryType.LastName,
            order: new UserData.DbOrder(NpgsqlMappingGenerator.DbOrderType.Asc, UserData.DbQueryType.LastUpdate)))
        {
            InsertUserData[index].Item1.Should().Be(row.FirstName);
            InsertUserData[index].Item2.Should().Be(row.LastName);
            ++index;
        }
    }

    [Fact]
    public async Task SelectAllOrderByDesc()
    {
        InsertUserData.Reverse();
        var index = 0;
        await foreach (var row in UserData.SelectAsync(Connection,
            UserData.DbQueryType.FirstName | UserData.DbQueryType.LastName,
            order: new UserData.DbOrder(NpgsqlMappingGenerator.DbOrderType.Desc, UserData.DbQueryType.LastUpdate)))
        {
            InsertUserData[index].Item1.Should().Be(row.FirstName);
            InsertUserData[index].Item2.Should().Be(row.LastName);
            ++index;
        }
    }

    [Fact]
    public async Task SelectMax()
    {
        await foreach (var row in Product.SelectAsync(Connection,
            Product.DbQueryType.PriceMax))
        {
            row.PriceMax.Should().Be(InsertProductData.Max(x => x.Item2));
        }
    }

    [Fact]
    public async Task SelectMin()
    {
        await foreach (var row in Product.SelectAsync(Connection,
            Product.DbQueryType.PriceMin))
        {
            row.PriceMin.Should().Be(InsertProductData.Min(x => x.Item2));
        }
    }

    [Fact]
    public async Task SelectAvg()
    {
        await foreach (var row in Product.SelectAsync(Connection,
            Product.DbQueryType.PriceAvg))
        {
            row.PriceAvg.Should().Be(InsertProductData.Average(x => x.Item2));
        }
    }

    [Fact]
    public async Task SelectWhereEquals()
    {
        foreach (var data in InsertProductData)
        {
            var selectResult = await Product.SelectAsync(
                Connection,
                Product.DbQueryType.Name,
                Product.DbParamPrice.CreateCondition(NpgsqlMappingGenerator.DbCompareOperator.Equals, data.Item2),
                order: new Product.DbOrder(NpgsqlMappingGenerator.DbOrderType.Asc, Product.DbQueryType.LastUpdate)).ToArrayAsync();
            selectResult.Length.Should().Be(1);
            selectResult[0].Name.Should().Be(data.Item1);
        }
    }

    [Fact]
    public async Task SelectWhereNotEquals()
    {
        foreach (var data in InsertProductData)
        {
            var selectResult = await Product.SelectAsync(
                Connection,
                Product.DbQueryType.Name,
                Product.DbParamPrice.CreateCondition(NpgsqlMappingGenerator.DbCompareOperator.NotEquals, data.Item2),
                order: new Product.DbOrder(NpgsqlMappingGenerator.DbOrderType.Asc, Product.DbQueryType.LastUpdate)).ToArrayAsync();
            selectResult.Length.Should().Be(InsertProductData.Where(x => x.Item2 != data.Item2).Count());
        }
    }

    [Fact]
    public async Task SelectWhereLessThan()
    {
        foreach (var data in InsertProductData)
        {
            var selectResult = await Product.SelectAsync(
                Connection,
                Product.DbQueryType.Name,
                Product.DbParamPrice.CreateCondition(NpgsqlMappingGenerator.DbCompareOperator.LessThan, data.Item2),
                order: new Product.DbOrder(NpgsqlMappingGenerator.DbOrderType.Asc, Product.DbQueryType.LastUpdate)).ToArrayAsync();
            selectResult.Length.Should().Be(InsertProductData.Where(x => x.Item2 < data.Item2).Count());
        }
    }

    [Fact]
    public async Task SelectWhereLessThanEqual()
    {
        foreach (var data in InsertProductData)
        {
            var selectResult = await Product.SelectAsync(
                Connection,
                Product.DbQueryType.Name,
                Product.DbParamPrice.CreateCondition(NpgsqlMappingGenerator.DbCompareOperator.LessThanEqual, data.Item2),
                order: new Product.DbOrder(NpgsqlMappingGenerator.DbOrderType.Asc, Product.DbQueryType.LastUpdate)).ToArrayAsync();
            selectResult.Length.Should().Be(InsertProductData.Where(x => x.Item2 <= data.Item2).Count());
        }
    }

    [Fact]
    public async Task SelectWhereGreaterThanl()
    {
        foreach (var data in InsertProductData)
        {
            var selectResult = await Product.SelectAsync(
                Connection,
                Product.DbQueryType.Name,
                Product.DbParamPrice.CreateCondition(NpgsqlMappingGenerator.DbCompareOperator.GreaterThan, data.Item2),
                order: new Product.DbOrder(NpgsqlMappingGenerator.DbOrderType.Asc, Product.DbQueryType.LastUpdate)).ToArrayAsync();
            selectResult.Length.Should().Be(InsertProductData.Where(x => x.Item2 > data.Item2).Count());
        }
    }

    [Fact]
    public async Task SelectWhereGreaterThanEquall()
    {
        foreach (var data in InsertProductData)
        {
            var selectResult = await Product.SelectAsync(
                Connection,
                Product.DbQueryType.Name,
                Product.DbParamPrice.CreateCondition(NpgsqlMappingGenerator.DbCompareOperator.GreaterThanEqual, data.Item2),
                order: new Product.DbOrder(NpgsqlMappingGenerator.DbOrderType.Asc, Product.DbQueryType.LastUpdate)).ToArrayAsync();
            selectResult.Length.Should().Be(InsertProductData.Where(x => x.Item2 >= data.Item2).Count());
        }
    }

    [Fact]
    public async Task SelectConditions()
    {
        //Func<Task> func = async () =>
        {
            await Product.SelectAsync(
            Connection,
            Product.DbQueryType.Name,
            new Product.DbConditions
            (
                NpgsqlMappingGenerator.DbLogicOperator.Or,
                Array.Empty<Product.IDbCondition>()
            ),
            order: new Product.DbOrder(NpgsqlMappingGenerator.DbOrderType.Asc, Product.DbQueryType.LastUpdate)).ToArrayAsync();
        };
        //await func.Should().NotThrowAsync<ArgumentException>();
    }
}