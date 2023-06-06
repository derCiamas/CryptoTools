using CryptoTools.Common.ExchangeRateProviders;
using CryptoTools.Common.Model;
using CryptoTools.Common.Model.Transactions;
using CryptoTools.Common.Settings;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoTools.Export.PortfolioPerformance
{
    public class PortfolioPerformanceExporter
    {
        private Type[] _ignoredTransactionsTypes;
        public PortfolioPerformanceExporter(Type[] ignoredTransactionsTypes = null)
        {
            _ignoredTransactionsTypes = ignoredTransactionsTypes;
        }

        public async Task<List<PortfolioPerformanceCSVLineBase>> ExportToDirectory(string directoryPath, IEnumerable<TransactionBase> transactions, IExchangeRateProvider exchangeRateProvider)
        {
            var evaluator = new PortfolioPerformanceTransactionEvaluator(exchangeRateProvider, _ignoredTransactionsTypes);
            var data = await evaluator.EvaluateTransactions(transactions);
            var depositAccountFileName = "deposits.csv";
            var depotFileName = "depottransactions.csv";

            var options = new TypeConverterOptions { Formats = new[] { "yyyy-MM-ddTHH:mm" } };
            var depositsAccountFilePath = Path.Combine(directoryPath, depositAccountFileName);
            if (File.Exists(depositsAccountFilePath))
            {
                File.Delete(depositsAccountFilePath);
            }
            using (FileStream fs = File.OpenWrite(depositsAccountFilePath))
            {
                using (var sw = new StreamWriter(fs))
                {
                    using (var csv = new CsvWriter(sw, new CsvConfiguration(CultureInfo.CurrentCulture) { HasHeaderRecord = false }))
                    {
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                        csv.WriteRecords(data.Where(e => e is PortfolioPerformanceDepositAccountCSVLine).Cast<PortfolioPerformanceDepositAccountCSVLine>());
                    }
                }
            }
            var depotFilePath = Path.Combine(directoryPath, depotFileName);
            if (File.Exists(depotFilePath))
            {
                File.Delete(depotFilePath);
            }
            using (FileStream fs = File.OpenWrite(depotFilePath))
            {
                using (var sw = new StreamWriter(fs))
                {
                    using (var csv = new CsvWriter(sw, new CsvConfiguration(CultureInfo.CurrentCulture) { HasHeaderRecord = false }))
                    {
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                        csv.WriteRecords(data.Where(e => e is PortfolioPerformanceSecurityAccountCSVLine).Cast<PortfolioPerformanceSecurityAccountCSVLine>());
                    }
                }
            }
            return data;
        }

        private class PortfolioPerformanceTransactionEvaluator
        {
            private static readonly CommonSymbol PPBaseCurrency = CommonSettings.BaseSymbol;
            private Type[] _ignoredTransactionsTypes;
            private delegate Task<List<PortfolioPerformanceCSVLineBase>> EvaluateTransactionDelegate(TransactionBase transaction);
            private static readonly List<EvaluateTransactionDelegate> Evaluators = new List<EvaluateTransactionDelegate>()
            {
                EvaluateBuySell,
                EvaluateDepositWithdrawal,
                EvaluateReward
            };
            private static IExchangeRateProvider _exchangeRateProvider;

            public PortfolioPerformanceTransactionEvaluator(IExchangeRateProvider exchangeRateProvider, Type[] ignoredTransactionsTypes)
            {
                _exchangeRateProvider = exchangeRateProvider;
                _ignoredTransactionsTypes = ignoredTransactionsTypes;
            }

            public async Task<List<PortfolioPerformanceCSVLineBase>> EvaluateTransactions(IEnumerable<TransactionBase> transactions)
            {
                var results = new List<PortfolioPerformanceCSVLineBase>();
                foreach (var transaction in transactions)
                {
                    var alreadyAdded = false;
                    foreach (var evaluator in Evaluators)
                    {
                        var data = await evaluator(transaction);
                        if (data.Count() > 0)
                        {
                            Console.WriteLine($"New transaction evaluated. Evaluator name: '{evaluator.Method.Name}', DateTime: '{transaction.Time}', Base: '{transaction.BaseSymbol}', Quoted: '{transaction.QuotedSymbol}'");
                            if (alreadyAdded)
                            {
                                throw new Exception("Same transaction has been evaluated as different type again.");
                            }
                            results.AddRange(data);
                            alreadyAdded = true;
                        }
                    }
                    if (alreadyAdded == false && !(_ignoredTransactionsTypes != null && _ignoredTransactionsTypes.Contains(transaction.GetType())))
                    {
                        //throw new Exception($"Transaction could not be evaluated: {transaction.BaseSymbol.Description}{transaction.QuotedSymbol?.Description} {transaction.Time}");
                    }
                }
                return results;
            }

            private static async Task<List<PortfolioPerformanceCSVLineBase>> EvaluateBuySell(TransactionBase transaction)
            {
                var transactionsList = new List<PortfolioPerformanceCSVLineBase>();
                if (transaction is BuySellTransaction)
                {
                    if (transaction.QuotedSymbol.Equals(PPBaseCurrency) || transaction.BaseSymbol.Equals(PPBaseCurrency))
                    {
                        var ppBaseCurrencyAmmount = transaction.QuotedSymbol.Equals(PPBaseCurrency) ? transaction.QuotedAmmount : transaction.BaseAmmount;
                        var ppCryptoAmmount = transaction.QuotedSymbol.Equals(PPBaseCurrency) ? transaction.BaseAmmount : transaction.QuotedAmmount;
                        var feeInPPBaseCurrency = 0m;
                        if (transaction.FeeSymbol != null)
                        {
                            feeInPPBaseCurrency = transaction.FeeSymbol.Equals(PPBaseCurrency) ? transaction.Fee : await _exchangeRateProvider.ExchangeSymbol(PPBaseCurrency, transaction.FeeSymbol, transaction.Fee, transaction.Time);
                        }
                        //Don't need two transaction. PP handles automatically

                        var type = ppBaseCurrencyAmmount > 0 ? PortfolioPerformanceSecurityAccountCSVLine.TransactionTypes.Sell : PortfolioPerformanceSecurityAccountCSVLine.TransactionTypes.Buy;
                        var ppSecurityTransaction = new PortfolioPerformanceSecurityAccountCSVLine()
                        {
                            Date = transaction.Time,
                            Type = type,
                            Value = type == PortfolioPerformanceSecurityAccountCSVLine.TransactionTypes.Sell ? (ppBaseCurrencyAmmount.Value + feeInPPBaseCurrency) * -1 : ppBaseCurrencyAmmount.Value * -1 + feeInPPBaseCurrency,
                            TransactionCurrency = PPBaseCurrency.Description,
                            TickerSymbol = MapTransactionToTickerSymbol(transaction),
                            SecurityName = transaction.QuotedSymbol.Equals(PPBaseCurrency) ? transaction.BaseSymbol.Description : transaction.QuotedSymbol.Description,
                            Fees = feeInPPBaseCurrency,
                            Shares = ppCryptoAmmount.Value
                        };
                        transactionsList.Add(ppSecurityTransaction);
                    }
                    else
                    {
                        //Crypto transaction
                        //1. Sell quoted to PPBaseCurrency
                        //2. Buy base with PPBaseCurrency
                        var sellValueInPPBaseCurrency = await _exchangeRateProvider.ExchangeSymbol(PPBaseCurrency, transaction.QuotedSymbol, transaction.QuotedAmmount.Value, transaction.Time);

                        var fakeSellTransaction = new BuySellTransaction()
                        {
                            BaseSymbol = PPBaseCurrency,
                            QuotedSymbol = transaction.QuotedSymbol
                        };

                        var sellTransaction = new PortfolioPerformanceSecurityAccountCSVLine()
                        {
                            Date = transaction.Time,
                            Type = PortfolioPerformanceSecurityAccountCSVLine.TransactionTypes.Sell,
                            Value = sellValueInPPBaseCurrency,
                            TransactionCurrency = PPBaseCurrency.Description,
                            TickerSymbol = MapTransactionToTickerSymbol(fakeSellTransaction),
                            SecurityName = transaction.QuotedSymbol.Description,
                            //Fee calculated only once, as this is a virtual construct (only one real transaction happened)
                            Fees = 0,
                            Shares = transaction.QuotedAmmount.Value,
                        };

                        transactionsList.Add(sellTransaction);

                        var fakeBuyTransaction = new BuySellTransaction()
                        {
                            BaseSymbol = transaction.BaseSymbol,
                            QuotedSymbol = PPBaseCurrency
                        };

                        var feeInPPBaseCurrency = await _exchangeRateProvider.ExchangeSymbol(PPBaseCurrency, transaction.QuotedSymbol, transaction.Fee, transaction.Time);
                        var buyTransaction = new PortfolioPerformanceSecurityAccountCSVLine()
                        {
                            Date = transaction.Time.AddSeconds(1),
                            Type = PortfolioPerformanceSecurityAccountCSVLine.TransactionTypes.Buy,
                            Value = sellTransaction.Value * -1 + feeInPPBaseCurrency,
                            Shares = transaction.BaseAmmount,
                            TransactionCurrency = PPBaseCurrency.Description,
                            TickerSymbol = MapTransactionToTickerSymbol(fakeBuyTransaction),
                            SecurityName = transaction.BaseSymbol.Description,
                            Fees = feeInPPBaseCurrency
                        };


                        transactionsList.Add(buyTransaction);
                    }
                }
                return transactionsList;
            }

            private static Task<List<PortfolioPerformanceCSVLineBase>> EvaluateDepositWithdrawal(TransactionBase transaction)
            {
                var transactionsList = new List<PortfolioPerformanceCSVLineBase>();
                var casted = transaction as DepositWithdrawalTransaction;
                if (casted != null)
                {
                    //Need to do it for all FIAT's
                    if (transaction.BaseSymbol.Equals(PPBaseCurrency))
                    {
                        var depositWithdrawalTransaction = new PortfolioPerformanceDepositAccountCSVLine()
                        {
                            Date = transaction.Time,
                            Value = transaction.BaseAmmount,
                            TransactionCurrency = transaction.BaseSymbol.Description,
                            Type = transaction.BaseAmmount > 0 ? PortfolioPerformanceDepositAccountCSVLine.TransactionTypes.Deposit : PortfolioPerformanceDepositAccountCSVLine.TransactionTypes.Removal
                        };
                        transactionsList.Add(depositWithdrawalTransaction);
                    }
                    else
                    {
                        var depositWithdrawalTransaction = new PortfolioPerformanceSecurityAccountCSVLine()
                        {
                            Date = transaction.Time,
                            Value = transaction.BaseAmmount,
                            TransactionCurrency = transaction.BaseSymbol.Description,
                            TickerSymbol = transaction.BaseSymbol.Description,
                            Type = transaction.BaseAmmount > 0 ? PortfolioPerformanceDepositAccountCSVLine.TransactionTypes.Deposit : PortfolioPerformanceDepositAccountCSVLine.TransactionTypes.Removal
                        };
                        transactionsList.Add(depositWithdrawalTransaction);
                    }
                   
                }
                return Task.FromResult(transactionsList);
            }

            private static Dictionary<string, List<RewardTransaction>> smallRewardsList = new Dictionary<string, List<RewardTransaction>>();
            private static async Task<List<PortfolioPerformanceCSVLineBase>> EvaluateReward(TransactionBase transaction)
            {
                var transactionsList = new List<PortfolioPerformanceCSVLineBase>();
                var casted = transaction as RewardTransaction;
                if (casted != null)
                {
                    //Gross Value needs to be filled in dividend
                    /*
                        1)Die Aktien-/Krypto-Dividende in Euro umrechnen, die Dividende in Euro eingeben
                        2)Direkt einen Kauf zum gleichen Kurs eintragen
                    */
                    var valueInPPBaseCurrency = await _exchangeRateProvider.ExchangeSymbol(PPBaseCurrency, transaction.BaseSymbol, transaction.BaseAmmount, transaction.Time);
                    var depositWithdrawalTransaction = new PortfolioPerformanceDepositAccountCSVLine()
                    {
                        Date = transaction.Time,
                        Value = valueInPPBaseCurrency,
                        TransactionCurrency = PPBaseCurrency.Description,
                        TickerSymbol = MapTransactionToTickerSymbol(transaction),
                        Type = PortfolioPerformanceDepositAccountCSVLine.TransactionTypes.Dividend
                    };


                    var securityTransaction = new PortfolioPerformanceSecurityAccountCSVLine()
                    {
                        Date = transaction.Time.AddMinutes(1),
                        Type = PortfolioPerformanceSecurityAccountCSVLine.TransactionTypes.Buy,
                        Value = depositWithdrawalTransaction.Value,
                        TransactionCurrency = PPBaseCurrency.Description,
                        TickerSymbol = MapTransactionToTickerSymbol(transaction),
                        SecurityName = transaction.BaseSymbol.Description,
                        Shares = transaction.BaseAmmount,
                    };


                    transactionsList.Add(depositWithdrawalTransaction);
                    transactionsList.Add(securityTransaction);

                }
                return transactionsList;
            }

            private static string MapTransactionToTickerSymbol(TransactionBase transaction)
            {
                if (transaction.Type == TransactionBase.TransactionType.Reward && transaction.BaseSymbol.Equals(Symbol.EUR))
                {
                    return Symbol.EUR.Description;
                }
                var cryptoSymbol = transaction.BaseSymbol == PPBaseCurrency ? transaction.QuotedSymbol : transaction.BaseSymbol;
                return cryptoSymbol.Description;
                //return CryptoTax.Common.Settings.CommonSettings.SymbolToTickerSymbolMappings.Single(s => s.SymbolDescription == cryptoSymbol.Description).Ticker;
            }
        }



        public abstract class PortfolioPerformanceCSVLineBase
        {
            public static (PortfolioPerformanceDepositAccountCSVLine DepositAccountLine, PortfolioPerformanceSecurityAccountCSVLine SecurityAccountLine) CreateLinePairForTransaction(string bla)
            {
                throw new NotImplementedException();
            }
        }

        public class PortfolioPerformanceSecurityAccountCSVLine : PortfolioPerformanceCSVLineBase
        {
            //Date,Type,Value,Transaction Currency,Gross Amount,Currency Gross Amount,Exchange Rate,Fees,Taxes,Shares,ISIN,WKN,Ticker Symbol,Security Name,Note
            public class TransactionTypes
            {
                public static readonly string TransferInbound = "Transfer (Inbound)";
                public static readonly string TransferOutbound = "Transfer (Outbound)";
                public static readonly string Buy = "Buy";
                public static readonly string Sell = "Sell";
                public static readonly string Deposit = "Deposit";
                public static readonly string Removal = "Removal";
            }
            [Index(0)]
            public DateTime Date { get; set; }
            [Index(1)]
            public string Type { get; set; }
            [Index(2)]
            public decimal Value { get; set; }
            [Index(3)]
            public string TransactionCurrency { get; set; }
            [Index(4)]
            public decimal GrossAmount { get; set; }
            [Index(5)]
            public decimal CurrencyGrossAmount { get; set; }
            [Index(6)]
            public decimal ExchangeRate { get; set; }
            [Index(7)]
            public decimal Fees { get; set; }
            [Index(8)]
            public decimal Taxes { get; set; }
            [Index(9)]
            public decimal Shares { get; set; }
            [Index(10)]
            public string ISIN { get; set; }
            [Index(11)]
            public string WKN { get; set; }
            [Index(12)]
            public string TickerSymbol { get; set; }
            [Index(13)]
            public string SecurityName { get; set; }
            [Index(14)]
            public string Note { get; set; }

        }

        public class PortfolioPerformanceDepositAccountCSVLine : PortfolioPerformanceCSVLineBase
        {
            public class TransactionTypes
            {
                public static readonly string Buy = "Buy";
                public static readonly string Sell = "Sell";
                public static readonly string Deposit = "Deposit";
                public static readonly string Removal = "Removal";
                public static readonly string Fee = "Fee";
                public static readonly string Dividend = "Dividend";
            }
            [Index(0)]
            public DateTime Date { get; set; }
            [Index(1)]
            public string Type { get; set; }
            [Index(2)]
            public decimal Value { get; set; }
            [Index(3)]
            public string TransactionCurrency { get; set; }
            [Index(4)]
            public decimal Taxes { get; set; }
            [Index(5)]
            public decimal Shares { get; set; }
            [Index(6)]
            public string ISIN { get; set; }
            [Index(7)]
            public string WKN { get; set; }
            [Index(8)]
            public string TickerSymbol { get; set; }
            [Index(9)]
            public string SecurityName { get; set; }
            [Index(10)]
            public string Note { get; set; }

        }
    }
}
