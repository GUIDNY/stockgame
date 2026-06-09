export default function GameControls({ onAnswer, disabled }) {
  return (
    <div className="flex gap-4 justify-center">
      <button
        onClick={() => onAnswer('up')}
        disabled={disabled}
        className="bg-green-500 hover:bg-green-600 disabled:bg-green-500/50 disabled:cursor-not-allowed text-white px-8 py-3 rounded-lg font-bold text-lg transition-colors"
      >
        ▲ עלייה
      </button>
      <button
        onClick={() => onAnswer('down')}
        disabled={disabled}
        className="bg-red-500 hover:bg-red-600 disabled:bg-red-500/50 disabled:cursor-not-allowed text-white px-8 py-3 rounded-lg font-bold text-lg transition-colors"
      >
        ▼ ירידה
      </button>
    </div>
  );
}
