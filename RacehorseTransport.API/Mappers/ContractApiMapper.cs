using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class ContractApiMapper
    {
        public static ContractResponse ToResponse(this BookingContractModel model) => new()
        {
            ContractId = model.ContractId,
            ContractCode = model.ContractCode,
            BookingId = model.BookingId,
            BookingCode = model.BookingCode,
            Version = model.Version,
            Status = model.Status,
            Reason = model.Reason,
            FileSha256 = model.FileSha256,
            PolicyVersion = model.PolicyVersion,
            FileUrl = model.FileUrl,
            FileUrlExpiresAt = model.FileUrlExpiresAt,
            AcceptedByUserId = model.AcceptedByUserId,
            AcceptedByName = model.AcceptedByName,
            AcceptedAt = model.AcceptedAt,
            AcceptedIp = model.AcceptedIp,
            AcceptedUserAgent = model.AcceptedUserAgent,
            AcceptedPolicyVersion = model.AcceptedPolicyVersion,
            CreatedAt = model.CreatedAt
        };

        public static AirWaybillResponse ToResponse(this AirWaybillModel model) => new()
        {
            TripId = model.TripId,
            TripCode = model.TripCode,
            AirWaybillNumber = model.AirWaybillNumber,
            AirWaybillFilePath = model.AirWaybillFilePath,
            AirWaybillFileUrl = model.AirWaybillFileUrl
        };
    }
}
