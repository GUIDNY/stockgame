import { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import CandlestickChart from '../components/CandlestickChart';
import patternDefinitions from '../data/patternDefinitions';

function shuffle(arr) {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

const MODES = [
  { id: 'quiz',     title: 'זיהוי תבנית',  color: '#388bfd', diff: 'מתחיל',  desc: 'ראה גרף — בחר את שם התבנית מ-4 אפשרויות.' },
  { id: 'speed',    title: 'מרוץ הזמן',    color: '#d29922', diff: 'בינוני', desc: '15 שניות לכל שאלה — מה יקרה?' },
  { id: 'survival', title: 'מצב הישרדות', color: '#f85149', diff: 'קשה',    desc: 'שגיאה אחת = Game Over. כמה רצף?' },
];

function ModeCatalog({ onSelect }) {
  return (
    <div className="bg-surface min-h-screen pt-20 pb-12">
      <div className="max-w-4xl mx-auto px-6">
        <h1 className="text-4xl font-bold text-text mb-2">מצבי תרגול</h1>
        <p className="text-text-2 mb-8">כל מצב מחדד יכולת אחרת — בחר והתחל</p>

        <div className="space-y-4 mb-8">
          {MODES.map((m) => (
            <button
              key={m.id}
              onClick={() => onSelect(m.id)}
              className="w-full flex items-start gap-4 p-6 rounded-lg border border-border bg-surface-container hover:border-primary transition-colors text-right"
              style={{ borderColor: m.color + '44' }}
            >
              <div className="flex-1">
                <div className="text-sm font-bold mb-1" style={{ color: m.color }}>{m.diff}</div>
                <h3 className="text-xl font-bold text-text mb-2">{m.title}</h3>
                <p className="text-sm text-text-2">{m.desc}</p>
              </div>
              <span className="text-3xl flex-shrink-0" style={{ color: m.color }}>←</span>
            </button>
          ))}
        </div>

        <div className="p-4 bg-primary/10 rounded-lg border border-primary/20 flex gap-3">
          <span className="text-xl flex-shrink-0">💡</span>
          <div className="text-text-2">
            <strong>המלצה:</strong> התחל מ"זיהוי תבנית" ואז עבור ל"מרוץ הזמן".
          </div>
        </div>
      </div>
    </div>
  );
}

const SPEED_SEC = 15;

function QuizMode({ onBack }) {
  const patterns = useMemo(() => shuffle([...patternDefinitions]), []);
  const [idx,      setIdx]     = useState(0);
  const [selected, setSelected] = useState(null);
  const [score,    setScore]   = useState(0);
  const [phase,    setPhase]   = useState('question');
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

  const restart = () => {
    if (timerRef.current) clearTimeout(timerRef.current);
    setIdx(0); setScore(0); setSelected(null); setPhase('question');
  };

  if (phase === 'done') {
    const pct = Math.round((score / total) * 100);
    return (
      <div className="bg-surface min-h-screen flex items-center justify-center pt-20">
        <div className="text-center">
          <div className="text-6xl mb-4">{pct >= 80 ? '🏆' : pct >= 60 ? '🎯' : '📖'}</div>
          <h2 className="text-4xl font-bold text-text mb-2">סיום הקוויז!</h2>
          <div className="text-5xl font-bold text-primary mb-2">{score}/{total}</div>
          <div className="text-2xl text-text mb-6">{pct}% דיוק</div>
          <p className="text-text-2 mb-8">{pct >= 80 ? 'מעולה! אתה מכיר היטב את התבניות.' : 'קרא את השיעורים וחזור לנסות!'}</p>
          <div className="flex gap-4 justify-center">
            <button onClick={restart} className="bg-primary hover:bg-primary-dark text-surface px-6 py-2 rounded-lg font-bold">נסה שוב</button>
            <button onClick={onBack} className="bg-surface-bright hover:bg-surface-container text-text px-6 py-2 rounded-lg font-bold">← מצבים</button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-surface min-h-screen pt-20 pb-4 flex flex-col">
      <div className="flex items-center justify-between px-4 py-3 bg-surface-container border-b border-border">
        <button onClick={onBack} className="text-primary hover:text-primary-dark font-bold">← מצבים</button>
        <div className="h-1 flex-1 bg-surface-bright mx-4 rounded-full overflow-hidden">
          <div className="h-full bg-primary transition-all" style={{ width: `${(idx / total) * 100}%` }} />
        </div>
        <span className="text-sm text-text-2 font-bold whitespace-nowrap">{idx + 1}/{total} ✓{score}</span>
      </div>

      <div className="flex-1 overflow-hidden">
        <CandlestickChart
          key={current.id}
          candles={current.questionCandles}
          revealCandles={[]}
          isRevealing={false}
          onRevealComplete={() => {}}
        />
      </div>

      <div className="bg-surface-container border-t border-border px-4 py-6 text-center">
        <p className="text-text-2 mb-4 font-semibold">מה שם התבנית?</p>
        <div className="grid grid-cols-2 gap-2 mb-4">
          {choices.map((choice) => {
            let btnClass = 'bg-surface-bright hover:bg-surface-container text-text';
            if (phase === 'answer') {
              if (choice === current.name) btnClass = 'bg-green-500/30 text-green-400';
              else if (choice === selected) btnClass = 'bg-red-500/30 text-red-400';
              else btnClass = 'bg-surface-bright/50 text-text-2 opacity-50';
            }
            return (
              <button
                key={choice}
                onClick={() => handleSelect(choice)}
                disabled={phase === 'answer'}
                className={`py-2 rounded-lg font-bold transition-colors ${btnClass}`}
              >
                {choice}
              </button>
            );
          })}
        </div>

        {phase === 'answer' && (
          <div className={`p-3 rounded-lg text-sm font-semibold ${
            selected === current.name
              ? 'bg-green-500/20 text-green-400'
              : 'bg-red-500/20 text-red-400'
          }`}>
            {selected === current.name
              ? `✓ נכון! — ${current.tip}`
              : `✗ זו: ${current.name} — ${current.tip}`}
          </div>
        )}
      </div>
    </div>
  );
}

function SpeedMode({ onBack, realPatterns }) {
  const all = useMemo(() => {
    const synth = patternDefinitions.map((p) => ({ ...p, isReal: false }));
    return shuffle([...realPatterns.slice(0, 15), ...synth]);
  }, [realPatterns]);

  const [idx,      setIdx]      = useState(0);
  const [timeLeft, setTimeLeft] = useState(SPEED_SEC);
  const [phase,    setPhase]    = useState('playing');
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

    const cls = correct ? 'bg-green-500/20' : 'bg-red-500/20';
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
      <div className="bg-surface min-h-screen flex items-center justify-center pt-20">
        <div className="text-center">
          <div className="text-6xl mb-4">{pct >= 80 ? '⚡' : '🕐'}</div>
          <h2 className="text-4xl font-bold text-text mb-2">סיים!</h2>
          <div className="text-5xl font-bold text-primary mb-2">{score}/{total}</div>
          <div className="text-2xl text-text mb-6">{pct}% דיוק</div>
          <div className="flex gap-4 justify-center">
            <button onClick={restart} className="bg-primary hover:bg-primary-dark text-surface px-6 py-2 rounded-lg font-bold">נסה שוב</button>
            <button onClick={onBack} className="bg-surface-bright hover:bg-surface-container text-text px-6 py-2 rounded-lg font-bold">← מצבים</button>
          </div>
        </div>
      </div>
    );
  }

  const timerPct   = (timeLeft / SPEED_SEC) * 100;
  const timerColor = timeLeft > 8 ? '#3fb950' : timeLeft > 4 ? '#d29922' : '#f85149';

  return (
    <div className={`bg-surface min-h-screen pt-20 pb-4 flex flex-col transition-colors ${flash}`}>
      <div className="flex items-center justify-between px-4 py-3 bg-surface-container border-b border-border">
        <button onClick={onBack} className="text-primary hover:text-primary-dark font-bold">← מצבים</button>
        <div className="flex gap-4 text-sm text-text-2 font-bold">
          <span>✓ {score}</span>
          <span style={{ color: '#f85149' }}>🔥 {streak}</span>
          <span>{idx + 1}/{total}</span>
        </div>
      </div>

      <div className="h-2 bg-surface-bright">
        <div
          className="h-full transition-all"
          style={{ width: `${timerPct}%`, background: timerColor }}
        />
      </div>

      <div className="text-center py-2" style={{ color: timerColor }}>
        <span className="font-bold text-xl">{timeLeft}s</span>
      </div>

      <div className="flex-1 overflow-hidden">
        <CandlestickChart
          key={`speed-${current.id || idx}`}
          candles={current.questionCandles}
          revealCandles={[]}
          isRevealing={false}
          onRevealComplete={() => {}}
        />
      </div>

      <div className="bg-surface-container border-t border-border px-4 py-4 flex gap-4">
        <button
          onClick={() => handleAnswer('up')}
          disabled={phase !== 'playing'}
          className="flex-1 bg-green-500 hover:bg-green-600 disabled:bg-green-500/50 text-white py-3 rounded-lg font-bold transition-colors"
        >
          ↑ עלייה
        </button>
        <button
          onClick={() => handleAnswer('down')}
          disabled={phase !== 'playing'}
          className="flex-1 bg-red-500 hover:bg-red-600 disabled:bg-red-500/50 text-white py-3 rounded-lg font-bold transition-colors"
        >
          ↓ ירידה
        </button>
      </div>
    </div>
  );
}

function SurvivalMode({ onBack, realPatterns }) {
  const makeQueue = useCallback(() => {
    const synth = patternDefinitions.map((p) => ({ ...p, isReal: false }));
    return shuffle([...realPatterns.slice(0, 30), ...synth, ...synth]);
  }, [realPatterns]);

  const [queue,   setQueue]     = useState(makeQueue);
  const [idx,     setIdx]       = useState(0);
  const [streak,  setStreak]    = useState(0);
  const [best,    setBest]      = useState(() => parseInt(localStorage.getItem('cg_survival') || '0'));
  const [phase,   setPhase]     = useState('playing');
  const [flash,   setFlash]     = useState('');
  const [isRevealing, setRevealing] = useState(false);

  const flashRef   = useRef(null);
  const advanceRef = useRef(null);

  const current = queue[idx % queue.length];

  const handleAnswer = useCallback((answer) => {
    if (phase !== 'playing') return;
    const correct = answer === current.answer;

    setFlash(correct ? 'bg-green-500/20' : 'bg-red-500/20');
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
    <div className={`bg-surface min-h-screen pt-20 pb-4 flex flex-col transition-colors ${flash}`}>
      <div className="flex items-center justify-between px-4 py-3 bg-surface-container border-b border-border">
        <button onClick={onBack} className="text-primary hover:text-primary-dark font-bold">← מצבים</button>
        <div className="text-right">
          <div className="text-xs text-text-2">רצף</div>
          <div className="text-2xl font-bold text-primary">{streak}</div>
        </div>
        <span className="text-sm text-text-2 font-bold">שיא: {best}</span>
      </div>

      <div className="flex-1 overflow-hidden">
        <CandlestickChart
          key={`surv-${current.id || (idx % queue.length)}`}
          candles={current.questionCandles}
          revealCandles={current.revealCandles}
          isRevealing={isRevealing}
          onRevealComplete={() => {}}
        />
      </div>

      <div className="bg-surface-container border-t border-border px-4 py-4">
        <div className="text-center text-red-400 font-bold mb-3">💀 שגיאה אחת = Game Over</div>
        <div className="flex gap-4">
          <button
            onClick={() => handleAnswer('up')}
            disabled={phase !== 'playing'}
            className="flex-1 bg-green-500 hover:bg-green-600 disabled:bg-green-500/50 text-white py-3 rounded-lg font-bold transition-colors"
          >
            ↑ עלייה
          </button>
          <button
            onClick={() => handleAnswer('down')}
            disabled={phase !== 'playing'}
            className="flex-1 bg-red-500 hover:bg-red-600 disabled:bg-red-500/50 text-white py-3 rounded-lg font-bold transition-colors"
          >
            ↓ ירידה
          </button>
        </div>
      </div>

      {phase === 'dead' && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-40">
          <div className="bg-surface-container rounded-lg p-8 text-center">
            <div className="text-6xl mb-4">💀</div>
            <h2 className="text-3xl font-bold text-text mb-2">Game Over</h2>
            <div className="text-5xl font-bold text-primary mb-2">{streak}</div>
            <div className="text-text-2 mb-2">רצף</div>
            {streak >= best && streak > 0 && <div className="text-green-400 font-bold mb-4">🏆 שיא חדש!</div>}
            <div className="text-text-2 mb-6">שיא: {best}</div>
            <p className="text-text-2 mb-8">פסלת על: <strong>{current.name}</strong></p>
            <div className="flex gap-4">
              <button onClick={restart} className="flex-1 bg-primary hover:bg-primary-dark text-surface px-4 py-2 rounded-lg font-bold">נסה שוב</button>
              <button onClick={onBack} className="flex-1 bg-surface-bright hover:bg-surface-container text-text px-4 py-2 rounded-lg font-bold">← מצבים</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default function PracticePage({ realPatterns = [] }) {
  const [mode, setMode] = useState(null);

  if (!mode) return <ModeCatalog onSelect={setMode} />;
  if (mode === 'quiz')     return <QuizMode     onBack={() => setMode(null)} />;
  if (mode === 'speed')    return <SpeedMode    onBack={() => setMode(null)} realPatterns={realPatterns} />;
  if (mode === 'survival') return <SurvivalMode onBack={() => setMode(null)} realPatterns={realPatterns} />;
}
