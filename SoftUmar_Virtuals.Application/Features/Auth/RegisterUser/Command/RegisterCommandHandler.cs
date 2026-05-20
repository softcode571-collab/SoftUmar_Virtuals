using BCrypt.Net;
using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Common;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command
{
    public record RegisterCommand(RegisterUserDTO register) : IRequest<Result<RegisterCommandOutput>>;

    public class RegisterCommandHandler(IRepository<User> userRepository) : IRequestHandler<RegisterCommand, Result<RegisterCommandOutput>>
{
    public async Task<Result<RegisterCommandOutput>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
            try
            {
                var dto = request.register;
                var userExist = userRepository.Exists(x => x.Email == dto.Email);

                if(userExist == true)
                {
                    return Result.Fail("User with this email already exists.");
                }

                if(dto.Password != dto.ConfirmPassword)
                {
                    return Result.Fail("Password and Confirm Password do not match.");
                }

                var encriptPass = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PasswordHash = encriptPass,
                    PhoneNumber = dto.PhoneNumber,
                    Country = dto.Country,


                };

                await userRepository.AddAsync(user, cancellationToken);
                await userRepository.SaveChanges(cancellationToken);



                return Result.Ok(new RegisterCommandOutput
                {
                    UserId = user.Id,
                    Message = "Registration Successful, Check Your Mail."
                });

            }
            catch (Exception ex)
            {

                return Result.Ok(new RegisterCommandOutput
                {

                    Message = ex.Message
                });
            }
            throw new NotImplementedException();
    }
}
}
