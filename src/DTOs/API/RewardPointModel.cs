using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class RewardDetails
    {
        public int RewardId { get; set; }
        public int Deduction { get; set; }
        public int RedeemAmount { get; set; }
        public DateTime StartDate { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class RewardPointResponse
    {
        public List<RewardDetails> Rewards { get; set; } = new List<RewardDetails>();
    }

    public class PassengerReedemReward
    {
        public string PassengerId { get; set; }
        public int RewardId { get; set; }
    }

    public class PassengerReedemRewardResponse
    {
        public int RewardPoint { get; set; }
        public decimal WalletAmount { get; set; }
    }
}
