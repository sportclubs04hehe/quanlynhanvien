using api.Model.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api.Controllers
{
    /// <summary>
    /// Base controller chứa các helper methods dùng chung cho tất cả controllers
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        #region User Information

        /// <summary>
        /// Lấy UserId (NhanVienId) của user hiện tại từ JWT token
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Không tìm thấy thông tin user</exception>
        protected Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Không tìm thấy thông tin user");

            return Guid.Parse(userIdClaim);
        }

        /// <summary>
        /// Lấy email của user hiện tại từ JWT token
        /// </summary>
        protected string? GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Lấy username của user hiện tại từ JWT token
        /// </summary>
        protected string? GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value;
        }

        #endregion

        #region Role Checking

        /// <summary>
        /// Kiểm tra user hiện tại có phải Giám Đốc không
        /// </summary>
        protected bool IsGiamDoc()
        {
            return User.IsInRole(AppRolesExtensions.GiamDoc);
        }

        /// <summary>
        /// Kiểm tra user hiện tại có phải Trưởng Phòng không
        /// </summary>
        protected bool IsTruongPhong()
        {
            return User.IsInRole(AppRolesExtensions.TruongPhong);
        }

        /// <summary>
        /// Kiểm tra user hiện tại có phải Nhân Viên không
        /// </summary>
        protected bool IsNhanVien()
        {
            return User.IsInRole(AppRolesExtensions.NhanVien);
        }

        /// <summary>
        /// Kiểm tra user hiện tại có phải Giám Đốc hoặc Trưởng Phòng không
        /// </summary>
        protected bool IsGiamDocOrTruongPhong()
        {
            return User.IsInRole(AppRolesExtensions.GiamDoc) || 
                   User.IsInRole(AppRolesExtensions.TruongPhong);
        }

        /// <summary>
        /// Kiểm tra user có thuộc một trong các roles được chỉ định không
        /// </summary>
        protected bool IsInAnyRole(params string[] roles)
        {
            return roles.Any(role => User.IsInRole(role));
        }

        #endregion

        #region Response Helpers

        /// <summary>
        /// Trả về BadRequest với message
        /// </summary>
        protected ActionResult BadRequest(string message)
        {
            return BadRequest(new { message });
        }

        /// <summary>
        /// Trả về NotFound với message
        /// </summary>
        protected ActionResult NotFound(string message)
        {
            return NotFound(new { message });
        }

        /// <summary>
        /// Trả về Forbidden với message
        /// </summary>
        protected ActionResult Forbidden(string message = "Bạn không có quyền truy cập tài nguyên này")
        {
            return StatusCode(403, new { message });
        }

        /// <summary>
        /// Trả về InternalServerError với message
        /// </summary>
        protected ActionResult InternalServerError(string message, string? error = null)
        {
            return StatusCode(500, new { message, error });
        }

        /// <summary>
        /// Trả về Success response với data
        /// </summary>
        protected ActionResult<T> Success<T>(T data, string? message = null)
        {
            if (message != null)
                return Ok(new { message, data });
            
            return Ok(data);
        }

        #endregion

        #region Validation Helpers

        /// <summary>
        /// Kiểm tra user có phải owner của resource không
        /// </summary>
        protected bool IsOwner(Guid resourceOwnerId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                return currentUserId == resourceOwnerId;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra user có quyền truy cập resource không (owner hoặc Giám Đốc/Trưởng Phòng)
        /// </summary>
        protected bool CanAccess(Guid resourceOwnerId)
        {
            return IsOwner(resourceOwnerId) || IsGiamDocOrTruongPhong();
        }

        /// <summary>
        /// Throw Forbidden exception nếu user không có quyền
        /// </summary>
        protected void RequireOwnerOrManager(Guid resourceOwnerId, string message = "Bạn không có quyền truy cập")
        {
            if (!CanAccess(resourceOwnerId))
                throw new UnauthorizedAccessException(message);
        }

        #endregion
    }
}
