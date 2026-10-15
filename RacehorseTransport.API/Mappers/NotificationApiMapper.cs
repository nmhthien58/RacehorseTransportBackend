using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class NotificationApiMapper
    {
        public static NotificationQueryOptions ToBusinessOptions(this NotificationQueryRequest request)
        {
            return new NotificationQueryOptions
            {
                IsRead = request.IsRead,
                Page = request.Page,
                Size = request.Size
            };
        }

        public static NotificationResponse ToResponse(this NotificationModel model)
        {
            return new NotificationResponse
            {
                NotificationId = model.NotificationId,
                Title = model.Title,
                Message = model.Message,
                ReferenceType = model.ReferenceType,
                ReferenceId = model.ReferenceId,
                IsRead = model.IsRead,
                CreatedAt = model.CreatedAt
            };
        }
    }
}
