using FluentAssertions;
using System;
using Xunit.Abstractions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestProject.Tests;

public class DateTimeTest : PrepareDataBase, IAsyncLifetime
{
    public DateTimeTest(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(DateTimeTest))
    {
    }

    public async Task InitializeAsync()
    {
        await DateTimeTable.CreateTableAsync(Connection).ConfigureAwait(false);
    }


    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Test()
    {
        var dateTime = new DateTime(1990, 1, 2, 3, 4, 5, DateTimeKind.Local);
        var dateTimeUtc = new DateTime(1990, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        var dateTimeOffset = new DateTimeOffset(1990, 1, 2, 3, 4, 5, TimeSpan.FromHours(9.0));

        OutputHelper.WriteLine($"Insert {nameof(dateTime)}:{dateTime}({dateTime.Kind})");
        OutputHelper.WriteLine($"Insert {nameof(dateTimeUtc)}:{dateTimeUtc}({dateTime.Kind})");
        OutputHelper.WriteLine($"Insert {nameof(dateTimeOffset)}:{dateTimeOffset}");

        await DateTimeTable.InsertAsync(Connection,
            new DateTimeTable.IDbParam[]
            {
                new DateTimeTable.DbParamDateTime(dateTime),
                new DateTimeTable.DbParamDateTimeUtc(dateTimeUtc),
                new DateTimeTable.DbParamDateTimeOffset(dateTimeOffset),
            }).ConfigureAwait(false);

        await foreach (var row in DateTimeTable.SelectAsync(Connection, DateTimeTable.DbColumnType.All))
        {
            OutputHelper.WriteLine($"Select DateTime:{row.DateTime}({row.DateTime.Kind})");
            OutputHelper.WriteLine($"Select DateTimeUtc:{row.DateTimeUtc}({row.DateTimeUtc.Kind})");
            OutputHelper.WriteLine($"Select DateTimeOffset:{row.DateTimeOffset}");
            row.DateTime.Should().Be(dateTime);
            row.DateTimeUtc.Should().Be(dateTimeUtc);
            row.DateTimeOffset.Should().Be(dateTimeOffset);
        }
    }
}