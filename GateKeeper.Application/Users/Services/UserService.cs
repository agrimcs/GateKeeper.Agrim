using GateKeeper.Application.Common.Exceptions;
using GateKeeper.Application.Users.DTOs;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Application.Users.Services;

/// <summary>
/// Application service for user-related operations.
/// Orchestrates domain logic, repositories, and external services.
/// </summary>
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    public async Task<UserProfileDto> RegisterAsync(
        RegisterUserDto dto, 
        CancellationToken cancellationToken = default)
    {
        // Create email value object (validates format)
        var email = Email.Create(dto.Email);

        // Check if user already exists
        if (await _userRepository.ExistsAsync(email, cancellationToken))
        {
            throw new DuplicateEmailException(dto.Email);
        }

        // Hash password using infrastructure service
        var passwordHash = _passwordHasher.HashPassword(dto.Password);

        // Create user aggregate using domain factory method
        var user = User.Register(email, passwordHash, dto.FirstName, dto.LastName);

        // Persist to database
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to response DTO
        return MapToProfileDto(user);
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    public async Task<UserProfileDto> LoginAsync(
        LoginUserDto dto, 
        CancellationToken cancellationToken = default)
    {
        var email = Email.Create(dto.Email);

        // Get user from repository
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, dto.Password))
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        // Record login in domain (raises domain event)
        user.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToProfileDto(user);
    }

    /// <summary>
    /// Gets user profile by ID.
    /// </summary>
    public async Task<UserProfileDto> GetProfileAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        return MapToProfileDto(user);
    }

    /// <summary>
    /// Updates user profile information.
    /// </summary>
    public async Task<UserProfileDto> UpdateProfileAsync(
        Guid userId,
        UpdateUserProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        // Use domain method to update (validates business rules)
        user.UpdateProfile(dto.FirstName, dto.LastName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToProfileDto(user);
    }

    /// <summary>
    /// Gets all users with pagination.
    /// </summary>
    public async Task<List<UserProfileDto>> GetAllUsersAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(skip, take, cancellationToken);
        return users.Select(MapToProfileDto).ToList();
    }

    private static UserProfileDto MapToProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
