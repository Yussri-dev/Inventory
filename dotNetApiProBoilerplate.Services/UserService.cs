using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.Auth.Results;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Users;
using Inventory.Dto.Users.Requests;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class UserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly InventoryDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(
            UserManager<ApplicationUser> userManager,
            InventoryDbContext context,
            IMapper mapper,
            ITenantContext tenantContext,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _context = context;
            _mapper = mapper;
            _tenantContext = tenantContext;
            _unitOfWork = unitOfWork;
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<UserResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    u.TenantId == tenantId &&
                    !u.IsDeleted);

            if (user == null)
                throw new NotFoundException("User", id);

            return _mapper.Map<UserResult>(user);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<UserResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var users = await _context.Users
                .Where(u => u.TenantId == tenantId && !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<UserResult>>(users);
        }

        // =========================
        // QUERY (paginé)
        // =========================
        public async Task<PagedResult<UserResult>> QueryAsync(UserQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Page", new[] { "Page must be >= 1." } }
                });

            if (query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                });

            var q = _context.Users
                .Where(u => u.TenantId == tenantId && !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                q = q.Where(u =>
                    EF.Functions.ILike(u.FullName ?? "", $"%{search}%") ||
                    EF.Functions.ILike(u.Email ?? "", $"%{search}%"));
            }

            if (query.Role.HasValue)
                q = q.Where(u => u.Role == query.Role.Value);

            if (query.IsActive.HasValue)
                q = q.Where(u => u.IsActive == query.IsActive.Value);

            q = query.SortBy?.ToLower() switch
            {
                "email" => query.Desc ? q.OrderByDescending(u => u.Email) : q.OrderBy(u => u.Email),
                "fullname" => query.Desc ? q.OrderByDescending(u => u.FullName) : q.OrderBy(u => u.FullName),
                "role" => query.Desc ? q.OrderByDescending(u => u.Role) : q.OrderBy(u => u.Role),
                _ => query.Desc ? q.OrderByDescending(u => u.CreatedAt) : q.OrderBy(u => u.CreatedAt)
            };

            var total = await q.CountAsync();

            var items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<UserResult>
            {
                Items = _mapper.Map<List<UserResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<UserResult> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    u.TenantId == tenantId &&
                    !u.IsDeleted);

            if (user == null)
                throw new NotFoundException("User", id);

            // Admin et SuperAdmin ne peuvent pas être modifiés par un autre Admin
            if (!_tenantContext.IsSuperAdmin &&
                (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin))
                throw new ForbiddenException("Cannot modify an Admin user.");

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (request.Role.HasValue)
            {
                // Un Admin ne peut pas promouvoir en Admin/SuperAdmin
                if (!_tenantContext.IsSuperAdmin &&
                    (request.Role == UserRole.Admin || request.Role == UserRole.SuperAdmin))
                    throw new ForbiddenException("Cannot assign Admin or SuperAdmin role.");

                user.Role = request.Role.Value;
            }

            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedByUserId = _tenantContext.UserId;

            await _userManager.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UserResult>(user);
        }

        // =========================
        // DEACTIVATE
        // =========================
        // UserService.cs
        public async Task<UserResult> DeactivateAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    u.TenantId == tenantId &&
                    !u.IsDeleted);

            if (user == null)
                throw new NotFoundException("User", id);

            if (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin)
                throw new ForbiddenException("Cannot deactivate an Admin.");

            user.IsActive = false;
            user.DeactivatedAt = DateTime.UtcNow;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedByUserId = _tenantContext.UserId;

            await _userManager.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UserResult>(user);
        }

        // =========================
        // ACTIVATE
        // =========================
        public async Task ActivateAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    u.TenantId == tenantId &&
                    !u.IsDeleted);

            if (user == null)
                throw new NotFoundException("User", id);

            user.IsActive = true;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedByUserId = _tenantContext.UserId;

            await _userManager.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        // =========================
        // DELETE (soft)
        // =========================
        public async Task DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    u.TenantId == tenantId &&
                    !u.IsDeleted);

            if (user == null)
                throw new NotFoundException("User", id);

            if (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin)
                throw new ForbiddenException("Cannot delete an Admin.");

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedByUserId = _tenantContext.UserId;
            user.ModifiedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}