using Npgsql;
using NpgsqlMappingGenerator;
namespace TestProject;

[DbViewGenerator]
[DbViewTable<UserAuthority>]
[DbViewInnerJoin<UserData, UserAuthority>(nameof(UserData.Id), nameof(UserAuthority.UserDataId))]
[DbViewInnerJoin<AuthorityType, UserAuthority>(nameof(AuthorityType.Id), nameof(UserAuthority.AuthorityTypeId))]
public partial class ViewUser
{
}