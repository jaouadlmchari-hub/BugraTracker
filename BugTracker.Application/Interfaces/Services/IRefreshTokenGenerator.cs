using BugTracker.Application.Models.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Services
{
    public interface IRefreshTokenGenerator
    {
        RefreshTokenResult Generate();
    }
}
