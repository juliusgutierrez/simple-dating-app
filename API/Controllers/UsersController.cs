using System.Formats.Asn1;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using API.Controllers;
using API.DTOs;
using API.Extensions;
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
    private readonly IPhotoService _photoService;

    public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
    {
      _photoService = photoService;
      _userRepository = userRepository;
      _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
    {
      return Ok(await _userRepository.GetMemberAsync());
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
      return await _userRepository.GetMemberByUsernameAsync(username);
      
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
    {
      //verify if the request has a token
      var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

      if(user == null) NotFound();

      _mapper.Map(memberUpdateDto, user);

      if (await _userRepository.SaveAllAsync()) return NoContent();

      return BadRequest("Failed to update user");
    }

    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
    {
      var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

      if (user == null) return NotFound();

      var result = await _photoService.AddPhotoAsync(file);

      if (result.Error != null) return BadRequest(result.Error.Message);

      var photo = new Photo
      {
        Url = result.SecureUrl.AbsoluteUri,
        PublicId = result.PublicId
      };

      if (user.Photos.Count == 0) photo.IsMain = true;

      user.Photos.Add(photo);

      if (await _userRepository.SaveAllAsync()) 
      {
        return CreatedAtAction(nameof(GetUsers), 
          new {username = user.UserName}, _mapper.Map<PhotoDto>(photo)
          );
      }

      return BadRequest("Problem adding photo");
    }

    [HttpPut("set-main-photo/{photoId}")]
    public async Task<ActionResult> SetMainPhoto(int photoId) 
    {
      var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

      if (user == null) return NotFound();

      var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

      if (photo == null) NotFound();

      if (photo.IsMain) return BadRequest("this is already your main photo");

      var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
      if (currentMain != null) currentMain.IsMain = false;
      photo.IsMain = true;

      if (await _userRepository.SaveAllAsync()) return NoContent();

      return BadRequest("Problem setting the main photo");
    }

    [HttpDelete("delete-photo/{photoId}")]
    public async Task<ActionResult> DeletePhoto(int photoId) 
    {
      var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
      var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
      
      if (photo == null) return NotFound();

      if (photo.IsMain) return BadRequest("You cannot delete your main photo");

      if (photo.PublicId != null)
      {
        var result = await _photoService.DeletePhotoAsync(photo.PublicId);
        if (result.Error != null) return BadRequest(result.Error.Message);
      }

      user.Photos.Remove(photo);
      if (await _userRepository.SaveAllAsync()) return Ok();

      return BadRequest("Problem delete photo");
    }


  }
}