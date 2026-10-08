using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IHorseService
    {
        Task<PagedBusinessResult<HorseModel>> GetHorsesAsync(HorseFilterOptions options, CurrentUser currentUser);
        Task<HorseModel> GetHorseByIdAsync(int id, CurrentUser currentUser);
        Task<HorseModel> CreateHorseAsync(HorseModel model, FileUploadModel? photo, CurrentUser currentUser);

        // photo = null thì giữ ảnh cũ; removePhoto = true thì xóa ảnh (bỏ qua khi có photo mới)
        Task<HorseModel> UpdateHorseAsync(int id, HorseModel model, FileUploadModel? photo, bool removePhoto, CurrentUser currentUser);
        Task DeleteHorseAsync(int id, CurrentUser currentUser);
    }
}
