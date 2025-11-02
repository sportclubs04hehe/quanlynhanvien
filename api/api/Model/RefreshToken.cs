using System.ComponentModel.DataAnnotations;

namespace api.Model
{
    /// <summary>
    /// Bảng lưu trữ Refresh Tokens cho mỗi user
    /// Dùng để làm mới Access Token khi hết hạn
    /// </summary>
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Token string (unique, hashed)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// User ID (FK to AspNetUsers)
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Thời gian tạo token
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Thời gian token hết hạn
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Token đã bị thu hồi (revoked) hay chưa
        /// </summary>
        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// Thời gian thu hồi token
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// IP Address của client khi tạo token
        /// </summary>
        [StringLength(50)]
        public string? CreatedByIp { get; set; }

        /// <summary>
        /// IP Address của client khi revoke token
        /// </summary>
        [StringLength(50)]
        public string? RevokedByIp { get; set; }

        /// <summary>
        /// Token mới thay thế token này (khi refresh)
        /// </summary>
        [StringLength(500)]
        public string? ReplacedByToken { get; set; }

        // Navigation property
        public User User { get; set; } = null!;

        // Helpers
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
