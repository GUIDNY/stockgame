import MiniChart from '../components/MiniChart';
import patternDefinitions from '../data/patternDefinitions';

const FEATURES = [
  { icon: '📊', title: '12 תבניות', desc: 'מהנרות הפשוטים ועד תבניות מורכבות' },
  { icon: '📡', title: 'נתוני שוק', desc: 'דוגמאות אמיתיות מהשוק' },
  { icon: '🎯', title: 'אתגר עצמך', desc: 'לחץ ↑ או ↓ לנחש את הכיוון' },
  { icon: '🔥', title: 'מעקב', desc: 'ציון וסטריק בזמן אמת' },
];

export default function HomePage({ onNavigate, dataStatus, realPatternCount }) {
  return (
    <div className="bg-surface min-h-screen pt-20 pb-12">
      <div className="max-w-6xl mx-auto px-6">
        {/* Hero */}
        <section className="text-center mb-16">
          <div className="inline-block bg-primary/10 text-primary px-4 py-2 rounded-full text-sm font-semibold mb-6">
            📊 אנליזה טכנית
          </div>
          <h1 className="text-5xl md:text-6xl font-bold text-text mb-6">
            למד לקרוא<br />
            <span className="text-primary">גרפי נרות</span>
          </h1>
          <p className="text-xl text-text-2 mb-8 max-w-2xl mx-auto">
            זהה תבניות נרות יפניים, נחש לאן המחיר הולך ושפר את הניתוח הטכני שלך
          </p>

          <div className="flex gap-4 justify-center mb-8">
            <button
              onClick={() => onNavigate('play')}
              className="bg-primary hover:bg-primary-dark text-surface px-8 py-3 rounded-lg font-bold transition-colors"
            >
              🎮 התחל לשחק
            </button>
            <button
              onClick={() => onNavigate('learn')}
              className="bg-surface-bright hover:bg-surface-container text-text px-8 py-3 rounded-lg font-bold transition-colors"
            >
              📚 לומד תבניות
            </button>
          </div>

          {dataStatus === 'ready' && realPatternCount > 0 && (
            <div className="inline-block bg-primary/10 text-primary px-6 py-2 rounded-lg text-sm font-semibold">
              ✓ {realPatternCount} דוגמאות אמיתיות נטענו מהשוק
            </div>
          )}
          {dataStatus === 'loading' && (
            <div className="inline-block bg-primary/10 text-primary px-6 py-2 rounded-lg text-sm font-semibold">
              ⏳ טוען נתוני שוק...
            </div>
          )}
        </section>

        {/* Features */}
        <section className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-16">
          {FEATURES.map((f) => (
            <div key={f.title} className="bg-surface-container rounded-lg p-6 border border-border">
              <div className="text-3xl mb-3">{f.icon}</div>
              <h3 className="text-lg font-bold text-text mb-2">{f.title}</h3>
              <p className="text-sm text-text-2">{f.desc}</p>
            </div>
          ))}
        </section>

        {/* CTA */}
        <section className="text-center py-12 bg-surface-container rounded-lg">
          <button
            onClick={() => onNavigate('play')}
            className="bg-primary hover:bg-primary-dark text-surface px-12 py-4 rounded-lg font-bold text-lg transition-colors"
          >
            בוא נתחיל ←
          </button>
        </section>
      </div>
    </div>
  );
}
