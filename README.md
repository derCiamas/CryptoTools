# CryptoTools

Solution for crypto related tools. The initial version contains the Kraken => PortfolioPerformance parser.

## Prerequisites
.NET 7.0 [Download Link][dotnetDownloadLink]

## Usage

In order to run the parser the following prerequisites are required:
- Kraken's ledger.csv file (export with default options)
- Krakens's OHLCVT files ([downloadable here][krakenData] => search for "Single Zip file", download and unzip it)

After downloading the both files you can start the tool from the CryptoToPortfolioPerformance director.
The following parameters need to be filled
- --ledgerCSV: the path to the downloaded ledger.csv
- --krakenOHLCVT: the path to the directory where you have unzipped the OHLCVT data
- --outputDir: the directory where the output files (deposits.csv and depottransactions.csv will be generated)

Example: 
```
dotnet run --ledgerCSV C:\PP\ledger.csv --krakenOHLCVT C:\PP\KrakenOHLCVT --outputDir C:\PP\Output
```


### Remarks
The current version using the Kraken OHLCVT data can be pretty memory consuming but also very exact with prices (the data contains prices with 1min resolution).

[dotnetDownloadLink]: https://dotnet.microsoft.com/en-us/download/dotnet/7.0
[krakenData]: https://support.kraken.com/hc/en-us/articles/360047124832-Downloadable-historical-OHLCVT-Open-High-Low-Close-Volume-Trades-data
