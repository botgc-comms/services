using System;
using System.Collections.Generic;

namespace BOTGC.POS.Models
{
    public sealed record Operator(Guid Id, string DisplayName, string ColorHex);
}
