﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class RewardDetails
    {
        public string RewardId { get; set; }
        public string Deduction { get; set; }
        public string RedeemAmount { get; set; }
        public string StartDate { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public string ExpiryDate { get; set; }
    }

    public class RewardPointResponse
    {
        public List<RewardDetails> Rewards { get; set; } = new List<RewardDetails>();
    }

    public class PassengerReedemRewardRequsest
    {
        public string PassengerId { get; set; }
        public string Deduction { get; set; }
        public string RedeemAmount { get; set; }
    }

    public class PassengerReedemRewardResponse
    {
        public string RewardPoint { get; set; }
        public string WalletAmount { get; set; }
    }

    public class PassengerEarnedRewardRespose
    {
        public string RewardPoint { get; set; }
    }
}