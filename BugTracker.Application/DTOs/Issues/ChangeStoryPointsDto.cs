using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.DTOs.Issues
{
    public class ChangeStoryPointsDto
    {
        [Range(0, 100, ErrorMessage = "Les Story Points doivent être compris entre 0 et 100.")]
        public int? StoryPoints { get; set; }
    }
}
