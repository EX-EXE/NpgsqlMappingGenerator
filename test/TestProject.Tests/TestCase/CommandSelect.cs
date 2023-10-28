using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using FluentAssertions;
using TestProject.TestCase;

namespace TestProject.Tests.TestCase;

public class CommandSelect : PrepareDataBase, IAsyncLifetime
{
    List<UserData> UserList = new();
    List<AuthorityType> AuthorityTypeList = new();
    List<UserAuthority> UserAuthorityList = new();

    public CommandSelect(ITestOutputHelper outputHelper)
        : base(outputHelper, nameof(ViewSelect))
    {
    }

    public async Task InitializeAsync()
    {
        await UserData.CreateTableAsync(Connection).ConfigureAwait(false);
        await AuthorityType.CreateTableAsync(Connection).ConfigureAwait(false);
        await UserAuthority.CreateTableAsync(Connection).ConfigureAwait(false);

        var rand = new Random();

        foreach (var num in Enumerable.Range(0, rand.Next(10, 100)))
        {
            var userData = new UserData()
            {
                Id = Guid.NewGuid(),
                FirstName = $"First{num}",
                LastName = $"Last{num}",
            };
            UserList.Add(userData);

            await UserData.InsertAsync(Connection,
                new UserData.IDbParam[]
                {
                    new UserData.DbParamId(userData.Id),
                    new UserData.DbParamFirstName(userData.FirstName),
                    new UserData.DbParamLastName(userData.LastName),
                }).ConfigureAwait(false);
        }
        foreach (var num in Enumerable.Range(0, rand.Next(5, 20)))
        {
            var type = new AuthorityType()
            {
                Id = Guid.NewGuid(),
                Name = $"Name{num}",
            };
            AuthorityTypeList.Add(type);

            await AuthorityType.InsertAsync(Connection,
                new AuthorityType.IDbParam[]
                {
                    new AuthorityType.DbParamId(type.Id),
                    new AuthorityType.DbParamName(type.Name),
                }).ConfigureAwait(false);
        }
        foreach (var user in UserList)
        {
            foreach (var type in AuthorityTypeList.Take(rand.Next(0, AuthorityTypeList.Count - 1)))
            {
                var userType = new UserAuthority()
                {
                    UserDataId = user.Id,
                    AuthorityTypeId = type.Id,
                    LastUpdate = DateTime.Now,
                };
                UserAuthorityList.Add(userType);

                await UserAuthority.InsertAsync(Connection,
                    new UserAuthority.IDbParam[]
                    {
                    new UserAuthority.DbParamUserDataId(user.Id),
                    new UserAuthority.DbParamAuthorityTypeId(type.Id),
                    }).ConfigureAwait(false);
            }
        }
    }


    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Select()
    {
        await foreach (var row in ViewUserAuthority.ExecuteAsync(Connection, ViewUserAuthority.DbQueryType.All))
        {
            var userData = UserList.First(x => x.Id == row.UserId);
            var typeData = AuthorityTypeList.First(x => x.Id == row.AuthorityId);
            userData.FirstName.Should().Be(row.FirstName);
            userData.LastName.Should().Be(row.LastName);
            typeData.Name.Should().Be(row.AuthorityName);
        }
    }
}