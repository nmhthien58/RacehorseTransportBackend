using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // Một con ngựa cần xếp vào khoang: tên để báo lỗi, hạng chuồng khách mua và vị trí được chọn
    public record CompartmentPlacement(string HorseName, string StallClass, int CompartmentNo, int PositionNo);

    // Xe chia khoang, mỗi khoang rộng 3 vị trí. Hạng chuồng khách mua quyết định một khoang chứa mấy con:
    // Ghép (Shared) 3, Rưỡi (Comfort) 2, Riêng (Private) 1. Một khoang chỉ chứa ngựa cùng hạng.
    // Hàm thuần để dễ viết unit test.
    public static class CompartmentRules
    {
        public static int HorsesPerCompartment(string stallClass) => stallClass switch
        {
            StallClasses.Shared => 3,
            StallClasses.Comfort => 2,
            StallClasses.Private => 1,
            _ => throw new BusinessException($"StallClass '{stallClass}' is invalid. Allowed values: {string.Join(", ", StallClasses.All)}.")
        };

        // Số khoang cần = ceil(Shared / 3) + ceil(Comfort / 2) + Private
        public static int RequiredCompartments(IEnumerable<string> stallClasses)
        {
            return stallClasses
                .GroupBy(s => s)
                .Sum(g => (g.Count() + HorsesPerCompartment(g.Key) - 1) / HorsesPerCompartment(g.Key));
        }

        // Số khoang mặc định của xe chưa khai báo: đủ chỗ cho sức chứa, mỗi khoang 3 vị trí
        public static int DefaultCompartmentCount(int capacityHorses)
        {
            int count = (capacityHorses + VehicleCompartments.PositionsPerCompartment - 1) / VehicleCompartments.PositionsPerCompartment;
            return Math.Clamp(count, 1, VehicleCompartments.MaxCompartments);
        }

        public static int ToStallSlot(int compartmentNo, int positionNo) =>
            (compartmentNo - 1) * VehicleCompartments.PositionsPerCompartment + positionNo;

        public static (int CompartmentNo, int PositionNo) FromStallSlot(int stallSlotNumber) =>
            ((stallSlotNumber - 1) / VehicleCompartments.PositionsPerCompartment + 1,
             (stallSlotNumber - 1) % VehicleCompartments.PositionsPerCompartment + 1);

        public static void Validate(TransportAsset vehicle, IReadOnlyCollection<CompartmentPlacement> horses)
        {
            if (horses.Count > vehicle.CapacityHorses)
            {
                throw new BusinessException($"Vehicle {vehicle.AssetCode} carries at most {vehicle.CapacityHorses} horse(s); this plan has {horses.Count}.");
            }

            int required = RequiredCompartments(horses.Select(h => h.StallClass));
            if (required > vehicle.CompartmentCount)
            {
                throw new BusinessException(
                    $"Vehicle {vehicle.AssetCode} has {vehicle.CompartmentCount} compartment(s) but these horses need {required} " +
                    "(a Shared compartment takes 3 horses, Comfort 2, Private 1). Choose a larger vehicle or move some horses to another trip.");
            }

            foreach (var horse in horses)
            {
                if (horse.CompartmentNo < 1 || horse.CompartmentNo > vehicle.CompartmentCount)
                {
                    throw new BusinessException(
                        $"{horse.HorseName} is placed in compartment {horse.CompartmentNo}, but vehicle {vehicle.AssetCode} has compartments 1 to {vehicle.CompartmentCount}.");
                }
                if (horse.PositionNo < 1 || horse.PositionNo > VehicleCompartments.PositionsPerCompartment)
                {
                    throw new BusinessException(
                        $"{horse.HorseName} has PositionNo {horse.PositionNo}; a compartment has positions 1 to {VehicleCompartments.PositionsPerCompartment}.");
                }
            }

            var samePosition = horses.GroupBy(h => (h.CompartmentNo, h.PositionNo)).FirstOrDefault(g => g.Count() > 1);
            if (samePosition != null)
            {
                throw new BusinessException(
                    $"{string.Join(" and ", samePosition.Select(h => h.HorseName))} cannot share position {samePosition.Key.PositionNo} of compartment {samePosition.Key.CompartmentNo}.");
            }

            foreach (var compartment in horses.GroupBy(h => h.CompartmentNo).OrderBy(g => g.Key))
            {
                // Hạng của khoang = hạng của con đứng ở vị trí nhỏ nhất
                var ordered = compartment.OrderBy(h => h.PositionNo).ToList();
                string stallClass = ordered[0].StallClass;
                var intruder = ordered.FirstOrDefault(h => h.StallClass != stallClass);
                if (intruder != null)
                {
                    throw new BusinessException(
                        $"Compartment {compartment.Key} holds {stallClass} horses; {intruder.HorseName} ({intruder.StallClass}) cannot share it.");
                }

                int limit = HorsesPerCompartment(stallClass);
                if (ordered.Count > limit)
                {
                    throw new BusinessException(
                        $"Compartment {compartment.Key} has {ordered.Count} {stallClass} horses, but a {stallClass} compartment takes at most {limit}.");
                }
            }
        }

        // Xếp lại vị trí khi đổi xe giữa đường (sự cố ReplaceVehicle): giữ thứ tự đầu vào,
        // mỗi hạng chuồng lấp đầy khoang của mình rồi sang khoang kế tiếp, bắt đầu từ firstCompartment.
        // Trả về vị trí theo đúng thứ tự của stallClasses.
        public static List<(int CompartmentNo, int PositionNo)> Pack(IReadOnlyList<string> stallClasses, int firstCompartment = 1)
        {
            var result = new (int CompartmentNo, int PositionNo)[stallClasses.Count];
            int nextCompartment = firstCompartment;

            foreach (var group in stallClasses.Select((s, i) => (StallClass: s, Index: i)).GroupBy(x => x.StallClass))
            {
                int limit = HorsesPerCompartment(group.Key);
                int position = 0;
                foreach (var item in group)
                {
                    if (position == limit)
                    {
                        nextCompartment++;
                        position = 0;
                    }
                    position++;
                    result[item.Index] = (nextCompartment, position);
                }
                nextCompartment++;
            }

            return result.ToList();
        }
    }
}
