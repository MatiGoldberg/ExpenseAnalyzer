using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Schema;

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

                // Remove OFX headers if present
                int xmlStart = ofxContent.IndexOf("<OFX", StringComparison.OrdinalIgnoreCase);
                if (xmlStart > 0)
                {
                    string ofxHeader = ofxContent.Substring(0, xmlStart);
                    if (!ValidateHeader(ofxHeader))
                    {
                        _error = "Unsupported OFX format.";
                        return false;
                    }
                    ofxContent = ofxContent.Substring(xmlStart);
                }
                else
                {
                    _error = "Invalid OFX format.";
                    return false;
                }

                // Convert SGML to XML (close tags)
                string xmlContent = PreprocessSgmlToXml(ofxContent);
                if (string.IsNullOrEmpty(xmlContent))
                {
                    _error = "Failed to preprocess SGML to XML.";
                    return false;
                }

                var doc = XDocument.Parse(xmlContent);

                var stmttrnrs = doc.Descendants().Where(x => x.Name.LocalName.ToUpper() == "STMTTRNRS");
                if (!stmttrnrs.Any())
                {
                    _error = "No STMTTRNRS section found.";
                    return false;
                }

                foreach (var stmt in stmttrnrs)
                {
                    var stmtrs = stmt.Descendants().FirstOrDefault(x => x.Name.LocalName.ToUpper() == "STMTRS");
                    if (stmtrs == null)
                        continue;

                    var account = ParseAccount(stmtrs);
                    var banktranlist = stmtrs.Descendants().FirstOrDefault(x => x.Name.LocalName.ToUpper() == "BANKTRANLIST");
                    if (banktranlist == null)
                        continue;

                    foreach (var trn in banktranlist.Elements().Where(x => x.Name.LocalName.ToUpper() == "STMTTRN"))
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

        public bool ValidateHeader(string ofxHeader)
        {
            // Expected fields and values
            var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"OFXHEADER", "100"},
                {"DATA", "OFXSGML"},
                {"VERSION", "102"},
                {"SECURITY", "NONE"},
                {"ENCODING", "USASCII"},
                {"CHARSET", "1252"},
                {"COMPRESSION", "NONE"},
                {"OLDFILEUID", "NONE"},
                {"NEWFILEUID", "NONE"}
            };

            var lines = ofxHeader.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    found[parts[0].Trim()] = parts[1].Trim();
                }
            }

            foreach (var kvp in expected)
            {
                if (!found.TryGetValue(kvp.Key, out var value) || !string.Equals(value, kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        private static string PreprocessSgmlToXml(string ofx)
        {
            // TODO: Fix this

            
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
            var curdef = stmtrs.Descendants().FirstOrDefault(x => x.Name.LocalName.ToUpper() == "CURDEF")?.Value ?? "";
            var bankacctfrom = stmtrs.Descendants().FirstOrDefault(x => x.Name.LocalName.ToUpper() == "BANKACCTFROM");
            string acctidStr = "0", accttypeStr = "Other";
            if (bankacctfrom != null)
            {
                acctidStr = bankacctfrom.Descendants().FirstOrDefault(x => x.Name.LocalName.ToUpper() == "ACCTID")?.Value ?? "0";
                accttypeStr = bankacctfrom.Descendants().FirstOrDefault(x => x.Name.LocalName.ToUpper() == "ACCTTYPE")?.Value ?? "Other";
            }
            else
            {
                acctidStr = stmtrs.Descendants().FirstOrDefault(x => x.Name.LocalName.ToUpper() == "ACCTID")?.Value ?? "0";
                accttypeStr = stmtrs.Descendants().FirstOrDefault(x => x.Name.LocalName.ToUpper() == "ACCTTYPE")?.Value ?? "Other";
            }
            long.TryParse(acctidStr, out var acctid);
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
                string Get(string tag) => trn.Elements().FirstOrDefault(x => x.Name.LocalName.ToUpper() == tag)?.Value ?? "";
                var dateStr = Get("DTPOSTED");
                var id = Get("FITID");
                var amtStr = Get("TRNAMT");
                var typeStr = Get("TRNTYPE");
                var name = Get("NAME");
                var memo = Get("MEMO");

                // OFX date: YYYYMMDD or YYYYMMDDHHMMSS.fff[tz]
                DateTime date = ParseOfxDate(dateStr);
                double.TryParse(amtStr.Replace(",", ""), out var amount);
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

        private static DateTime ParseOfxDate(string dateStr)
        {
            // Remove timezone if present
            int bracket = dateStr.IndexOf('[');
            if (bracket > 0)
                dateStr = dateStr.Substring(0, bracket);
            // Remove . and everything after
            int dot = dateStr.IndexOf('.');
            if (dot > 0)
                dateStr = dateStr.Substring(0, dot);
            // Parse
            if (DateTime.TryParseExact(dateStr, new[] { "yyyyMMddHHmmss", "yyyyMMdd" }, null, System.Globalization.DateTimeStyles.None, out var dt))
                return dt;
            return DateTime.MinValue;
        }
    }
}
