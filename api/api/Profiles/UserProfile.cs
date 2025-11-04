using api.DTO;
using api.Model;
using AutoMapper;

namespace api.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // Map NhanVien -> UserDto
            CreateMap<NhanVien, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email ?? string.Empty))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.PhongBanId, opt => opt.MapFrom(src => src.PhongBanId))
                .ForMember(dest => dest.ChucVuId, opt => opt.MapFrom(src => src.ChucVuId))
                .ForMember(dest => dest.QuanLyId, opt => opt.MapFrom(src => src.QuanLyId))
                .ForMember(dest => dest.TenQuanLy, opt => opt.MapFrom(src => src.QuanLy != null ? src.QuanLy.TenDayDu : null));

            // Map RegisterUserDto -> NhanVien (chỉ dùng cho các field của NhanVien)
            CreateMap<RegisterUserDto, NhanVien>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Id sẽ được set từ User.Id
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.PhongBan, opt => opt.Ignore())
                .ForMember(dest => dest.ChucVu, opt => opt.Ignore())
                .ForMember(dest => dest.QuanLy, opt => opt.Ignore())
                .ForMember(dest => dest.DonYeuCaus, opt => opt.Ignore());

            // Map UpdateUserDto -> NhanVien
            CreateMap<UpdateUserDto, NhanVien>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.PhongBan, opt => opt.Ignore())
                .ForMember(dest => dest.ChucVu, opt => opt.Ignore())
                .ForMember(dest => dest.QuanLy, opt => opt.Ignore())
                .ForMember(dest => dest.DonYeuCaus, opt => opt.Ignore());
        }
    }
}
