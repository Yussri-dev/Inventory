using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashMovements.Requests;
using Inventory.Dto.CashMovements.Results;

namespace Inventory.Services.Mapper
{
    public class CashMovementProfile : Profile
    {
        public CashMovementProfile()
        {
            CreateMap<CreateCashMovementRequest, CashMovement>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MovementDate, opt => opt.Ignore())
                .ForMember(dest => dest.CashSession, opt => opt.Ignore());

            CreateMap<UpdateCashMovementRequest, CashMovement>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MovementDate, opt => opt.Ignore())
                .ForMember(dest => dest.CashSession, opt => opt.Ignore())
                .ForMember(dest => dest.Reason,
                    opt => opt.Condition(src => src.Reason != null))
                .ForMember(dest => dest.ReferenceType,
                    opt => opt.Condition(src => src.ReferenceType != null))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.BalanceBefore, opt => opt.MapFrom(src => src.BalanceBefore))
                .ForMember(dest => dest.BalanceAfter, opt => opt.MapFrom(src => src.BalanceAfter))
                .ForMember(dest => dest.ReferenceId, opt => opt.MapFrom(src => src.ReferenceId))
                .ForMember(dest => dest.ReferenceType, opt => opt.MapFrom(src => src.ReferenceType));

            CreateMap<CashMovement, CashMovementResult>();
        }
    }
}