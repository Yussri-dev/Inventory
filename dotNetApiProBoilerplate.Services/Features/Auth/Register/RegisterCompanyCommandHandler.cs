using Inventory.Dto.Auth.Results;
using Inventory.Services.Abstractions;
using Inventory.Services.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Inventory.Services.Features.Auth.Register
{
    public sealed class RegisterCompanyCommandHandler
            : IRequestHandler<RegisterCompanyCommand, AuthResult>
    {
        private readonly AuthService _authService;
        private readonly IProductProvisioningService _productProvisioningService;
        private readonly ILogger<RegisterCompanyCommandHandler> _logger;

        public RegisterCompanyCommandHandler(
            AuthService authService,
            IProductProvisioningService productProvisioningService,
            ILogger<RegisterCompanyCommandHandler> logger)
        {
            _authService = authService;
            _productProvisioningService = productProvisioningService;
            _logger = logger;
        }

        public async Task<AuthResult> Handle(
            RegisterCompanyCommand command,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starting company registration for {Email}",
                command.Request.Email);

            var authResult = await _authService.RegisterCompanyAsync(
                command.Request,
                cancellationToken);

            if (!authResult.TenantId.HasValue ||
                authResult.TenantId.Value == Guid.Empty)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            "TenantId",
                            new[]
                            {
                                "The registered company has no valid tenant."
                            }
                        }
                    });
            }

            _logger.LogInformation(
                "Company registered. TenantId: {TenantId}, UserId: {UserId}",
                authResult.TenantId.Value,
                authResult.UserId);

            var importedCount =
                await _productProvisioningService
                    .ProvisionCatalogProductsAsync(
                        authResult.TenantId.Value,
                        authResult.UserId,
                        cancellationToken);

            _logger.LogInformation(
                "{ImportedCount} products imported for tenant {TenantId}",
                importedCount,
                authResult.TenantId.Value);

            return authResult;
        }
    }
}
