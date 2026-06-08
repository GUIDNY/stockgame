import { useState } from 'react';
import { IconHome, IconMarket, IconLesson, IconBook, IconTrain, IconGame, IconMenu } from './Icons';

export default function Navbar({ page, onNavigate, score, streak }) {
  const [menuOpen, setMenuOpen] = useState(false);

  const handleNavClick = (target) => {
    onNavigate(target);
    setMenuOpen(false);
  };

  return (
    <nav className="navbar">
      <button className="nav-brand" onClick={() => handleNavClick('home')}>
        <span className="nav-logo"><IconHome /></span>
        <span className="nav-title">קרא את הגרף</span>
      </button>

      <button className="nav-hamburger" onClick={() => setMenuOpen(!menuOpen)}>
        <IconMenu />
      </button>

      <div className={`nav-links ${menuOpen ? 'open' : ''}`}>
        <button className={`nav-link ${page === 'market' ? 'active' : ''}`} onClick={() => handleNavClick('market')}>
          <span className="nav-link-icon"><IconMarket /></span>שוק
        </button>
        <button className={`nav-link ${page === 'lessons' ? 'active' : ''}`} onClick={() => handleNavClick('lessons')}>
          <span className="nav-link-icon"><IconLesson /></span>שיעורים
        </button>
        <button className={`nav-link ${page === 'learn' ? 'active' : ''}`} onClick={() => handleNavClick('learn')}>
          <span className="nav-link-icon"><IconBook /></span>תבניות
        </button>
        <button className={`nav-link ${page === 'practice' ? 'active' : ''}`} onClick={() => handleNavClick('practice')}>
          <span className="nav-link-icon"><IconTrain /></span>תרגול
        </button>
        <button className={`nav-link ${page === 'play' ? 'active' : ''}`} onClick={() => handleNavClick('play')}>
          <span className="nav-link-icon"><IconGame /></span>משחק
        </button>
      </div>

    </nav>
  );
}
