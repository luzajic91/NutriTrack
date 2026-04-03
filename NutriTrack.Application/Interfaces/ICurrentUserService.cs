using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Interfaces
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        string Role { get; }
    }
}
