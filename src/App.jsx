import { useState, useCallback } from 'react';
import Navbar from './components/Navbar';
import HomePage from './pages/HomePage';
import LearnPage from './pages/LearnPage';
import LessonsPage from './pages/LessonsPage';
import PracticePage from './pages/PracticePage';
import MarketPage from './pages/MarketPage';
import PlayPage from './pages/PlayPage';
import { useMarketData } from './hooks/useMarketData';

export default function App() {
  const [page, setPage] = useState('home');
  const [score, setScore] = useState(0);
  const [streak, setStreak] = useState(0);
  const [filterPatternId, setFilterPatternId] = useState(null);
  const [timeframe, setTimeframe] = useState('1D');

  const marketData = useMarketData(timeframe);

  const navigate = useCallback((target, opts = {}) => {
    setFilterPatternId(opts.patternId || null);
    setPage(target);
  }, []);

  const handleScoreChange = useCallback((s, st) => {
    setScore(s);
    setStreak(st);
  }, []);

  const handlePractice = useCallback((pattern) => {
    navigate('play', { patternId: pattern.id });
  }, [navigate]);

  const handleTimeframeChange = useCallback((tf) => {
    setTimeframe(tf);
    // Reset score/streak when timeframe changes
    setScore(0);
    setStreak(0);
  }, []);

  return (
    <div className="min-h-screen bg-surface flex flex-col" dir="rtl">
      <Navbar page={page} onNavigate={navigate} score={score} streak={streak} />

      <main className="flex-1 overflow-y-auto flex flex-col min-h-0">
        {page === 'home' && (
          <HomePage
            onNavigate={navigate}
            dataStatus={marketData.status}
            realPatternCount={marketData.realPatterns.length}
          />
        )}
        {page === 'market' && (
          <MarketPage />
        )}
        {page === 'lessons' && (
          <LessonsPage onNavigate={navigate} />
        )}
        {page === 'practice' && (
          <PracticePage realPatterns={marketData.realPatterns} />
        )}
        {page === 'learn' && (
          <LearnPage onPractice={handlePractice} />
        )}
        {page === 'play' && (
          <PlayPage
            key={`${filterPatternId || 'all'}-${timeframe}`}
            realPatterns={marketData.realPatterns}
            dataStatus={marketData.status}
            onScoreChange={handleScoreChange}
            filterPatternId={filterPatternId}
            timeframe={timeframe}
            onTimeframeChange={handleTimeframeChange}
          />
        )}
      </main>
    </div>
  );
}
