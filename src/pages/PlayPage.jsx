import { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import CandlestickChart from '../components/CandlestickChart';
import GameControls from '../components/GameControls';
import ResultOverlay from '../components/ResultOverlay';
import patternDefinitions from '../data/patternDefinitions';
import { TIMEFRAMES } from '../hooks/useMarketData';

function shuffle(arr) {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

const DIFF_COLOR = { easy: '#3fb950', medium: '#d29922', hard: '#f85149' };
const DIFF_LABEL = { easy: 'קל', medium: 'בינוני', hard: 'קשה' };

export default function PlayPage({
  realPatterns = [],
  dataStatus,
  onScoreChange,
  filterPatternId,
  timeframe = '1D',
  onTimeframeChange,
}) {
  const queue = useMemo(() => {
    let base;
    if (filterPatternId) {
      const realFor = realPatterns.filter((p) => p.patternId === filterPatternId);
      const synthFor = patternDefinitions.filter((p) => p.id === filterPatternId);
      base = realFor.length >= 3 ? realFor : [...realFor, ...synthFor];
    } else {
      const synthetic = patternDefinitions.map((p) => ({ ...p, isReal: false }));
      base = [...realPatterns.slice(0, 30), ...synthetic];
    }
    return shuffle(base);
  }, [realPatterns, filterPatternId]);

  const [idx, setIdx] = useState(0);
  const [phase, setPhase] = useState('playing');
  const [score, setScore] = useState(0);
  const [streak, setStreak] = useState(0);
  const [isCorrect, setIsCorrect] = useState(null);
  const [flashBg, setFlashBg] = useState('');
  const [showTfMenu, setShowTfMenu] = useState(false);

  const flashRef = useRef(null);
  const current = queue[idx];
  const total = queue.length;
  const tf = TIMEFRAMES[timeframe] || TIMEFRAMES['1D'];

  useEffect(() => { onScoreChange?.(score, streak); }, [score, streak]);

  const handleAnswer = useCallback((answer) => {
    if (phase !== 'playing') return;
    const correct = answer === current.answer;
    setIsCorrect(correct);

    const bg = correct ? 'bg-green-500/20' : 'bg-red-500/20';
    setFlashBg(bg);
    if (flashRef.current) clearTimeout(flashRef.current);
    flashRef.current = setTimeout(() => setFlashBg(''), 500);

    if (correct) {
      setScore((s) => s + 1);
      setStreak((s) => {
        const n = s + 1;
        const best = Math.max(parseInt(localStorage.getItem('cg_best') || '0'), n);
        localStorage.setItem('cg_best', best);
        return n;
      });
    } else {
      setStreak(0);
    }
    setPhase('revealing');
  }, [phase, current]);

  const handleRevealComplete = useCallback(() => setPhase('result'), []);

  const handleNext = useCallback(() => {
    if (idx + 1 >= total) { setPhase('done'); return; }
    setIdx((i) => i + 1);
    setPhase('playing');
    setIsCorrect(null);
  }, [idx, total]);

  useEffect(() => {
    const onKey = (e) => {
      if (e.key === 'Escape') { setShowTfMenu(false); return; }
      if (phase === 'playing') {
        if (e.key === 'ArrowUp')   { e.preventDefault(); handleAnswer('up');   return; }
        if (e.key === 'ArrowDown') { e.preventDefault(); handleAnswer('down'); return; }
      }
      if (phase === 'result') {
        if (e.key === ' ' || e.key === 'Enter' || e.key === 'ArrowLeft') {
          e.preventDefault(); handleNext();
        }
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [phase, handleAnswer, handleNext]);

  useEffect(() => () => { if (flashRef.current) clearTimeout(flashRef.current); }, []);

  if (phase === 'done') {
    const pct = Math.round((score / total) * 100);
    const best = parseInt(localStorage.getItem('cg_best') || '0');
    return (
      <div className="fixed inset-0 bg-surface flex items-center justify-center pt-20">
        <div className="text-center">
          <div className="text-6xl mb-4">
            {pct >= 80 ? '🏆' : pct >= 60 ? '📈' : '📊'}
          </div>
          <h1 className="text-4xl font-bold text-text mb-2">סיום!</h1>
          <div className="text-5xl font-bold text-primary mb-2">
            {score}<span className="text-2xl text-text-2">/{total}</span>
          </div>
          <div className="text-2xl text-text mb-4">{pct}% דיוק</div>
          <div className="text-lg text-text-2 mb-6">שיא אישי: {best} 🔥</div>
          <p className="text-text-2 mb-8">
            {pct === 100 ? 'מושלם! אתה מקצוען.' : pct >= 80 ? 'מצוין! הבסיס חזק.' : pct >= 60 ? 'טוב! המשך לתרגל.' : 'המשך לתרגל — זה בא עם ניסיון!'}
          </p>
          <button onClick={() => window.location.reload()} className="bg-primary hover:bg-primary-dark text-surface px-8 py-3 rounded-lg font-bold">
            שחק שוב
          </button>
        </div>
      </div>
    );
  }

  return (
    <div
      className={`bg-surface h-screen flex flex-col transition-colors ${flashBg}`}
      onClick={() => showTfMenu && setShowTfMenu(false)}
    >
      {/* Progress bar */}
      <div className="h-1 bg-surface-bright">
        <div className="h-full bg-primary transition-all" style={{ width: `${(idx / total) * 100}%` }} />
      </div>

      {/* Meta bar */}
      <div className="flex flex-wrap justify-between items-center gap-3 px-4 py-3 bg-surface-container border-b border-border">
        <div className="flex items-center gap-2 flex-wrap">
          {current.isReal && current.ticker
            ? <span className="bg-primary/20 text-primary px-3 py-1 rounded-lg text-sm font-bold whitespace-nowrap">{current.ticker}</span>
            : <span className="bg-surface-bright text-text-2 px-3 py-1 rounded-lg text-sm font-bold">דוגמה</span>
          }

          {/* Timeframe menu */}
          <div className="relative" onClick={(e) => e.stopPropagation()}>
            <button
              onClick={() => setShowTfMenu((v) => !v)}
              className="bg-surface-bright hover:bg-surface-container text-text px-3 py-1 rounded-lg text-sm font-bold whitespace-nowrap"
            >
              {timeframe} {showTfMenu ? '▲' : '▼'}
            </button>

            {showTfMenu && (
              <div className="absolute top-10 right-0 bg-surface-container border border-border rounded-lg shadow-lg z-10 min-w-48">
                <div className="px-4 py-2 text-xs text-text-2 font-bold border-b border-border">בחר טיים-פריים</div>
                {Object.entries(TIMEFRAMES).map(([key, val]) => (
                  <button
                    key={key}
                    type="button"
                    onClick={() => { onTimeframeChange?.(key); setShowTfMenu(false); }}
                    className={`w-full text-right px-4 py-2 text-sm transition-colors ${
                      timeframe === key
                        ? 'bg-primary/20 text-primary'
                        : 'text-text hover:bg-surface-bright'
                    }`}
                  >
                    <div className="font-bold">{key}</div>
                    <div className="text-xs text-text-2">{val.label}</div>
                  </button>
                ))}
                <div className="px-4 py-2 border-t border-border text-xs text-text-2">
                  📡 שינוי טיים-פריים מוריד נתונים חדשים
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="flex items-center gap-3">
          <span className="text-xs font-bold" style={{ color: DIFF_COLOR[current.difficulty] }}>
            {DIFF_LABEL[current.difficulty]}
          </span>
          <span className="text-xs font-bold text-text-2">{idx + 1} / {total}</span>
          <div className="flex items-center gap-2 pl-2 border-l border-border">
            <div className="text-right">
              <div className="text-xs text-text-2">ציון</div>
              <div className="font-bold text-text">{score}</div>
            </div>
            <div className="text-right">
              <div className="text-xs text-text-2">סטריק</div>
              <div className="font-bold text-text">{streak}</div>
            </div>
          </div>
        </div>
      </div>

      {/* Loading notice */}
      {dataStatus === 'loading' && (
        <div className="bg-yellow-500/20 text-yellow-400 px-4 py-2 text-center text-sm font-semibold">
          ⏳ טוען נתוני {tf.sublabel} מהשוק...
        </div>
      )}

      {/* Chart */}
      <div className="flex-1 min-h-0 bg-surface-bright">
        <CandlestickChart
          key={`${current.id || idx}-${timeframe}`}
          candles={current.questionCandles}
          revealCandles={current.revealCandles}
          isRevealing={phase === 'revealing'}
          onRevealComplete={handleRevealComplete}
        />
      </div>

      {/* Controls */}
      <div className="bg-surface-container border-t border-border px-4 py-6 text-center">
        <p className="text-text-2 mb-4 font-semibold">מה יקרה אחר כך?</p>
        <GameControls
          onAnswer={handleAnswer}
          disabled={phase !== 'playing'}
        />
        <p className="text-text-2 text-sm mt-4">↑↓ מקלדת</p>
      </div>

      {/* Result overlay */}
      {phase === 'result' && (
        <ResultOverlay
          isCorrect={isCorrect}
          pattern={current}
          timeframe={timeframe}
          onNext={handleNext}
        />
      )}
    </div>
  );
}
