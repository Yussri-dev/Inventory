using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.ReturnLines.Requests;
using Inventory.Dto.ReturnLines.Results;

namespace Inventory.Services.Mapping
{
    public class ReturnLineProfile : Profile
    {
        public ReturnLineProfile()
        {
            //create
            CreateMap<CreateReturnLineRequest, ReturnLine>()
               .ForMember(dest => dest.Id, opt => opt.Ignore())

               // Navigation
               .ForMember(dest => dest.Product, opt => opt.Ignore())
               .ForMember(dest => dest.Return, opt => opt.Ignore());

            CreateMap<UpdateReturnLineRequest, ReturnLine>()
               .ForMember(dest => dest.Id, opt => opt.Ignore())

               .ForMember(dest => dest.Product, opt => opt.Ignore())
               .ForMember(dest => dest.Return, opt => opt.Ignore());



            // =========================
            // RESULT
            // =========================
            CreateMap<ReturnLine, ReturnLineResult>();
        }
    }
}
