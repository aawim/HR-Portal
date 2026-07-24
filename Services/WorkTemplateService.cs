using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.WorkPlanning;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class WorkTemplateService : IWorkTemplateService
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IOperationLogService _logService;

        public WorkTemplateService(
            IDbContextFactory<HrmTeContext> dbFactory, IOperationLogService logService)
        {
            _dbFactory = dbFactory;
            _logService = logService;
        }

        public async Task<List<WorkTemplateDto>> GetAllAsync(bool includeInactive = true)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var query = db.WorkTemplates
        .AsNoTracking()
        .AsQueryable();

    if (!includeInactive)
    {
        query = query.Where(x => x.IsActive);
    }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new WorkTemplateDto
            {
                WorkTemplateId = x.WorkTemplateId,

                WorkTemplateTypeId = x.WorkTemplateTypeId,

                WorkTemplateTypeName =
                    x.WorkTemplateType != null
                        ? x.WorkTemplateType.Name
                        : string.Empty,

                OrganisationBusinessEntityId =
                    x.OrganisationBusinessEntityId,

                Name = x.Name,

                Code = x.Code,

                Description = x.Description,

                DefaultStartTime = x.DefaultStartTime,

                DefaultEndTime = x.DefaultEndTime,

                EndsNextDay = x.EndsNextDay,

                DefaultGraceMinutes = x.DefaultGraceMinutes,

                RequiresAttendance = x.RequiresAttendance,

                RequiresCheckOut = x.RequiresCheckOut,

                IsRepeatable = x.IsRepeatable,

                IsGlobal = x.IsGlobal,

                IsActive = x.IsActive,

                EffectiveFrom = x.EffectiveFrom,

                EffectiveTo = x.EffectiveTo,

                SegmentCount = x.WorkTemplateSegments
                    .Count(segment => segment.IsActive)
            })
            .ToListAsync();
            }


        public async Task<List<WorkTemplateDto>> GetAvailableAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);

            return await db.WorkTemplates
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    (!x.EffectiveFrom.HasValue ||
                     x.EffectiveFrom.Value <= today) &&
                    (!x.EffectiveTo.HasValue ||
                     x.EffectiveTo.Value >= today))
                .OrderBy(x => x.Name)
                .Select(x => new WorkTemplateDto
                {
                    WorkTemplateId = x.WorkTemplateId,
                    WorkTemplateTypeId = x.WorkTemplateTypeId,

                    WorkTemplateTypeName =
                        x.WorkTemplateType.Name,

                    OrganisationBusinessEntityId =
                        x.OrganisationBusinessEntityId,

                    Name = x.Name,
                    Code = x.Code,
                    Description = x.Description,

                    DefaultStartTime = x.DefaultStartTime,
                    DefaultEndTime = x.DefaultEndTime,

                    EndsNextDay = x.EndsNextDay,
                    DefaultGraceMinutes = x.DefaultGraceMinutes,

                    RequiresAttendance = x.RequiresAttendance,
                    RequiresCheckOut = x.RequiresCheckOut,

                    IsRepeatable = x.IsRepeatable,
                    IsGlobal = x.IsGlobal,
                    IsActive = x.IsActive,

                    EffectiveFrom = x.EffectiveFrom,
                    EffectiveTo = x.EffectiveTo,

                    SegmentCount = x.WorkTemplateSegments
                        .Count(segment => segment.IsActive)
                })
                .ToListAsync();
        }


        public async Task<WorkTemplateDto?> GetByIdAsync(int workTemplateId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.WorkTemplates

                .Where(x => x.WorkTemplateId == workTemplateId)

                .Select(x => new WorkTemplateDto
                {
                    WorkTemplateId = x.WorkTemplateId,

                    WorkTemplateTypeId = x.WorkTemplateTypeId,

                    WorkTemplateTypeName =
                x.WorkTemplateType != null
                    ? x.WorkTemplateType.Name
                    : string.Empty,

                    OrganisationBusinessEntityId =
                x.OrganisationBusinessEntityId,

                    Name = x.Name,

                    Code = x.Code,

                    Description = x.Description,

                    DefaultStartTime = x.DefaultStartTime,

                    DefaultEndTime = x.DefaultEndTime,

                    EndsNextDay = x.EndsNextDay,

                    DefaultGraceMinutes = x.DefaultGraceMinutes,

                    RequiresAttendance = x.RequiresAttendance,

                    RequiresCheckOut = x.RequiresCheckOut,

                    IsRepeatable = x.IsRepeatable,

                    IsGlobal = x.IsGlobal,

                    IsActive = x.IsActive,

                    EffectiveFrom = x.EffectiveFrom,

                    EffectiveTo = x.EffectiveTo,

                    SegmentCount = x.WorkTemplateSegments

                    .Count(segment => segment.IsActive)


                })

                .FirstOrDefaultAsync();
        }


        public async Task<ServiceResult> CreateAsync(CreateWorkTemplateDto dto)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var name = dto.Name.Trim();
            var code = string.IsNullOrWhiteSpace(dto.Code)
                ? null
                : dto.Code.Trim();

            var duplicateNameExists = await db.WorkTemplates
                .AnyAsync(x =>
                    x.IsActive &&
                    x.Name == name &&
                    x.OrganisationBusinessEntityId ==
                        dto.OrganisationBusinessEntityId);

            if (duplicateNameExists)
            {
                return ServiceResult.Failed(
                    "An active work template with the same name already exists.");
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                var duplicateCodeExists = await db.WorkTemplates
                    .AnyAsync(x =>
                        x.IsActive &&
                        x.Code == code &&
                        x.OrganisationBusinessEntityId ==
                            dto.OrganisationBusinessEntityId);

                if (duplicateCodeExists)
                {
                    return ServiceResult.Failed(
                        "An active work template with the same code already exists.");
                }
            }

            var validationResult = ValidateTemplateDatesAndTimes(
                dto.DefaultStartTime,
                dto.DefaultEndTime,
                dto.EndsNextDay,
                dto.EffectiveFrom,
                dto.EffectiveTo);

            if (!validationResult.Success)
                return validationResult;



            var availableTemplateTypes = await db.WorkTemplateTypes
                    .AsNoTracking()
                    .Select(x => new
                    {
                        x.WorkTemplateTypeId,
                        x.Name
                    })
                    .ToListAsync();

                            var templateType = await db.WorkTemplateTypes
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x =>
                                    x.WorkTemplateTypeId == dto.WorkTemplateTypeId);

                            if (templateType == null)
                            {
                                var availableIds = availableTemplateTypes.Any()
                                    ? string.Join(", ",
                                        availableTemplateTypes.Select(x =>
                                            $"{x.WorkTemplateTypeId} - {x.Name}"))
                                    : "No work template types were returned.";

                                return ServiceResult.Failed(
                                    $"Work template type ID {dto.WorkTemplateTypeId} was not found. " +
                                    $"Available types: {availableIds}");
                            }


            var templateTypeExists = await db.WorkTemplateTypes
                .AnyAsync(x =>
                    x.WorkTemplateTypeId == dto.WorkTemplateTypeId);



            if (!templateTypeExists)
            {
                return ServiceResult.Failed(
                    "The selected work template type does not exist.");
            }

            if (dto.OrganisationBusinessEntityId.HasValue)
            {
                var organisationExists = await db.Organisations
                    .AnyAsync(x =>
                        x.BusinessEntityID == dto.OrganisationBusinessEntityId.Value);

                if (!organisationExists)
                {
                    return ServiceResult.Failed(
                        "The selected organisation does not exist.");
                }
            }

            var entity = new WorkTemplate
            {
                WorkTemplateTypeId = dto.WorkTemplateTypeId,
                OrganisationBusinessEntityId =
                    dto.OrganisationBusinessEntityId,

                Name = name,
                Code = code,
                Description = string.IsNullOrWhiteSpace(dto.Description)
                    ? null
                    : dto.Description.Trim(),

                DefaultStartTime = dto.DefaultStartTime,
                DefaultEndTime = dto.DefaultEndTime,
                EndsNextDay = dto.EndsNextDay,
                DefaultGraceMinutes = dto.DefaultGraceMinutes,

                RequiresAttendance = dto.RequiresAttendance,
                RequiresCheckOut = dto.RequiresCheckOut,
                IsRepeatable = dto.IsRepeatable,
                IsGlobal = dto.IsGlobal,
                IsActive = dto.IsActive,

                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,

                OperationLogId = dto.OperationLogId
            };

            db.WorkTemplates.Add(entity);

            await db.SaveChangesAsync();

            return ServiceResult.Ok("Work template created successfully.");
        }




        public async Task<ServiceResult> UpdateAsync(UpdateWorkTemplateDto dto)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var entity = await db.WorkTemplates
                .FirstOrDefaultAsync(x =>
                    x.WorkTemplateId == dto.WorkTemplateId);

            if (entity == null)
            {
                return ServiceResult.Failed(
                    "Work template not found.");
            }

            var name = dto.Name.Trim();
            var code = string.IsNullOrWhiteSpace(dto.Code)
                ? null
                : dto.Code.Trim();

            var duplicateNameExists = await db.WorkTemplates
                .AnyAsync(x =>
                    x.WorkTemplateId != dto.WorkTemplateId &&
                    x.IsActive &&
                    x.Name == name &&
                    x.OrganisationBusinessEntityId ==
                        dto.OrganisationBusinessEntityId);

            if (duplicateNameExists)
            {
                return ServiceResult.Failed(
                    "Another active work template with the same name already exists.");
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                var duplicateCodeExists = await db.WorkTemplates
                    .AnyAsync(x =>
                        x.WorkTemplateId != dto.WorkTemplateId &&
                        x.IsActive &&
                        x.Code == code &&
                        x.OrganisationBusinessEntityId ==
                            dto.OrganisationBusinessEntityId);

                if (duplicateCodeExists)
                {
                    return ServiceResult.Failed(
                        "Another active work template with the same code already exists.");
                }
            }

            var validationResult = ValidateTemplateDatesAndTimes(
                dto.DefaultStartTime,
                dto.DefaultEndTime,
                dto.EndsNextDay,
                dto.EffectiveFrom,
                dto.EffectiveTo);

            if (!validationResult.Success)
                return validationResult;

            var templateTypeExists = await db.WorkTemplateTypes
                .AnyAsync(x =>
                    x.WorkTemplateTypeId == dto.WorkTemplateTypeId);

            if (!templateTypeExists)
            {
                return ServiceResult.Failed(
                    "The selected work template type does not exist.");
            }

            if (dto.OrganisationBusinessEntityId.HasValue)
            {
                var organisationExists = await db.Organisations
                    .AnyAsync(x =>
                        x.BusinessEntityID ==
                        dto.OrganisationBusinessEntityId.Value);

                if (!organisationExists)
                {
                    return ServiceResult.Failed(
                        "The selected organisation does not exist.");
                }
            }

            entity.WorkTemplateTypeId = dto.WorkTemplateTypeId;
            entity.OrganisationBusinessEntityId = dto.OrganisationBusinessEntityId;

            entity.Name = name;
            entity.Code = code;
            entity.Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim();

            entity.DefaultStartTime = dto.DefaultStartTime;
            entity.DefaultEndTime = dto.DefaultEndTime;
            entity.EndsNextDay = dto.EndsNextDay;
            entity.DefaultGraceMinutes = dto.DefaultGraceMinutes;

            entity.RequiresAttendance = dto.RequiresAttendance;
            entity.RequiresCheckOut = dto.RequiresCheckOut;
            entity.IsRepeatable = dto.IsRepeatable;
            entity.IsGlobal = dto.IsGlobal;
            entity.IsActive = dto.IsActive;

            entity.EffectiveFrom = dto.EffectiveFrom;
            entity.EffectiveTo = dto.EffectiveTo;

            entity.OperationLogId = dto.OperationLogId;

            await db.SaveChangesAsync();

            return ServiceResult.Ok("Work template updated successfully.");
        }


        public async Task<ServiceResult> ArchiveAsync(int workTemplateId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var entity = await db.WorkTemplates
                .FirstOrDefaultAsync(x =>
                    x.WorkTemplateId == workTemplateId);

            if (entity == null)
                return ServiceResult.Failed(
                    "Template not found.");

            entity.IsActive = false;

            await db.SaveChangesAsync();

            return ServiceResult.Ok("Template archived.");
        }



        //public async Task<ServiceResult> ActivateAsync(int workTemplateId)
        //{
        //    await using var db = await _dbFactory.CreateDbContextAsync();

        //    var template = await db.WorkTemplates
        //        .FirstOrDefaultAsync(x => x.WorkTemplateId == workTemplateId);

        //    if (template == null)
        //    {
        //        return ServiceResult.Ok("Work template not found.");
        //    }

        //    if (template.IsActive)
        //    {
        //        return ServiceResult.Ok("Work template is already active.");
        //    }

        //    // Check for duplicate active name
        //    var duplicateNameExists = await db.WorkTemplates
        //        .AnyAsync(x =>
        //            x.WorkTemplateId != workTemplateId &&
        //            x.IsActive &&
        //            x.OrganisationBusinessEntityId == template.OrganisationBusinessEntityId &&
        //            x.Name == template.Name);

        //    if (duplicateNameExists)
        //    {
        //        return ServiceResult.Failed(
        //            "Another active work template with the same name already exists.");
        //    }

        //    // Check for duplicate active code (if one exists)
        //    if (!string.IsNullOrWhiteSpace(template.Code))
        //    {
        //        var duplicateCodeExists = await db.WorkTemplates
        //            .AnyAsync(x =>
        //                x.WorkTemplateId != workTemplateId &&
        //                x.IsActive &&
        //                x.OrganisationBusinessEntityId == template.OrganisationBusinessEntityId &&
        //                x.Code == template.Code);

        //        if (duplicateCodeExists)
        //        {
        //            return ServiceResult.Failed(
        //                "Another active work template with the same code already exists.");
        //        }
        //    }

        //    template.IsActive = true;
        //    //template. = DateTime.UtcNow;

        //    await db.SaveChangesAsync();

        //    return ServiceResult.Ok("Work template activated successfully.");
        //}






        private static ServiceResult ValidateTemplateDatesAndTimes(
            TimeOnly? defaultStartTime,
            TimeOnly? defaultEndTime,
            bool endsNextDay,
            DateOnly? effectiveFrom,
            DateOnly? effectiveTo)
        {
            // Start and end times should either both exist or both be empty.
            if (defaultStartTime.HasValue != defaultEndTime.HasValue)
            {
                return ServiceResult.Failed(
                    "Both default start time and default end time must be provided.");
            }

            if (defaultStartTime.HasValue &&
                defaultEndTime.HasValue)
            {
                var startTime = defaultStartTime.Value;
                var endTime = defaultEndTime.Value;

                // Same-day shift: end time must be later.
                if (!endsNextDay && endTime <= startTime)
                {
                    return ServiceResult.Failed(
                        "The end time must be later than the start time unless the template ends on the next day.");
                }

                // Overnight shift: end time should be earlier than or equal to start.
                if (endsNextDay && endTime > startTime)
                {
                    return ServiceResult.Failed(
                        "For an overnight template, the end time should be earlier than or equal to the start time.");
                }
            }

            if (effectiveFrom.HasValue &&
                effectiveTo.HasValue &&
                effectiveTo.Value < effectiveFrom.Value)
            {
                return ServiceResult.Failed(
                    "The effective-to date cannot be earlier than the effective-from date.");
            }

            return ServiceResult.Ok();
        }



        /// Segment related methods
        ///  Load Segments 

        public async Task<List<WorkTemplateSegmentDto>> GetSegmentsAsync(int workTemplateId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.WorkTemplateSegments
          .AsNoTracking()
          .Where(x =>
              x.WorkTemplateId == workTemplateId)
          .OrderBy(x => x.SequenceNumber)
          .Select(x => new WorkTemplateSegmentDto
          {
              WorkTemplateSegmentId =
                  x.WorkTemplateSegmentId,

              WorkTemplateId =
                  x.WorkTemplateId,

              WorkSegmentTypeId =
                  x.WorkSegmentTypeId,

              WorkSegmentTypeName =
                  x.WorkSegmentType.Name,

              Name =
                  x.Name,

              Description =
                  x.Description,

              SequenceNumber =
                  x.SequenceNumber,

              OffsetMinutes =
                  x.OffsetMinutes,

              DurationMinutes = (int)
                  x.DurationMinutes,

              GraceBeforeMinutes =
                  x.GraceBeforeMinutes,

              GraceAfterMinutes =
                  x.GraceAfterMinutes,

              IsMandatory =
                  x.IsMandatory,

              RequiresAttendance =
                  x.RequiresAttendance,

              RequiresLocationValidation =
                  x.RequiresLocationValidation,

              RequiresDeviceValidation =
                  x.RequiresDeviceValidation,

              IsActive =
                  x.IsActive,

              IsPaid =
                  x.IsPaid,

              OperationLogId =
                  x.OperationLogId
          })
          .ToListAsync();
        }


        public async Task<List<WorkSegmentTypeDto>> GetAllSegmentTypesAsync()
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.WorkSegmentTypes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new WorkSegmentTypeDto
                {
                    WorkSegmentTypeId = x.WorkSegmentTypeId,
                    Name = x.Name
                })
                .ToListAsync();
        }



        public async Task<List<WorkTemplateSegmentDto>> GetAllSegmentsAsync(int workTemplateId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.WorkTemplateSegments
                .AsNoTracking()
                .Where(x =>
                    x.IsActive)
                .OrderBy(x => x.SequenceNumber)
                .Select(x => new WorkTemplateSegmentDto
                {
                    WorkTemplateSegmentId =
                        x.WorkTemplateSegmentId,

                   WorkSegmentTypeId = x.WorkSegmentTypeId,

                   DurationMinutes = (int) x.DurationMinutes,

                    WorkTemplateId =
                        x.WorkTemplateId,

                    Name =
                        x.Name,

                    Description =
                        x.Description,

                    IsPaid =
                        x.IsPaid,

                    RequiresAttendance =
                        x.RequiresAttendance,

                    RequiresLocationValidation =
                        x.RequiresLocationValidation,

                    RequiresDeviceValidation =
                        x.RequiresDeviceValidation,

                    DisplayOrder = x.SequenceNumber,

                    IsActive =
                        x.IsActive
                })
                .ToListAsync();
        }



        // Create Segments


        public async Task<ServiceResult> CreateSegmentAsync(SaveWorkTemplateSegmentDto dto)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();



            if (dto.WorkTemplateId <= 0)
            {
                return ServiceResult.Failed(
                    "A valid work template is required.");
            }
 

            var template = await db.WorkTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.WorkTemplateId == dto.WorkTemplateId &&
                    x.IsActive);

            if (template == null)
            {
                return ServiceResult.Failed(
                    "The selected work template does not exist or is inactive.");
            }

            var name = dto.Name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return ServiceResult.Failed(
                    "Segment name is required.");
            }

            var duplicateNameExists =
                await db.WorkTemplateSegments.AnyAsync(x =>
                    x.WorkTemplateId == dto.WorkTemplateId &&
                    x.IsActive &&
                    x.Name == name);

            if (duplicateNameExists)
            {
                return ServiceResult.Failed(
                    "An active segment with the same name already exists.");
            }

            


            var nextDisplayOrder = await db.WorkTemplateSegments .Where(x =>
               x.WorkTemplateId == dto.WorkTemplateId)
                   .Select(x => (int?)x.SequenceNumber)
                   .MaxAsync() ?? 0;


            var operationLog = await _logService.CreateAsync(
                  db,
                  actionId: SharedConfig.OperationLogActionTypes.WORK_TEMPLATE_SEGMENT_CREATE,
                  remarks: "Work Template Segment Create");

            var segment = new WorkTemplateSegment
            {
                WorkTemplateId = dto.WorkTemplateId,
                WorkSegmentTypeId = dto.WorkSegmentTypeId,
                Name = name,
                Description = string.IsNullOrWhiteSpace(dto.Description)
                        ? null
                        : dto.Description.Trim(),
       
                IsPaid = dto.IsPaid,

                DurationMinutes = dto.DurationMinutes,
                OffsetMinutes = dto.OffsetMinutes,
                GraceAfterMinutes = dto.GraceAfterMinutes,
                GraceBeforeMinutes = dto.GraceBeforeMinutes,
                RequiresAttendance = dto.RequiresAttendance,

                RequiresLocationValidation = dto.RequiresLocationValidation,

                RequiresDeviceValidation = dto.RequiresDeviceValidation,

                SequenceNumber = dto.DisplayOrder > 0 ? dto.DisplayOrder : nextDisplayOrder + 1,

                IsActive = true,
      
                CreatedAt = DateTime.UtcNow,

                IsMandatory = dto.IsMandatory,

                OperationLogId = operationLog.OperationLogId,

            };


            var validationResult = await ValidateSegmentAsync( db,
                segment.WorkTemplateId,
                segment.WorkTemplateSegmentId,
                segment.WorkSegmentTypeId,
                segment.SequenceNumber,
                segment.OffsetMinutes,
                (int)segment.DurationMinutes,
                segment.GraceAfterMinutes,
                segment.GraceBeforeMinutes );


            if (!validationResult.Success)
            {
                return validationResult;
            }

   
            db.WorkTemplateSegments.Add(segment);

            await db.SaveChangesAsync();

            return ServiceResult.Ok(
                "Work template segment created successfully.");
        }


        // update Segments

        public async Task<ServiceResult> UpdateSegmentAsync(SaveWorkTemplateSegmentDto dto)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            if (dto.WorkTemplateSegmentId <= 0)
            {
                return ServiceResult.Failed(
                    "A valid segment is required.");
            }

            var segment = await db.WorkTemplateSegments
                .FirstOrDefaultAsync(x =>
                    x.WorkTemplateSegmentId ==
                        dto.WorkTemplateSegmentId);

            if (segment == null)
            {
                return ServiceResult.Failed(
                    "Work template segment not found.");
            }

            var name = dto.Name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return ServiceResult.Failed(
                    "Segment name is required.");
            }

            var operationLog = await _logService.CreateAsync(db,
                actionId: SharedConfig.OperationLogActionTypes.WORK_TEMPLATE_SEGMENT_UPDATE,
                remarks: "Work Template Segment Uodate");


            var duplicateNameExists =
                await db.WorkTemplateSegments.AnyAsync(x =>
                    x.WorkTemplateSegmentId !=
                        dto.WorkTemplateSegmentId &&
                    x.WorkTemplateId ==
                        segment.WorkTemplateId &&
                    x.IsActive &&
                    x.Name == name);

            if (duplicateNameExists)
            {
                return ServiceResult.Failed(
                    "Another active segment with the same name already exists.");
            }

            //var validationResult =
            //    await ValidateSegmentAsync(
            //        db,
            //        segment.WorkTemplateId,
            //        segment.WorkTemplateSegmentId

            //        );


            var validationResult = await ValidateSegmentAsync(
             db,
                segment.WorkTemplateId,
                segment.WorkTemplateSegmentId,
                segment.WorkSegmentTypeId,
                 segment.SequenceNumber,
                 segment.OffsetMinutes,
                 (int)segment.DurationMinutes,
                 segment.GraceAfterMinutes,
                 segment.GraceBeforeMinutes
                       
                   );

            if (!validationResult.Success)
            {
                return validationResult;
            }

            segment.Name =
                name;

            segment.Description =
                string.IsNullOrWhiteSpace(dto.Description)
                    ? null
                    : dto.Description.Trim();
 

            segment.IsPaid =
                dto.IsPaid;

            segment.RequiresAttendance =
                dto.RequiresAttendance;

            segment.RequiresLocationValidation =
                dto.RequiresLocationValidation;

            segment.RequiresDeviceValidation =
                dto.RequiresDeviceValidation;

            segment.SequenceNumber =
                dto.SequenceNumber;

            segment.IsActive =
                dto.IsActive;

            segment.UpdatedAt = DateTime.UtcNow;

            segment.OperationLogId = operationLog.OperationLogId;



            await db.SaveChangesAsync();

            return ServiceResult.Ok(
                "Work template segment updated successfully.");
        }


        /// Archive segment
        /// 



        public async Task<ServiceResult> ArchiveSegmentAsync(
    int workTemplateSegmentId)
{
    await using var db =
        await _dbFactory.CreateDbContextAsync();

    var segment = await db.WorkTemplateSegments
        .FirstOrDefaultAsync(x =>
            x.WorkTemplateSegmentId ==
                workTemplateSegmentId);

    if (segment == null)
    {
        return ServiceResult.Failed(
            "Work template segment not found.");
    }

    if (!segment.IsActive)
    {
        return ServiceResult.Ok(
            "Work template segment is already archived.");
    }

    segment.IsActive = false;
            segment.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return ServiceResult.Ok(
        "Work template segment archived successfully.");
    }



        public string FormatTime(DateOnly? time)
        {
            return time?.ToString("HH:mm") ?? "";
        }




        private async Task<ServiceResult> ValidateSegmentAsync(
            HrmTeContext db,
            int workTemplateId,
            int? currentSegmentId,
            int workSegmentTypeId,
            int sequenceNumber,
            int offsetMinutes,
            int durationMinutes,
            int graceBeforeMinutes,
            int graceAfterMinutes)
        {
            // ---------------------------------------------------------
            // 1. Basic validation
            // ---------------------------------------------------------

            if (workTemplateId <= 0)
            {
                return ServiceResult.Failed(
                    "A valid work template is required.");
            }

            if (workSegmentTypeId <= 0)
            {
                return ServiceResult.Failed(
                    "A valid work segment type is required.");
            }

            if (sequenceNumber <= 0)
            {
                return ServiceResult.Failed(
                    "Sequence number must be greater than zero.");
            }

            if (offsetMinutes < 0)
            {
                return ServiceResult.Failed(
                    "Offset minutes cannot be negative.");
            }

            if (durationMinutes <= 0)
            {
                return ServiceResult.Failed(
                    "Duration must be greater than zero.");
            }

            if (graceBeforeMinutes < 0)
            {
                return ServiceResult.Failed(
                    "Grace-before minutes cannot be negative.");
            }

            if (graceAfterMinutes < 0)
            {
                return ServiceResult.Failed(
                    "Grace-after minutes cannot be negative.");
            }

            // Optional safety limits.
            if (durationMinutes > 24 * 60)
            {
                return ServiceResult.Failed(
                    "A segment duration cannot exceed 24 hours.");
            }

            if (graceBeforeMinutes > durationMinutes)
            {
                return ServiceResult.Failed(
                    "Grace-before minutes cannot exceed the segment duration.");
            }

            if (graceAfterMinutes > durationMinutes)
            {
                return ServiceResult.Failed(
                    "Grace-after minutes cannot exceed the segment duration.");
            }

            // ---------------------------------------------------------
            // 2. Confirm parent template exists
            // ---------------------------------------------------------

            var templateExists = await db.WorkTemplates
                .AsNoTracking()
                .AnyAsync(x =>
                    x.WorkTemplateId == workTemplateId);

            if (!templateExists)
            {
                return ServiceResult.Failed(
                    "The selected work template does not exist.");
            }

            // ---------------------------------------------------------
            // 3. Confirm segment type exists
            // ---------------------------------------------------------

            var segmentTypeExists = await db.WorkSegmentTypes
                .AsNoTracking()
                .AnyAsync(x =>
                    x.WorkSegmentTypeId == workSegmentTypeId);

            if (!segmentTypeExists)
            {
                return ServiceResult.Failed(
                    "The selected work segment type does not exist.");
            }

            // ---------------------------------------------------------
            // 4. Prevent duplicate sequence numbers
            // ---------------------------------------------------------

            var sequenceExists = await db.WorkTemplateSegments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.WorkTemplateId == workTemplateId &&
                    x.SequenceNumber == sequenceNumber &&
                    x.IsActive &&
                    (!currentSegmentId.HasValue ||
                     x.WorkTemplateSegmentId != currentSegmentId.Value));

            if (sequenceExists)
            {
                return ServiceResult.Failed(
                    $"Sequence number {sequenceNumber} is already used by another segment.");
            }

            // ---------------------------------------------------------
            // 5. Validate overlap
            // ---------------------------------------------------------

            var newSegmentStart = offsetMinutes;
            var newSegmentEnd = offsetMinutes + durationMinutes;

            var existingSegments = await db.WorkTemplateSegments
                .AsNoTracking()
                .Where(x =>
                    x.WorkTemplateId == workTemplateId &&
                    x.IsActive &&
                    (!currentSegmentId.HasValue ||
                     x.WorkTemplateSegmentId != currentSegmentId.Value))
                .Select(x => new
                {
                    x.WorkTemplateSegmentId,
                    x.Name,
                    x.OffsetMinutes,
                    x.DurationMinutes,
                    x.SequenceNumber
                })
                .ToListAsync();

            foreach (var existingSegment in existingSegments)
            {
                var existingStart =
                    existingSegment.OffsetMinutes;

                var existingEnd =
                    existingSegment.OffsetMinutes +
                    existingSegment.DurationMinutes;

                /*
                 * Two segments overlap when:
                 *
                 * newStart < existingEnd
                 * &&
                 * newEnd > existingStart
                 *
                 * Touching boundaries are allowed:
                 *
                 * Segment A: 0 - 120
                 * Segment B: 120 - 150
                 */

                var overlaps =
                    newSegmentStart < existingEnd &&
                    newSegmentEnd > existingStart;

                if (overlaps)
                {
                    return ServiceResult.Failed(
                        $"This segment overlaps with " +
                        $"'{existingSegment.Name}' " +
                        $"at sequence {existingSegment.SequenceNumber}.");
                }
            }

            // ---------------------------------------------------------
            // 6. Optional: validate segment against template duration
            // ---------------------------------------------------------
 


             return ServiceResult.Ok(
                "Segment validation completed successfully.");


        }

    }
}
