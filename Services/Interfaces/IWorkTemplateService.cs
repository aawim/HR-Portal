using HRM.DTOs.WorkPlanning;
using HRM.DTOs;


namespace HRM.Services.Interfaces
{
    public interface IWorkTemplateService
    {
        Task<List<WorkTemplateDto>> GetAllAsync(
       bool includeInactive = true);

        Task<WorkTemplateDto?> GetByIdAsync(
            int workTemplateId);

        Task<ServiceResult> CreateAsync(
            CreateWorkTemplateDto dto);

        Task<ServiceResult> UpdateAsync(UpdateWorkTemplateDto dto);

        Task<ServiceResult> ArchiveAsync(int workTemplateId);

        //Task<ServiceResult> ActivateAsync(int workTemplateId);

        //Task<List<WorkTemplateSegmentDto>> GetAllSegmentsAsync();

        Task<List<WorkSegmentTypeDto>> GetAllSegmentTypesAsync();

        Task<List<WorkTemplateSegmentDto>> GetSegmentsAsync(int workTemplateId);

        Task<ServiceResult> CreateSegmentAsync(SaveWorkTemplateSegmentDto dto);

        Task<ServiceResult> UpdateSegmentAsync(SaveWorkTemplateSegmentDto dto);

        Task<ServiceResult> ArchiveSegmentAsync(int workTemplateSegmentId);


 



    }
}
