import { useState } from 'react';
import PatternCard from '../components/PatternCard';
import PatternModal from '../components/PatternModal';
import patternDefinitions from '../data/patternDefinitions';

const FILTERS = ['all', 'bullish', 'bearish', 'easy', 'medium', 'hard'];
const FILTER_LABELS = {
  all: 'הכל',
  bullish: '▲ שוריות',
  bearish: '▼ דוביות',
  easy: 'קל',
  medium: 'בינוני',
  hard: 'קשה',
};

export default function LearnPage({ onPractice }) {
  const [filter, setFilter] = useState('all');
  const [selectedPattern, setSelectedPattern] = useState(null);

  const filtered = patternDefinitions.filter((p) => {
    if (filter === 'all') return true;
    if (filter === 'bullish' || filter === 'bearish') return p.direction === filter;
    if (filter === 'easy' || filter === 'medium' || filter === 'hard') return p.difficulty === filter;
    return true;
  });

  const bullish = filtered.filter((p) => p.direction === 'bullish');
  const bearish = filtered.filter((p) => p.direction === 'bearish');

  return (
    <div className="bg-surface min-h-screen pt-20 pb-12">
      <div className="max-w-7xl mx-auto px-6">
        <h1 className="text-4xl font-bold text-text mb-8">ספריית תבניות</h1>

        {/* Filters */}
        <div className="flex gap-2 mb-8 flex-wrap">
          {FILTERS.map((f) => (
            <button
              key={f}
              onClick={() => setFilter(f)}
              className={`px-4 py-2 rounded-lg font-semibold transition-colors ${
                filter === f
                  ? 'bg-primary text-surface'
                  : 'bg-surface-container text-text-2 hover:bg-surface-bright'
              }`}
            >
              {FILTER_LABELS[f]}
            </button>
          ))}
        </div>

        {/* Bullish */}
        {bullish.length > 0 && (
          <div className="mb-12">
            <h2 className="text-2xl font-bold text-text mb-6">▲ תבניות עלייה</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {bullish.map((p) => (
                <PatternCard
                  key={p.id}
                  pattern={p}
                  onClick={() => setSelectedPattern(p)}
                />
              ))}
            </div>
          </div>
        )}

        {/* Bearish */}
        {bearish.length > 0 && (
          <div>
            <h2 className="text-2xl font-bold text-text mb-6">▼ תבניות ירידה</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {bearish.map((p) => (
                <PatternCard
                  key={p.id}
                  pattern={p}
                  onClick={() => setSelectedPattern(p)}
                />
              ))}
            </div>
          </div>
        )}

        {selectedPattern && (
          <PatternModal
            pattern={selectedPattern}
            onClose={() => setSelectedPattern(null)}
            onPractice={(p) => {
              onPractice(p);
              setSelectedPattern(null);
            }}
          />
        )}
      </div>
    </div>
  );
}
