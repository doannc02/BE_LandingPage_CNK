/**
 * Mock data cho Branch entity — dựa đúng cấu trúc backend:
 * src/NunchakuClub.Domain/Entities/Branch.cs
 *
 * ⚠️  Lưu ý khác biệt so với yêu cầu ban đầu:
 *  - Không có field `hotlines`/`mapUrl` → dùng `phone` (cần thêm vào backend entity)
 *    và tọa độ `latitude`/`longitude` để tự tạo embed URL
 *  - `imageUrl` → entity dùng `thumbnail`
 *  - `operatingHours` → entity dùng `schedule`
 *
 * Khi tích hợp API thật:
 *   const res = await fetch('/api/branches');
 *   const branches = await res.json(); // thay thế mảng này
 */

/** @type {Branch[]} */
export const mockBranches = [
  {
    id: 'cs-ha-dong',
    code: 'CS-HD',
    name: 'Cơ Sở Hà Đông',
    shortName: 'Hà Đông',
    address: 'Số 8B, Ngõ 12, Đường Quang Trung, Phường La Khê, Hà Đông, Hà Nội',
    thumbnail: 'https://images.unsplash.com/photo-1590556409324-aa1d726e5c3c?w=800&auto=format&fit=crop',
    area: 'Hà Đông',
    latitude: 20.9729,
    longitude: 105.7877,
    schedule: 'T2 – T6: 17:30 – 19:30 | T7 – CN: 07:00 – 09:00',
    fee: '350.000 VNĐ / tháng',
    isFree: false,
    description: 'Cơ sở gốc và lớn nhất của Võ đường Côn Nhị Khúc Hà Đông. Sân tập rộng 300m², đầy đủ dụng cụ tập luyện chuyên dụng.',
    isActive: true,
    // --- field bổ sung ngoài entity (cần thêm vào backend nếu muốn) ---
    phone: '0912 345 678',
  },
  {
    id: 'cs-cau-giay',
    code: 'CS-CG',
    name: 'Cơ Sở Cầu Giấy',
    shortName: 'Cầu Giấy',
    address: 'Số 34, Phố Dịch Vọng Hậu, Phường Dịch Vọng Hậu, Cầu Giấy, Hà Nội',
    thumbnail: 'https://images.unsplash.com/photo-1555597673-b21d5c935865?w=800&auto=format&fit=crop',
    area: 'Cầu Giấy',
    latitude: 21.0378,
    longitude: 105.7862,
    schedule: 'T2 – T6: 18:00 – 20:00 | T7: 08:00 – 10:00',
    fee: '380.000 VNĐ / tháng',
    isFree: false,
    description: 'Cơ sở mở rộng phục vụ học viên khu vực Cầu Giấy, Từ Liêm. Huấn luyện viên giàu kinh nghiệm, lịch tập linh hoạt.',
    isActive: true,
    phone: '0987 654 321',
  },
  {
    id: 'cs-thanh-xuan',
    code: 'CS-TX',
    name: 'Cơ Sở Thanh Xuân',
    shortName: 'Thanh Xuân',
    address: 'Số 56, Đường Nguyễn Trãi, Phường Thanh Xuân Trung, Thanh Xuân, Hà Nội',
    thumbnail: 'https://images.unsplash.com/photo-1610639489813-fc4adaba27a4?w=800&auto=format&fit=crop',
    area: 'Thanh Xuân',
    latitude: 20.9951,
    longitude: 105.8042,
    schedule: 'T3 – T7: 17:00 – 19:00',
    fee: '350.000 VNĐ / tháng',
    isFree: false,
    description: 'Cơ sở mới khai trương, không gian hiện đại, phù hợp cho cả người lớn và trẻ em từ 8 tuổi.',
    isActive: true,
    phone: '0933 111 222',
  },
  {
    id: 'cs-dong-da',
    code: 'CS-DD',
    name: 'Cơ Sở Đống Đa',
    shortName: 'Đống Đa',
    address: 'Số 18, Phố Hoàng Cầu, Phường Ô Chợ Dừa, Đống Đa, Hà Nội',
    thumbnail: 'https://images.unsplash.com/photo-1601987177651-8edfe6c20009?w=800&auto=format&fit=crop',
    area: 'Đống Đa',
    latitude: 21.0245,
    longitude: 105.8412,
    schedule: 'T2 – T6: 18:30 – 20:30',
    fee: null,
    isFree: true,
    description: 'Cơ sở cộng đồng — miễn phí cho học viên dưới 15 tuổi. Chương trình đặc biệt hỗ trợ trẻ em có hoàn cảnh khó khăn.',
    isActive: true,
    phone: '0966 789 000',
  },
];

/**
 * TypeScript type reference (nếu dùng TS):
 *
 * interface Branch {
 *   id: string;
 *   code: string;
 *   name: string;
 *   shortName?: string;
 *   address?: string;
 *   thumbnail?: string;
 *   area?: string;
 *   latitude?: number;
 *   longitude?: number;
 *   schedule?: string;
 *   fee?: string | null;
 *   isFree: boolean;
 *   description?: string;
 *   isActive: boolean;
 *   phone?: string;          // field bổ sung, cần thêm vào backend entity
 * }
 */
