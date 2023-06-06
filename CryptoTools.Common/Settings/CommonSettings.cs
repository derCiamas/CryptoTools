using CryptoTools.Common.Model;
using System.Collections.Generic;

namespace CryptoTools.Common.Settings
{
    public class CommonSettings
    {
        public static CommonSymbol BaseSymbol => Symbol.EUR;

        public static List<(string SymbolDescription, string Ticker)> SymbolToTickerSymbolMappings
        {
            get
            {
                return new List<(string SymbolDescription, string Ticker)>()
                {
                    new ("CAKE","CAKEEUR"),
                    new ("ALGO","ALGOEUR"),
                    new ("XLM","XXLMZEUR"),
                    new ("KSM","KSMEUR"),
                    new ("MLN","XMLNZEUR"),
                    new ("DOT","DOTEUR"),
                    new ("ETH","XETHZEUR"),
                    new ("BNB","BNBEUR"),
                    new ("DAI","DAIEUR"),
                    new ("XDG","XDGEUR"),
                    new ("ADA","ADAEUR"),
                    new ("XRP","XRP-EUR"),
                    new ("NANO","NANO-EUR"),
                    new ("XMR","XXMRZEUR"),
                    new ("COMP","COMPEUR"),
                    new ("GRT","GRTEUR"),
                    new ("XBT","XBTCZEUR")
                };
            }
        }
    }
}
