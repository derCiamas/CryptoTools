using CryptoTools.Common.Model;
using CryptoTools.Common.Model.Transactions;
using CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators;
using CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators.Models;
using CryptoToPortfolioPerformance.Parsing.Kraken.Models;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Kraken
{
    public class KrakenCSVParser : ISourceParser
    {
        private static Dictionary<string, string> _krakenLedgerToSymbolMapping;
        private readonly string _symbolMappingsFilePath;
        private readonly string _csvToBeParsed;
        public KrakenCSVParser(string csvToBeParsed, string symbolMappingsFilePath)
        {
            if (string.IsNullOrEmpty(csvToBeParsed) || !File.Exists(csvToBeParsed))
            {
                throw new ArgumentException($"No {nameof(csvToBeParsed)} provided or the file does not exist");
            }
            if (string.IsNullOrEmpty(symbolMappingsFilePath) || !File.Exists(symbolMappingsFilePath))
            {
                throw new ArgumentException($"No {nameof(symbolMappingsFilePath)} provided or the file does not exist");
            }
            _symbolMappingsFilePath = symbolMappingsFilePath;
            _csvToBeParsed = csvToBeParsed;
        }

        public Task<IEnumerable<TransactionBase>> ParseUnderlyingSource()
        {
            return ParseFile(_csvToBeParsed);
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

        private async Task<IEnumerable<TransactionBase>> ParseFile(string filePath)
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
            var transactionEvaluator = new TransactionEvaluator();
            foreach (var item in ledgerItems)
            {
                var el = transactionEvaluator.Evaluate(item, ref ledgerItems);
                if (el != null)
                {
                    transactions.Add(el);
                }
            }
            if (ledgerItems.Count(e => !e.AlreadyUsed) != 0)
            {
                Console.WriteLine("WARNING! Some transactions have not been included in the export data cause these could not be evaluated. This might happen if, for example, you have exported the ledger shortly after receiving the staking rewards but before the funds were restaked automatically. Try re-exporting the ledger again and running the app.");
            }
            return transactions;
        }

        private class TransactionEvaluator
        {

            private readonly List<TransactionEvaluatorBase> Evaluators = new List<TransactionEvaluatorBase>()
            {
                new DepositEvaluator(_krakenLedgerToSymbolMapping),
                new TradeEvaluator(_krakenLedgerToSymbolMapping),
                new SpotBuyEvaluator(_krakenLedgerToSymbolMapping),
                new WithdrawalEvaluator(_krakenLedgerToSymbolMapping),
                new StakeEvaluator(_krakenLedgerToSymbolMapping),
                new StakeRewardEvaluator(_krakenLedgerToSymbolMapping),
                new UnstakeEvaluator(_krakenLedgerToSymbolMapping)
            };

            public TransactionBase Evaluate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
            {
                if (evaluationItem.AlreadyUsed)
                {
                    return null;
                }
                foreach (var evaluator in Evaluators)
                {
                    var result = evaluator.Evaluate(evaluationItem, ref allItems);
                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            }



        }



    }
}
