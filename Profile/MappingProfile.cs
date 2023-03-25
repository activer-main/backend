namespace ActiverWebAPI.Profile;

using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using AutoMapper;
using ActiverWebAPI.Interfaces.Service;
using Microsoft.Extensions.Configuration;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserInfoDTO, UserDTO>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));
        CreateMap<TokenDTO, UserDTO>()
            .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src));
    }

    public MappingProfile(IPasswordHasher passwordHasher, IConfiguration configuration) : this()
    {
        CreateMap<UserSignUpDTO, User>()
            .ForMember(dest => dest.HashedPassword, opt => opt.MapFrom(src => passwordHasher.HashPassword(src.Password)));
        CreateMap<User, UserInfoDTO>()
            .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar == null ? null : configuration["Server:Domain"] + $"api/user/avatar/{src.Avatar.Id}"))
            .ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.Area == null ? null : src.Area.Content))
            .ForMember(dest => dest.Profession, opt => opt.MapFrom(src => src.Professions == null ? null : string.Join("/", src.Professions.Select(p => p.Content))))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender == null ? null : src.Gender.Content));
    }
}
