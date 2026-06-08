import MiniChart from '../components/MiniChart';
import patternDefinitions from '../data/patternDefinitions';

const FEATURES = [
  { icon: '📊', title: '12 תבניות', desc: 'מהנרות הפשוטים ועד תבניות מורכבות כמו כוכב הבוקר ושלושה חיילים' },
  { icon: '📡', title: 'נתוני שוק אמיתיים', desc: 'דוגמאות אמיתיות מ-SPY, AAPL, TSLA ועוד מניות מובילות' },
  { icon: '🎯', title: 'קרא את הגרף', desc: 'לחץ ↑ או ↓ לנחש את הכיוון — וקבל הסבר מיד אחרי' },
  { icon: '🔥', title: 'מעקב קדמה', desc: 'ציון, סטריק ואחוזי דיוק לכל תבנית' },
];

export default function HomePage({ onNavigate, dataStatus, realPatternCount }) {
  const previews = patternDefinitions.slice(0, 6);

  return (
    <div className="home-page">
      {/* Hero */}
      <section className="hero">
        <div className="hero-inner">
          <div className="hero-eyebrow">
            <span className="hero-badge">📊 גרפים פיננסיים</span>
          </div>
          <h1 className="hero-title">
            למד לקרוא<br />
            <span className="hero-accent">גרפי נרות</span>
          </h1>
          <p className="hero-sub">
            זהה תבניות נרות יפניים, נחש לאן המחיר הולך ושפר את הניתוח הטכני שלך —
            עם דוגמאות אמיתיות מהשוק.
          </p>

          <div className="hero-ctas">
            <button className="btn-primary" onClick={() => onNavigate('play')}>
              🎮 התחל לשחק
            </button>
            <button className="btn-secondary" onClick={() => onNavigate('learn')}>
              📚 לומד תבניות
            </button>
          </div>

          {dataStatus === 'ready' && realPatternCount > 0 && (
            <div className="hero-data-badge">
              ✓ {realPatternCount} דוגמאות אמיתיות נטענו מהשוק
            </div>
          )}
          {dataStatus === 'loading' && (
            <div className="hero-data-badge loading">⏳ טוען נתוני שוק...</div>
          )}
        </div>
      </section>

      {/* Pattern preview strip */}
      <section className="preview-section">
        <h2 className="preview-title">תבניות שתלמד</h2>
        <div className="preview-strip">
          {previews.map((p) => (
            <div key={p.id} className="preview-chip">
              <MiniChart candles={p.questionCandles.slice(-8)} width={140} height={56} />
              <div className="preview-chip-name">
                <span className={`preview-dir ${p.direction}`}>
                  {p.direction === 'bullish' ? '▲' : '▼'}
                </span>
                {p.name}
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Features */}
      <section className="features-section">
        <div className="features-grid">
          {FEATURES.map((f) => (
            <div key={f.title} className="feature-card">
              <div className="feature-icon">{f.icon}</div>
              <h3 className="feature-title">{f.title}</h3>
              <p className="feature-desc">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Bottom CTA */}
      <section className="home-bottom-cta">
        <button className="btn-primary large" onClick={() => onNavigate('play')}>
          מוכן? בוא נתחיל ←
        </button>
      </section>
    </div>
  );
}
