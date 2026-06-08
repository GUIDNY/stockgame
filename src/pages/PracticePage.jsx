import { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import CandlestickChart from '../components/CandlestickChart';
import { IconPuzzle, IconZap, IconSkull, IconTrophy, IconTarget, IconBook, IconClock, IconFire } from '../components/Icons';
import patternDefinitions from '../data/patternDefinitions';

function shuffle(arr) {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

/* ── Mode catalog ─────────────────────────────────────────────────────────── */
const MODES = [
  { id: 'quiz',     icon: IconPuzzle, title: 'זיהוי תבנית',  color: '#388bfd', diff: 'מתחיל',  desc: 'ראה גרף — בחר את שם התבנית מ-4 אפשרויות. מחדד זיכרון ושמות.' },
  { id: 'speed',    icon: IconZap, title: 'מרוץ הזמן',    color: '#d29922', diff: 'בינוני', desc: '15 שניות לכל שאלה — מה יקרה? מהירות ודיוק.' },
  { id: 'survival', icon: IconSkull, title: 'מצב הישרדות', color: '#f85149', diff: 'קשה',    desc: 'שגיאה אחת = Game Over. כמה רצף תצבור?' },
];

function ModeCatalog({ onSelect }) {
  return (
    <div className="practice-page">
      <div className="practice-header">
        <div className="practice-header-inner">
          <h1 className="practice-title">מצבי תרגול</h1>
          <p className="practice-sub">כל מצב מחדד יכולת אחרת — בחר והתחל</p>
        </div>
      </div>
      <div className="practice-content">
        <div className="practice-modes">
          {MODES.map((m) => (
            <button
              key={m.id}
              type="button"
              className="mode-card"
              style={{ '--mode-color': m.color }}
              onClick={() => onSelect(m.id)}
            >
              <div className="mode-icon" style={{ background: m.color + '1a', color: m.color }}><m.icon /></div>
              <div className="mode-body">
                <div className="mode-diff" style={{ color: m.color }}>{m.diff}</div>
                <h3 className="mode-title">{m.title}</h3>
                <p className="mode-desc">{m.desc}</p>
              </div>
              <span className="mode-arrow" style={{ color: m.color }}>←</span>
            </button>
          ))}
        </div>
        <div className="practice-tip">
          <span>💡</span>
          <span>
            <strong>המלצה:</strong> התחל מ"זיהוי תבנית" ואז עבור ל"מרוץ הזמן".
          </span>
        </div>
      </div>
    </div>
  );
}

/* ── QUIZ MODE ────────────────────────────────────────────────────────────── */
function QuizMode({ onBack }) {
  const patterns = useMemo(() => shuffle([...patternDefinitions]), []);
  const [idx,      setIdx]     = useState(0);
  const [selected, setSelected] = useState(null);
  const [score,    setScore]   = useState(0);
  const [phase,    setPhase]   = useState('question'); // 'question' | 'answer' | 'done'
  const timerRef = useRef(null);

  const current = patterns[idx];
  const total   = patterns.length;

  const choices = useMemo(() => {
    if (!current) return [];
    const wrong = shuffle(patternDefinitions.filter((p) => p.id !== current.id)).slice(0, 3).map((p) => p.name);
    return shuffle([current.name, ...wrong]);
  }, [current]);

  const advance = useCallback(() => {
    if (idx + 1 >= total) { setPhase('done'); return; }
    setIdx((i) => i + 1);
    setSelected(null);
    setPhase('question');
  }, [idx, total]);

  const handleSelect = useCallback((choice) => {
    if (phase !== 'question') return;
    setSelected(choice);
    if (choice === current.name) setScore((s) => s + 1);
    setPhase('answer');
    timerRef.current = setTimeout(advance, 2200);
  }, [phase, current, advance]);

  useEffect(() => () => { if (timerRef.current) clearTimeout(timerRef.current); }, []);

  // Restart
  const restart = () => {
    if (timerRef.current) clearTimeout(timerRef.current);
    setIdx(0); setScore(0); setSelected(null); setPhase('question');
  };

  if (phase === 'done') {
    const pct = Math.round((score / total) * 100);
    return (
      <div className="practice-page">
        <div className="pr-result">
          <div className="pr-emoji">
            {pct >= 80 ? <IconTrophy /> : pct >= 60 ? <IconTarget /> : <IconBook />}
          </div>
          <h2>סיום הקוויז!</h2>
          <div className="pr-score">{score}/{total}</div>
          <div className="pr-pct">{pct}% דיוק</div>
          <p className="pr-msg">{pct >= 80 ? 'מעולה! אתה מכיר היטב את התבניות.' : 'קרא את השיעורים וחזור לנסות!'}</p>
          <div className="pr-btns">
            <button type="button" className="pr-btn primary" onClick={restart}>נסה שוב</button>
            <button type="button" className="pr-btn secondary" onClick={onBack}>← מצבים</button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="practice-page quiz-page">
      {/* Header */}
      <div className="quiz-header">
        <button type="button" className="sv-back" onClick={onBack}>← מצבים</button>
        <div className="quiz-progress-bar">
          <div className="quiz-progress-fill" style={{ width: `${(idx / total) * 100}%` }} />
        </div>
        <span className="quiz-counter">{idx + 1}/{total} · ✓{score}</span>
      </div>

      {/* Chart */}
      <div className="quiz-chart-area">
        <CandlestickChart
          key={current.id}
          candles={current.questionCandles}
          revealCandles={[]}
          isRevealing={false}
          onRevealComplete={() => {}}
        />
      </div>

      <p className="quiz-question">מה שם התבנית?</p>

      {/* Choices */}
      <div className="quiz-choices">
        {choices.map((choice) => {
          let cls = 'choice-btn';
          if (phase === 'answer') {
            if (choice === current.name) cls += ' correct';
            else if (choice === selected) cls += ' wrong';
            else cls += ' dim';
          }
          return (
            <button
              key={choice}
              type="button"
              className={cls}
              onClick={() => handleSelect(choice)}
              disabled={phase === 'answer'}
            >
              {choice}
            </button>
          );
        })}
      </div>

      {/* Feedback */}
      {phase === 'answer' && (
        <div className={`quiz-feedback ${selected === current.name ? 'correct' : 'wrong'}`}>
          {selected === current.name
            ? `✓ נכון! — ${current.tip}`
            : `✗ זו: ${current.name} — ${current.tip}`}
        </div>
      )}
    </div>
  );
}

/* ── SPEED MODE ───────────────────────────────────────────────────────────── */
const SPEED_SEC = 15;

function SpeedMode({ onBack, realPatterns }) {
  const all = useMemo(() => {
    const synth = patternDefinitions.map((p) => ({ ...p, isReal: false }));
    return shuffle([...realPatterns.slice(0, 15), ...synth]);
  }, [realPatterns]);

  const [idx,      setIdx]      = useState(0);
  const [timeLeft, setTimeLeft] = useState(SPEED_SEC);
  const [phase,    setPhase]    = useState('playing'); // 'playing' | 'paused' | 'done'
  const [score,    setScore]    = useState(0);
  const [streak,   setStreak]   = useState(0);
  const [flash,    setFlash]    = useState('');

  const flashRef  = useRef(null);
  const advanceRef = useRef(null);

  const current = all[idx];
  const total   = all.length;

  const advance = useCallback((correct) => {
    if (correct) { setScore((s) => s + 1); setStreak((s) => s + 1); }
    else setStreak(0);

    const cls = correct ? 'flash-c' : 'flash-w';
    setFlash(cls);
    if (flashRef.current) clearTimeout(flashRef.current);
    flashRef.current = setTimeout(() => setFlash(''), 600);

    setPhase('paused');
    advanceRef.current = setTimeout(() => {
      if (idx + 1 >= total) { setPhase('done'); return; }
      setIdx((i) => i + 1);
      setTimeLeft(SPEED_SEC);
      setPhase('playing');
    }, 700);
  }, [idx, total]);

  // Countdown
  useEffect(() => {
    if (phase !== 'playing') return;
    if (timeLeft <= 0) { advance(false); return; }
    const t = setTimeout(() => setTimeLeft((s) => s - 1), 1000);
    return () => clearTimeout(t);
  }, [timeLeft, phase, advance]);

  const handleAnswer = useCallback((answer) => {
    if (phase !== 'playing') return;
    advance(answer === current.answer);
  }, [phase, current, advance]);

  useEffect(() => {
    const onKey = (e) => {
      if (phase === 'playing') {
        if (e.key === 'ArrowUp')   { e.preventDefault(); handleAnswer('up');   }
        if (e.key === 'ArrowDown') { e.preventDefault(); handleAnswer('down'); }
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [phase, handleAnswer]);

  useEffect(() => () => {
    if (flashRef.current) clearTimeout(flashRef.current);
    if (advanceRef.current) clearTimeout(advanceRef.current);
  }, []);

  const restart = () => {
    if (flashRef.current) clearTimeout(flashRef.current);
    if (advanceRef.current) clearTimeout(advanceRef.current);
    setIdx(0); setScore(0); setStreak(0); setTimeLeft(SPEED_SEC); setFlash(''); setPhase('playing');
  };

  if (phase === 'done') {
    const pct = Math.round((score / total) * 100);
    return (
      <div className="practice-page">
        <div className="pr-result">
          <div className="pr-emoji">
            {pct >= 80 ? <IconZap /> : <IconClock />}
          </div>
          <h2>סיים!</h2>
          <div className="pr-score">{score}/{total}</div>
          <div className="pr-pct">{pct}% דיוק</div>
          <div className="pr-btns">
            <button type="button" className="pr-btn primary" onClick={restart}>נסה שוב</button>
            <button type="button" className="pr-btn secondary" onClick={onBack}>← מצבים</button>
          </div>
        </div>
      </div>
    );
  }

  const timerPct   = (timeLeft / SPEED_SEC) * 100;
  const timerColor = timeLeft > 8 ? '#3fb950' : timeLeft > 4 ? '#d29922' : '#f85149';

  return (
    <div className={`practice-page speed-page ${flash}`}>
      <div className="speed-header">
        <button type="button" className="sv-back" onClick={onBack}>← מצבים</button>
        <div className="speed-stats">
          <span className="speed-stat">✓ {score}</span>
          <span className="speed-stat fire">{streak}</span>
          <span className="speed-stat">{idx + 1}/{total}</span>
        </div>
      </div>

      <div className="speed-timer-bar">
        <div
          className="speed-timer-fill"
          style={{ width: `${timerPct}%`, background: timerColor, transition: 'width 1s linear' }}
        />
        <span className="speed-timer-num" style={{ color: timerColor }}>{timeLeft}s</span>
      </div>

      <div className="speed-chart">
        <CandlestickChart
          key={`speed-${current.id || idx}`}
          candles={current.questionCandles}
          revealCandles={[]}
          isRevealing={false}
          onRevealComplete={() => {}}
        />
      </div>

      <div className="speed-controls">
        <button type="button" className="ctrl-btn up" onClick={() => handleAnswer('up')} disabled={phase !== 'playing'}>
          <span className="ctrl-arrow">↑</span><span className="ctrl-label">למעלה</span>
        </button>
        <button type="button" className="ctrl-btn down" onClick={() => handleAnswer('down')} disabled={phase !== 'playing'}>
          <span className="ctrl-arrow">↓</span><span className="ctrl-label">למטה</span>
        </button>
      </div>
    </div>
  );
}

/* ── SURVIVAL MODE ────────────────────────────────────────────────────────── */
function SurvivalMode({ onBack, realPatterns }) {
  const makeQueue = useCallback(() => {
    const synth = patternDefinitions.map((p) => ({ ...p, isReal: false }));
    return shuffle([...realPatterns.slice(0, 30), ...synth, ...synth]);
  }, [realPatterns]);

  const [queue,   setQueue]     = useState(makeQueue);
  const [idx,     setIdx]       = useState(0);
  const [streak,  setStreak]    = useState(0);
  const [best,    setBest]      = useState(() => parseInt(localStorage.getItem('cg_survival') || '0'));
  const [phase,   setPhase]     = useState('playing'); // 'playing' | 'revealing' | 'dead'
  const [flash,   setFlash]     = useState('');
  const [isRevealing, setRevealing] = useState(false);

  const flashRef   = useRef(null);
  const advanceRef = useRef(null);

  const current = queue[idx % queue.length];

  const handleAnswer = useCallback((answer) => {
    if (phase !== 'playing') return;
    const correct = answer === current.answer;

    setFlash(correct ? 'flash-c' : 'flash-w');
    if (flashRef.current) clearTimeout(flashRef.current);
    flashRef.current = setTimeout(() => setFlash(''), 600);

    setRevealing(true);
    setPhase('revealing');

    if (correct) {
      const n = streak + 1;
      setStreak(n);
      const nb = Math.max(parseInt(localStorage.getItem('cg_survival') || '0'), n);
      localStorage.setItem('cg_survival', nb);
      setBest(nb);
      advanceRef.current = setTimeout(() => {
        setRevealing(false);
        setIdx((i) => i + 1);
        setPhase('playing');
        setFlash('');
      }, 1800);
    } else {
      advanceRef.current = setTimeout(() => {
        setRevealing(false);
        setPhase('dead');
      }, 1200);
    }
  }, [phase, current, streak]);

  useEffect(() => {
    const onKey = (e) => {
      if (phase === 'playing') {
        if (e.key === 'ArrowUp')   { e.preventDefault(); handleAnswer('up');   }
        if (e.key === 'ArrowDown') { e.preventDefault(); handleAnswer('down'); }
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [phase, handleAnswer]);

  useEffect(() => () => {
    if (flashRef.current) clearTimeout(flashRef.current);
    if (advanceRef.current) clearTimeout(advanceRef.current);
  }, []);

  const restart = () => {
    if (flashRef.current) clearTimeout(flashRef.current);
    if (advanceRef.current) clearTimeout(advanceRef.current);
    setQueue(makeQueue());
    setIdx(0); setStreak(0); setFlash(''); setRevealing(false); setPhase('playing');
  };

  return (
    <div className={`practice-page survival-page ${flash}`}>
      <div className="survival-header">
        <button type="button" className="sv-back" onClick={onBack}>← מצבים</button>
        <div className="survival-streak-display">
          <span className="survival-streak-label">רצף</span>
          <span className="survival-streak-num">{streak}</span>
        </div>
        <span className="survival-best">שיא: {best}</span>
      </div>

      <div className="survival-chart">
        <CandlestickChart
          key={`surv-${current.id || (idx % queue.length)}`}
          candles={current.questionCandles}
          revealCandles={current.revealCandles}
          isRevealing={isRevealing}
          onRevealComplete={() => {}}
        />
      </div>

      <div className="survival-controls">
        <div className="survival-lives"><IconSkull /> שגיאה אחת = Game Over</div>
        <div className="speed-controls">
          <button type="button" className="ctrl-btn up" onClick={() => handleAnswer('up')} disabled={phase !== 'playing'}>
            <span className="ctrl-arrow">↑</span><span className="ctrl-label">למעלה</span>
          </button>
          <button type="button" className="ctrl-btn down" onClick={() => handleAnswer('down')} disabled={phase !== 'playing'}>
            <span className="ctrl-arrow">↓</span><span className="ctrl-label">למטה</span>
          </button>
        </div>
      </div>

      {/* Game Over overlay */}
      {phase === 'dead' && (
        <div className="game-over-overlay">
          <div className="game-over-card">
            <div className="go-emoji"><IconSkull /></div>
            <h2 className="go-title">Game Over</h2>
            <div className="go-streak">{streak}</div>
            <div className="go-label">רצף</div>
            {streak >= best && streak > 0 && <div className="go-new-best"><IconTrophy /> שיא חדש!</div>}
            <div className="go-best">שיא: {best}</div>
            <p className="go-pattern">
              פסלת על: <strong>{current.name}</strong>
            </p>
            <div className="go-btns">
              <button type="button" className="pr-btn primary" onClick={restart}>נסה שוב</button>
              <button type="button" className="pr-btn secondary" onClick={onBack}>← מצבים</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

/* ── Root ─────────────────────────────────────────────────────────────────── */
export default function PracticePage({ realPatterns = [] }) {
  const [mode, setMode] = useState(null);

  if (!mode) return <ModeCatalog onSelect={setMode} />;
  if (mode === 'quiz')     return <QuizMode     onBack={() => setMode(null)} realPatterns={realPatterns} />;
  if (mode === 'speed')    return <SpeedMode    onBack={() => setMode(null)} realPatterns={realPatterns} />;
  if (mode === 'survival') return <SurvivalMode onBack={() => setMode(null)} realPatterns={realPatterns} />;
}
