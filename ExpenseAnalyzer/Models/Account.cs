using System;

namespace ExpenseAnalyzer.Models
{
    public enum AccountType
    {
        Checking,
        Savings,
        Creditline,
        Other
    }

    public class Account
    {
        public required string Currency { get; set; } // CURDEF
        public long AccountId { get; set; }   // ACCTID
        public AccountType AccountType { get; set; } // ACCTTYPE
    }
}
