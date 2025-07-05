using System;

namespace ExpenseAnalyzer.Models
{
    public enum TransactionType
    {
        Unknown = 0,
        Credit = 1,
        Debit = 2
    }

    public class Transaction
    {
        public DateTime Date { get; set; } // DTPOSTED
        public required string ID { get; set; }      // FITID
        public required double Amount { get; set; }  // TRNAMT
        public required TransactionType Type { get; set; } // TRNTYPE
        public required string Name { get; set; } // NAME
        public required string Memo { get; set; } // MEMO
        public required Account Account { get; set; } // Associated account
        public  ExpenseCategory? Category { get; set; } // Associated category
    }
}
