using ExpenseAnalyzer.Models;
using System.Collections.Generic;
using System.Text;

namespace ExpenseAnalyzer.Tests
{
    public class OfxBuilder
    {

        private static string OfxHeader =
            @"OFXHEADER:100
DATA:OFXSGML
VERSION:102
SECURITY:NONE
ENCODING:USASCII
CHARSET:1252
COMPRESSION:NONE
OLDFILEUID:NONE
NEWFILEUID:NONE
";

        private readonly List<string> _sections = new();
        private StringBuilder _currentSection;

        public OfxBuilder()
        {
            _sections = new List<string>();
            _currentSection = new StringBuilder();
        }

        public OfxBuilder AddSection() // STMTTRNRS
        {
            if (_currentSection.Length > 0)
            {
                _sections.Add(_currentSection.ToString());
                _currentSection = new StringBuilder();
            }
            return this;
        }

        public OfxBuilder SetAccount(Account account)
        {
            _currentSection.AppendLine($"\t\t\t\t<CURDEF>{account.Currency}");
            _currentSection.AppendLine("\t\t\t\t<BANKACCTFROM>");
            _currentSection.AppendLine($"\t\t\t\t\t<BANKID>USA"); // Removed: Account does not have BankId
            _currentSection.AppendLine($"\t\t\t\t\t<ACCTID>{account.AccountId}");
            _currentSection.AppendLine($"\t\t\t\t\t<ACCTTYPE>{account.AccountType}");
            _currentSection.AppendLine("\t\t\t\t</BANKACCTFROM>");
            return this;
        }

        public OfxBuilder AddTransactions(IEnumerable<Transaction> transactions)
        {
            _currentSection.AppendLine("\t\t\t\t<BANKTRANLIST>");
            foreach (var tx in transactions)
            {
                AddTransaction(tx);
            }
            _currentSection.AppendLine("\t\t\t\t</BANKTRANLIST>");
            return this;
        }

        private OfxBuilder AddTransaction(Transaction tx)
        {
            _currentSection.AppendLine("\t\t\t\t\t<STMTTRN>");
            _currentSection.AppendLine($"\t\t\t\t\t\t<TRNTYPE>{tx.Type}");
            _currentSection.AppendLine($"\t\t\t\t\t\t<DTPOSTED>{tx.Date:yyyyMMddHHmmss}");
            _currentSection.AppendLine($"\t\t\t\t\t\t<TRNAMT>{tx.Amount}");
            _currentSection.AppendLine($"\t\t\t\t\t\t<FITID>{tx.ID}");
            if (!string.IsNullOrEmpty(tx.Name))
                _currentSection.AppendLine($"\t\t\t\t\t\t<NAME>{tx.Name}");
            if (!string.IsNullOrEmpty(tx.Memo))
                _currentSection.AppendLine($"\t\t\t\t\t\t<MEMO>{tx.Memo}");
            _currentSection.AppendLine("\t\t\t\t\t</STMTTRN>");
            return this;
        }

        public string Build()
        {
            if (_currentSection.Length > 0)
                _sections.Add(_currentSection.ToString());
            var sb = new StringBuilder();
            sb.AppendLine(OfxHeader);
            sb.AppendLine("<OFX>");
            sb.AppendLine("\t<BANKMSGSRSV1>");
            foreach (var section in _sections)
            {
                sb.AppendLine("\t\t<STMTTRNRS>");
                sb.AppendLine("\t\t\t<STMTRS>");
                sb.Append(section);
                sb.AppendLine("\t\t\t</STMTRS>");
                sb.AppendLine("\t\t</STMTTRNRS>");
            }
            sb.AppendLine("\t</BANKMSGSRSV1>");
            sb.AppendLine("</OFX>");
            return sb.ToString();
        }
    }
}
