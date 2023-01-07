using System.Text;
using System.Security.Cryptography;
using DatingApp.Data;
using DatingApp.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.DTOs;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Controllers
{

  public class AccountController : BaseAPIController
  {
    private readonly DataContext context;

    public AccountController(DataContext context)
    {
      this.context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AppUser>> Register(RegisterDto registerDto) 
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

        return user;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AppUser>> Login(LoginDto loginDto)
    {
        var user = await this.context.Users.SingleOrDefaultAsync(x => x.UserName == loginDto.Username);

        if (user == null) return Unauthorized("invalid username");

        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
        for (int i = 0; i < computeHash.Length; i++) 
        {
            if (computeHash[i] != user.PasswordHash[i]) return Unauthorized("invalid password");
        }

        return user;
    }

    private async Task<bool> UserExist(string username) 
    {
        return await this.context.Users.AnyAsync(x => x.UserName == username.ToLower());
    }
  }
}