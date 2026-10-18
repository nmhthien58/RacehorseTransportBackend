using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Ước tính thời gian vận chuyển từ quãng đường và phương thức. Dùng chung cho báo giá, tạo đơn và lập chuyến.
    public interface ITransitTimeEstimator
    {
        TransitEstimateModel Estimate(TransitEstimateInputModel input);
    }
}
