using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ExpenseAnalyzer.Models
{
    public class OfxParser
    {
        private readonly List<Transaction> _transactions = new();
        private string? _error;

        public OfxParser() { }

        public bool Parse(string ofxContent)
        {
            _transactions.Clear();
            _error = null;
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(ofxContent))
                {
                    _error = "Input is empty.";
                    return false;
                }

                // OFX files may not be well-formed XML, so fix tags if needed
                string xmlContent = PreprocessOfxToXml(ofxContent);
                var doc = XDocument.Parse(xmlContent);

                var stmttrnrs = doc.Descendants("STMTTRNRS"); // Collection of account transactions
                if (!stmttrnrs.Any())
                {
                    _error = "No STMTTRNRS section found.";
                    return false;
                }

                foreach (var stmt in stmttrnrs)
                {
                    var stmtrs = stmt.Descendants("STMTRS").FirstOrDefault(); // Account info
                    if (stmtrs == null)
                        continue;

                    var account = ParseAccount(stmtrs);
                    var banktranlist = stmtrs.Descendants("BANKTRANLIST").FirstOrDefault();
                    if (banktranlist == null)
                        continue;

                    foreach (var trn in banktranlist.Elements("STMTTRN"))
                    {
                        var transaction = ParseTransaction(trn, account);
                        if (transaction != null)
                            _transactions.Add(transaction);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _error = $"Parsing failed: {ex.Message}";
                return false;
            }
        }

        public string? GetError() => _error;

        public IReadOnlyList<Transaction> GetTransactions() => _transactions.AsReadOnly();

        private static string PreprocessOfxToXml(string ofx)
        {
            // OFX files often have tags like <TAG>value\n, not <TAG>value</TAG>
            // Convert to well-formed XML
            string xml = Regex.Replace(ofx, @"<(\w+)>(([^<\r\n]+))", "<$1>$2</$1>");
            // Remove headers if present
            int xmlStart = xml.IndexOf("<OFX", StringComparison.OrdinalIgnoreCase);
            if (xmlStart > 0)
                xml = xml.Substring(xmlStart);
            return xml;
        }

        private static Account ParseAccount(XElement stmtrs)
        {
            var curdef = stmtrs.Element("CURDEF")?.Value ?? "";
            var acctidStr = stmtrs.Descendants("ACCTID").FirstOrDefault()?.Value ?? "0";
            long.TryParse(acctidStr, out var acctid);
            var accttypeStr = stmtrs.Descendants("ACCTTYPE").FirstOrDefault()?.Value ?? "Other";
            Enum.TryParse<AccountType>(accttypeStr, true, out var acctType);
            return new Account
            {
                Currency = curdef,
                AccountId = acctid,
                AccountType = acctType
            };
        }

        private static Transaction? ParseTransaction(XElement trn, Account account)
        {
            try
            {
                var dateStr = trn.Element("DTPOSTED")?.Value ?? "";
                var id = trn.Element("FITID")?.Value ?? "";
                var amtStr = trn.Element("TRNAMT")?.Value ?? "0";
                var typeStr = trn.Element("TRNTYPE")?.Value ?? "Unknown";
                var name = trn.Element("NAME")?.Value ?? "";
                var memo = trn.Element("MEMO")?.Value ?? "";

                DateTime.TryParse(dateStr, out var date);
                double.TryParse(amtStr, out var amount);
                Enum.TryParse<TransactionType>(typeStr, true, out var type);

                return new Transaction
                {
                    Date = date,
                    ID = id,
                    Amount = amount,
                    Type = type,
                    Name = name,
                    Memo = memo,
                    Account = account
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
