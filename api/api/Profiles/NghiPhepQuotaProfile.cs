using AutoMapper;
using api.DTO;
using api.Model;

namespace api.Profiles
{
    public class NghiPhepQuotaProfile : Profile
    {
        public NghiPhepQuotaProfile()
        {
            // NghiPhepQuota -> NghiPhepQuotaDto
            CreateMap<NghiPhepQuota, NghiPhepQuotaDto>()
                .ForMember(dest => dest.TenNhanVien, opt => opt.MapFrom(src => src.NhanVien.TenDayDu))
                .ForMember(dest => dest.SoNgayPhepConLai, opt => opt.MapFrom(src => src.SoNgayPhepConLai))
                .ForMember(dest => dest.DaVuotQuota, opt => opt.MapFrom(src => src.DaVuotQuota));

            // UpsertNghiPhepQuotaDto -> NghiPhepQuota (for create)
            CreateMap<UpsertNghiPhepQuotaDto, NghiPhepQuota>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SoNgayDaSuDung, opt => opt.Ignore()) // Calculated field
                .ForMember(dest => dest.TongSoGioLamThem, opt => opt.Ignore()) // Calculated field
                .ForMember(dest => dest.NgayTao, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.NgayCapNhat, opt => opt.Ignore())
                .ForMember(dest => dest.NhanVien, opt => opt.Ignore());
        }
    }
}
