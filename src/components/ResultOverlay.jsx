const DIFF_LABEL = { easy: 'קל ⭐', medium: 'בינוני ⭐⭐', hard: 'קשה ⭐⭐⭐' };

export default function ResultOverlay({ isCorrect, pattern, onNext }) {
  return (
    <div className={`result-overlay ${isCorrect ? 'correct' : 'wrong'}`} onClick={onNext}>
      <div className="result-card" onClick={(e) => e.stopPropagation()}>

        {/* Result badge */}
        <div className={`result-badge ${isCorrect ? 'correct' : 'wrong'}`}>
          <span className="result-icon">{isCorrect ? '✓' : '✗'}</span>
          <div>
            <div className="result-title">{isCorrect ? 'נכון!' : 'לא נכון'}</div>
            {pattern.isReal && pattern.ticker && (
              <div className="result-ticker">{pattern.ticker} — נתונים אמיתיים</div>
            )}
          </div>
        </div>

        {/* Pattern details */}
        <div className="result-pattern">
          <div className="rp-header">
            <span className={`rp-dir ${pattern.direction}`}>
              {pattern.direction === 'bullish' ? '▲ שורי' : '▼ דובי'}
            </span>
            <span className="rp-diff">{DIFF_LABEL[pattern.difficulty]}</span>
          </div>

          <h3 className="rp-name">
            {pattern.name}
            <span className="rp-en"> {pattern.english}</span>
          </h3>

          <p className="rp-desc">{pattern.description}</p>

          <div className="rp-tip">
            <span>💡</span>
            {pattern.tip}
          </div>

          <p className="rp-explanation">{pattern.explanation}</p>

          {pattern.checklist && (
            <ul className="rp-checklist">
              {pattern.checklist.map((item, i) => (
                <li key={i}><span className="rp-check">✓</span>{item}</li>
              ))}
            </ul>
          )}
        </div>

        <button className="result-next-btn" onClick={onNext}>
          הבא <span className="result-next-hint">Space / ←</span>
        </button>
      </div>
    </div>
  );
}
