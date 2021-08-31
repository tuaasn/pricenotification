using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Binance.Cache
{
    public sealed class CandlestickCacheEventArgs : CacheEventArgs
    {
        #region Public Properties

        /// <summary>
        /// The candlesticks.
        /// </summary>
        public IEnumerable<Candlestick> Candlesticks { get; }
        public string Symbol { get; set; }
        public CandlestickInterval CandlestickInterval { get; set; }

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="candlesticks">The candlesticks.</param>
        public CandlestickCacheEventArgs(IEnumerable<Candlestick> candlesticks, string symbol, CandlestickInterval candlestickInterval)
        {
            Throw.IfNull(candlesticks, nameof(candlesticks));

            Candlesticks = candlesticks;
            Symbol = symbol;
            CandlestickInterval = candlestickInterval;
        }

        #endregion Constructors
    }
}
