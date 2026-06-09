import { useEffect } from 'react';
import LessonChart from './LessonChart';

const DIFF_LABEL = { easy: 'קל', medium: 'בינוני', hard: 'קשה' };
const DIFF_COLOR = { easy: '#3fb950', medium: '#d29922', hard: '#f85149' };

export default function PatternModal({ pattern, onClose, onPractice }) {
  useEffect(() => {
    const handle = (e) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handle);
    return () => window.removeEventListener('keydown', handle);
  }, [onClose]);

  if (!pattern) return null;

  const combined = [...pattern.questionCandles, ...pattern.revealCandles];

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-surface-container rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
        <div className="p-6 border-b border-border">
          <div className="flex justify-between items-start mb-4">
            <div>
              <h2 className="text-2xl font-bold text-text mb-2">{pattern.name}</h2>
              <div className="flex gap-3">
                <span className="text-sm font-semibold" style={{ color: DIFF_COLOR[pattern.difficulty] }}>
                  {DIFF_LABEL[pattern.difficulty]}
                </span>
                <span className="text-sm text-text-2">ניצחון: {pattern.winRate}%</span>
              </div>
            </div>
            <button onClick={onClose} className="text-text-2 hover:text-text">✕</button>
          </div>
          <p className="text-text-2">{pattern.description}</p>
        </div>

        <div className="p-6 border-b border-border">
          <h3 className="font-bold text-text mb-4">תרשים</h3>
          <LessonChart candles={combined} />
        </div>

        <div className="p-6">
          <div className="mb-6">
            <h3 className="font-bold text-text mb-3">בדיקות:</h3>
            <ul className="space-y-2">
              {pattern.checklist.map((item, i) => (
                <li key={i} className="flex gap-2 text-sm text-text-2">
                  <span className="text-primary">✓</span> {item}
                </li>
              ))}
            </ul>
          </div>

          <div className="mb-6 p-4 bg-surface rounded-lg border border-border">
            <p className="text-sm text-text-2 italic">{pattern.tip}</p>
          </div>

          <div className="mb-6">
            <h3 className="font-bold text-text mb-2">הסבר:</h3>
            <p className="text-sm text-text-2">{pattern.explanation}</p>
          </div>

          <button
            onClick={() => onPractice(pattern)}
            className="w-full bg-primary hover:bg-primary-dark text-surface py-3 rounded-lg font-bold transition-colors"
          >
            תרגל דפוס זה →
          </button>
        </div>
      </div>
    </div>
  );
}
