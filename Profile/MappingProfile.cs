﻿namespace ActiverWebAPI.Profile;

using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using AutoMapper;
using ActiverWebAPI.Interfaces.Service;
using Microsoft.Extensions.Configuration;
using ActiverWebAPI.Enums;
using ActiverWebAPI.Interfaces.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Services.ActivityServices;
using ActiverWebAPI.Services.TagServices;

public class MappingProfile : Profile
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TagService _tagService;
    private readonly IRepository<Location, int> _locationRepository;

    public MappingProfile()
    {
        CreateMap<UserInfoDTO, UserDTO>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));
        CreateMap<TokenDTO, UserDTO>()
            .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src));
        CreateMap<Activity, ActivityDTO>()
            .ForMember(dest => dest.Trend, opt => opt.MapFrom(src => src.ActivityClickedCount))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images == null ? null : src.Images.Select(x => x.ImageURL)))
            .ForMember(dest => dest.Fee, opt => opt.MapFrom(src => src.Fee == null ? null : src.Fee.Select(x => x.Fee)))
            .ForMember(dest => dest.Sources, opt => opt.MapFrom(src => src.Sources == null ? null : src.Sources.Select(x => x.SourceURL)))
            .ForMember(dest => dest.Connections, opt => opt.MapFrom(src => src.Connections == null ? null : src.Connections.Select(x => x.Content)))
            .ForMember(dest => dest.Holders, opt => opt.MapFrom(src => src.Holders == null ? null : src.Holders.Select(x => x.HolderName)))
            .ForMember(dest => dest.Objectives, opt => opt.MapFrom(src => src.Objectives == null ? null : src.Objectives.Select(x => x.ObjectiveName)))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags == null ? null : src.Tags.Select(x => new TagDTO {
                Id = x.Id,
                Text = x.Text,
                Type = x.Type,
                Trend = x.TagClickCount,
                UserVoted = false,
                ActivityAmount = x.Activities == null ? 0 : x.Activities.Count
            })))
            //.ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.UserVoteTagInActivity == null ? null : src.UserVoteTagInActivity.Select(x => 
            //            new TagDTO { 
            //                Text = x.Tag.Text, 
            //                Type = x.Tag.Type, 
            //                Trend = x.Tag.TagClickCount,
            //                UserVoted = false, 
            //                ActivityAmount = x.Tag.Activities == null ? 0 : x.Tag.Activities.Count 
            //            }
            //        ).Distinct()
            //    )
            //)
            .ForMember(dest => dest.Branches, opt => opt.MapFrom(src => src.Branches));
        CreateMap<Branch, BranchDTO>()
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location == null ? null : src.Location.Select(x => x.Content)));
        CreateMap<BranchDate, BranchDateDTO>();

        CreateMap<SegmentsRequestBaseDTO, SegmentsResponseBaseDTO<ActivityDTO>>()
            .ForMember(dest => dest.TotalPage, opt => opt.Ignore())
            .ForMember(dest => dest.TotalData, opt => opt.Ignore())
            .ForMember(dest => dest.SearchData, opt => opt.Ignore());

        CreateMap<SegmentsRequestDTO, SegmentsResponseDTO<ActivityDTO>>()
            .ForMember(dest => dest.TotalPage, opt => opt.Ignore())
            .ForMember(dest => dest.TotalData, opt => opt.Ignore())
            .ForMember(dest => dest.SearchData, opt => opt.Ignore());

        CreateMap<ActivitySegmentDTO, SegmentsResponseDTO<ActivityDTO>>()
            .ForMember(dest => dest.TotalPage, opt => opt.Ignore())
            .ForMember(dest => dest.TotalData, opt => opt.Ignore())
            .ForMember(dest => dest.SearchData, opt => opt.Ignore());

        CreateMap<TagPostDTO, Tag>();
    }

    public MappingProfile(
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        IUnitOfWork unitOfWork,
        TagService tagService
        ) : this()
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _tagService = tagService;
        _locationRepository = unitOfWork.Repository<Location, int>();

        CreateMap<UserSignUpDTO, User>()
            .ForMember(dest => dest.HashedPassword, opt => opt.MapFrom(src => passwordHasher.HashPassword(src.Password)));
        CreateMap<User, UserInfoDTO>()
            .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar == null ? null : _configuration["Server:Domain"] + $"api/user/avatar/{src.Id}"))
            .ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.Area == null ? null : src.Area.Content))
            .ForMember(dest => dest.Professions, opt => opt.MapFrom(src => src.Professions == null ? null : src.Professions.Select(x => new UserProfessionDTO { Id = x.Id, Profession = x.Content }).ToList()))
            .ForMember(dest => dest.County, opt => opt.MapFrom(src => src.County == null ? null : src.County.Content))
            .ForMember(dest => dest.EmailVerified, opt => opt.MapFrom(src => src.Verified))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => ((UserGender)src.Gender).ToString()))
            .ForMember(dest => dest.Birthday, opt => opt.MapFrom(src => src.Birthday == null ? null : src.Birthday.Value.ToString("yyyy-mm-dd")));
        CreateMap<BranchDateDTO, BranchDate>();
        CreateMap<BranchPostDTO, Branch>()
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location == null ? null : MapLocationsAsync(src.Location).Result));
        CreateMap<ActivityPostDTO, Activity>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images == null ? null : src.Images.Select(x => new Image { ImageURL = x })))
            .ForMember(dest => dest.Sources, opt => opt.MapFrom(src => src.Sources == null ? null : src.Sources.Select(x => new Source { SourceURL = x })))
            .ForMember(dest => dest.Connections, opt => opt.MapFrom(src => src.Connections == null ? null : MapConnectionsAsync(src.Connections).Result))
            .ForMember(dest => dest.Holders, opt => opt.MapFrom(src => src.Holders == null ? null : MapHoldersAsync(src.Holders).Result))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags == null ? null : MapTagsAsync(src.Tags).Result))
            .ForMember(dest => dest.Branches, opt => opt.MapFrom(src => src.Branches))
            .ForMember(dest => dest.Objectives, opt => opt.MapFrom(src => src.Objectives == null ? null : MapObjectivesAsync(src.Objectives).Result));
    }

    private async Task<List<Connection>> MapConnectionsAsync(IEnumerable<string> connections)
    {
        var result = new List<Connection>();

        foreach (var x in connections)
        {
            var connection = await _unitOfWork.Repository<Connection, int>()
                .GetAll(c => c.Content.Equals(x))
                .FirstOrDefaultAsync();

            if (connection == null)
                result.Add(new Connection { Content = x });
            else
                result.Add(connection);
        }

        return result;
    }

    private async Task<List<Holder>> MapHoldersAsync(IEnumerable<string> holders)
    {
        var result = new List<Holder>();

        foreach (var x in holders)
        {
            var holder = await _unitOfWork.Repository<Holder, int>()
                .GetAll(c => c.HolderName.Equals(x))
                .FirstOrDefaultAsync();

            if (holder == null)
                result.Add(new Holder { HolderName = x });
            else
                result.Add(holder);
        }

        return result;
    }

    private async Task<List<Objective>> MapObjectivesAsync(IEnumerable<string> objectives)
    {
        var result = new List<Objective>();

        foreach (var x in objectives)
        {
            var objective = await _unitOfWork.Repository<Objective, int>()
                .GetAll(c => c.ObjectiveName.Equals(x))
                .FirstOrDefaultAsync();

            if (objective == null)
                result.Add(new Objective { ObjectiveName = x });
            else
                result.Add(objective);
        }

        return result;
    }

    private async Task<List<Tag>> MapTagsAsync(IEnumerable<TagPostDTO> tags)
    {
        var result = new List<Tag>();
        foreach (var tag in tags)
        {
            //var existingTag = _tagService.GetLocal().FirstOrDefault(t => t.Text == tag.Text && t.Type == tag.Type);
            //existingTag ??= _tagService.GetAll(t => t.Text == tag.Text && t.Type == tag.Type).FirstOrDefault();
            //if (existingTag == null)
            //{
            //    var newTag = new Tag
            //    {
            //        Text = tag.Text,
            //        Type = tag.Type
            //    };
            //    await _tagService.AddAsync(newTag);
            //    existingTag = newTag;
            //}
            //result.Add(existingTag);
            result.Add(new Tag { Text = tag.Text, Type = tag.Type });
        }

        //for(int i = 1; i < result.Count; i++)
        //{
        //    var tag = result[i];
        //    var tagFind = _tagService.GetLocal().FirstOrDefault(x => x.Type == tag.Type && x.Text == tag.Text);
        //    if (tagFind != null)
        //        result[i] = tagFind;
        //}

        return result;
    }

    private async Task<List<Location>> MapLocationsAsync(IEnumerable<string> locations)
    {
        var result = new List<Location>();

        foreach (var location in locations)
        {
            result.Add(new Location
            {
                Content = location
            });
        }

        return result;
    }
}

