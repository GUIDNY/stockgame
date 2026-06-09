import MiniChart from './MiniChart';

const DIFF_LABEL = { easy: 'קל', medium: 'בינוני', hard: 'קשה' };
const DIFF_COLOR = { easy: '#3fb950', medium: '#d29922', hard: '#f85149' };

export default function PatternCard({ pattern, onClick }) {
  return (
    <div
      onClick={onClick}
      className="bg-surface-container border border-border rounded-lg p-4 cursor-pointer hover:border-primary transition-all"
    >
      <MiniChart candles={pattern.questionCandles.slice(-10)} width="100%" height="100" />
      <div className="mt-4">
        <div className="text-sm font-bold mb-2" style={{ color: DIFF_COLOR[pattern.difficulty] }}>
          {DIFF_LABEL[pattern.difficulty]}
        </div>
        <h3 className="text-lg font-bold text-text mb-1">{pattern.name}</h3>
        <p className="text-sm text-text-2 mb-3">{pattern.description}</p>
        <div className="text-sm text-text-3">ניצחון: {pattern.winRate}%</div>
      </div>
    </div>
  );
}
