using System;
using Xunit;
using ExpenseAnalyzer.Models;
using System.Collections.Generic;
using ExpenseAnalyzer.Tests;

namespace ExpenseAnalyzer.Tests
{
    public class OfxParserTests
    {
        [Fact]
        public void OfxBuilderTest()
        {
            var builder = new OfxBuilder();
            builder.AddSection()
                   .SetAccount(OfxBuilderSamples.SampleAccount)
                   .AddTransactions(new[] { OfxBuilderSamples.SampleTransaction, OfxBuilderSamples.SampleTransaction });
            string ofx = builder.Build();
            File.WriteAllText("/Users/matigoldberg/Drive/Code/ExpenseAnalyzer/ExpenseAnalyzer/test.ofx", ofx);
        }

        [Fact]
        public void Parse_ReturnsFalse_OnEmptyInput()
        {
            var parser = new OfxParser();
            bool result = parser.Parse("");
            Assert.False(result);
            Assert.Equal("Input is empty.", parser.GetError());
        }

        [Fact]
        public void Parse_ReturnsFalse_EmptyOfx()
        {
            var parser = new OfxParser();
            var builder = new OfxBuilder();
            builder.AddSection();
            string emptyOfx = builder.Build();
            bool result = parser.Parse(emptyOfx);
            Assert.False(result);
            Assert.Equal("No STMTTRNRS section found.", parser.GetError());
        }

        [Fact]
        public void Parse_SingleSectionSingleTransaction()
        {
            var account = OfxBuilderSamples.SampleAccount;
            var transaction = OfxBuilderSamples.SampleTransaction;
            var builder = new OfxBuilder();
            builder.AddSection()
                   .SetAccount(account)
                   .AddTransactions(new[] { transaction });
            string ofx = builder.Build();
            var parser = new OfxParser();
            bool result = parser.Parse(ofx);
            Assert.True(result);
            var txs = parser.GetTransactions();
            Assert.Single(txs);
            var tx = txs[0];
            Assert.Equal(transaction.ID, tx.ID);
            Assert.Equal(transaction.Amount, tx.Amount);
            Assert.Equal(transaction.Type, tx.Type);
            Assert.Equal(transaction.Name, tx.Name);
            Assert.Equal(transaction.Memo, tx.Memo);
            Assert.NotNull(tx.Account);
            Assert.Equal(account.Currency, tx.Account.Currency);
            Assert.Equal(account.AccountId, tx.Account.AccountId);
            Assert.Equal(account.AccountType, tx.Account.AccountType);
        }

        [Fact]
        public void Parse_ParsesMultipleTransactions()
        {
            var account = OfxBuilderSamples.SampleAccount;
            var transaction1 = OfxBuilderSamples.SampleTransaction;
            var transaction2 = new Transaction
            {
                Date = new DateTime(2025, 1, 2),
                ID = "TXN1002",
                Amount = -100.13,
                Type = TransactionType.Credit,
                Name = "Spending at Store",
                Memo = "N/A",
                Account = account
            };
            var builder = new OfxBuilder();
            builder.AddSection()
                   .SetAccount(account)
                   .AddTransactions(new[] { transaction1, transaction2 });
            string ofx = builder.Build();
            var parser = new OfxParser();
            bool result = parser.Parse(ofx);
            Assert.True(result);
            var txs = parser.GetTransactions();
            Assert.Equal(2, txs.Count);
            var tx1 = txs[0];
            Assert.Equal(transaction1.ID, tx1.ID);
            Assert.Equal(transaction1.Amount, tx1.Amount);
            Assert.Equal(transaction1.Type, tx1.Type);
            Assert.Equal(transaction1.Name, tx1.Name);
            Assert.Equal(transaction1.Memo, tx1.Memo);
            Assert.NotNull(tx1.Account);
            Assert.Equal(account.Currency, tx1.Account.Currency);
            Assert.Equal(account.AccountId, tx1.Account.AccountId);
            Assert.Equal(account.AccountType, tx1.Account.AccountType);
        }

        [Fact]
        public void Parse_HandlesMissingOptionalFields()
        {
            var account = new Account
            {
                AccountId = 111222333,
                AccountType = AccountType.Checking,
                Currency = "USD"
            };
            var transaction = new Transaction
            {
                Date = new DateTime(2025, 7, 1),
                ID = "TXN3",
                Amount = -50.00,
                Type = TransactionType.Debit,
                Name = string.Empty,
                Memo = string.Empty,
                Account = account
            };
            var builder = new OfxBuilder();
            builder.AddSection()
                   .SetAccount(account)
                   .AddTransactions(new[] { transaction });
            string ofx = builder.Build();
            var parser = new OfxParser();
            bool result = parser.Parse(ofx);
            Assert.True(result);
            var txs = parser.GetTransactions();
            Assert.Single(txs);
            var tx = txs[0];
            Assert.Equal("TXN3", tx.ID);
            Assert.Equal(-50.00, tx.Amount);
            Assert.Equal(TransactionType.Debit, tx.Type);
            Assert.True(string.IsNullOrEmpty(tx.Name));
            Assert.True(string.IsNullOrEmpty(tx.Memo));
        }

        [Fact]
        public void Parse_ReturnsFalse_OnInvalidXml()
        {
            var parser = new OfxParser();
            // Build an invalid OFX (missing <OFX> root)
            var builder = new OfxBuilder();
            string invalidOfx = "not xml" + builder.Build();
            bool result = parser.Parse(invalidOfx);
            Assert.False(result);
            Assert.Contains("Invalid OFX format", parser.GetError());
        }

        [Fact]
        public void Parse_ReturnsFalse_WhenNoStmttrnrs()
        {
            var parser = new OfxParser();
            // Build OFX with no STMTTRNRS section
            var builder = new OfxBuilder();
            builder.AddSection();
            string ofx = builder.Build();
            bool result = parser.Parse(ofx);
            Assert.False(result);
            Assert.Equal("No STMTTRNRS section found.", parser.GetError());
        }


    }

    public static class OfxBuilderSamples
    {
            public static Account SampleAccount = new Account
            {
                AccountId = 1234567890,
                AccountType = AccountType.Checking,
                Currency = "USD"
            };
            public static Transaction SampleTransaction = new Transaction
            {
                Date = new DateTime(2025, 1, 1),
                ID = "TXN1001",
                Amount = 1000.01,
                Type = TransactionType.Credit,
                Name = "ACH Deposit From Source",
                Memo = "Memo Note",
                Account = SampleAccount
            };
    }

    public static class OfxParserSamples
    {
        public static string OfxHeader =
            @"OFXHEADER:100
            DATA:OFXSGML
            VERSION:102
            SECURITY:NONE
            ENCODING:USASCII
            CHARSET:1252
            COMPRESSION:NONE
            OLDFILEUID:NONE
            NEWFILEUID:NONE";

        public static string EmptyOfx = OfxHeader + "\n" + @"<OFX></OFX>";

        public static string SingleTransaction = OfxHeader + "\n" + @"<OFX>
    <BANKMSGSRSV1>
        <STMTTRNRS>
            <TRNUID>1
            <STATUS>
                <CODE>0
                <SEVERITY>INFO
            </STATUS>
            <STMTRS>
                <CURDEF>USD
                <BANKACCTFROM>
                    <BANKID>USA
                    <ACCTID>1234567890
                    <ACCTTYPE>CHECKING
                </BANKACCTFROM>

                <BANKTRANLIST>
                    <DTSTART>20250101
                    <DTEND>20251231
                    <STMTTRN>
                        <TRNTYPE>CREDIT
                        <DTPOSTED>20250101000000.000[-08:PST]
                        <TRNAMT>1000.01

                        <FITID>20250101 3791312 729,121 252,504,282,646
                        <NAME>ACH Deposit From Source
                        <MEMO>Memo Note
                    </STMTTRN>
                </BANKTRANLIST>
            </STMTRS>
        </STMTTRNRS>
    </BANKMSGSRSV1>
</OFX>";

        public static string MultipleTransactions = OfxHeader + "\n" + @"<OFX>
    <BANKMSGSRSV1>
        <STMTTRNRS>
            <TRNUID>1
            <STATUS>
                <CODE>0
                <SEVERITY>INFO
            </STATUS>
            <STMTRS>
                <CURDEF>USD
                <BANKACCTFROM>
                    <BANKID>USA
                    <ACCTID>1234567890
                    <ACCTTYPE>CHECKING
                </BANKACCTFROM>

                <BANKTRANLIST>
                    <DTSTART>20250101
                    <DTEND>20251231
                    <STMTTRN>
                        <TRNTYPE>CREDIT
                        <DTPOSTED>20250101000000.000[-08:PST]
                        <TRNAMT>1000.01

                        <FITID>20250101 3791312 729,121 252,504,282,646
                        <NAME>ACH Deposit From Source
                        <MEMO>Memo Note
                    </STMTTRN>
                                        <STMTTRN>
                        <TRNTYPE>CREDIT
                        <DTPOSTED>20250102000000.000[-08:PST]
                        <TRNAMT>-100.77

                        <FITID>20250102 3721412 149,131 272,554,282,646
                        <NAME>Purchase at Store
                        <MEMO>Store Purchase Memo
                    </STMTTRN>
                    <STMTTRN>
                        <TRNTYPE>CREDIT
                        <DTPOSTED>20250103000000.000[-08:PST]
                        <TRNAMT>-201.33

                        <FITID>20250103 3794832 329,159 281,563,212,699
                        <NAME>Purchase at Store 2
                        <MEMO>N/A
                    </STMTTRN>
                </BANKTRANLIST>
            </STMTRS>
        </STMTTRNRS>
    </BANKMSGSRSV1>
</OFX>";

    }
}
