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

  const [idx, setIdx]           = useState(0);
  const [phase, setPhase]       = useState('playing'); // 'playing' | 'revealing' | 'result' | 'done'
  const [score, setScore]       = useState(0);
  const [streak, setStreak]     = useState(0);
  const [isCorrect, setIsCorrect] = useState(null);
  const [flashClass, setFlash]  = useState('');
  const [showTfMenu, setShowTfMenu] = useState(false);

  const flashRef = useRef(null);
  const current  = queue[idx];
  const total    = queue.length;
  const tf       = TIMEFRAMES[timeframe] || TIMEFRAMES['1D'];

  // Sync score upward
  useEffect(() => { onScoreChange?.(score, streak); }, [score, streak]);

  const handleAnswer = useCallback((answer) => {
    if (phase !== 'playing') return;

    const correct = answer === current.answer;
    setIsCorrect(correct);

    // Flash background
    const cls = correct ? 'flash-correct' : 'flash-wrong';
    setFlash(cls);
    if (flashRef.current) clearTimeout(flashRef.current);
    flashRef.current = setTimeout(() => setFlash(''), 500);

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

  // Keyboard handler
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

  // Cleanup flash timer on unmount
  useEffect(() => () => { if (flashRef.current) clearTimeout(flashRef.current); }, []);

  /* ── DONE screen ─────────────────────────────────────────── */
  if (phase === 'done') {
    const pct = Math.round((score / total) * 100);
    const best = parseInt(localStorage.getItem('cg_best') || '0');
    return (
      <div className="done-screen">
        <div className="done-card">
          <div className="done-emoji">{pct >= 80 ? '🏆' : pct >= 60 ? '📈' : '📊'}</div>
          <h1>סיום!</h1>
          <div className="done-score">{score}<span className="done-total">/{total}</span></div>
          <div className="done-pct">{pct}% דיוק</div>
          <div className="done-best">שיא אישי: {best} 🔥</div>
          <p className="done-msg">
            {pct === 100 ? 'מושלם! אתה מקצוען.' : pct >= 80 ? 'מצוין! הבסיס חזק.' : pct >= 60 ? 'טוב! המשך לתרגל.' : 'המשך לתרגל — זה בא עם ניסיון!'}
          </p>
          <button className="done-restart" onClick={() => window.location.reload()}>שחק שוב</button>
        </div>
      </div>
    );
  }

  /* ── Main game view ──────────────────────────────────────── */
  return (
    <div
      className={`play-page ${flashClass}`}
      onClick={() => showTfMenu && setShowTfMenu(false)}
    >
      {/* Progress bar */}
      <div className="play-progress-track">
        <div className="play-progress-fill" style={{ width: `${(idx / total) * 100}%` }} />
      </div>

      {/* Meta bar */}
      <div className="play-meta">
        <div className="play-meta-left">
          {current.isReal && current.ticker
            ? <span className="ticker-badge">{current.ticker}</span>
            : <span className="ticker-badge synthetic">דוגמה</span>
          }

          {/* Timeframe picker */}
          <div className="tf-selector" onClick={(e) => e.stopPropagation()}>
            <button
              className={`tf-btn ${showTfMenu ? 'open' : ''}`}
              onClick={() => setShowTfMenu((v) => !v)}
              type="button"
            >
              <span className="tf-icon">🕐</span>
              {timeframe}
              <span className="tf-arrow">{showTfMenu ? '▲' : '▼'}</span>
            </button>

            {showTfMenu && (
              <div className="tf-menu">
                <div className="tf-menu-title">בחר טיים-פריים</div>
                {Object.entries(TIMEFRAMES).map(([key, val]) => (
                  <button
                    key={key}
                    type="button"
                    className={`tf-option ${timeframe === key ? 'active' : ''}`}
                    onClick={() => { onTimeframeChange?.(key); setShowTfMenu(false); }}
                  >
                    <div className="tf-opt-key">{key}</div>
                    <div className="tf-opt-info">
                      <span className="tf-opt-label">{val.label}</span>
                      <span className="tf-opt-sub">{val.sublabel}</span>
                    </div>
                    {timeframe === key && <span className="tf-opt-check">✓</span>}
                  </button>
                ))}
                <div className="tf-menu-note">
                  📡 שינוי טיים-פריים מוריד נתונים חדשים מ-Yahoo Finance
                </div>
              </div>
            )}
          </div>
        </div>

        <span className="diff-badge" style={{ color: DIFF_COLOR[current.difficulty] }}>
          {DIFF_LABEL[current.difficulty]}
        </span>
        <span className="question-count">{idx + 1} / {total}</span>
      </div>

      {/* Data loading notice */}
      {dataStatus === 'loading' && (
        <div className="data-notice loading">⏳ טוען נתוני {tf.sublabel} מהשוק...</div>
      )}

      {/* Chart */}
      <div className="play-chart-area">
        <CandlestickChart
          key={`${current.id || idx}-${timeframe}`}
          candles={current.questionCandles}
          revealCandles={current.revealCandles}
          isRevealing={phase === 'revealing'}
          onRevealComplete={handleRevealComplete}
        />
      </div>

      {/* Controls */}
      <div className="play-bottom">
        <p className="play-question">מה יקרה אחר כך?</p>
        <GameControls
          onAnswer={handleAnswer}
          disabled={phase !== 'playing'}
        />
        <p className="play-hint">↑↓ מקלדת</p>
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
