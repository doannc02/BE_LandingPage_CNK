// lib/firebase.ts
// Copy file này vào NextJS project: src/lib/firebase.ts
// Điền các giá trị từ Firebase Console → Project Settings → General → Your apps

import { initializeApp, getApps, getApp, type FirebaseApp } from 'firebase/app';
import { getDatabase, type Database } from 'firebase/database';
import { getMessaging, type Messaging } from 'firebase/messaging';

const firebaseConfig = {
  apiKey:            process.env.NEXT_PUBLIC_FIREBASE_API_KEY!,
  authDomain:        process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN!,
  databaseURL:       process.env.NEXT_PUBLIC_FIREBASE_DATABASE_URL!,   // Realtime DB URL
  projectId:         process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID!,
  storageBucket:     process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET!,
  messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID!,
  appId:             process.env.NEXT_PUBLIC_FIREBASE_APP_ID!,
};

// Singleton — tránh khởi tạo nhiều lần trong Next.js hot reload
function getFirebaseApp(): FirebaseApp {
  return getApps().length ? getApp() : initializeApp(firebaseConfig);
}

export function getDb(): Database {
  return getDatabase(getFirebaseApp());
}

// Chỉ gọi trên client (browser), không gọi trên server
export function getFirebaseMessaging(): Messaging | null {
  if (typeof window === 'undefined') return null;
  return getMessaging(getFirebaseApp());
}

// .env.local cần có:
// NEXT_PUBLIC_FIREBASE_API_KEY=...
// NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=...
// NEXT_PUBLIC_FIREBASE_DATABASE_URL=https://your-project-default-rtdb.firebaseio.com
// NEXT_PUBLIC_FIREBASE_PROJECT_ID=...
// NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=...
// NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=...
// NEXT_PUBLIC_FIREBASE_APP_ID=...
// NEXT_PUBLIC_FIREBASE_VAPID_KEY=...  (từ Firebase Console → Cloud Messaging → Web Push certificates)
