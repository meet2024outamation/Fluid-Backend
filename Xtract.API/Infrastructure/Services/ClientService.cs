using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;
using SharedKernel.Services;
using Xtract.API.Infrastructure.Interfaces;
using Xtract.API.Models.Client;
using Xtract.Entities.Context;

namespace Xtract.API.Infrastructure.Services;

public class ClientService : ServiceBase, IClientService
{
    private readonly XtractDbContext _context;
    private readonly ILogger<ClientService> _logger;

    public ClientService(XtractDbContext context, ILogger<ClientService> logger, IUser user) : base(context, user)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ClientResponse>> CreateAsync(CreateClientRequest request, int currentUserId)
    {
        try
        {
            // Check if client code already exists
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.Code == request.Code);

            if (existingClient != null)
            {
                _logger.LogWarning("Attempted to create client with existing code: {Code}", request.Code);

                var validationError = new ValidationError
                {
                    Key = nameof(request.Code),
                    ErrorMessage = $"Client with code '{request.Code}' already exists."
                };

                return Result<ClientResponse>.Invalid(new List<ValidationError> { validationError });
            }

            var client = new Xtract.Entities.Entities.Client
            {
                Name = request.Name,
                Code = request.Code,
                Status = request.Status,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            // Load the created client with user information
            var createdClient = await _context.Clients
                .Include(c => c.CreatedByUser)
                .FirstAsync(c => c.Id == client.Id);

            var response = new ClientResponse(
                createdClient.Id,
                createdClient.Name,
                createdClient.Code,
                createdClient.Status,
                createdClient.CreatedAt,
                createdClient.UpdatedAt,
                createdClient.CreatedBy,
                createdClient.CreatedByUser.Name
            );

            _logger.LogInformation("Client created successfully with ID: {ClientId}", createdClient.Id);
            return Result<ClientResponse>.Created(response, "Client created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client with code: {Code}", request.Code);
            return Result<ClientResponse>.Error("An error occurred while creating the client.");
        }
    }

    public async Task<Result<List<ClientListResponse>>> GetAllAsync()
    {
        try
        {
            var clients = await _context.Clients
                .Include(c => c.CreatedByUser)
                .Select(c => new ClientListResponse(
                    c.Id,
                    c.Name,
                    c.Code,
                    c.Status,
                    c.CreatedAt,
                    c.CreatedByUser.Name
                ))
                .OrderBy(c => c.Name)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} clients", clients.Count);
            return Result<List<ClientListResponse>>.Success(clients, $"Retrieved {clients.Count} clients successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clients");
            return Result<List<ClientListResponse>>.Error("An error occurred while retrieving clients.");
        }
    }

    public async Task<Result<ClientResponse>> GetByIdAsync(int id)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                _logger.LogWarning("Client with ID {ClientId} not found", id);
                return Result<ClientResponse>.NotFound();
            }

            var response = new ClientResponse(
                client.Id,
                client.Name,
                client.Code,
                client.Status,
                client.CreatedAt,
                client.UpdatedAt,
                client.CreatedBy,
                client.CreatedByUser.Name
            );

            _logger.LogInformation("Retrieved client with ID: {ClientId}", id);
            return Result<ClientResponse>.Success(response, "Client retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client with ID: {ClientId}", id);
            return Result<ClientResponse>.Error("An error occurred while retrieving the client.");
        }
    }

    public async Task<Result<ClientResponse>> UpdateAsync(int id, UpdateClientRequest request)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                _logger.LogWarning("Client with ID {ClientId} not found for update", id);
                return Result<ClientResponse>.NotFound();
            }

            // Check if the new code conflicts with another client
            if (request.Code != client.Code)
            {
                var existingClient = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Code == request.Code && c.Id != id);

                if (existingClient != null)
                {
                    _logger.LogWarning("Attempted to update client {ClientId} with existing code: {Code}", id, request.Code);

                    var validationError = new ValidationError
                    {
                        Key = nameof(request.Code),
                        ErrorMessage = $"Client with code '{request.Code}' already exists."
                    };

                    return Result<ClientResponse>.Invalid(new List<ValidationError> { validationError });
                }
            }

            // Update the client properties
            client.Name = request.Name;
            client.Code = request.Code;
            client.Status = request.Status;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new ClientResponse(
                client.Id,
                client.Name,
                client.Code,
                client.Status,
                client.CreatedAt,
                client.UpdatedAt,
                client.CreatedBy,
                client.CreatedByUser.Name
            );

            _logger.LogInformation("Client updated successfully with ID: {ClientId}", id);
            return Result<ClientResponse>.Success(response, "Client updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client with ID: {ClientId}", id);
            return Result<ClientResponse>.Error("An error occurred while updating the client.");
        }
    }
}