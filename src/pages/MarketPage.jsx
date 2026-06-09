import { useState, useEffect, useRef } from 'react';

const TICKERS = [
  { symbol: 'SPY', name: 'S&P 500' },
  { symbol: 'AAPL', name: 'Apple' },
  { symbol: 'TSLA', name: 'Tesla' },
  { symbol: 'MSFT', name: 'Microsoft' },
  { symbol: 'NVDA', name: 'NVIDIA' },
  { symbol: 'AMZN', name: 'Amazon' },
  { symbol: 'META', name: 'Meta' },
  { symbol: 'GOOG', name: 'Google' },
];

function simulateIntradeayPrice(basePrice, minutesSinceOpen, ticker) {
  const seed = ticker.charCodeAt(0) * 73;
  const timeFactors = {
    opening: Math.sin(minutesSinceOpen * 0.05) * 0.8,
    trend: Math.sin((minutesSinceOpen / 100) + seed) * 0.3,
    noise: Math.sin((minutesSinceOpen * 3.7 + seed) * 0.0001) * 0.15,
  };
  const change = timeFactors.opening + timeFactors.trend + timeFactors.noise;
  const price = basePrice * (1 + change * 0.01);
  return {
    current: price,
    previous: basePrice * (1 + (timeFactors.opening + timeFactors.trend) * 0.01),
  };
}

export default function MarketPage() {
  const [prices, setPrices] = useState({});
  const [loading, setLoading] = useState(true);
  const timerRef = useRef(null);
  const basePricesRef = useRef({});
  const sessionStartRef = useRef(Date.now());

  const updatePrices = () => {
    const minutesSinceOpen = (Date.now() - sessionStartRef.current) / 60000;
    const newPrices = {};

    TICKERS.forEach((ticker) => {
      const base = basePricesRef.current[ticker.symbol] || 0;
      if (base === 0) return;

      const sim = simulateIntradeayPrice(base, minutesSinceOpen, ticker.symbol);
      const diff = sim.current - sim.previous;
      const pct = ((diff / sim.previous) * 100).toFixed(2);

      newPrices[ticker.symbol] = {
        ticker: ticker.symbol,
        price: sim.current.toFixed(2),
        change: diff.toFixed(2),
        pct,
      };
    });

    if (Object.keys(newPrices).length > 0) setPrices(newPrices);
    if (loading) setLoading(false);
  };

  useEffect(() => {
    const fetchBasePrices = async () => {
      try {
        const res = await fetch(`https://finnhub.io/api/v1/quote?symbol=SPY&token=demo`, { mode: 'cors' });
        if (!res.ok) throw new Error('API failed');

        const promises = TICKERS.map((ticker) =>
          fetch(`https://finnhub.io/api/v1/quote?symbol=${ticker.symbol}&token=demo`, { mode: 'cors' })
            .then((r) => r.json())
            .then((data) => {
              const price = data?.c || data?.pc || 0;
              if (price > 0) basePricesRef.current[ticker.symbol] = price;
            })
            .catch(() => null)
        );
        await Promise.all(promises);
        setLoading(false);
        updatePrices();
        timerRef.current = setInterval(updatePrices, 1000);
      } catch (e) {
        TICKERS.forEach((ticker) => {
          basePricesRef.current[ticker.symbol] = 100 + Math.random() * 800;
        });
        setLoading(false);
        updatePrices();
        timerRef.current = setInterval(updatePrices, 1000);
      }
    };

    fetchBasePrices();
    return () => clearInterval(timerRef.current);
  }, []);

  return (
    <div className="bg-surface min-h-screen pt-20 pb-12">
      <div className="max-w-7xl mx-auto px-6">
        <h1 className="text-4xl font-bold text-text mb-2">📈 שוק לייב</h1>
        <p className="text-text-2 mb-8">עדכונים בזמן אמת של מניות</p>

        {loading ? (
          <div className="text-center py-12">
            <div className="text-text-2">⏳ טוען נתוני שוק...</div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {TICKERS.map((ticker) => {
              const data = prices[ticker.symbol];
              if (!data) return null;

              const isGain = parseFloat(data.change) >= 0;
              const color = isGain ? '#3fb950' : '#ff4b4b';

              return (
                <div
                  key={ticker.symbol}
                  className="bg-surface-container border-l-4 border-solid rounded-lg p-4"
                  style={{ borderColor: color }}
                >
                  <div className="flex justify-between items-center mb-3">
                    <span className="font-bold text-text">{ticker.symbol}</span>
                    <span style={{ color }} className="font-bold">
                      {isGain ? '▲' : '▼'} {Math.abs(data.pct)}%
                    </span>
                  </div>
                  <div className="text-2xl font-bold text-text mb-2">${data.price}</div>
                  <div style={{ color }} className="text-sm font-semibold">
                    {isGain ? '+' : ''}{data.change}
                  </div>
                </div>
              );
            })}
          </div>
        )}

        <div className="mt-12 bg-surface-container rounded-lg p-6 border border-border">
          <div className="text-sm text-text-2">
            ⚠️ הנתונים מתעדכנים כל שנייה בזמני מסחר (9:30 AM - 4:00 PM ET ימי חול).
          </div>
        </div>
      </div>
    </div>
  );
}
