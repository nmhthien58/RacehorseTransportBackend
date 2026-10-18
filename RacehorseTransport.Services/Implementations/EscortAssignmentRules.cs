using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // Phân công escort theo chuồng (R-06). Hàm thuần để dễ viết unit test.
    //   - Mỗi con ngựa có đúng một escort, escort đó phải là Escort trong đoàn của chính chuyến.
    //   - Mỗi escort phụ trách tối đa HorsesPerGroom (3) con.
    //   - Con nào chưa được chọn escort thì tự phân: xếp theo số ô chuồng, giao cho escort đầu tiên còn chỗ
    //     (trưởng nhóm escort trước, rồi theo thứ tự trong danh sách đoàn) => ô 1-3 cho escort 1, ô 4-6 cho escort 2...
    public static class EscortAssignmentRules
    {
        public static void Apply(IReadOnlyCollection<TripHorseInputModel> horses, IReadOnlyCollection<TripCrewInputModel> crew)
        {
            var escortIds = crew
                .Where(c => c.CrewRole == CrewRoles.Escort)
                .OrderByDescending(c => c.IsLead)
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            var invalid = horses
                .Where(h => h.EscortUserId.HasValue && !escortIds.Contains(h.EscortUserId.Value))
                .Select(h => h.EscortUserId!.Value)
                .Distinct()
                .ToList();
            if (invalid.Count > 0)
            {
                throw new BusinessException($"User(s) with ID {string.Join(", ", invalid)} are not escorts on this trip, so they cannot be put in charge of a horse.");
            }

            var load = escortIds.ToDictionary(id => id, id => horses.Count(h => h.EscortUserId == id));
            var overloaded = load.Where(x => x.Value > PricingService.HorsesPerGroom).Select(x => x.Key).ToList();
            if (overloaded.Count > 0)
            {
                throw new BusinessException($"An escort can look after at most {PricingService.HorsesPerGroom} horses; user(s) with ID {string.Join(", ", overloaded)} have more.");
            }

            foreach (var horse in horses.Where(h => !h.EscortUserId.HasValue).OrderBy(h => h.StallSlotNumber))
            {
                int? free = escortIds.Cast<int?>().FirstOrDefault(id => load[id!.Value] < PricingService.HorsesPerGroom);
                if (free == null)
                {
                    throw new BusinessException($"Not enough escorts: each escort looks after at most {PricingService.HorsesPerGroom} horses.");
                }
                horse.EscortUserId = free;
                load[free.Value]++;
            }
        }
    }
}
