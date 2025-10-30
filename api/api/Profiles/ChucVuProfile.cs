using api.DTO;
using api.Model;
using AutoMapper;

namespace api.Profiles
{
    public class ChucVuProfile : Profile
    {
        public ChucVuProfile()
        {
            CreateMap<ChucVu, ChucVuDto>()
                .ForMember(dest => dest.SoLuongNhanVien, 
                    opt => opt.MapFrom(src => src.NhanViens != null ? src.NhanViens.Count : 0));
            
            CreateMap<CreateChucVuDto, ChucVu>();
            
            CreateMap<UpdateChucVuDto, ChucVu>();
        }
    }
}
