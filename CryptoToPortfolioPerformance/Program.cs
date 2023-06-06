using CryptoTools.Common.ExchangeRateProviders;
using CryptoTools.Common.Model.Transactions;
using CryptoTools.Export.PortfolioPerformance;
using CryptoTools.Parsing.Parsing;
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
        static void DoWork(string ledgerFile, string krakenCSVPath, string outputDir)
        {
            var parser = new KrakenCSVParser();
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
            Console.WriteLine("HI!");
            var ledgerCSV = new Option<string>(
                                        name: "--ledgerCSV",
                                        description: "The Kraken's ledger CSV file.");    
            var krakenOHCDirectory = new Option<string>(
                                        name: "--krakenOHLCVT",
                                        description: "The directory containing Kraken's OHLCVT data.");
            var outputDir = new Option<string>(
                                     name: "--outputDir",
                                     description: "The output dir");
            var rootCommand = new RootCommand("");
            rootCommand.AddOption(ledgerCSV);
            rootCommand.AddOption(krakenOHCDirectory);
            rootCommand.AddOption(outputDir);
            rootCommand.SetHandler((l, k, o)=>DoWork(l, k, o), ledgerCSV, krakenOHCDirectory, outputDir);
            rootCommand.Invoke(args);
        }

    }
}
