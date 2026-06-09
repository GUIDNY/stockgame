const DIFF_LABEL = { easy: 'קל', medium: 'בינוני', hard: 'קשה' };

export default function ResultOverlay({ isCorrect, pattern, onNext }) {
  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-40 p-4" onClick={onNext}>
      <div className={`bg-surface-container rounded-lg p-8 max-w-md text-center transition-transform transform ${
        isCorrect ? 'scale-100' : 'scale-100'
      }`} onClick={(e) => e.stopPropagation()}>

        <div className="text-6xl mb-4">
          {isCorrect ? '✓' : '✗'}
        </div>

        <h2 className={`text-2xl font-bold mb-2 ${
          isCorrect ? 'text-green-400' : 'text-red-400'
        }`}>
          {isCorrect ? 'נכון!' : 'טעות'}
        </h2>

        <p className="text-lg text-text mb-6">{pattern.name}</p>

        {!isCorrect && (
          <div className="bg-surface rounded-lg p-4 mb-6">
            <p className="text-text-2 text-sm">
              התשובה הנכונה היא: <span className="font-bold text-primary">{pattern.answer === 'up' ? '▲ עלייה' : '▼ ירידה'}</span>
            </p>
          </div>
        )}

        {pattern.checklist && (
          <div className="mb-6 text-left">
            <p className="text-sm text-text-2 font-bold mb-2">בדיקות:</p>
            <ul className="text-xs text-text-2 space-y-1">
              {pattern.checklist.slice(0, 2).map((item, i) => (
                <li key={i}>• {item}</li>
              ))}
            </ul>
          </div>
        )}

        <button
          onClick={onNext}
          className="w-full bg-primary hover:bg-primary-dark text-surface py-3 rounded-lg font-bold transition-colors"
        >
          המשך →
        </button>
      </div>
    </div>
  );
}
