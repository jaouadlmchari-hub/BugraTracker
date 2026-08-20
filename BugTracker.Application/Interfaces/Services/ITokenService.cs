using BugTracker.Application.Models.Auth;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Interfaces.Services;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user);
}