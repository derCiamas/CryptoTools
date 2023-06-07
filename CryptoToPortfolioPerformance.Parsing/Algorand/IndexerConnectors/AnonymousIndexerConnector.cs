using CryptoToPortfolioPerformance.Parsing.Algorand.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Algorand.IndexerConnectors
{
    /// <summary>
    /// Indexer connector without any auth related configuration
    /// </summary>
    public class AnonymousIndexerConnector : IIndexerConnector
    {
        private readonly string _indexerBaseUrl;
        private readonly int _defaultWait = 1000;
        public AnonymousIndexerConnector(string indexerBaseUrl)
        {
            _indexerBaseUrl = indexerBaseUrl;
        }

        public async Task<IEnumerable<AlgorandTransaction>> GetWalletTransactions(string walletAddress)
        {
            var parsedResponses = new List<IndexerAccountTransactionsResponse>();
            var client = GetHttpClient();
            IndexerAccountTransactionsResponse parsedResponse;
            string nextToken = "";
            Console.WriteLine("Starting getting wallet transactions...");
            do
            {
                var url = string.IsNullOrEmpty(nextToken) ? $"{_indexerBaseUrl}/v2/accounts/{walletAddress}/transactions" : $"{_indexerBaseUrl}/v2/accounts/{walletAddress}/transactions?next={nextToken}";
                var response = await client.GetStringAsync(url);
                parsedResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<IndexerAccountTransactionsResponse>(response);
                parsedResponses.Add(parsedResponse);
                Console.WriteLine($"Received response with {parsedResponse.Transactions.Count} transactions");
                if (!string.IsNullOrEmpty(parsedResponse.NextToken))
                {
                    nextToken = parsedResponse.NextToken;
                    Console.WriteLine($"Next token in response. Will wait {_defaultWait}ms and load next transactions");
                    await Task.Delay(_defaultWait);
                }
            } while (!string.IsNullOrEmpty(parsedResponse.NextToken));
            Console.WriteLine($"Finished getting transactions. Total number received: {parsedResponses.Sum(t=>t.Transactions.Count)}");
            return parsedResponses.SelectMany(r => r.Transactions);
        }

        private HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
