import { useState } from 'react';

export default function Navbar({ page, onNavigate, score, streak }) {
  const [menuOpen, setMenuOpen] = useState(false);

  const handleNavClick = (target) => {
    onNavigate(target);
    setMenuOpen(false);
  };

  return (
    <nav className="navbar">
      <button className="nav-brand" onClick={() => handleNavClick('home')}>
        <span className="nav-logo">📊</span>
        <span className="nav-title">קרא את הגרף</span>
      </button>

      <button className="nav-hamburger" onClick={() => setMenuOpen(!menuOpen)}>
        <span className="hamburger-line"></span>
        <span className="hamburger-line"></span>
        <span className="hamburger-line"></span>
      </button>

      <div className={`nav-links ${menuOpen ? 'open' : ''}`}>
        <button className={`nav-link ${page === 'market' ? 'active' : ''}`} onClick={() => handleNavClick('market')}>
          <span className="nav-link-icon">📈</span>שוק
        </button>
        <button className={`nav-link ${page === 'lessons' ? 'active' : ''}`} onClick={() => handleNavClick('lessons')}>
          <span className="nav-link-icon">🎓</span>שיעורים
        </button>
        <button className={`nav-link ${page === 'learn' ? 'active' : ''}`} onClick={() => handleNavClick('learn')}>
          <span className="nav-link-icon">📚</span>תבניות
        </button>
        <button className={`nav-link ${page === 'practice' ? 'active' : ''}`} onClick={() => handleNavClick('practice')}>
          <span className="nav-link-icon">🏋️</span>תרגול
        </button>
        <button className={`nav-link ${page === 'play' ? 'active' : ''}`} onClick={() => handleNavClick('play')}>
          <span className="nav-link-icon">🎮</span>משחק
        </button>
      </div>

      {page === 'play' && (
        <div className="nav-game-stats">
          <div className="nav-stat">
            <span className="nav-stat-label">ציון</span>
            <span className="nav-stat-val">{score}</span>
          </div>
          <div className="nav-stat-div" />
          <div className="nav-stat">
            <span className="nav-stat-label">סטריק</span>
            <span className={`nav-stat-val ${streak > 2 ? 'fire' : ''}`}>{streak}{streak > 2 ? ' 🔥' : ''}</span>
          </div>
        </div>
      )}
    </nav>
  );
}
