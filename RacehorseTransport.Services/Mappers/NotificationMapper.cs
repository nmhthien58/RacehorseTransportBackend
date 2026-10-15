using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class NotificationMapper
    {
        public static NotificationModel ToBusinessModel(this SystemNotification entity)
        {
            return new NotificationModel
            {
                NotificationId = entity.NotificationId,
                Title = entity.Title,
                Message = entity.Message,
                ReferenceType = entity.ReferenceType,
                ReferenceId = entity.ReferenceId,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
