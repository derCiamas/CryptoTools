using CryptoTools.Common.ExchangeRateProviders;
using CryptoTools.Common.Model.Transactions;
using CryptoTools.Export.PortfolioPerformance;
using CryptoTools.Parsing.Parsing;
using CryptoToPortfolioPerformance.Parsing.Parsing.Kraken;
using System;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace CryptoTools
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ledgerFile"></param>
        /// <param name="krakenCSVPath"></param>
        /// <param name="outputDir"></param>
        static void DoWork(string ledgerFile, string krakenCSVPath, string symbolMappingsFilePath, string outputDir)
        {
            var parser = new KrakenCSVParser(symbolMappingsFilePath);
            var data = parser.ParseFile(ledgerFile);
            data.Wait();
            var grouped = data.Result.Where(t => t.Type == Common.Model.Transactions.TransactionBase.TransactionType.Reward).GroupBy(g => g.BaseSymbol);
            foreach (var group in grouped)
            {
                Console.WriteLine($"Rewards '{group.Key.Description}': {group.Sum(e => e.BaseAmmount)}");
            }
            var provider = new KrakenCSVExchangeRateProvider(krakenCSVPath);
            var exporter = new PortfolioPerformanceExporter(new[] { typeof(StakeUnstakeTransaction) });
            var task = exporter.ExportToDirectory(outputDir, data.Result, provider);
            task.Wait();
            Console.WriteLine("Done!");
            Console.ReadLine();
        }
        static void Main(string[] args)
        {
            var ledgerCSV = new Option<string>(
                                        name: "--ledgerCSV",
                                        description: "The Kraken's ledger CSV file.");    
            var krakenOHCDirectory = new Option<string>(
                                        name: "--krakenOHLCVT",
                                        description: "The directory containing Kraken's OHLCVT data.");
            var symbolMappingsFilePath = new Option<string>(
                                        name: "--symbolMappings",
                                        description: "The json file with symbol mappings.");
            var outputDir = new Option<string>(
                                     name: "--outputDir",
                                     description: "The output dir");
            var rootCommand = new RootCommand("");
            rootCommand.AddOption(ledgerCSV);
            rootCommand.AddOption(krakenOHCDirectory);
            rootCommand.AddOption(outputDir);
            rootCommand.AddOption(symbolMappingsFilePath);
            rootCommand.SetHandler((l, k, s, o)=>DoWork(l, k, s, o), ledgerCSV, krakenOHCDirectory, symbolMappingsFilePath, outputDir);
            rootCommand.Invoke(args);
        }

    }
}
