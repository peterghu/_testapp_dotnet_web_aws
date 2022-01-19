using _testapp_dotnet_web_aws.Data;
using _testapp_dotnet_web_aws.v1.Models;
using AutoMapper;

namespace _testapp_dotnet_web_aws.v1
{
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// Create a map configuration below.
        ///
        /// To use the profile:
        /// var dto = _mapper.Map<![CDATA[<TDestination>]]>(TSourceObject)
        /// </summary>
        public AutoMapperProfile()
        {
            //TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            CreateMap<RequestLogs, RequestLogModel>();
            //CreateMap<entity, model>()
            //    .ForMember(dest => dest.x, opt => opt.MapFrom(src => src.y));
        }
    }
}