# Frontend Prompt: Tính năng "Hệ Thống Cơ Sở Võ Đường"

## Bối cảnh dự án

Bạn đang xây dựng giao diện cho website **Võ Đường Côn Nhị Khúc Hà Đông** — một trang web giới thiệu và tuyển sinh học viên cho câu lạc bộ võ thuật. Stack frontend: **Next.js (App Router) + Tailwind CSS + TypeScript**.

Thiết kế đã có design system với các token sau (dùng nhất quán, không tự ý thay đổi):
- **Background chính:** `zinc-950` (#09090b)
- **Background card:** `zinc-900` (#18181b)
- **Border mặc định:** `zinc-700/60`
- **Accent chính:** `amber-500` (#f59e0b) — màu đặc trưng võ đường
- **Text chính:** `white`
- **Text phụ:** `zinc-400`
- **Font heading:** bold/extrabold
- **Border radius card:** `rounded-2xl`

---

## Nhiệm vụ

Implement **2 trang** và các components liên quan:

1. **Trang danh sách cơ sở** — route: `/co-so`
2. **Trang chi tiết cơ sở** — route: `/co-so/[id]`

---

## API Contract (đã có sẵn, không cần mock)

Base URL: `process.env.NEXT_PUBLIC_API_URL` (ví dụ: `https://api.example.com`)

### TypeScript types (copy nguyên vào `types/branch.ts`):

```typescript
export interface BranchCoachSummary {
  coachId: string;
  fullName: string;
  /** "HeadCoach" | "AssistantCoach" */
  title: string;
  avatarUrl: string | null;
}

export interface BranchListItem {
  id: string;
  code: string;
  name: string;
  shortName: string | null;
  address: string | null;
  area: string | null;
  thumbnail: string | null;
  schedule: string | null;
  fee: string | null;
  isFree: boolean;
  isActive: boolean;
  coaches: BranchCoachSummary[];
}

export interface BranchCoachDetail {
  coachId: string;
  fullName: string;
  /** "HeadCoach" | "AssistantCoach" */
  title: string;
  bio: string | null;
  specialization: string | null;
  yearsOfExperience: number;
  certifications: string[] | null;
  avatarUrl: string | null;
  coverImageUrl: string | null;
  phone: string | null;
  email: string | null;
}

export interface BranchGalleryItem {
  id: string;
  mediaUrl: string;
  /** "Image" | "Video" */
  mediaType: string;
  caption: string | null;
  displayOrder: number;
}

export interface BranchDetail {
  id: string;
  code: string;
  name: string;
  shortName: string | null;
  address: string | null;
  area: string | null;
  thumbnail: string | null;
  latitude: number | null;
  longitude: number | null;
  schedule: string | null;
  fee: string | null;
  isFree: boolean;
  description: string | null;
  isActive: boolean;
  coaches: BranchCoachDetail[];
  gallery: BranchGalleryItem[];
}
```

### Endpoints:

```
GET /api/branches?isActive=true
→ BranchListItem[]

GET /api/branches/{id}
→ BranchDetail   (404 nếu không tồn tại)
```

### Hàm fetch (tạo trong `lib/api/branches.ts`):

```typescript
export async function getBranches(): Promise<BranchListItem[]> {
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/branches?isActive=true`, {
    next: { revalidate: 3600 }, // ISR mỗi 1 giờ
  });
  if (!res.ok) throw new Error('Failed to fetch branches');
  return res.json();
}

export async function getBranchById(id: string): Promise<BranchDetail | null> {
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/branches/${id}`, {
    next: { revalidate: 3600 },
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error('Failed to fetch branch detail');
  return res.json();
}
```

---

## Trang 1: Danh sách cơ sở — `/co-so`

**File:** `app/co-so/page.tsx` (Server Component)

### Layout tổng thể

```
┌─────────────────────────────────────────────────────────┐
│  HERO SECTION (full-width, dark, amber accent)           │
│  • Breadcrumb: Trang chủ > Cơ Sở                        │
│  • H1: "Hệ Thống Cơ Sở Võ Đường"                       │
│  • Subtitle ngắn                                         │
│  • 3 stat chips: số cơ sở | học viên | năm hoạt động   │
├─────────────────────────────────────────────────────────┤
│  BRANCH LIST + MAP (2 cột trên desktop)                 │
│                                                          │
│  [Left 2/5: Cards cuộn] | [Right 3/5: Google Map sticky]│
│                                                          │
│  - Card click → cập nhật map                            │
│  - Map sticky top-24 khi scroll                         │
└─────────────────────────────────────────────────────────┘
```

### Hero Section

```tsx
// Nền: zinc-950, có glow amber mờ phía trên
// Padding: py-20 sm:py-28
// Breadcrumb: text-zinc-500, dấu "/" phân cách, link "Trang chủ" → hover amber
// H1: text-4xl sm:text-5xl font-extrabold text-white
//   "Hệ Thống Cơ Sở " + <span className="text-amber-500">Võ Đường</span>
// Subtitle: text-zinc-400, max-w-2xl mx-auto text-center
// Stat chips (3 cái nằm ngang): số nền zinc-900, border zinc-700, amber value
```

### Branch Cards (Left panel)

Mỗi card hiển thị thông tin từ `BranchListItem`:

```
┌──────────────────────┐
│  [Thumbnail image]   │  ← h-44, object-cover, hover scale-105
│  [Area badge] amber  │  ← absolute bottom-left
│  [Miễn phí] emerald  │  ← chỉ hiện khi isFree=true
├──────────────────────┤
│  Tên cơ sở (bold)    │
│  📍 Địa chỉ          │
│  🕐 Lịch tập         │
│  💰 Học phí / Miễn phí│
├──────────────────────┤
│  [Avatar] [Avatar]   │  ← HLV thumbnails (tối đa 3 avatar nhỏ + "+N" nếu >3)
│  Tên HLV chính       │  ← HeadCoach: bold, AssistantCoach: text-zinc-400
├──────────────────────┤
│  [Xem bản đồ] [Chi tiết] │
└──────────────────────┘
```

**Trạng thái active card** (khi đang xem trên map):
- `border-amber-500 shadow-amber-500/20 shadow-xl scale-[1.01]`
- Badge "Đang xem" với pulse dot màu amber ở góc trên phải

**Hover** (khi chưa active):
- `hover:border-amber-400/70 hover:shadow-amber-500/10`

**Phần hiển thị HLV trong card:**

```tsx
// Nhóm avatar nhỏ (w-8 h-8 rounded-full, -ml-2 để overlap)
// HeadCoach: hiển thị title "HLV Trưởng" bằng badge amber nhỏ
// AssistantCoach: "HLV Phụ trách" bằng text zinc-500 nhỏ
// Chỉ hiện max 3 avatar, nếu thêm thì "+N" trong circle zinc-700
```

**Nút "Xem bản đồ":**
- Primary: `bg-amber-500 hover:bg-amber-400 text-black rounded-xl`
- Click → cập nhật bản đồ phải + scroll đến map section trên mobile

**Nút "Chi tiết":**
- Outline: `border border-zinc-600 hover:border-amber-500 text-zinc-300 rounded-xl`
- Click → navigate đến `/co-so/{id}`

### Map Panel (Right panel)

```tsx
// Sticky top-24 trên desktop
// Info bar phía trên iframe: tên cơ sở + địa chỉ đang xem
// Iframe nhúng Google Maps:
//   src = `https://maps.google.com/maps?q=${lat},${lng}&z=16&output=embed&hl=vi`
//   key={branch.id} để force re-mount khi chuyển cơ sở
//   aspect-ratio: 4/3 trên mobile, 16/11 trên desktop
//   border: rounded-2xl overflow-hidden border border-zinc-700/60
// Link "Mở trong Google Maps" phía dưới iframe:
//   href = `https://www.google.com/maps/dir/?api=1&destination=${lat},${lng}`
//   target="_blank", text-zinc-500 hover:text-amber-400
```

**Fallback khi branch không có tọa độ:**
```tsx
// Centered placeholder: icon bản đồ opacity-30 + text "Chưa có bản đồ"
```

### Responsive

- **Mobile** (`< lg`): cards 1 cột, map ẩn mặc định, hiện ra (slide-down) khi click "Xem bản đồ"
- **Tablet** (`md`): cards grid 2 cột, map full-width bên dưới
- **Desktop** (`lg+`): flex row, cards 2/5 (scrollable max-h-[720px]), map 3/5 sticky

---

## Trang 2: Chi tiết cơ sở — `/co-so/[id]`

**File:** `app/co-so/[id]/page.tsx` (Server Component)
**generateStaticParams:** không cần (dùng ISR)
**notFound():** gọi khi API trả 404

### Layout tổng thể

```
┌─────────────────────────────────────────────────────────┐
│  HERO: Ảnh thumbnail full-width (h-72 sm:h-96)          │
│  Overlay tối + gradient. Tên cơ sở + area badge         │
│  Breadcrumb: Trang chủ > Cơ Sở > [Tên cơ sở]           │
├─────────────────────────────────────────────────────────┤
│  THÔNG TIN TỔNG QUAN  (2 cột trên desktop)              │
│  Left: Địa chỉ, Lịch tập, Học phí, Mô tả               │
│  Right: Google Maps iframe                              │
├─────────────────────────────────────────────────────────┤
│  HUẤN LUYỆN VIÊN (cards ngang)                          │
├─────────────────────────────────────────────────────────┤
│  GALLERY ẢNH (grid responsive, lightbox khi click)      │
└─────────────────────────────────────────────────────────┘
```

### Section 1: Hero

```tsx
// relative h-72 sm:h-96 overflow-hidden
// next/image với fill objectFit="cover" priority
// Gradient overlay: from-black/80 via-black/40 to-transparent (bottom → top)
// Nội dung absolute bottom-0 px-6 pb-8:
//   - Breadcrumb nhỏ text-zinc-400
//   - Area badge: bg-amber-500 text-black px-3 py-1 rounded-full text-sm
//   - H1: text-3xl sm:text-4xl font-extrabold text-white
//   - Address với icon pin, text-zinc-300 text-sm
```

### Section 2: Thông tin + Bản đồ

```tsx
// 2 cột: lg:grid lg:grid-cols-2 lg:gap-8
// Left column — InfoCard (bg-zinc-900 rounded-2xl p-6):
//   Các hàng thông tin với icon + label:
//   🕐 Lịch tập:   branch.schedule
//   💰 Học phí:    branch.fee hoặc badge "Miễn phí" emerald
//   📍 Địa chỉ:   branch.address
//   📝 Mô tả:     branch.description (text-zinc-400 leading-relaxed)

// Right column — Map iframe:
//   Cùng style như trang list
//   + nút "Chỉ đường" nổi bật hơn (bg-zinc-800 hover:bg-zinc-700)
```

### Section 3: Huấn Luyện Viên

```
Tiêu đề section: "Huấn Luyện Viên Cơ Sở"
Underline amber: w-12 h-1 bg-amber-500 rounded mt-2
```

Mỗi coach card (`BranchCoachDetailDto`):

```
┌──────────────────────────────────────────────────────────┐
│  [Cover image top, h-32, object-cover]                   │
│  [Avatar tròn 80px, border-2 amber, absolute -mt-10 ml-4]│
├──────────────────────────────────────────────────────────┤
│  Title badge: "HLV Trưởng" amber | "HLV Phụ trách" zinc │
│  FullName (text-xl font-bold)                            │
│  Specialization (text-amber-400 text-sm)                 │
│  YearsOfExperience năm kinh nghiệm (text-zinc-500 text-xs)│
├──────────────────────────────────────────────────────────┤
│  Bio (line-clamp-3, text-zinc-400 text-sm)               │
├──────────────────────────────────────────────────────────┤
│  Chứng chỉ (nếu có):                                     │
│  Chips nhỏ bg-zinc-800 text-zinc-300 text-xs rounded-full│
├──────────────────────────────────────────────────────────┤
│  [Gọi điện] [Email] — chỉ hiện nếu có phone/email       │
└──────────────────────────────────────────────────────────┘
```

**Title mapping:**
```typescript
const COACH_TITLE_LABEL: Record<string, string> = {
  HeadCoach: 'HLV Trưởng',
  AssistantCoach: 'HLV Phụ trách',
};
const COACH_TITLE_STYLE: Record<string, string> = {
  HeadCoach: 'bg-amber-500 text-black',
  AssistantCoach: 'bg-zinc-700 text-zinc-300',
};
```

Grid coaches: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6`

### Section 4: Gallery

```
Tiêu đề section: "Hình Ảnh Cơ Sở"
```

- **Grid ảnh:** `grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3`
- Mỗi ảnh: `aspect-square`, `object-cover`, `rounded-xl`, hover `brightness-75 scale-105 transition`
- Chỉ hiển thị items có `mediaType === "Image"` trong grid
- Click ảnh → mở **Lightbox** (modal full-screen):
  - Nền `bg-black/95`
  - Ảnh centered `max-h-[90vh] max-w-[90vw] object-contain`
  - Caption bên dưới (nếu có) `text-zinc-400 text-sm text-center`
  - Nút đóng (X) góc trên phải
  - Điều hướng trái/phải bằng nút hoặc swipe
- Nếu `mediaType === "Video"`: render `<video>` tag thay vì `<img>`, có controls
- Nếu gallery rỗng: ẩn toàn bộ section

---

## Error & Loading States

### Loading skeleton (dùng cho trang list):
```tsx
// Skeleton card: bg-zinc-800 animate-pulse rounded-2xl
// Skeleton image: h-44 rounded-t-2xl
// Skeleton lines: h-4 rounded w-3/4, w-1/2, etc.
// Hiển thị 3–4 skeleton cards trong grid
```

### Error boundary:
```tsx
// Nếu fetch thất bại → hiển thị centered error message
// Icon cảnh báo amber + text "Không thể tải dữ liệu. Vui lòng thử lại."
// Nút "Thử lại" → router.refresh()
```

### Not found (trang detail):
```tsx
// Gọi notFound() từ next/navigation khi API trả 404
// Tạo app/co-so/not-found.tsx: thông báo "Cơ sở không tồn tại"
// Link quay lại "/co-so"
```

---

## File structure gợi ý

```
app/
├── co-so/
│   ├── page.tsx                    ← Server Component, fetch list
│   ├── loading.tsx                 ← Skeleton UI
│   ├── not-found.tsx               ← 404 page
│   └── [id]/
│       ├── page.tsx                ← Server Component, fetch detail
│       └── loading.tsx             ← Skeleton detail

components/branches/
├── BranchCard.tsx                  ← Card cho list
├── BranchListSection.tsx           ← Client Component (có state map)
├── BranchMapPanel.tsx              ← Google Maps iframe
├── BranchCoachCard.tsx             ← Card HLV trong trang detail
├── BranchGallery.tsx               ← Grid ảnh + Lightbox (Client)
└── BranchInfoCard.tsx              ← Thông tin + icon (trang detail)

lib/api/
└── branches.ts                     ← Fetch functions

types/
└── branch.ts                       ← TypeScript types (copy từ trên)
```

---

## Lưu ý quan trọng

1. **`BranchListSection`** phải là **Client Component** (`'use client'`) vì cần state `selectedBranch` để cập nhật map và handle click events. Trang `page.tsx` fetch data server-side rồi truyền qua props.

2. **`BranchGallery`** phải là **Client Component** vì cần state lightbox.

3. **Google Maps iframe** không cần API key với URL pattern:
   ```
   https://maps.google.com/maps?q={lat},{lng}&z=16&output=embed&hl=vi
   ```
   Thêm `key={branch.id}` trên `<iframe>` để React re-mount khi đổi cơ sở.

4. **next/image** cho tất cả ảnh từ API — thêm domain vào `next.config.js`:
   ```js
   images: { remotePatterns: [{ protocol: 'https', hostname: '**' }] }
   ```

5. **Accessibility:**
   - `aria-label` cho các icon buttons
   - `role="button"` + `tabIndex={0}` + `onKeyDown` cho các card clickable
   - `alt` text đầy đủ cho tất cả ảnh

6. **Không dùng thư viện UI ngoài** (không MUI, Chakra, shadcn) — chỉ Tailwind + React thuần.

7. **Không hardcode text** — tất cả label tiếng Việt tập trung vào 1 file `constants/labels.ts` nếu cần reuse.
