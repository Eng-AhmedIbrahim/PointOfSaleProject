using POS.Contract.Dtos.AppDateDtos;
using POS.Core.Entities.Date;

namespace POS.API.Controllers;

public class AppDateController : BaseApiController
{
    private readonly IAppDateService _appSettingService;
    private readonly IMapper _mapper;

    public AppDateController(IAppDateService appSettingService,
        IMapper mapper)
    {
        _appSettingService = appSettingService;
        _mapper = mapper;
    }

    [HttpGet("get-app-date")]
    public async Task<ActionResult<AppDateToReturnDto>> GetAppDate()
    {
        var appDate = await _appSettingService.GetAppDateAsync();
        var mappedAppDate = _mapper.Map<AppDate, AppDateToReturnDto>(appDate);
        return Ok(mappedAppDate);
    }

    [HttpPut("update-app-date")] 
    public async Task<ActionResult<AppDateToReturnDto>> UpdateAppDate()
    {
        var appDate = await _appSettingService.UpdateAppDate();
        var mappedAppDate = _mapper.Map<AppDate, AppDateToReturnDto>(appDate);

        return Ok(mappedAppDate);
    }
}