using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.SupplierReturns.Requests;
using Inventory.Dto.SupplierReturns.Results;

namespace Inventory.Services.Mapping
{
    public class SupplierReturnProfile : Profile
    {
        public SupplierReturnProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateSupplierReturnRequest, SupplierReturn>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ReturnDate, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedDate, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore());

            CreateMap<CreateCompleteSupplierReturnRequest, SupplierReturn>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateSupplierReturnRequest, SupplierReturn>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ReturnDate, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore())

                // Controlled updates
                .ForMember(dest => dest.Reason,
                    opt => opt.Condition(src => src.Reason != null))
                .ForMember(dest => dest.Notes,
                    opt => opt.Condition(src => src.Notes != null));

            // =========================
            // RESULT
            // =========================
            CreateMap<SupplierReturn, SupplierReturnResult>();
        }
    }
}
