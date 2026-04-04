using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Mapper
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<ApplicationUser, UserResult>()
                .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));
        }
    }
}
