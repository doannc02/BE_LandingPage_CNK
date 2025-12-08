using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace NunchakuClub.Application.Features.Auth.Commands;

public record RegisterCommand(
    string Email, 
    string Username, 
    string Password, 
    string FullName) : IRequest<Result<Guid>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    
    public RegisterCommandHandler(
        IApplicationDbContext context, 
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }
    
    public async Task<Result<Guid>> Handle(
        RegisterCommand request, 
        CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return Result<Guid>.Failure("Email already exists");
        
        if (await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
            return Result<Guid>.Failure("Username already exists");
        
        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            Role = UserRole.Member,
            Status = UserStatus.Active,
            EmailVerified = false
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(user.Id);
    }
}
