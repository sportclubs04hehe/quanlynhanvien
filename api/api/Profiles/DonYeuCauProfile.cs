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
                .ForMember(dest => dest.LoaiNghiPhepText, opt => opt.MapFrom(src => 
                    src.LoaiNghiPhep.HasValue ? src.LoaiNghiPhep.Value.ToDisplayName() : null))
                .ForMember(dest => dest.TenNhanVien, opt => opt.MapFrom(src => src.NhanVien.TenDayDu))
                .ForMember(dest => dest.EmailNhanVien, opt => opt.MapFrom(src => src.NhanVien.User.Email))
                .ForMember(dest => dest.PhongBan, opt => opt.MapFrom(src => src.NhanVien.PhongBan))
                .ForMember(dest => dest.ChucVu, opt => opt.MapFrom(src => src.NhanVien.ChucVu))
                .ForMember(dest => dest.TenNguoiDuyet, opt => opt.MapFrom(src => src.NguoiDuyet != null ? src.NguoiDuyet.TenDayDu : null))
                .ForMember(dest => dest.SoNgay, opt => opt.MapFrom(src => CalculateSoNgay(src)))
                .ForMember(dest => dest.SoNgayThucTe, opt => opt.MapFrom(src => CalculateSoNgayThucTe(src)));
        }

        /// <summary>
        /// Tính số ngày nghỉ hiển thị (int - để hiển thị gọn)
        /// </summary>
        private static int? CalculateSoNgay(DonYeuCau don)
        {
            if (!don.NgayBatDau.HasValue || !don.NgayKetThuc.HasValue)
                return null;

            // Nếu không phải đơn nghỉ phép, tính số ngày bình thường
            if (don.LoaiDon != LoaiDonYeuCau.NghiPhep)
                return (int)(don.NgayKetThuc.Value.Date - don.NgayBatDau.Value.Date).TotalDays + 1;

            // Nếu là đơn nghỉ phép, tính dựa trên LoaiNghiPhep
            if (!don.LoaiNghiPhep.HasValue)
                return (int)(don.NgayKetThuc.Value.Date - don.NgayBatDau.Value.Date).TotalDays + 1;

            return don.LoaiNghiPhep.Value switch
            {
                LoaiNghiPhep.BuoiSang => 0, // Hiển thị 0, nhưng SoNgayThucTe = 0.5
                LoaiNghiPhep.BuoiChieu => 0, // Hiển thị 0, nhưng SoNgayThucTe = 0.5
                LoaiNghiPhep.MotNgay => 1,
                LoaiNghiPhep.NhieuNgay => (int)(don.NgayKetThuc.Value.Date - don.NgayBatDau.Value.Date).TotalDays + 1,
                _ => null
            };
        }

        /// <summary>
        /// Tính số ngày nghỉ thực tế (decimal - chính xác cho tính lương)
        /// </summary>
        private static decimal? CalculateSoNgayThucTe(DonYeuCau don)
        {
            if (!don.NgayBatDau.HasValue || !don.NgayKetThuc.HasValue)
                return null;

            // Nếu không phải đơn nghỉ phép
            if (don.LoaiDon != LoaiDonYeuCau.NghiPhep)
                return null;

            // Nếu chưa có LoaiNghiPhep
            if (!don.LoaiNghiPhep.HasValue)
                return (decimal)(don.NgayKetThuc.Value.Date - don.NgayBatDau.Value.Date).TotalDays + 1;

            return don.LoaiNghiPhep.Value switch
            {
                LoaiNghiPhep.BuoiSang => 0.5m, // 0.5 ngày
                LoaiNghiPhep.BuoiChieu => 0.5m, // 0.5 ngày
                LoaiNghiPhep.MotNgay => 1m,
                LoaiNghiPhep.NhieuNgay => (decimal)(don.NgayKetThuc.Value.Date - don.NgayBatDau.Value.Date).TotalDays + 1,
                _ => null
            };
        }
    }
}

