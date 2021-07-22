using System;
using System.Collections.Generic;

#nullable disable

namespace SecuredNetCoreApi.Models
{
    public partial class RefreshToken
    {
        public int TokenId { get; set; }
        public string UserId { get; set; }
        public string Token { get; set; }
        public DateTime? ExpireDate { get; set; }
    }
}
