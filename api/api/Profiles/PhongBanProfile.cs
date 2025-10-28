using api.DTO;
using api.Model;
using AutoMapper;

namespace api.Profiles
{
    public class PhongBanProfile : Profile
    {
        public PhongBanProfile()
        {
            CreateMap<PhongBan, PhongBanDto>()
                .ForMember(dest => dest.SoLuongNhanVien, 
                    opt => opt.MapFrom(src => src.NhanViens != null ? src.NhanViens.Count : 0));
            
            CreateMap<CreatePhongBanDto, PhongBan>();
            
            CreateMap<UpdatePhongBanDto, PhongBan>();
        }
    }
}
