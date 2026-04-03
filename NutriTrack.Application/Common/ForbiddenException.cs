using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Common
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
    }
}
