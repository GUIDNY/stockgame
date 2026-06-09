import { useState } from 'react';

export default function Navbar({ page, onNavigate, score, streak }) {
  const [menuOpen, setMenuOpen] = useState(false);

  const handleNavClick = (target) => {
    onNavigate(target);
    setMenuOpen(false);
  };

  const navItems = [
    { label: 'שוק', icon: '📈', target: 'market' },
    { label: 'שיעורים', icon: '📚', target: 'lessons' },
    { label: 'תבניות', icon: '📖', target: 'learn' },
    { label: 'תרגול', icon: '🎓', target: 'practice' },
    { label: 'משחק', icon: '🎮', target: 'play' },
  ];

  return (
    <header className="fixed top-0 left-0 right-0 z-50 bg-surface-container/80 backdrop-blur-md border-b border-border h-16">
      <div className="flex flex-row-reverse justify-between items-center h-full px-6">
        <div className="flex items-center gap-3">
          <span className="text-2xl">📊</span>
          <h1 className="text-lg font-bold text-primary hidden sm:block">קרא את הגרף</h1>
        </div>

        {/* Desktop Nav */}
        <nav className="hidden md:flex gap-1 items-center">
          {navItems.map((item) => (
            <button
              key={item.target}
              onClick={() => handleNavClick(item.target)}
              className={`flex items-center gap-2 px-3 py-2 rounded-lg transition-colors ${
                page === item.target
                  ? 'bg-primary/20 text-primary'
                  : 'text-text-2 hover:bg-surface-bright'
              }`}
            >
              <span className="text-sm">{item.icon}</span>
              <span className="text-sm font-semibold">{item.label}</span>
            </button>
          ))}
        </nav>

        {/* Mobile Hamburger */}
        <button
          onClick={() => setMenuOpen(!menuOpen)}
          className="md:hidden p-2 hover:bg-surface-bright rounded-lg"
        >
          <span className="material-symbols-outlined">menu</span>
        </button>
      </div>

      {/* Mobile Menu */}
      {menuOpen && (
        <div className="md:hidden absolute top-16 left-0 right-0 bg-surface-container border-b border-border">
          {navItems.map((item) => (
            <button
              key={item.target}
              onClick={() => handleNavClick(item.target)}
              className={`w-full flex items-center gap-3 px-6 py-3 text-right transition-colors ${
                page === item.target
                  ? 'bg-primary/20 text-primary'
                  : 'text-text-2 hover:bg-surface-bright'
              }`}
            >
              <span className="text-xl">{item.icon}</span>
              <span className="font-semibold">{item.label}</span>
            </button>
          ))}
        </div>
      )}
    </header>
  );
}
