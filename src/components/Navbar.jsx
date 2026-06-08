export default function Navbar({ page, onNavigate, score, streak }) {
  return (
    <nav className="navbar">
      <button className="nav-brand" onClick={() => onNavigate('home')}>
        <span className="nav-logo">📊</span>
        <span className="nav-title">קרא את הגרף</span>
      </button>

      <div className="nav-links">
        <button className={`nav-link ${page === 'market' ? 'active' : ''}`} onClick={() => onNavigate('market')}>
          <span className="nav-link-icon">📈</span>שוק
        </button>
        <button className={`nav-link ${page === 'lessons' ? 'active' : ''}`} onClick={() => onNavigate('lessons')}>
          <span className="nav-link-icon">🎓</span>שיעורים
        </button>
        <button className={`nav-link ${page === 'learn' ? 'active' : ''}`} onClick={() => onNavigate('learn')}>
          <span className="nav-link-icon">📚</span>תבניות
        </button>
        <button className={`nav-link ${page === 'practice' ? 'active' : ''}`} onClick={() => onNavigate('practice')}>
          <span className="nav-link-icon">🏋️</span>תרגול
        </button>
        <button className={`nav-link ${page === 'play' ? 'active' : ''}`} onClick={() => onNavigate('play')}>
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
