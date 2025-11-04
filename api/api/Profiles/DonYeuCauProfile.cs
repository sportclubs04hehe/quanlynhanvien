using AutoMapper;
using api.DTO;
using api.Model;
using api.Model.Enums;

namespace api.Profiles
{
    public class DonYeuCauProfile : Profile
    {
        public DonYeuCauProfile()
        {
            // CreateDonYeuCauDto -> DonYeuCau
            CreateMap<CreateDonYeuCauDto, DonYeuCau>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.NhanVienId, opt => opt.Ignore()) // Sẽ set từ current user
                .ForMember(dest => dest.TrangThai, opt => opt.MapFrom(src => TrangThaiDon.DangChoDuyet))
                .ForMember(dest => dest.NgayTao, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.NgayCapNhat, opt => opt.Ignore())
                .ForMember(dest => dest.DuocChapThuanBoi, opt => opt.Ignore())
                .ForMember(dest => dest.GhiChuNguoiDuyet, opt => opt.Ignore())
                .ForMember(dest => dest.NgayDuyet, opt => opt.Ignore())
                .ForMember(dest => dest.NhanVien, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiDuyet, opt => opt.Ignore())
                .ForMember(dest => dest.ThongBaos, opt => opt.Ignore());

            // UpdateDonYeuCauDto -> DonYeuCau (chỉ update các field được phép)
            CreateMap<UpdateDonYeuCauDto, DonYeuCau>()
                .ForMember(dest => dest.NgayCapNhat, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.LoaiDon, opt => opt.Ignore()) // Không cho đổi loại đơn
                .ForMember(dest => dest.NhanVienId, opt => opt.Ignore())
                .ForMember(dest => dest.TrangThai, opt => opt.Ignore())
                .ForMember(dest => dest.NgayTao, opt => opt.Ignore())
                .ForMember(dest => dest.DuocChapThuanBoi, opt => opt.Ignore())
                .ForMember(dest => dest.GhiChuNguoiDuyet, opt => opt.Ignore())
                .ForMember(dest => dest.NgayDuyet, opt => opt.Ignore())
                .ForMember(dest => dest.NhanVien, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiDuyet, opt => opt.Ignore())
                .ForMember(dest => dest.ThongBaos, opt => opt.Ignore());

            // DonYeuCau -> DonYeuCauDto
            CreateMap<DonYeuCau, DonYeuCauDto>()
                .ForMember(dest => dest.LoaiDonText, opt => opt.MapFrom(src => src.LoaiDon.ToDisplayName()))
                .ForMember(dest => dest.TrangThaiText, opt => opt.MapFrom(src => src.TrangThai.ToDisplayName()))
                .ForMember(dest => dest.TenNhanVien, opt => opt.MapFrom(src => src.NhanVien.TenDayDu))
                .ForMember(dest => dest.EmailNhanVien, opt => opt.MapFrom(src => src.NhanVien.User.Email))
                .ForMember(dest => dest.PhongBan, opt => opt.MapFrom(src => src.NhanVien.PhongBan))
                .ForMember(dest => dest.ChucVu, opt => opt.MapFrom(src => src.NhanVien.ChucVu))
                .ForMember(dest => dest.TenNguoiDuyet, opt => opt.MapFrom(src => src.NguoiDuyet != null ? src.NguoiDuyet.TenDayDu : null))
                .ForMember(dest => dest.SoNgay, opt => opt.MapFrom(src => 
                    src.NgayBatDau.HasValue && src.NgayKetThuc.HasValue 
                        ? (int)(src.NgayKetThuc.Value - src.NgayBatDau.Value).TotalDays + 1 
                        : (int?)null));
        }
    }
}

