import { useEffect } from 'react';
import MiniChart from './MiniChart';

const DIFF_LABEL = { easy: 'קל ⭐', medium: 'בינוני ⭐⭐', hard: 'קשה ⭐⭐⭐' };
const DIFF_COLOR = { easy: '#3fb950', medium: '#d29922', hard: '#f85149' };

export default function PatternModal({ pattern, onClose, onPractice }) {
  useEffect(() => {
    const onKey = (e) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  const allCandles = [...pattern.questionCandles, ...pattern.revealCandles];

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-box" onClick={(e) => e.stopPropagation()}>
        <button className="modal-close" onClick={onClose} aria-label="סגור">✕</button>

        {/* Header */}
        <div className="modal-header">
          <div className="modal-tags">
            <span className={`modal-dir ${pattern.direction}`}>
              {pattern.direction === 'bullish' ? '▲ שורי' : '▼ דובי'}
            </span>
            <span className="modal-diff" style={{ color: DIFF_COLOR[pattern.difficulty] }}>
              {DIFF_LABEL[pattern.difficulty]}
            </span>
            <span className="modal-winrate">✓ {pattern.winRate}% דיוק</span>
          </div>
          <h2 className="modal-name">{pattern.name}</h2>
          <p className="modal-en">{pattern.english}</p>
        </div>

        {/* Chart */}
        <div className="modal-chart-wrap">
          <MiniChart candles={allCandles} width={480} height={160} />
          <div className="modal-chart-label">תבנית מלאה כולל המשך</div>
        </div>

        {/* Description */}
        <p className="modal-desc">{pattern.description}</p>

        {/* Checklist */}
        <div className="modal-section">
          <h4 className="modal-section-title">מה לחפש</h4>
          <ul className="modal-checklist">
            {pattern.checklist.map((item, i) => (
              <li key={i} className="modal-check-item">
                <span className="check-icon">✓</span>
                {item}
              </li>
            ))}
          </ul>
        </div>

        {/* Explanation */}
        <div className="modal-section">
          <h4 className="modal-section-title">הסבר</h4>
          <p className="modal-explanation">{pattern.explanation}</p>
        </div>

        {/* Tip */}
        <div className="modal-tip">
          <span className="tip-bulb">💡</span>
          <span>{pattern.tip}</span>
        </div>

        {/* CTA */}
        <button className="modal-practice-btn" onClick={() => onPractice(pattern)}>
          תרגל תבנית זו ←
        </button>
      </div>
    </div>
  );
}
