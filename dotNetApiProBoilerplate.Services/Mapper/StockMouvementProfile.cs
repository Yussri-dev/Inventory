using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.StockMouvements.Requests;
using Inventory.Dto.StockMouvements.Results;

namespace Inventory.Services.Mapping
{
    public class StockMouvementProfile : Profile
    {
        public StockMouvementProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateStockMouvementRequest, StockMovement>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MovementDate, opt => opt.Ignore())

                // Quantities calculated in service
                .ForMember(dest => dest.QuantityBefore, opt => opt.Ignore())
                .ForMember(dest => dest.QuantityAfter, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateStockMouvementRequest, StockMovement>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MovementDate, opt => opt.Ignore())

                // Quantities immutable once created
                .ForMember(dest => dest.QuantityChange, opt => opt.Ignore())
                .ForMember(dest => dest.QuantityBefore, opt => opt.Ignore())
                .ForMember(dest => dest.QuantityAfter, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Product, opt => opt.Ignore())

                // Controlled updates
                .ForMember(dest => dest.ReferenceId,
                    opt => opt.Condition(src => src.ReferenceId.HasValue))
                .ForMember(dest => dest.ReferenceNumber,
                    opt => opt.Condition(src => src.ReferenceNumber != null))
                .ForMember(dest => dest.Notes,
                    opt => opt.Condition(src => src.Notes != null));

            // =========================
            // RESULT
            // =========================
            CreateMap<StockMovement, StockMouvementResult>();
        }
    }
}
