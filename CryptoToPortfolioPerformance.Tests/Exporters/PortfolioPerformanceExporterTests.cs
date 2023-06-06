using CryptoTools.Common.Model.Transactions;
using CryptoTools.Export.PortfolioPerformance;
using CryptoTools.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoTools.Tests.Exporters
{
    [TestClass]
    public class PortfolioPerformanceExporterTests
    {

        [TestMethod]
        public async Task BuySellEvaluatedCorrectly()
        {
            //Original Securities Line: 2021-01-18T00:00,Buy,25.04,EUR,,,,0.04,0.00,0.770416,,,XMLNZEUR,MLN,
            //Original Deposit Line: 2021-01-18T00:00,Buy,-25.04,EUR,,,,,XMLNZEUR,MLN,

            var rateProvider = new TestExchangeRateProvider();
            rateProvider.ExchangeRates.Add((Common.Model.CommonSymbol.MLN.Description, Common.Model.CommonSymbol.EUR.Description), 32.45m);
            rateProvider.ExchangeRates.Add((Common.Model.CommonSymbol.EUR.Description, Common.Model.CommonSymbol.MLN.Description), 1 / 32.45m);
            var testTransaction = new BuySellTransaction()
            {
                BaseAmmount = 0.770416m, //10,
                BaseSymbol = Common.Model.CommonSymbol.MLN,
                QuotedAmmount = -25, //10
                QuotedSymbol = Common.Model.CommonSymbol.EUR,
                Time = new DateTime(2021, 1, 18),
                Fee = 0.04m,
                FeeSymbol = Common.Model.CommonSymbol.EUR
            };
            var exporter = new PortfolioPerformanceExporter();
            var results = await exporter.ExportToDirectory(@"C:\Test\PP", new List<TransactionBase>() { testTransaction }, rateProvider);
            Assert.IsTrue(results.Count == 1);
            var transaction = results[0] as PortfolioPerformanceExporter.PortfolioPerformanceSecurityAccountCSVLine;
            Assert.IsTrue(transaction.Value == testTransaction.QuotedAmmount * -1 + testTransaction.Fee);
            Assert.IsTrue(transaction.Fees == testTransaction.Fee);
            Assert.IsTrue(transaction.TransactionCurrency == Common.Model.CommonSymbol.EUR.Description);
            Assert.IsTrue(transaction.Type == "Buy");
        }

        [TestMethod]
        public async Task DepositEvaluatedCorrectly()
        {
            var depositTransaction = new DepositWithdrawalTransaction(10, Common.Model.CommonSymbol.EUR, new DateTime(2021, 1, 18));
            var exporter = new PortfolioPerformanceExporter();
            var results = await exporter.ExportToDirectory(@"C:\Test\PP", new List<TransactionBase>() { depositTransaction }, new TestExchangeRateProvider());
            var transaction = results[0] as PortfolioPerformanceExporter.PortfolioPerformanceDepositAccountCSVLine;
            Assert.IsTrue(results.Count == 1);
            Assert.IsNotNull(transaction);
            Assert.IsTrue(transaction.Value == depositTransaction.BaseAmmount);
            Assert.IsTrue(transaction.TransactionCurrency == Common.Model.CommonSymbol.EUR.Description);
            Assert.IsTrue(transaction.Type == PortfolioPerformanceExporter.PortfolioPerformanceDepositAccountCSVLine.TransactionTypes.Deposit);
        }

        [TestMethod]
        public async Task WithdaralEvaluatedCorrectly()
        {
            var depositTransaction = new DepositWithdrawalTransaction(-10, Common.Model.CommonSymbol.EUR, new DateTime(2021, 1, 18));
            var exporter = new PortfolioPerformanceExporter();
            var results = await exporter.ExportToDirectory(@"C:\Test\PP", new List<TransactionBase>() { depositTransaction }, new TestExchangeRateProvider());
            var transaction = results[0] as PortfolioPerformanceExporter.PortfolioPerformanceDepositAccountCSVLine;
            Assert.IsTrue(results.Count == 1);
            Assert.IsNotNull(transaction);
            Assert.IsTrue(transaction.Value == depositTransaction.BaseAmmount);
            Assert.IsTrue(transaction.TransactionCurrency == Common.Model.CommonSymbol.EUR.Description);
            Assert.IsTrue(transaction.Type == PortfolioPerformanceExporter.PortfolioPerformanceDepositAccountCSVLine.TransactionTypes.Removal);
        }

        [TestMethod]
        public async Task RewardEvaluatedCorrectly()
        {
            var rateProvider = new TestExchangeRateProvider();
            rateProvider.ExchangeRates.Add((Common.Model.CommonSymbol.ADA.Description, Common.Model.CommonSymbol.EUR.Description), 10);
            rateProvider.ExchangeRates.Add((Common.Model.CommonSymbol.EUR.Description, Common.Model.CommonSymbol.ADA.Description), 1);
            var rewardTransaction = new RewardTransaction()
            {
                BaseAmmount = 10,
                BaseSymbol = Common.Model.CommonSymbol.ADA,
                Time = new DateTime(2021, 1, 18)
            };
            var exporter = new PortfolioPerformanceExporter();
            var results = await exporter.ExportToDirectory(@"C:\Test\PP", new List<TransactionBase>() { rewardTransaction }, rateProvider);
            /*
                1)Die Aktien-/Krypto-Dividende in Euro umrechnen, die Dividende in Euro eingeben
                2)Direkt einen Kauf zum gleichen Kurs eintragen
            */
            Assert.IsTrue(results.Count == 2);

            var dividendetransaction = results[0] as PortfolioPerformanceExporter.PortfolioPerformanceDepositAccountCSVLine;
            var buytransaction = results[1] as PortfolioPerformanceExporter.PortfolioPerformanceSecurityAccountCSVLine;

            Assert.IsTrue(dividendetransaction.Type == PortfolioPerformanceExporter.PortfolioPerformanceDepositAccountCSVLine.TransactionTypes.Dividend);
            Assert.IsNotNull(dividendetransaction.Value == await rateProvider.ExchangeSymbol(Common.Model.CommonSymbol.EUR, Common.Model.CommonSymbol.ADA, rewardTransaction.BaseAmmount, rewardTransaction.Time));

            Assert.IsTrue(buytransaction.Value == dividendetransaction.Value);
            Assert.IsTrue(buytransaction.Type == PortfolioPerformanceExporter.PortfolioPerformanceSecurityAccountCSVLine.TransactionTypes.Buy);
            Assert.IsTrue(buytransaction.Shares == rewardTransaction.BaseAmmount);
        }

        [TestMethod]
        public async Task CryptoTransactionEvaluatedCorrectly()
        {
            var rateProvider = new TestExchangeRateProvider();
            rateProvider.ExchangeRates.Add((Common.Model.CommonSymbol.ADA.Description, Common.Model.CommonSymbol.EUR.Description), 10);
            rateProvider.ExchangeRates.Add((Common.Model.CommonSymbol.ALGORAND.Description, Common.Model.CommonSymbol.EUR.Description), 5);
            rateProvider.ExchangeRates.Add((Common.Model.CommonSymbol.EUR.Description, Common.Model.CommonSymbol.ADA.Description), 0.1m);
            rateProvider.ExchangeRates.Add((Common.Model.CommonSymbol.EUR.Description, Common.Model.CommonSymbol.ALGORAND.Description), 0.2m);


            var testTransaction = new BuySellTransaction()
            {
                BaseAmmount = 1, //10,
                BaseSymbol = Common.Model.CommonSymbol.ADA,
                QuotedAmmount = -2, //10
                QuotedSymbol = Common.Model.CommonSymbol.ALGORAND,
                Time = DateTime.UtcNow,
                Fee = 1
            };

            var exporter = new PortfolioPerformanceExporter();
            var results = (await exporter.ExportToDirectory(@"C:\Test\PP", new List<TransactionBase>() { testTransaction }, rateProvider)).Cast<PortfolioPerformanceExporter.PortfolioPerformanceSecurityAccountCSVLine>();
            //Transaction count
            Assert.IsTrue(results.Count() == 2);
            var sell = results.ElementAt(0);
            var buy = results.ElementAt(1);

            //Transaction type
            Assert.IsTrue(sell.Type == PortfolioPerformanceExporter.PortfolioPerformanceSecurityAccountCSVLine.TransactionTypes.Sell);
            Assert.IsTrue(buy.Type == PortfolioPerformanceExporter.PortfolioPerformanceSecurityAccountCSVLine.TransactionTypes.Buy);

            //Currency
            Assert.IsTrue(sell.TransactionCurrency == Common.Model.CommonSymbol.EUR.Description);
            Assert.IsTrue(buy.TransactionCurrency == Common.Model.CommonSymbol.EUR.Description);

            //Fees
            Assert.IsTrue(results.Count(r => r.Fees > 0) == 1);

            //Values
            Assert.IsTrue(sell.Value == -10);
            Assert.IsTrue(sell.Fees == 0);

            //Values
            var fees = await rateProvider.ExchangeSymbol(Common.Model.CommonSymbol.EUR, Common.Model.CommonSymbol.ALGORAND, testTransaction.Fee, testTransaction.Time);
            Assert.IsTrue(buy.Value == sell.Value * -1 + fees);
            Assert.IsTrue(buy.Fees == fees);
        }
    }
}
