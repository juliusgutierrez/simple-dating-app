using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using API.Controllers;
using API.DTOs;
using API.Interfaces;
using AutoMapper;
using DatingApp.Data;
using DatingApp.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.Controllers
{
  
  [Authorize]
  public class UsersController: BaseAPIController
  {
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
      _userRepository = userRepository;
      _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
    {
      return Ok(await _userRepository.GetMemberAsync());
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDto>> GetUserByUsername(string username)
    {
      return await _userRepository.GetMemberByUsernameAsync(username);
      
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
    {
      //verify if the request has a token
      var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var user = await _userRepository.GetUserByUsernameAsync(username);

      if(user == null) NotFound();

      _mapper.Map(memberUpdateDto, user);

      if (await _userRepository.SaveAllAsync()) return NoContent();

      return BadRequest("Failed to update user");
    }
  }
}