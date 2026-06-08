export default function GameControls({ onAnswer, disabled }) {
  return (
    <div className="controls">
      <button
        className="ctrl-btn up"
        onClick={() => onAnswer('up')}
        disabled={disabled}
        title="חץ למעלה ↑"
      >
        <span className="ctrl-arrow">↑</span>
        <span className="ctrl-label">למעלה</span>
        <span className="ctrl-key">↑</span>
      </button>
      <button
        className="ctrl-btn down"
        onClick={() => onAnswer('down')}
        disabled={disabled}
        title="חץ למטה ↓"
      >
        <span className="ctrl-arrow">↓</span>
        <span className="ctrl-label">למטה</span>
        <span className="ctrl-key">↓</span>
      </button>
    </div>
  );
}
