namespace API.Controllers;
using System.Collections.Generic;
using API.Data;
using API.DataEntities;
using API.DTOs;
using API.Extensions;
using API.Helpers;
using API.Services;
using API.UnitOfWork;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class UsersController : BaseApiController
{
     private readonly IUnitOfWork _unitOfWork;
    private readonly IPhotoService _photoService;
    private readonly IMapper _mapper;

    public UsersController(IUnitOfWork unitOfWork, IPhotoService photoService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _photoService = photoService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberResponse>>> GetAllAsync([FromQuery] UserParams userParams)
    {
        userParams.CurrentUsername = User.GetUserName();
        var members = await _unitOfWork.UserRepository.GetMembersAsync(userParams);
        Response.AddPaginationHeader(members);
        return Ok(members);
    }

    [HttpGet("{username}", Name = "GetByUsername")] // api/users/Calamardo
    public async Task<ActionResult<MemberResponse>> GetByUsernameAsync(string username)
    {
        var member = await _unitOfWork.UserRepository.GetMemberAsync(username.ToLowerInvariant());

        if (member == null)
        {
            return NotFound();
        }

        return member;
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateRequest request)
    {

        var user = await _unitOfWork.UserRepository.GetByUsernameAsync(User.GetUserName());

        if (user == null)
        {
            return BadRequest("Could not find user");
        }

        _mapper.Map(request, user);
        _unitOfWork.UserRepository.Update(user);

        if (await _unitOfWork.Complete())
        {
            return NoContent();
        }

        return BadRequest("Update user failed!");
    }

    [HttpPost("photo")]
    public async Task<ActionResult<PhotoResponse>> AddPhoto(IFormFile file)
    {
        var user = await _unitOfWork.UserRepository.GetByUsernameAsync(User.GetUserName());

        if (user == null)
        {
            return BadRequest("Cannot update user");
        }

        var result = await _photoService.AddPhotoAsync(file);

        if (result.Error != null)
        {
            return BadRequest(result.Error.Message);
        }

        var photo = new Photo
        {
            Url = result.SecureUrl.AbsoluteUri,
            PublicId = result.PublicId
        };

        if (user.Photos.Count == 0)
        {
            photo.IsMain = true;
        }

        user.Photos.Add(photo);

        if (await _unitOfWork.Complete())
        {
            return CreatedAtAction("GetByUsername",
                new { username = user.UserName }, _mapper.Map<PhotoResponse>(photo));
        }

        return BadRequest("Problem adding the photo");
    }

    [HttpPut("photo/{photoId:int}")]
    public async Task<ActionResult> SetPhotoAsMain(int photoId)
    {
        var user = await _unitOfWork.UserRepository.GetByUsernameAsync(User.GetUserName());
        if (user == null) return BadRequest("User not found");
        var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null || photo.IsMain) return BadRequest("Can't set this photo as the main one!");
        var currentMain = user.Photos.FirstOrDefault(p => p.IsMain);
        if (currentMain != null) currentMain.IsMain = false;
        photo.IsMain = true;
        if (await _unitOfWork.Complete()) return NoContent();
        return BadRequest("There was a problem.");
    }

    [HttpDelete("photo/{photoId:int}")]
    public async Task<ActionResult> DeletePhoto(int photoId)
    {
        var user = await _unitOfWork.UserRepository.GetByUsernameAsync(User.GetUserName());
        if (user == null) return BadRequest("User not found");
        var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null || photo.IsMain) return BadRequest("This photo can't be deleted");
        if (photo.PublicId != null)
        {
            var result = await _photoService.DeletePhotoAsync(photo.PublicId);
            if (result.Error != null) return BadRequest(result.Error.Message);
        }
        user.Photos.Remove(photo);
        if (await _unitOfWork.Complete()) return Ok();
        return BadRequest("There was a problem when deleting the photo");
    }
}
