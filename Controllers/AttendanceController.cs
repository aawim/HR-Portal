using Microsoft.AspNetCore.Mvc;
using HRM.Services; 
using HRM.Components.Shared;


[Route("api/[controller]")]
[ApiController]
public class AttendanceController : ControllerBase
{
    private readonly OperationLogService _logService;

    public AttendanceController(OperationLogService logService)
    {
        _logService = logService;
    }

    //// GET: api/Organisations
    //[HttpGet]
    //public async Task<IActionResult> LogAction()
    //{
    //    await LogService.CreateAndSaveLogAsync("User checked in", SharedConfig.OperationLogActionTypes.ATTENDANCE_LOG_CREATE);
    //    return Ok();
    //}
}