import { useState } from 'react';
import lessons from '../data/lessons';
import LessonChart from '../components/LessonChart';

// ── Lesson catalog ─────────────────────────────────────────────────────────
function LessonCard({ lesson, onStart }) {
  return (
    <button className="lesson-card" onClick={() => onStart(lesson)}>
      <div className="lc-icon" style={{ background: lesson.color + '18', color: lesson.color }}>
        {lesson.icon}
      </div>
      <div className="lc-body">
        <div className="lc-meta">
          <span className="lc-duration">⏱ {lesson.duration}</span>
          <span className="lc-slides">{lesson.slides.length} שקפים</span>
        </div>
        <h3 className="lc-title">{lesson.title}</h3>
        <p className="lc-desc">{lesson.description}</p>
      </div>
      <span className="lc-arrow" style={{ color: lesson.color }}>←</span>
    </button>
  );
}

// ── Slide viewer ───────────────────────────────────────────────────────────
function SlideViewer({ lesson, onClose, onPractice }) {
  const [slideIdx, setSlideIdx] = useState(0);
  const slide  = lesson.slides[Math.min(slideIdx, lesson.slides.length - 1)];
  const total  = lesson.slides.length;
  const isLast = slideIdx >= total - 1;

  return (
    <div className="slide-viewer">
      {/* Header */}
      <div className="sv-header">
        <button className="sv-back" onClick={onClose}>← חזרה</button>
        <div className="sv-title-row">
          <span className="sv-lesson-icon">{lesson.icon}</span>
          <span className="sv-lesson-title">{lesson.title}</span>
        </div>
        <span className="sv-counter">{slideIdx + 1} / {total}</span>
      </div>

      {/* Progress bar */}
      <div className="sv-progress">
        <div className="sv-progress-fill" style={{ width: `${((slideIdx + 1) / total) * 100}%`, background: lesson.color }} />
      </div>

      {/* Content */}
      <div className="sv-content">
        {/* Chart */}
        {slide.visual && (
          <div className="sv-chart-wrap">
            <LessonChart
              candles={slide.visual.candles}
              overlays={slide.visual.overlays}
              width={560}
              height={200}
            />
            {slide.visual.caption && (
              <p className="sv-chart-caption">{slide.visual.caption}</p>
            )}
          </div>
        )}

        {/* Text */}
        <div className="sv-text-wrap">
          <h2 className="sv-slide-title" style={{ color: lesson.color }}>
            {slide.title}
          </h2>

          <div className="sv-body">
            {slide.body.split('\n\n').map((para, i) => (
              <p key={i} className="sv-para">{para}</p>
            ))}
          </div>

          {slide.keyPoints && (
            <ul className="sv-points">
              {slide.keyPoints.map((pt, i) => (
                <li key={i} className="sv-point">
                  <span className="sv-dot" style={{ background: lesson.color }} />
                  {pt}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* Navigation */}
      <div className="sv-nav">
        <button
          className="sv-btn secondary"
          onClick={() => setSlideIdx((i) => i - 1)}
          disabled={slideIdx === 0}
        >
          → הקודם
        </button>

        {isLast ? (
          <button
            className="sv-btn primary"
            style={{ background: lesson.color }}
            onClick={() => onPractice(lesson)}
          >
            תרגל עכשיו ←
          </button>
        ) : (
          <button
            className="sv-btn primary"
            style={{ background: lesson.color }}
            onClick={() => setSlideIdx((i) => i + 1)}
          >
            הבא ←
          </button>
        )}
      </div>
    </div>
  );
}

// ── Main component ─────────────────────────────────────────────────────────
export default function LessonsPage({ onNavigate }) {
  const [activeLesson, setActiveLesson] = useState(null);

  if (activeLesson) {
    return (
      <SlideViewer
        lesson={activeLesson}
        onClose={() => setActiveLesson(null)}
        onPractice={() => { setActiveLesson(null); onNavigate('play'); }}
      />
    );
  }

  return (
    <div className="lessons-page">
      <div className="lessons-header">
        <div className="lessons-header-inner">
          <h1 className="lessons-title">שיעורים</h1>
          <p className="lessons-sub">
            {lessons.length} שיעורים · {lessons.reduce((a, l) => a + l.slides.length, 0)} שקפים
          </p>
        </div>
      </div>

      <div className="lessons-content">
        <div className="lessons-intro">
          <p>
            כל שיעור כולל גרפים מוסברים עם קווים, רמות ואנוטציות. קרא את השיעורים לפי הסדר לתוצאה הטובה ביותר.
          </p>
        </div>

        <div className="lessons-list">
          {lessons.map((lesson, i) => (
            <div key={lesson.id} className="lesson-item">
              <div className="lesson-num" style={{ color: lesson.color }}>
                {String(i + 1).padStart(2, '0')}
              </div>
              <LessonCard lesson={lesson} onStart={setActiveLesson} />
            </div>
          ))}
        </div>

        <div className="lessons-tip">
          <div className="lessons-tip-icon">💡</div>
          <div>
            <strong>טיפ:</strong> לאחר כל שיעור לחץ "תרגל עכשיו" כדי לראות את התבניות בפעולה במשחק.
          </div>
        </div>
      </div>
    </div>
  );
}
