namespace ActiverWebAPI.Profile;

using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using AutoMapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserInfoDTO>();
        CreateMap<UserInfoDTO, UserDTO>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));
        CreateMap<TokenDTO, UserDTO>()
            .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src));
    }
}
