import { useState } from 'react';
import PatternCard from '../components/PatternCard';
import PatternModal from '../components/PatternModal';
import patternDefinitions from '../data/patternDefinitions';

const FILTERS = [
  { id: 'all', label: 'הכל' },
  { id: 'bullish', label: '▲ שוריות' },
  { id: 'bearish', label: '▼ דוביות' },
  { id: 'easy', label: 'קל ⭐' },
  { id: 'medium', label: 'בינוני ⭐⭐' },
  { id: 'hard', label: 'קשה ⭐⭐⭐' },
];

export default function LearnPage({ onPractice }) {
  const [filter, setFilter] = useState('all');
  const [selected, setSelected] = useState(null);

  const filtered = patternDefinitions.filter((p) => {
    if (filter === 'all') return true;
    return p.direction === filter || p.difficulty === filter;
  });

  const bullish = filtered.filter((p) => p.direction === 'bullish');
  const bearish = filtered.filter((p) => p.direction === 'bearish');

  return (
    <div className="learn-page">
      {/* Page header */}
      <div className="learn-header">
        <div className="learn-header-inner">
          <h1 className="learn-title">מדריך תבניות</h1>
          <p className="learn-subtitle">
            {patternDefinitions.length} תבניות נרות יפניים עם דוגמאות ואחוזי דיוק
          </p>
        </div>
      </div>

      {/* Filter bar */}
      <div className="filter-bar">
        <div className="filter-bar-inner">
          {FILTERS.map((f) => (
            <button
              key={f.id}
              className={`filter-btn ${filter === f.id ? 'active' : ''}`}
              onClick={() => setFilter(f.id)}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      {/* Pattern grid */}
      <div className="learn-content">
        {bullish.length > 0 && (
          <section className="pattern-section">
            <h2 className="section-heading">
              <span className="section-dot bullish" />
              תבניות שוריות
              <span className="section-count">{bullish.length}</span>
            </h2>
            <div className="pattern-grid">
              {bullish.map((p) => (
                <PatternCard key={p.id} pattern={p} onClick={setSelected} />
              ))}
            </div>
          </section>
        )}

        {bearish.length > 0 && (
          <section className="pattern-section">
            <h2 className="section-heading">
              <span className="section-dot bearish" />
              תבניות דוביות
              <span className="section-count">{bearish.length}</span>
            </h2>
            <div className="pattern-grid">
              {bearish.map((p) => (
                <PatternCard key={p.id} pattern={p} onClick={setSelected} />
              ))}
            </div>
          </section>
        )}

        {filtered.length === 0 && (
          <div className="learn-empty">
            <p>אין תבניות לסינון זה</p>
          </div>
        )}
      </div>

      {/* Modal */}
      {selected && (
        <PatternModal
          pattern={selected}
          onClose={() => setSelected(null)}
          onPractice={(p) => { setSelected(null); onPractice(p); }}
        />
      )}
    </div>
  );
}
