using System.Text;
using System.Security.Cryptography;
using DatingApp.Data;
using DatingApp.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.DTOs;
using Microsoft.EntityFrameworkCore.Diagnostics;
using API.Interfaces;

namespace API.Controllers
{

  public class AccountController : BaseAPIController
  {
    private readonly DataContext context;
    private readonly ITokenService tokenService;

    public AccountController(DataContext context, ITokenService tokenService)
    {
      this.context = context;
      this.tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) 
    {
        if (await UserExist(registerDto.Username)) return BadRequest("Username is taken");
        
        using var hmac = new HMACSHA512();
        var user = new AppUser
        {
            UserName = registerDto.Username,
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
            PasswordSalt = hmac.Key,
        };

        this.context.Users.Add(user);
        await this.context.SaveChangesAsync();

        return new UserDto 
        {
          Username = user.UserName,
          Token = this.tokenService.CreateToken(user)  
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await this.context.Users.SingleOrDefaultAsync(x => x.UserName == loginDto.Username);

        if (user == null) return Unauthorized("invalid username");

        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
        for (int i = 0; i < computeHash.Length; i++) 
        {
            if (computeHash[i] != user.PasswordHash[i]) return Unauthorized("invalid password");
        }

        return new UserDto 
        {
          Username = user.UserName,
          Token = this.tokenService.CreateToken(user)
        };
    }

    private async Task<bool> UserExist(string username) 
    {
        return await this.context.Users.AnyAsync(x => x.UserName == username.ToLower());
    }
  }
}