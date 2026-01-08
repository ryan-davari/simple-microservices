using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.Models;

namespace PlatformService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlatformsController : ControllerBase
{
    private IPlatformRepo _repository;
    private IMapper _mapper;

    public PlatformsController(IPlatformRepo repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PlatformReadDto>> GetPlatforms()
    {
        Console.WriteLine("--> Getting Platforms...");
        var platforms = _repository.GetAllPlatforms();
        return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(platforms));
    }

    [HttpGet("{id}", Name = "GetPlatformById")]
    public ActionResult<PlatformReadDto> GetPlatformById([FromRoute]int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        Console.WriteLine("--> Getting Platform by id...");
        var platform = _repository.GetPlatformById(id);
        if (platform == null)
        {
            return NotFound();
        }
        return Ok(_mapper.Map<PlatformReadDto>(platform));
    }

    [HttpPost]
    public ActionResult<PlatformReadDto> CreatePlatform([FromBody]PlatformCreateDto platformCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var platformModel = _mapper.Map<Platform>(platformCreateDto);
        _repository.CreatePlatform(platformModel);

        _repository.SaveChanges();
        var platformReadDto = _mapper.Map<PlatformReadDto>(platformModel);

        return CreatedAtRoute(nameof(GetPlatformById), new { Id = platformReadDto.Id }, platformReadDto);
    }
}
