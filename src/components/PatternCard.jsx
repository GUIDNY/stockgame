import MiniChart from './MiniChart';

const DIFF_LABEL = { easy: 'קל', medium: 'בינוני', hard: 'קשה' };
const DIFF_COLOR = { easy: '#3fb950', medium: '#d29922', hard: '#f85149' };

export default function PatternCard({ pattern, onClick }) {
  // Show only the last 8-10 candles of questionCandles as the mini preview
  const previewCandles = pattern.questionCandles.slice(-10);

  return (
    <button className="pattern-card" onClick={() => onClick(pattern)}>
      <div className="pc-chart">
        <MiniChart candles={previewCandles} width={180} height={72} />
      </div>

      <div className="pc-body">
        <div className="pc-tags">
          <span className={`pc-dir ${pattern.direction}`}>
            {pattern.direction === 'bullish' ? '▲' : '▼'}
            {' '}{pattern.direction === 'bullish' ? 'שורי' : 'דובי'}
          </span>
          <span className="pc-diff" style={{ color: DIFF_COLOR[pattern.difficulty] }}>
            {DIFF_LABEL[pattern.difficulty]}
          </span>
        </div>

        <h3 className="pc-name">{pattern.name}</h3>
        <p className="pc-en">{pattern.english}</p>
        <p className="pc-desc">{pattern.description}</p>

        <div className="pc-footer">
          <span className="pc-winrate">✓ {pattern.winRate}% דיוק</span>
          <span className="pc-arrow">←</span>
        </div>
      </div>
    </button>
  );
}
