export default function MiniChart({ candles, width = 180, height = 80 }) {
  if (!candles || candles.length === 0) return null;

  const PAD_X = 4, PAD_Y = 6;
  const innerW = width - PAD_X * 2;
  const innerH = height - PAD_Y * 2;

  const prices = candles.flatMap((c) => [c.high, c.low]);
  const minP = Math.min(...prices);
  const maxP = Math.max(...prices);
  const range = maxP - minP || 1;

  const toY = (p) => PAD_Y + (1 - (p - minP) / range) * innerH;
  const cw = innerW / candles.length;
  const bodyW = Math.max(1, cw * 0.6);

  return (
    <svg
      width={width}
      height={height}
      style={{ display: 'block', overflow: 'visible' }}
      aria-hidden="true"
    >
      {candles.map((c, i) => {
        const cx = PAD_X + i * cw + cw / 2;
        const isGreen = c.close >= c.open;
        const color = isGreen ? '#3fb950' : '#f85149';
        const bodyTop = toY(Math.max(c.open, c.close));
        const bodyBot = toY(Math.min(c.open, c.close));
        const bodyH = Math.max(1.5, bodyBot - bodyTop);

        return (
          <g key={i}>
            <line x1={cx} y1={toY(c.high)} x2={cx} y2={toY(c.low)} stroke={color} strokeWidth={1} />
            <rect
              x={cx - bodyW / 2}
              y={bodyTop}
              width={bodyW}
              height={bodyH}
              fill={color}
              rx={0.5}
            />
          </g>
        );
      })}
    </svg>
  );
}
