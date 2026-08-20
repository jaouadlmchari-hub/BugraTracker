using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Models.Auth
{
    public record AccessTokenResult(string Token ,DateTime ExpiresAt);
}
