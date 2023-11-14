using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NpgsqlMappingGenerator;

namespace TestProject.TestCase
{
    [DbCommand("""
SELECT public.userdata.id as userid, public.userdata.first_name as first_name, public.userdata.last_name as last_name, public.authority_type.name as authority_name , public.authority_type.id as authorityid 
FROM public.user_authority 
JOIN public.userdata ON public.userdata.id = public.user_authority.user_data_id 
JOIN public.authority_type ON public.authority_type.id = public.user_authority.authority_type_id 
""")]
    public partial class ViewUserAuthority
    {
        [DbColumn<DbParamGuid>("userid")]
        public Guid UserId { get; set; }

        [DbColumn<DbParamGuid>("authorityid")]
        public Guid AuthorityId { get; set; }

        [DbColumn<DbParamString>("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [DbColumn<DbParamString>("last_name")]
        public string LastName { get; set; } = string.Empty;

        [DbColumn<DbParamString>("authority_name")]
        public string AuthorityName { get; set; } = string.Empty;
    }

    [DbCommand("""
SELECT public.userdata.id as userid, public.userdata.first_name as first_name, public.userdata.last_name as last_name, public.authority_type.name as authority_name , public.authority_type.id as authorityid
FROM public.user_authority
JOIN public.userdata ON public.userdata.id = public.user_authority.user_data_id AND public.userdata.id = @param_userid
JOIN public.authority_type ON public.authority_type.id = public.user_authority.authority_type_id
""")]
    [DbCommandParam<DbParamGuid>("@param_userid")]
    public partial class ViewUserAuthorityOn
    {
        [DbColumn<DbParamGuid>("userid")]
        public Guid UserId { get; set; }

        [DbColumn<DbParamGuid>("authorityid")]
        public Guid AuthorityId { get; set; }

        [DbColumn<DbParamString>("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [DbColumn<DbParamString>("last_name")]
        public string LastName { get; set; } = string.Empty;

        [DbColumn<DbParamString>("authority_name")]
        public string AuthorityName { get; set; } = string.Empty;
    }

}
