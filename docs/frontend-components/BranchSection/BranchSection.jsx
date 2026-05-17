/**
 * BranchSection — Danh sách cơ sở võ đường + Google Maps tích hợp
 *
 * Dependencies: React, Tailwind CSS
 * Không cần thư viện icon ngoài — dùng SVG inline
 *
 * Cách dùng:
 *   import BranchSection from './BranchSection/BranchSection';
 *   <BranchSection />
 *
 * Tích hợp API thật: thay mockBranches bằng kết quả fetch từ /api/branches
 */

import { useState, useCallback, useRef, memo } from 'react';
import { mockBranches } from './mockBranches';

// ─── Helpers ────────────────────────────────────────────────────────────────

function buildEmbedUrl(lat, lng) {
  if (!lat || !lng) return null;
  return `https://maps.google.com/maps?q=${lat},${lng}&z=16&output=embed&hl=vi`;
}

function buildDirectionsUrl(lat, lng) {
  return `https://www.google.com/maps/dir/?api=1&destination=${lat},${lng}`;
}

// ─── SVG Icons ───────────────────────────────────────────────────────────────

const IconPin = ({ className = '' }) => (
  <svg className={`shrink-0 ${className}`} xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
    <path fillRule="evenodd" d="M11.54 22.351l.07.04.028.016a.76.76 0 00.723 0l.028-.015.071-.041a16.975 16.975 0 001.144-.742 19.58 19.58 0 002.683-2.282C18.045 17.69 20 14.7 20 11c0-4.418-3.582-8-8-8S4 6.582 4 11c0 3.7 1.955 6.69 3.713 8.327a19.58 19.58 0 002.683 2.282c.38.26.747.492 1.144.742zM12 14a3 3 0 100-6 3 3 0 000 6z" clipRule="evenodd" />
  </svg>
);

const IconClock = ({ className = '' }) => (
  <svg className={`shrink-0 ${className}`} xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <circle cx="12" cy="12" r="10" />
    <polyline points="12 6 12 12 16 14" />
  </svg>
);

const IconPhone = ({ className = '' }) => (
  <svg className={`shrink-0 ${className}`} xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07A19.5 19.5 0 013.07 9.81a19.79 19.79 0 01-3.07-8.67A2 2 0 012 .99h3a2 2 0 012 1.72 12.84 12.84 0 00.7 2.81 2 2 0 01-.45 2.11L6.09 8.89a16 16 0 006 6l1.27-1.27a2 2 0 012.11-.45 12.84 12.84 0 002.81.7A2 2 0 0122 16z" />
  </svg>
);

const IconMap = ({ className = '' }) => (
  <svg className={`shrink-0 ${className}`} xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polygon points="3 6 9 3 15 6 21 3 21 18 15 21 9 18 3 21" />
    <line x1="9" y1="3" x2="9" y2="18" />
    <line x1="15" y1="6" x2="15" y2="21" />
  </svg>
);

const IconExternalLink = ({ className = '' }) => (
  <svg className={`shrink-0 ${className}`} xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M18 13v6a2 2 0 01-2 2H5a2 2 0 01-2-2V8a2 2 0 012-2h6" />
    <polyline points="15 3 21 3 21 9" />
    <line x1="10" y1="14" x2="21" y2="3" />
  </svg>
);

const IconGift = ({ className = '' }) => (
  <svg className={`shrink-0 ${className}`} xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 12 20 22 4 22 4 12" />
    <rect x="2" y="7" width="20" height="5" />
    <line x1="12" y1="22" x2="12" y2="7" />
    <path d="M12 7H7.5a2.5 2.5 0 010-5C11 2 12 7 12 7z" />
    <path d="M12 7h4.5a2.5 2.5 0 000-5C13 2 12 7 12 7z" />
  </svg>
);

// ─── BranchCard ──────────────────────────────────────────────────────────────

const BranchCard = memo(function BranchCard({ branch, isActive, onSelect }) {
  const hasMap = !!(branch.latitude && branch.longitude);

  return (
    <article
      role="button"
      tabIndex={0}
      onClick={() => onSelect(branch)}
      onKeyDown={(e) => e.key === 'Enter' && onSelect(branch)}
      aria-pressed={isActive}
      className={[
        'group relative flex flex-col cursor-pointer rounded-2xl overflow-hidden',
        'border-2 outline-none focus-visible:ring-2 focus-visible:ring-amber-500 focus-visible:ring-offset-2 focus-visible:ring-offset-zinc-950',
        'transition-all duration-300 ease-out',
        isActive
          ? 'border-amber-500 shadow-xl shadow-amber-500/20 scale-[1.01]'
          : 'border-zinc-700/60 hover:border-amber-400/70 hover:shadow-lg hover:shadow-amber-500/10 hover:scale-[1.005]',
      ].join(' ')}
    >
      {/* Active indicator */}
      {isActive && (
        <div className="absolute top-3 right-3 z-10 flex items-center gap-1.5 px-2 py-1 rounded-full bg-amber-500 text-black text-xs font-bold">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-black opacity-75" />
            <span className="relative inline-flex rounded-full h-2 w-2 bg-black" />
          </span>
          Đang xem
        </div>
      )}

      {/* Thumbnail */}
      <div className="relative h-44 bg-zinc-800 overflow-hidden">
        {branch.thumbnail ? (
          <img
            src={branch.thumbnail}
            alt={branch.name}
            loading="lazy"
            className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-zinc-600">
            <IconMap className="w-12 h-12" />
          </div>
        )}
        {/* Gradient overlay */}
        <div className="absolute inset-0 bg-gradient-to-t from-black/75 via-black/10 to-transparent" />

        {/* Area badge */}
        <div className="absolute bottom-3 left-3 flex items-center gap-2">
          {branch.area && (
            <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-500 text-black">
              {branch.area}
            </span>
          )}
          {branch.isFree && (
            <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500 text-white flex items-center gap-1">
              <IconGift /> Miễn phí
            </span>
          )}
        </div>
      </div>

      {/* Content */}
      <div className="flex flex-col flex-1 bg-zinc-900 p-4 gap-3">
        <h3 className="font-bold text-white text-lg leading-snug">{branch.name}</h3>

        <div className="flex flex-col gap-2 text-sm text-zinc-400">
          {branch.address && (
            <div className="flex items-start gap-2">
              <IconPin className="text-amber-500 mt-0.5" />
              <span className="line-clamp-2">{branch.address}</span>
            </div>
          )}
          {branch.schedule && (
            <div className="flex items-start gap-2">
              <IconClock className="text-amber-500 mt-0.5" />
              <span>{branch.schedule}</span>
            </div>
          )}
          {branch.phone && (
            <div className="flex items-center gap-2">
              <IconPhone className="text-amber-500" />
              <span className="font-medium text-zinc-300">{branch.phone}</span>
            </div>
          )}
          {!branch.isFree && branch.fee && (
            <p className="text-xs text-zinc-500 mt-1 pl-6">Học phí: <span className="text-amber-400 font-medium">{branch.fee}</span></p>
          )}
        </div>

        {/* CTA buttons */}
        <div className="flex gap-2 mt-auto pt-2">
          <button
            onClick={(e) => { e.stopPropagation(); onSelect(branch); }}
            disabled={!hasMap}
            className={[
              'flex-1 flex items-center justify-center gap-1.5 px-3 py-2.5 rounded-xl text-sm font-semibold',
              'transition-all duration-200',
              hasMap
                ? 'bg-amber-500 hover:bg-amber-400 active:scale-95 text-black cursor-pointer'
                : 'bg-zinc-700 text-zinc-500 cursor-not-allowed',
            ].join(' ')}
          >
            <IconMap />
            Xem bản đồ
          </button>

          {branch.phone && (
            <a
              href={`tel:${branch.phone.replace(/\s/g, '')}`}
              onClick={(e) => e.stopPropagation()}
              className="flex-1 flex items-center justify-center gap-1.5 px-3 py-2.5 rounded-xl text-sm font-medium
                border border-zinc-600 text-zinc-300
                hover:border-amber-500 hover:text-amber-400 hover:bg-amber-500/5
                active:scale-95 transition-all duration-200"
            >
              <IconPhone />
              Gọi hotline
            </a>
          )}
        </div>
      </div>
    </article>
  );
});

// ─── MapPanel ────────────────────────────────────────────────────────────────

function MapPanel({ branch }) {
  const embedUrl = buildEmbedUrl(branch?.latitude, branch?.longitude);
  const directionsUrl = branch?.latitude
    ? buildDirectionsUrl(branch.latitude, branch.longitude)
    : null;

  return (
    <div className="flex flex-col gap-3">
      {/* Info bar */}
      {branch && (
        <div className="flex items-center gap-3 bg-zinc-900 border border-zinc-700/60 rounded-xl px-4 py-3">
          <span className="relative flex h-2.5 w-2.5 shrink-0">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" />
            <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-amber-500" />
          </span>
          <div className="min-w-0">
            <p className="text-white font-semibold truncate">{branch.name}</p>
            {branch.address && (
              <p className="text-zinc-500 text-xs truncate">{branch.address}</p>
            )}
          </div>
        </div>
      )}

      {/* Map iframe */}
      <div className="relative rounded-2xl overflow-hidden border border-zinc-700/60 bg-zinc-900"
           style={{ aspectRatio: '4/3' }}>
        {embedUrl ? (
          <iframe
            key={branch?.id}
            src={embedUrl}
            width="100%"
            height="100%"
            style={{ border: 0, display: 'block' }}
            allowFullScreen
            loading="lazy"
            referrerPolicy="no-referrer-when-downgrade"
            title={`Bản đồ ${branch?.name}`}
          />
        ) : (
          <div className="absolute inset-0 flex flex-col items-center justify-center gap-3 text-zinc-600">
            <IconMap className="w-14 h-14 opacity-30" />
            <p className="text-sm">Chưa có tọa độ cho cơ sở này</p>
          </div>
        )}
      </div>

      {/* Open in Google Maps */}
      {directionsUrl && (
        <a
          href={directionsUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="flex items-center justify-center gap-1.5 py-2 text-sm text-zinc-500
            hover:text-amber-400 transition-colors duration-200"
        >
          <IconExternalLink />
          Chỉ đường trong Google Maps
        </a>
      )}
    </div>
  );
}

// ─── BranchSection (main export) ─────────────────────────────────────────────

export default function BranchSection({
  /**
   * Thay `branches` bằng dữ liệu từ API:
   *   const [branches, setBranches] = useState([]);
   *   useEffect(() => { fetch('/api/branches').then(r=>r.json()).then(setBranches) }, []);
   */
  branches = mockBranches,
}) {
  const activeBranches = branches.filter((b) => b.isActive);
  const [selectedBranch, setSelectedBranch] = useState(activeBranches[0] ?? null);
  const mapRef = useRef(null);

  const handleSelect = useCallback((branch) => {
    setSelectedBranch(branch);
    // Mobile: cuộn xuống bản đồ sau khi chọn cơ sở
    if (window.innerWidth < 1024 && mapRef.current) {
      setTimeout(() => {
        mapRef.current.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 100);
    }
  }, []);

  return (
    <section className="relative bg-zinc-950 py-16 sm:py-24 px-4 sm:px-6 lg:px-8 overflow-hidden">
      {/* Decorative background glow */}
      <div className="pointer-events-none absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[400px]
        bg-amber-500/5 blur-[120px] rounded-full" aria-hidden="true" />

      <div className="relative max-w-7xl mx-auto">
        {/* ── Header ─────────────────────────────────────────────────────── */}
        <div className="text-center mb-12 sm:mb-16">
          <p className="inline-flex items-center gap-2 text-amber-500 text-xs font-bold uppercase tracking-[0.2em] mb-3">
            <span className="w-8 h-px bg-amber-500" />
            Địa Điểm Luyện Tập
            <span className="w-8 h-px bg-amber-500" />
          </p>
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-white mb-4 leading-tight">
            Hệ Thống Cơ Sở{' '}
            <span className="text-amber-500">Võ Đường</span>
          </h2>
          <p className="text-zinc-400 text-base sm:text-lg max-w-2xl mx-auto leading-relaxed">
            Côn Nhị Khúc Hà Đông có nhiều cơ sở trải dài khắp Hà Nội.
            Chọn địa điểm gần bạn nhất để bắt đầu hành trình võ thuật.
          </p>
        </div>

        {/* ── Thống kê nhanh ─────────────────────────────────────────────── */}
        <div className="grid grid-cols-3 gap-4 sm:gap-6 mb-12 max-w-md mx-auto">
          {[
            { value: `${activeBranches.length}`, label: 'Cơ sở' },
            { value: '500+', label: 'Học viên' },
            { value: '10+', label: 'Năm hoạt động' },
          ].map((stat) => (
            <div key={stat.label} className="text-center">
              <p className="text-2xl sm:text-3xl font-extrabold text-amber-500">{stat.value}</p>
              <p className="text-zinc-500 text-xs sm:text-sm mt-0.5">{stat.label}</p>
            </div>
          ))}
        </div>

        {/* ── Main layout: Cards (trái) + Map (phải) ─────────────────────── */}
        <div className="flex flex-col lg:flex-row gap-6 lg:gap-8 items-start">

          {/* ── Left: Branch cards ────────────────────────────────────────── */}
          <div className="w-full lg:w-2/5">
            {/* Scrollable container on desktop */}
            <div className={[
              'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-1 gap-4',
              'lg:max-h-[720px] lg:overflow-y-auto lg:pr-1',
              /* Custom scrollbar — cần thêm plugin tailwind-scrollbar hoặc CSS toàn cục */
            ].join(' ')}
              style={{
                scrollbarWidth: 'thin',
                scrollbarColor: '#52525b transparent',
              }}
            >
              {activeBranches.map((branch) => (
                <BranchCard
                  key={branch.id}
                  branch={branch}
                  isActive={selectedBranch?.id === branch.id}
                  onSelect={handleSelect}
                />
              ))}

              {activeBranches.length === 0 && (
                <div className="col-span-2 py-16 text-center text-zinc-600">
                  <IconPin className="w-10 h-10 mx-auto mb-3 opacity-30" />
                  <p>Chưa có cơ sở nào đang hoạt động.</p>
                </div>
              )}
            </div>
          </div>

          {/* ── Right: Map ───────────────────────────────────────────────── */}
          <div
            ref={mapRef}
            className="w-full lg:w-3/5 lg:sticky lg:top-24"
            style={{ scrollMarginTop: '6rem' }}
          >
            <MapPanel branch={selectedBranch} />

            {/* Branch description */}
            {selectedBranch?.description && (
              <div className="mt-4 p-4 bg-zinc-900 border border-zinc-700/60 rounded-xl">
                <p className="text-zinc-400 text-sm leading-relaxed">{selectedBranch.description}</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </section>
  );
}
