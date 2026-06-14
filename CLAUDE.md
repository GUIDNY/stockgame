# קרא את הגרף (Chart Game) - Claude Code Configuration

## Project Overview
A comprehensive financial chart pattern learning game built with:
- **Framework**: Vite + React (no TypeScript)
- **Styling**: Tailwind CSS (dark mode, RTL Hebrew support)
- **Charts**: TradingView lightweight-charts v4 + Custom SVG rendering
- **Data**: Yahoo Finance API (via Vite proxy) + Synthetic pattern generation
- **Deployment**: GitHub + Vercel

## MCP Servers
- **Bloom**: Image generation for promotional graphics and marketing materials
  - Server: https://www.trybloom.ai/api/mcp
  - Transport: HTTP
  - Use for creating pattern visualizations, tutorial graphics, social media content

## Key Directories
- `src/pages/` - Main pages (Home, Play, Learn, Practice, Lessons, Market)
- `src/components/` - Reusable components (Navbar, PatternCard, Charts, etc.)
- `src/data/` - Pattern definitions, lessons content
- `src/hooks/` - useMarketData hook for Yahoo Finance integration
- `src/utils/` - Pattern detection algorithms

## Available Commands
```bash
npm run dev      # Start dev server (http://localhost:5173)
npm run build    # Build for production
```

## Design System (Tailwind CSS)
- **Primary Color**: #44e092 (vibrant green)
- **Secondary**: #ffb4aa (red)
- **Tertiary**: #c1c1ff (blue)
- **Surface**: #0f131c (dark background)
- **Font**: Be Vietnam Pro

## Game Modes
1. **Play Mode** - Real-time pattern matching with TradingView charts
2. **Learn Mode** - Pattern catalog with filtering
3. **Practice Mode** - 3 sub-modes:
   - Quiz: Multiple choice pattern identification
   - Speed: 15-second countdown challenges
   - Survival: One-strike game over
4. **Lessons** - 16 interactive lessons with animated visualizations
5. **Market** - Real-time stock ticker data

## Recent Changes
- Complete Tailwind CSS migration (May 2026)
- Fixed chart visibility with h-screen + min-h-0 flex trick
- Added Bloom MCP for image generation capabilities

## GitHub
Repository: https://github.com/GUIDNY/stockgame
