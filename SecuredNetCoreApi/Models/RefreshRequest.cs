using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecuredNetCoreApi.Models
{
    public class RefreshRequest
    {
        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }
    }
}
