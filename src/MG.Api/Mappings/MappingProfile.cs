using AutoMapper;
using MG.Models.DTOs.Authentication;
using MG.Models.DTOs.Data;
using MG.Models.Entities;

namespace MG.Api.Mappings;

public class MappingProfile : Profile {
	public MappingProfile() {
		CreateMap<DataItem,DataResponse>();
		CreateMap<User,LoginResponse>().ForMember(dest => dest.Token,opt => opt.Ignore()); // Token will be set separately
	}
}
