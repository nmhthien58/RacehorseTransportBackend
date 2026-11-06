namespace RacehorseTransport.Services.Implementations
{
    // Phí đi đường vòng chia theo số ngựa còn trên xe (Chính sách 9.4). Hàm thuần để dễ viết unit test.
    public static class DetourShareRules
    {
        public static decimal Share(decimal totalExtraCost, int myHorses, int horsesOnBoard)
        {
            if (totalExtraCost <= 0 || myHorses <= 0 || horsesOnBoard <= 0)
            {
                return 0m;
            }
            return Math.Round(totalExtraCost * myHorses / horsesOnBoard, 2, MidpointRounding.AwayFromZero);
        }
    }
}
