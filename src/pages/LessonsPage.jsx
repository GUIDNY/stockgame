import { useState } from 'react';
import lessons from '../data/lessons';
import LessonChart from '../components/LessonChart';

function LessonCard({ lesson, onStart }) {
  return (
    <button
      onClick={() => onStart(lesson)}
      className="flex items-start gap-4 p-4 rounded-lg border border-border bg-surface-container hover:border-primary transition-colors text-right"
    >
      <div className="flex-1">
        <div className="flex gap-2 mb-2">
          <span className="text-xs text-text-2">⏱ {lesson.duration}</span>
          <span className="text-xs text-text-2">{lesson.slides.length} שקפים</span>
        </div>
        <h3 className="text-lg font-bold text-text mb-1">{lesson.title}</h3>
        <p className="text-sm text-text-2">{lesson.description}</p>
      </div>
      <span className="text-2xl flex-shrink-0" style={{ color: lesson.color }}>←</span>
    </button>
  );
}

function SlideViewer({ lesson, onClose, onPractice }) {
  const [slideIdx, setSlideIdx] = useState(0);
  const slide = lesson.slides[Math.min(slideIdx, lesson.slides.length - 1)];
  const total = lesson.slides.length;
  const isLast = slideIdx >= total - 1;

  return (
    <div className="bg-surface min-h-screen pt-20 pb-12 flex flex-col">
      {/* Header */}
      <div className="bg-surface-container border-b border-border px-6 py-4 flex items-center justify-between">
        <button onClick={onClose} className="text-primary hover:text-primary-dark font-bold">← חזרה</button>
        <div className="flex items-center gap-3">
          <span className="text-2xl">{lesson.icon}</span>
          <h2 className="text-lg font-bold text-text">{lesson.title}</h2>
        </div>
        <span className="text-sm text-text-2 font-bold">{slideIdx + 1} / {total}</span>
      </div>

      {/* Progress bar */}
      <div className="h-1 bg-surface-bright">
        <div className="h-full transition-all" style={{ width: `${((slideIdx + 1) / total) * 100}%`, background: lesson.color }} />
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto px-6 py-8">
        <div className="max-w-4xl mx-auto">
          {/* Chart */}
          {slide.visual && (
            <div className="mb-8">
              <LessonChart
                candles={slide.visual.candles}
                overlays={slide.visual.overlays}
                width={560}
                height={200}
              />
              {slide.visual.caption && (
                <p className="text-sm text-text-2 mt-3 text-center">{slide.visual.caption}</p>
              )}
            </div>
          )}

          {/* Text */}
          <div>
            <h2 className="text-3xl font-bold mb-4" style={{ color: lesson.color }}>
              {slide.title}
            </h2>

            <div className="space-y-4 text-text-2 mb-6">
              {slide.body.split('\n\n').map((para, i) => (
                <p key={i} className="leading-relaxed">{para}</p>
              ))}
            </div>

            {slide.keyPoints && (
              <ul className="space-y-2">
                {slide.keyPoints.map((pt, i) => (
                  <li key={i} className="flex gap-3 text-text-2">
                    <span className="w-2 h-2 rounded-full flex-shrink-0 mt-2" style={{ background: lesson.color }} />
                    {pt}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      </div>

      {/* Navigation */}
      <div className="bg-surface-container border-t border-border px-6 py-4 flex gap-4 justify-between">
        <button
          onClick={() => setSlideIdx((i) => i - 1)}
          disabled={slideIdx === 0}
          className="px-6 py-2 rounded-lg font-bold disabled:opacity-50 disabled:cursor-not-allowed bg-surface-bright text-text hover:bg-surface-container"
        >
          → הקודם
        </button>

        {isLast ? (
          <button
            onClick={() => onPractice(lesson)}
            className="px-6 py-2 rounded-lg font-bold text-surface"
            style={{ background: lesson.color }}
          >
            תרגל עכשיו ←
          </button>
        ) : (
          <button
            onClick={() => setSlideIdx((i) => i + 1)}
            className="px-6 py-2 rounded-lg font-bold text-surface"
            style={{ background: lesson.color }}
          >
            הבא ←
          </button>
        )}
      </div>
    </div>
  );
}

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
    <div className="bg-surface min-h-screen pt-20 pb-12">
      <div className="max-w-7xl mx-auto px-6">
        <h1 className="text-4xl font-bold text-text mb-2">שיעורים</h1>
        <p className="text-text-2 mb-8">
          {lessons.length} שיעורים · {lessons.reduce((a, l) => a + l.slides.length, 0)} שקפים
        </p>

        <div className="mb-8 p-6 bg-surface-container rounded-lg border border-border">
          <p className="text-text-2">
            כל שיעור כולל גרפים מוסברים עם קווים, רמות ואנוטציות. קרא את השיעורים לפי הסדר לתוצאה הטובה ביותר.
          </p>
        </div>

        <div className="space-y-6">
          {lessons.map((lesson, i) => (
            <div key={lesson.id} className="flex gap-4 items-start">
              <div className="text-3xl font-bold flex-shrink-0 pt-2" style={{ color: lesson.color }}>
                {String(i + 1).padStart(2, '0')}
              </div>
              <LessonCard lesson={lesson} onStart={setActiveLesson} />
            </div>
          ))}
        </div>

        <div className="mt-12 p-6 bg-primary/10 rounded-lg border border-primary/20 flex gap-4">
          <div className="text-2xl">💡</div>
          <div className="text-text-2">
            <strong>טיפ:</strong> לאחר כל שיעור לחץ "תרגל עכשיו" כדי לראות את התבניות בפעולה במשחק.
          </div>
        </div>
      </div>
    </div>
  );
}
