using CryptoTools.Common.Model;
using CryptoTools.Common.Model.Transactions;
using CryptoTools.Parsing.Parsing;
using CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Models;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Parsing.Kraken
{
    public class KrakenCSVParser : CSVParserBase
    {
        private static Dictionary<string, string> _krakenLedgerToSymbolMapping;
        private readonly string _symbolMappingsFilePath;
        public KrakenCSVParser(string symbolMappingsFilePath)
        {
            if(string.IsNullOrEmpty(symbolMappingsFilePath) || !System.IO.File.Exists(symbolMappingsFilePath))
            {
                throw new ArgumentException($"No {nameof(symbolMappingsFilePath)} provided or the file does not exist");
            }
            _symbolMappingsFilePath = symbolMappingsFilePath; 
        }

        private void InitializeMappings()
        {
            if (!string.IsNullOrEmpty(_symbolMappingsFilePath))
            {
                var stringValue = File.ReadAllText(_symbolMappingsFilePath);
                var mappings = Newtonsoft.Json.JsonConvert.DeserializeObject<List<KrakenLedgerDataToSymbolMapping>>(stringValue);
                _krakenLedgerToSymbolMapping = new Dictionary<string, string>();
                foreach (var mapping in mappings)
                {
                    foreach (var krakenInputValue in mapping.KrakenInputValues)
                    {
                        _krakenLedgerToSymbolMapping.Add(krakenInputValue, mapping.Symbol);
                    }
                }
            }
        }

        public override async Task<IEnumerable<TransactionBase>> ParseFile(string filePath)
        {
            InitializeMappings();
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"Path {filePath} does not exist.");
            }
            List<ToBeEvaluatedItem> ledgerItems = new List<ToBeEvaluatedItem>();
            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.CreateSpecificCulture("en-US")))
                {
                    await foreach (var item in csv.GetRecordsAsync<KrakenLedgerItem>())
                    {
                        ledgerItems.Add(new ToBeEvaluatedItem(item));
                    }
                }
            }
            var transactions = new List<TransactionBase>();

            foreach (var item in ledgerItems)
            {
                var el = TransactionEvaluators.Evaluate(item, ref ledgerItems);
                if (el != null)
                {
                    transactions.Add(el);
                }
            }
            if(ledgerItems.Count(e => !e.AlreadyUsed) != 0)
            {
                Console.WriteLine("WARNING! Some transactions have not been included in the export data cause these could not be evaluated. This might happen if, for example, you have exported the ledger shortly after receiving the staking rewards but before the funds were restaked automatically. Try re-exporting the ledger again and running the app.");
            }
            return transactions;
        }

        private class ToBeEvaluatedItem
        {
            public KrakenLedgerItem Item { get; private set; }
            public bool AlreadyUsed { get; set; }

            public ToBeEvaluatedItem(KrakenLedgerItem item)
            {
                Item = item;
                AlreadyUsed = false;
            }
        }
        private class TransactionEvaluators
        {
            private delegate TransactionBase EvaluationDelegate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems);

            private static readonly List<EvaluationDelegate> Evaluators = new List<EvaluationDelegate>()
            {
                DepositEvaluator,
                TradeEvaluator,
                SpotBuyEvaluator,
                WithdrawalEvaluator,
                StakeEvaluator,
                StakeRewardEvaluator,
                UnstakeEvaluator
            };

            public static TransactionBase Evaluate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
            {
                if (evaluationItem.AlreadyUsed)
                {
                    return null;
                }
                foreach (var evaluator in Evaluators)
                {
                    var result = evaluator(evaluationItem, ref allItems);
                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            }

            /// <summary>
            /// Deposit: 2 x 'deposit' type, same ammount, same refid
            /// </summary>
            /// <param name="evaluationItem"></param>
            /// <param name="allItems"></param>
            /// <returns></returns>
            public static TransactionBase DepositEvaluator(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
            {
                var item = evaluationItem.Item;
                if (item.Type != KrakenLedgerItemTransactionTypes.Deposit)
                {
                    return null;
                }
                var otherItems = allItems.Where(i => !i.AlreadyUsed && i.Item.RefId == item.RefId && i.Item != item);
                if (otherItems.Count() != 1 || otherItems.FirstOrDefault()?.Item.Type != KrakenLedgerItemTransactionTypes.Deposit)
                {
                    return null;
                }
                evaluationItem.AlreadyUsed = true;
                otherItems.ToList().ForEach(e => e.AlreadyUsed = true);
                var transaction = new DepositWithdrawalTransaction(
                        item.Amount,
                        SymbolFromKrakenString(item.Asset),
                        item.Time
                    );
                return transaction;
            }

            /// <summary>
            /// - 2 x 'trade' type same refid
            /// - 1st => what is being spent
            /// - 2nd => what is being received
            /// </summary>
            /// <param name="evaluationItem"></param>
            /// <param name="allItems"></param>
            /// <returns></returns>
            public static TransactionBase TradeEvaluator(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
            {
                var item = evaluationItem.Item;
                if (item.Type != KrakenLedgerItemTransactionTypes.Trade)
                {
                    return null;
                }
                var secondItem = allItems.SingleOrDefault(i => !i.AlreadyUsed && i.Item.RefId == item.RefId && i.Item != item);
                if (secondItem == null || secondItem.Item.Type != KrakenLedgerItemTransactionTypes.Trade)
                {
                    return null;
                }
                var pair = new List<ToBeEvaluatedItem>() { evaluationItem, secondItem };
                var baseAsset = pair.SingleOrDefault(i => i.Item.Amount > 0);
                var quotedAsset = pair.SingleOrDefault(e => e != baseAsset);

                baseAsset.AlreadyUsed = true;
                quotedAsset.AlreadyUsed = true;

                var feeSymbol = secondItem.Item.Fee > 0 ? SymbolFromKrakenString(secondItem.Item.Asset) : SymbolFromKrakenString(item.Asset);
                var fee = secondItem.Item.Fee > 0 ? secondItem.Item.Fee : item.Fee;
                return new BuySellTransaction()
                {
                    BaseAmmount = baseAsset.Item.Amount,
                    BaseSymbol = SymbolFromKrakenString(baseAsset.Item.Asset),
                    QuotedAmmount = quotedAsset.Item.Amount,
                    QuotedSymbol = SymbolFromKrakenString(quotedAsset.Item.Asset),
                    Time = baseAsset.Item.Time,
                    //Theoretically the secondItem should contain Fee, wasn't able to find the documentation for Ledgers.csv so doing this check
                    Fee = fee,
                    FeeSymbol = feeSymbol
                };
            }

            /// <summary>
            /// 3. Spot buy:
            /// - 'spend' type same refid
            /// - 'receive' type
            /// </summary>
            /// <param name="evaluationItem"></param>
            /// <param name="allItems"></param>
            /// <returns></returns>
            public static TransactionBase SpotBuyEvaluator(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
            {
                var item = evaluationItem.Item;
                if (item.Type != KrakenLedgerItemTransactionTypes.Spend)
                {
                    return null;
                }
                var secondItem = allItems.SingleOrDefault(i => !i.AlreadyUsed && i.Item.RefId == item.RefId && i.Item != item);
                if (secondItem == null || secondItem.Item.Type != KrakenLedgerItemTransactionTypes.Receive)
                {
                    return null;
                }

                evaluationItem.AlreadyUsed = true;
                secondItem.AlreadyUsed = true;

                return new BuySellTransaction()
                {
                    BaseAmmount = secondItem.Item.Amount,
                    BaseSymbol = SymbolFromKrakenString(secondItem.Item.Asset),
                    QuotedAmmount = evaluationItem.Item.Amount,
                    QuotedSymbol = SymbolFromKrakenString(evaluationItem.Item.Asset),
                    Time = secondItem.Item.Time
                };
            }

            /// <summary>
            ///  4. Withdrawal:
            /// - 2 x 'withdrawal' type, same ammount same refid
            /// </summary>
            /// <param name="evaluationItem"></param>
            /// <param name="allItems"></param>
            /// <returns></returns>
            public static TransactionBase WithdrawalEvaluator(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
            {
                var item = evaluationItem.Item;
                if (item.Type != KrakenLedgerItemTransactionTypes.Withdrawal)
                {
                    return null;
                }
                var secondItem = allItems.SingleOrDefault(i => !i.AlreadyUsed && i.Item.RefId == item.RefId && i.Item.Amount == item.Amount && i.Item != item);
                if (secondItem == null || secondItem.Item.Type != KrakenLedgerItemTransactionTypes.Withdrawal)
                {
                    return null;
                }

                evaluationItem.AlreadyUsed = true;
                secondItem.AlreadyUsed = true;

                var transaction = new DepositWithdrawalTransaction(item.Amount, SymbolFromKrakenString(item.Asset), item.Time);
                return transaction;
            }

            /// <summary>
            ///   5. Stake:
            /// - 1 x 'withdrawal' and 1 x 'transfer' sub 'spottostaking' sharing same refid
            /// - 1 x 'deposit' Symbol.S and 1 x 'transfer' sub 'stakingfromspot' sharing same refid
            /// - both 'transfer' share same ammount 'stakingtospot' > 0 'spotfromstaking' < 0
            /// </summary>
            /// <param name="evaluationItem"></param>
            /// <param name="allItems"></param>
            /// <returns></returns>
            public static TransactionBase StakeEvaluator(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
            {
                var list = new List<ToBeEvaluatedItem>();
                var withdrawalItem = evaluationItem.Item;
                if (withdrawalItem.Type != KrakenLedgerItemTransactionTypes.Withdrawal)
                {
                    return null;
                }
                list.Add(evaluationItem);
                var transferItem = allItems.SingleOrDefault(i =>
                                                    !i.AlreadyUsed &&
                                                    i.Item.RefId == withdrawalItem.RefId &&
                                                    i.Item.Type == KrakenLedgerItemTransactionTypes.Transfer &&
                                                    i.Item.Subtype == KrakenLedgerItemTransactionTypes.SpotToStaking &&
                                                    !list.Contains(i));
                if (transferItem == null)
                {
                    return null;
                }
                list.Add(transferItem);

                var secondTransferItem = allItems.SingleOrDefault(i =>
                                                   !i.AlreadyUsed &&
                                                   i.Item.Type == KrakenLedgerItemTransactionTypes.Transfer &&
                                                   i.Item.Subtype == KrakenLedgerItemTransactionTypes.StakingFromSpot &&
                                                   i.Item.Amount == transferItem.Item.Amount * -1 &&
                                                   !list.Contains(i));


                if (secondTransferItem == null)
                {
                    return null;
                }

                var depositItems = allItems.Where(i =>
                                                    !i.AlreadyUsed &&
                                                    i.Item.Type == KrakenLedgerItemTransactionTypes.Deposit &&
                                                    i.Item.RefId == secondTransferItem.Item.RefId &&
                                                    !list.Contains(i));
                if (depositItems.Count() == 0)
                {
                    return null;
                }




                var depositItem = depositItems.SingleOrDefault(i => i.Item.RefId == secondTransferItem.Item.RefId);
                if (depositItem == null)
                {
                    return null;
                }
                list.Add(depositItem);

                list.Add(secondTransferItem);
                list.ForEach(e => e.AlreadyUsed = true);

                return new StakeUnstakeTransaction()
                {
                    BaseAmmount = depositItem.Item.Amount,
                    BaseSymbol = SymbolFromKrakenString(depositItem.Item.Asset),
                    IsStake = true,
                    Time = depositItem.Item.Time
                };
            }

            /// <summary>
            ///   6. Staking reward which is afterwards automatic restaked (see 7)
            /// - 1 x 'deposit' Symbol.S
            ///  7. Automatic restake
            /// - 1 x 'staking'
            ///     9. Parachain rewards (ETH Rewards)
            /// - 1 x 'deposit' and 1 x 'staking' sharing same ammount and currency
            /// </summary>
            /// <param name="evaluationItem"></param>
            /// <param name="allItems"></param>
            /// <returns></returns>
            public static TransactionBase StakeRewardEvaluator(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
            {
                if (evaluationItem.Item.Type != KrakenLedgerItemTransactionTypes.Deposit)
                {
                    return null;
                }
                //Problem with small stakes (like BTC), need to order by date and find the nearest one
                var possibleItems = allItems.Where(i => !i.AlreadyUsed && i != evaluationItem && i.Item.Type == KrakenLedgerItemTransactionTypes.Staking && i.Item.Time > evaluationItem.Item.Time && i.Item.Asset == evaluationItem.Item.Asset && i.Item.Amount == evaluationItem.Item.Amount);

                var orderedByDate = possibleItems.OrderBy(i => i.Item.Time);
                var restakeItem = orderedByDate.FirstOrDefault(i => i.Item.Time > evaluationItem.Item.Time);

                //var restakeItem = allItems.SingleOrDefault(i => !i.AlreadyUsed && i != evaluationItem && i.Item.Type == KrakenLedgerItemTransactionTypes.Staking && i.Item.Time > evaluationItem.Item.Time && i.Item.Asset == evaluationItem.Item.Asset && i.Item.Amount == evaluationItem.Item.Amount);

                if (restakeItem == null)
                {
                    return null;
                }
                if (!string.IsNullOrEmpty(evaluationItem.Item.Subtype) || !string.IsNullOrEmpty(restakeItem.Item.Subtype))
                {
                    return null;
                }

                restakeItem.AlreadyUsed = true;
                evaluationItem.AlreadyUsed = true;

                return new RewardTransaction()
                {
                    BaseAmmount = evaluationItem.Item.Amount,
                    BaseSymbol = SymbolFromKrakenString(evaluationItem.Item.Asset),
                    Time = evaluationItem.Item.Time
                };
            }

            /// <summary>
            ///  8. Unstake:
            /// - 1 x 'withdrawal' and 1 x 'transfer' sub 'stakingtospot' sharing same refid
            /// - 1 x 'deposit' and 1 x 'transfer' sub 'spotfromstaking' sharing same refid
            /// - both 'transfer' share same ammount 'stakingtospot' < 0 'spotfromstaking' > 0
            /// </summary>
            /// <param name="evaluationItem"></param>
            /// <param name="allItems"></param>
            /// <returns></returns>
            public static TransactionBase UnstakeEvaluator(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
            {
                var list = new List<ToBeEvaluatedItem>();
                var withdrawalItem = evaluationItem.Item;
                if (withdrawalItem.Type != KrakenLedgerItemTransactionTypes.Withdrawal)
                {
                    return null;
                }
                list.Add(evaluationItem);
                var transferItem = allItems.SingleOrDefault(i =>
                                                    !i.AlreadyUsed &&
                                                    i.Item.RefId == withdrawalItem.RefId &&
                                                    i.Item.Type == KrakenLedgerItemTransactionTypes.Transfer &&
                                                    i.Item.Subtype == KrakenLedgerItemTransactionTypes.StakingToSpot &&
                                                    !list.Contains(i));
                if (transferItem == null)
                {
                    return null;
                }
                list.Add(transferItem);

                var secondTransferItem = allItems.SingleOrDefault(i =>
                                                   !i.AlreadyUsed &&
                                                   i.Item.Type == KrakenLedgerItemTransactionTypes.Transfer &&
                                                   i.Item.Subtype == KrakenLedgerItemTransactionTypes.SpotFromStaking &&
                                                   i.Item.Amount == transferItem.Item.Amount * -1 &&
                                                   !list.Contains(i));


                if (secondTransferItem == null)
                {
                    return null;
                }

                var depositItems = allItems.Where(i =>
                                                    !i.AlreadyUsed &&
                                                    i.Item.Type == KrakenLedgerItemTransactionTypes.Deposit &&
                                                    i.Item.RefId == secondTransferItem.Item.RefId &&
                                                    !list.Contains(i));
                if (depositItems.Count() == 0)
                {
                    return null;
                }




                var depositItem = depositItems.SingleOrDefault(i => i.Item.RefId == secondTransferItem.Item.RefId);
                if (depositItem == null)
                {
                    return null;
                }
                list.Add(depositItem);

                list.Add(secondTransferItem);
                list.ForEach(e => e.AlreadyUsed = true);

                return new StakeUnstakeTransaction()
                {
                    BaseAmmount = depositItem.Item.Amount,
                    BaseSymbol = SymbolFromKrakenString(depositItem.Item.Asset),
                    IsStake = true,
                    Time = depositItem.Item.Time
                };
            }

            private static CommonSymbol SymbolFromKrakenString(string val)
            {
                var symbol = "";
                if (_krakenLedgerToSymbolMapping.TryGetValue(val, out symbol))
                {
                    return new CommonSymbol(symbol);
                }
                throw new ArgumentException($"Symbol '{val}' not found");
                
            }
        }

        private class KrakenLedgerItemTransactionTypes
        {
            public static readonly string Deposit = "deposit";
            public static readonly string Trade = "trade";
            public static readonly string Spend = "spend";
            public static readonly string Receive = "receive";
            public static readonly string Withdrawal = "withdrawal";
            public static readonly string Transfer = "transfer";
            public static readonly string SpotToStaking = "spottostaking";
            public static readonly string StakingFromSpot = "stakingfromspot";
            public static readonly string StakingToSpot = "stakingtospot";
            public static readonly string SpotFromStaking = "spotfromstaking";
            public static readonly string Staking = "staking";
        }
        private class KrakenLedgerItem
        {
            [Name("txid")]
            public string TxId { get; set; }
            [Name("refid")]
            public string RefId { get; set; }
            [Name("time")]
            public DateTime Time { get; set; }
            [Name("type")]
            public string Type { get; set; }
            [Name("subtype")]
            public string Subtype { get; set; }
            [Name("aclass")]
            public string AssetClass { get; set; }
            [Name("asset")]
            public string Asset { get; set; }
            [Name("amount")]
            public decimal Amount { get; set; }
            [Name("fee")]
            public decimal Fee { get; set; }
            [Name("balance")]
            public decimal? Balance { get; set; }
        }
    }
}
