namespace CryptoTools.Common.Model
{
    public abstract class Symbol
    {
        public static readonly CommonSymbol EUR = new CommonSymbol("EUR");
        public static readonly CommonSymbol ALGORAND = new CommonSymbol("ALGO");
        public static readonly CommonSymbol ETH = new CommonSymbol("ETH");
        public static readonly CommonSymbol BTC = new CommonSymbol("XBT");
        public static readonly CommonSymbol DOT = new CommonSymbol("DOT");
        public static readonly CommonSymbol KSM = new CommonSymbol("KSM");
        public static readonly CommonSymbol ADA = new CommonSymbol("ADA");
        public static readonly CommonSymbol XLM = new CommonSymbol("XLM");
        public static readonly CommonSymbol KAR = new CommonSymbol("KAR");
        public static readonly CommonSymbol SOL = new CommonSymbol("SOL");
        public static readonly CommonSymbol MOVR = new CommonSymbol("MOVR");
        public static readonly CommonSymbol NANO = new CommonSymbol("NANO");
        public static readonly CommonSymbol MLN = new CommonSymbol("MLN");
        public static readonly CommonSymbol DOGE = new CommonSymbol("XDG");
        public static readonly CommonSymbol XMR = new CommonSymbol("XMR");
        public static readonly CommonSymbol XRP = new CommonSymbol("XRP");
        public static readonly CommonSymbol SDN = new CommonSymbol("SDN");
        public static readonly CommonSymbol PHA = new CommonSymbol("PHA");
        public static readonly CommonSymbol MATIC = new CommonSymbol("MATIC");
        public static readonly CommonSymbol ATOM = new CommonSymbol("ATOM");
        public static readonly CommonSymbol IGNOREME = new CommonSymbol("IGNOREME");
        public static readonly CommonSymbol MOONBEAM = new CommonSymbol("GLMR");
        public static readonly CommonSymbol ASTR = new CommonSymbol("ASTR");
        public static readonly CommonSymbol ACA = new CommonSymbol("ASTR");
        public static readonly CommonSymbol CFG = new CommonSymbol("CFG");
        public static readonly CommonSymbol USDT = new CommonSymbol("USDT");

        public abstract string Description { get; }

        public override bool Equals(object obj)
        {
            if (obj is Symbol)
            {
                return (obj as Symbol).Description == Description;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 17 * Description.GetHashCode();
        }

        public override string ToString()
        {
            return Description;
        }
    }

    public class CommonSymbol : Symbol
    {
        private string _description;
        public override string Description => _description;

        public CommonSymbol(string description)
        {
            _description = description;
        }
    }
}
