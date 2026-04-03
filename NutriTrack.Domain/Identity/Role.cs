using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.Identity;

public class Role
{
    public int RoleId { get; set; }
    public string Name { get; set; } = default!;
}