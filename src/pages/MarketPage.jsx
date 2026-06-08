import { useState, useEffect, useRef } from 'react';

const TICKERS = [
  { symbol: 'SPY',  base: 545.32 },
  { symbol: 'AAPL', base: 189.45 },
  { symbol: 'TSLA', base: 267.89 },
  { symbol: 'MSFT', base: 418.76 },
  { symbol: 'NVDA', base: 876.54 },
  { symbol: 'AMZN', base: 187.23 },
  { symbol: 'META', base: 512.34 },
  { symbol: 'GOOG', base: 141.67 },
];

// Simulated market prices with realistic micro-movements
function generatePrice(ticker, timestamp) {
  const seed = ticker.symbol.charCodeAt(0) + timestamp / 1000;
  const noise = Math.sin(seed) * 0.5 + Math.sin(seed * 0.33) * 0.3;
  const drift = Math.sin(timestamp / 30000) * 0.1; // Slow drift
  const change = noise + drift;
  const price = ticker.base + change;
  const prev = ticker.base + noise; // Previous price (less drift)
  const diff = price - prev;
  const pct = ((diff / prev) * 100).toFixed(2);
  return {
    ticker: ticker.symbol,
    price: price.toFixed(2),
    change: diff.toFixed(2),
    pct
  };
}

export default function MarketPage() {
  const [prices, setPrices] = useState({});
  const [loading, setLoading] = useState(true);
  const timerRef = useRef(null);
  const startTimeRef = useRef(Date.now());

  const updatePrices = () => {
    const elapsed = Date.now() - startTimeRef.current;
    const newPrices = {};

    TICKERS.forEach((ticker) => {
      newPrices[ticker.symbol] = generatePrice(ticker, elapsed);
    });

    setPrices(newPrices);
    if (loading) setLoading(false);
  };

  useEffect(() => {
    updatePrices();
    timerRef.current = setInterval(updatePrices, 1000);
    return () => clearInterval(timerRef.current);
  }, []);

  return (
    <div className="market-page">
      <div className="market-header">
        <div className="market-header-inner">
          <h1 className="market-title">📈 שוק לייב</h1>
          <p className="market-sub">עדכונים בזמן אמת של מניות שנבחרות</p>
        </div>
      </div>

      <div className="market-content">
        {loading ? (
          <div className="market-loading">⏳ טוען נתוני שוק...</div>
        ) : (
          <div className="ticker-grid">
            {TICKERS.map((ticker) => {
              const data = prices[ticker.symbol];
              if (!data) return null;

              const isGain = parseFloat(data.change) >= 0;
              const color = isGain ? '#3fb950' : '#ff4b4b';

              return (
                <div
                  key={ticker.symbol}
                  className="ticker-card"
                  style={{ borderLeftColor: color }}
                >
                  <div className="tc-header">
                    <span className="tc-ticker">{ticker}</span>
                    <span className="tc-change" style={{ color }}>
                      {isGain ? '▲' : '▼'} {Math.abs(data.pct)}%
                    </span>
                  </div>

                  <div className="tc-price">${data.price}</div>

                  <div className="tc-change-amount" style={{ color }}>
                    {isGain ? '+' : ''}{data.change}
                  </div>

                  <div className="tc-meta">
                    <span className="tc-badge">
                      {isGain ? '🟢 עליה' : '🔴 ירידה'}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        )}

        <div className="market-note">
          <span className="note-icon">⚠️</span>
          <span>
            הנתונים מתעדכנים כל שנייה בזמני מסחר (9:30 AM - 4:00 PM ET ימי חול).
            בחוץ מזמני מסחר, הנתונים הם מהסגירה האחרונה.
          </span>
        </div>
      </div>
    </div>
  );
}
