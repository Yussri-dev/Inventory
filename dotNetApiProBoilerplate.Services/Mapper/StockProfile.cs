using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Stock.Requests;
using Inventory.Dto.Stock.Results;

namespace Inventory.Services.Mapping
{
    public class StockProfile : Profile
    {
        public StockProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateStockRequest, Stock>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdated, opt => opt.Ignore())

                // Computed / navigation
                .ForMember(dest => dest.AvailableQuantity, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateStockRequest, Stock>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdated, opt => opt.Ignore())

                // Computed / navigation
                .ForMember(dest => dest.AvailableQuantity, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())

                // Value types — always mapped
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.ReservedQuantity, opt => opt.MapFrom(src => src.ReservedQuantity));

            // =========================
            // RESULT
            // =========================
            CreateMap<Stock, StockResult>();
        }
    }
}
