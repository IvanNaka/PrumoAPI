using System;
using System.Collections.Generic;

namespace Prumo.Application.DTOs.Roadmap
{
    public class ReorderRoadmapDto
    {
        public List<Guid> OrderedIds { get; set; } = new List<Guid>();
    }
}
