export default function LessonChart({ candles, overlays = [], width = 520, height = 200 }) {
  if (!candles || candles.length === 0) return null;

  const PAD = { top: 24, right: 8, bottom: 8, left: 8 };
  const innerW = width - PAD.left - PAD.right;
  const innerH = height - PAD.top - PAD.bottom;

  const prices = candles.flatMap((c) => [c.high, c.low]);
  overlays.forEach((o) => {
    if (o.type === 'hline') prices.push(o.price);
    if (o.type === 'zone') prices.push(o.priceHigh, o.priceLow);
    if (o.type === 'trendline') prices.push(o.fromPrice, o.toPrice);
    if (o.type === 'maline') o.values?.forEach((v) => v != null && prices.push(v));
  });
  const rawMin = Math.min(...prices), rawMax = Math.max(...prices);
  const spread = rawMax - rawMin || 1;
  const minP = rawMin - spread * 0.04, maxP = rawMax + spread * 0.04;
  const priceRange = maxP - minP;

  const toY = (p) => PAD.top + (1 - (p - minP) / priceRange) * innerH;
  const toX = (i) => PAD.left + i * (innerW / candles.length) + innerW / candles.length / 2;
  const cw = innerW / candles.length;
  const bodyW = Math.max(1.5, cw * 0.58);

  return (
    <svg width="100%" viewBox={`0 0 ${width} ${height}`} style={{ display: 'block', overflow: 'visible' }} aria-hidden="true">
      {/* Phase backgrounds */}
      {overlays.filter((o) => o.type === 'phase').map((o, i) => {
        const x1 = PAD.left + o.fromIdx * cw, x2 = PAD.left + o.toIdx * cw;
        return (
          <g key={`ph-${i}`}>
            <rect x={x1} y={PAD.top} width={x2 - x1} height={innerH} fill={o.color} opacity={0.09} />
            <text x={(x1 + x2) / 2} y={PAD.top - 6} textAnchor="middle" fontSize={9} fill={o.color} fontWeight="700">{o.label}</text>
          </g>
        );
      })}

      {/* Zones */}
      {overlays.filter((o) => o.type === 'zone').map((o, i) => (
        <rect key={`z-${i}`} x={PAD.left} y={toY(o.priceHigh)} width={innerW} height={Math.max(2, toY(o.priceLow) - toY(o.priceHigh))} fill={o.color} opacity={0.14} />
      ))}

      {/* Candles */}
      {candles.map((c, i) => {
        const cx = toX(i), isGreen = c.close >= c.open;
        const color = isGreen ? '#3fb950' : '#f85149';
        const bodyTop = toY(Math.max(c.open, c.close)), bodyBot = toY(Math.min(c.open, c.close));
        return (
          <g key={i}>
            <line x1={cx} y1={toY(c.high)} x2={cx} y2={toY(c.low)} stroke={color} strokeWidth={1} />
            <rect x={cx - bodyW / 2} y={bodyTop} width={bodyW} height={Math.max(1.5, bodyBot - bodyTop)} fill={color} rx={0.5} />
          </g>
        );
      })}

      {/* Horizontal lines */}
      {overlays.filter((o) => o.type === 'hline').map((o, i) => {
        const y = toY(o.price);
        return (
          <g key={`hl-${i}`}>
            <line x1={PAD.left} y1={y} x2={PAD.left + innerW} y2={y} stroke={o.color} strokeWidth={1.5} strokeDasharray={o.dashed ? '5,3' : undefined} />
            {o.label && <text x={PAD.left + 4} y={y - 3} fontSize={9.5} fill={o.color} fontWeight="600">{o.label}</text>}
          </g>
        );
      })}

      {/* Trend lines */}
      {overlays.filter((o) => o.type === 'trendline').map((o, i) => (
        <line key={`tl-${i}`} x1={toX(o.fromIdx)} y1={toY(o.fromPrice)} x2={toX(o.toIdx)} y2={toY(o.toPrice)} stroke={o.color} strokeWidth={2} strokeDasharray={o.dashed ? '5,3' : undefined} />
      ))}

      {/* Moving average lines */}
      {overlays.filter((o) => o.type === 'maline').map((o, oi) => {
        const pts = [];
        o.values?.forEach((v, i) => { if (v != null) pts.push(`${toX(i)},${toY(v)}`); });
        if (pts.length < 2) return null;
        return (
          <g key={`ma-${oi}`}>
            <polyline points={pts.join(' ')} fill="none" stroke={o.color} strokeWidth={1.8} strokeLinejoin="round" />
            {o.label && <text x={toX(o.values.length - 1) - 2} y={toY(o.values[o.values.length - 1]) - 4} fontSize={9} fill={o.color} fontWeight="600" textAnchor="end">{o.label}</text>}
          </g>
        );
      })}

      {/* Arrows / annotations */}
      {overlays.filter((o) => o.type === 'arrow').map((o, i) => {
        const cx = toX(o.idx), cy = o.position === 'top' ? PAD.top + 12 : PAD.top + innerH - 10;
        return <text key={`ar-${i}`} x={cx} y={cy} textAnchor="middle" fontSize={11} fill={o.color || '#d29922'} fontWeight="700">{o.text}</text>;
      })}
    </svg>
  );
}
