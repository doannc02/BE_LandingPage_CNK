# 🎨 Custom Layout System Documentation

## Overview

Hệ thống Custom Layout cho phép admin tạo và quản lý giao diện website động thông qua JSON configuration, không cần can thiệp code.

## 🏗️ Architecture

### Core Concepts

1. **SectionType**: Định nghĩa các loại component có thể sử dụng (Hero, Blog, Contact Form, etc.)
2. **LayoutTemplate**: Template layout có thể tái sử dụng cho nhiều pages
3. **Page Layout Config**: Cấu hình layout riêng cho từng page (có thể override template)
4. **Frontend Runtime Engine**: React component render sections động dựa trên JSON config

### Data Flow

```
Admin Creates Layout → JSON Config → Database → API → Frontend Runtime → Rendered Page
```

---

## 📦 Database Schema

### Entities

#### 1. **SectionType**
Định nghĩa các loại sections có thể sử dụng

```csharp
public class SectionType
{
    public Guid Id { get; set; }
    public string Name { get; set; }            // "Hero Banner"
    public string TypeKey { get; set; }         // "hero"
    public string Description { get; set; }
    public string Icon { get; set; }
    public string Category { get; set; }        // "Header", "Content", "Footer"
    public string PreviewImageUrl { get; set; }
    public string ConfigSchema { get; set; }    // JSON schema for config fields
    public string DefaultConfig { get; set; }   // Default configuration
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}
```

#### 2. **LayoutTemplate**
Templates có thể tái sử dụng

```csharp
public class LayoutTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Description { get; set; }
    public string PreviewImageUrl { get; set; }
    public string LayoutConfig { get; set; }    // JSON: entire layout configuration
    public string Category { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int UsageCount { get; set; }
}
```

#### 3. **Page** (Extended)
Thêm fields cho layout customization

```csharp
public class Page
{
    // ... existing fields ...

    public Guid? LayoutTemplateId { get; set; }
    public LayoutTemplate LayoutTemplate { get; set; }
    public string LayoutConfig { get; set; }     // JSON: custom layout for this page
    public int LayoutVersion { get; set; }
}
```

---

## 📄 JSON Schema

### Page Layout Config

```json
{
  "layoutTemplateId": "uuid-here-or-null",
  "sections": [
    {
      "id": "unique-section-id",
      "type": "section-type-key",
      "name": "Display Name",
      "order": 1,
      "isVisible": true,
      "config": {
        // Section-specific configuration
      }
    }
  ],
  "theme": {
    "colors": {
      "primary": "#3B82F6",
      "secondary": "#10B981",
      // ... more colors
    },
    "typography": {
      "fontFamily": "Inter, sans-serif",
      "baseFontSize": "16px",
      // ... more typography
    },
    "logoUrl": "/media/logo.png",
    "faviconUrl": "/media/favicon.ico"
  },
  "customCss": "/* optional custom CSS */",
  "customJs": "// optional custom JS",
  "version": 1
}
```

### Section Config Schema (for SectionType)

```json
{
  "fields": [
    {
      "name": "title",
      "label": "Title",
      "type": "text",
      "required": true,
      "placeholder": "Enter title",
      "helpText": "Main heading for the section",
      "defaultValue": "Welcome"
    },
    {
      "name": "layout",
      "label": "Layout Style",
      "type": "select",
      "options": [
        { "label": "2 Columns", "value": "grid-2" },
        { "label": "3 Columns", "value": "grid-3" }
      ]
    },
    {
      "name": "backgroundImage",
      "label": "Background Image",
      "type": "image",
      "required": false
    }
  ]
}
```

---

## 🎨 Section Types

### Built-in Section Types

#### 1. **Hero Banner** (`hero`)
Large header section với background image và CTAs

**Config Fields:**
- `title`: string
- `subtitle`: string
- `backgroundImage`: string (URL)
- `backgroundOverlay`: string (CSS color)
- `height`: string (CSS height)
- `alignment`: "left" | "center" | "right"
- `ctaButtons`: array of { text, link, style }

#### 2. **Blog Grid** (`blog-grid`)
Hiển thị blog posts trong grid layout

**Config Fields:**
- `title`: string
- `subtitle`: string
- `layout`: "grid-2" | "grid-3" | "grid-4"
- `limit`: number
- `showExcerpt`: boolean
- `showAuthor`: boolean
- `categoryFilter`: string | null

#### 3. **Features Grid** (`features-grid`)
Hiển thị features/services

**Config Fields:**
- `title`: string
- `layout`: "grid-2" | "grid-3" | "grid-4"
- `features`: array of { icon, title, description }

#### 4. **Contact Form** (`contact-form`)
Form liên hệ

**Config Fields:**
- `title`: string
- `fields`: array of field names
- `showMap`: boolean
- `mapAddress`: string

#### 5. **Call to Action** (`call-to-action`)
CTA banner

**Config Fields:**
- `title`: string
- `description`: string
- `buttonText`: string
- `buttonLink`: string
- `backgroundColor`: string
- `textColor`: string

---

## 🔌 API Endpoints

### Section Types

```http
GET    /api/section-types              # List all section types
GET    /api/section-types/{id}         # Get section type by ID
POST   /api/section-types              # Create new section type (Admin)
PUT    /api/section-types/{id}         # Update section type (Admin)
DELETE /api/section-types/{id}         # Delete section type (Admin)
```

### Layout Templates

```http
GET    /api/layout-templates           # List all templates
GET    /api/layout-templates/{id}      # Get template by ID
POST   /api/layout-templates           # Create template
PUT    /api/layout-templates/{id}      # Update template
DELETE /api/layout-templates/{id}      # Delete template
POST   /api/layout-templates/{id}/duplicate  # Duplicate template
```

### Page Layouts

```http
GET    /api/pages/{id}/layout          # Get page layout config
PUT    /api/pages/{id}/layout          # Update page layout
POST   /api/pages/{id}/apply-template  # Apply template to page
```

---

## 💻 Frontend Implementation

### Component Registry

Map section types to React components:

```tsx
// componentRegistry.ts
import { HeroSection } from './sections/HeroSection';
import { BlogGridSection } from './sections/BlogGridSection';
import { ContactFormSection } from './sections/ContactFormSection';

export const COMPONENT_REGISTRY = {
  'hero': HeroSection,
  'blog-grid': BlogGridSection,
  'contact-form': ContactFormSection,
  // ... more sections
};
```

### Runtime Engine

```tsx
// PageRenderer.tsx
import { COMPONENT_REGISTRY } from './componentRegistry';
import { ThemeProvider } from './ThemeProvider';

export const PageRenderer = ({ pageConfig }) => {
  const { sections, theme } = pageConfig;

  return (
    <ThemeProvider theme={theme}>
      <div className="page-container">
        {sections
          .filter(s => s.isVisible)
          .sort((a, b) => a.order - b.order)
          .map(section => {
            const Component = COMPONENT_REGISTRY[section.type];

            if (!Component) {
              console.warn(`Section type "${section.type}" not found`);
              return null;
            }

            return (
              <Component
                key={section.id}
                config={section.config}
                sectionId={section.id}
              />
            );
          })}
      </div>
    </ThemeProvider>
  );
};
```

### Example Section Component

```tsx
// sections/HeroSection.tsx
export const HeroSection = ({ config }) => {
  const {
    title,
    subtitle,
    backgroundImage,
    backgroundOverlay,
    height,
    alignment,
    ctaButtons = []
  } = config;

  return (
    <section
      className="hero-section"
      style={{
        backgroundImage: `linear-gradient(${backgroundOverlay}, ${backgroundOverlay}), url(${backgroundImage})`,
        height,
        textAlign: alignment
      }}
    >
      <div className="hero-content">
        <h1>{title}</h1>
        {subtitle && <p>{subtitle}</p>}

        <div className="hero-cta">
          {ctaButtons.map((btn, idx) => (
            <a
              key={idx}
              href={btn.link}
              className={`btn btn-${btn.style}`}
            >
              {btn.text}
            </a>
          ))}
        </div>
      </div>
    </section>
  );
};
```

### Theme Provider

```tsx
// ThemeProvider.tsx
export const ThemeProvider = ({ theme, children }) => {
  useEffect(() => {
    if (!theme) return;

    // Inject CSS variables
    const root = document.documentElement;

    if (theme.colors) {
      Object.entries(theme.colors).forEach(([key, value]) => {
        root.style.setProperty(`--color-${key}`, value);
      });
    }

    if (theme.typography) {
      root.style.setProperty('--font-family', theme.typography.fontFamily);
      root.style.setProperty('--font-size-base', theme.typography.baseFontSize);
    }

    // Update favicon & logo
    if (theme.faviconUrl) {
      const favicon = document.querySelector('link[rel="icon"]');
      if (favicon) favicon.setAttribute('href', theme.faviconUrl);
    }
  }, [theme]);

  return <>{children}</>;
};
```

---

## 🔄 Workflow

### 1. Admin Creates Layout Template

```typescript
// POST /api/layout-templates
{
  "name": "Homepage Template",
  "slug": "homepage-template",
  "description": "Default homepage layout",
  "category": "Homepage",
  "layout": {
    "sections": [
      {
        "id": "hero-1",
        "type": "hero",
        "order": 1,
        "config": { ... }
      }
    ],
    "theme": { ... }
  }
}
```

### 2. Apply Template to Page

```typescript
// POST /api/pages/{pageId}/apply-template
{
  "templateId": "template-uuid"
}
```

### 3. Customize Page Layout

```typescript
// PUT /api/pages/{pageId}/layout
{
  "sections": [
    // Modified sections
  ],
  "theme": {
    // Custom theme for this page
  }
}
```

### 4. Frontend Fetches & Renders

```typescript
// GET /api/pages/homepage?include=layout
const response = await fetch('/api/pages/homepage?include=layout');
const page = await response.json();

// Render page with layout
<PageRenderer pageConfig={page.layout} />
```

---

## 🎯 Use Cases

### Use Case 1: Marketing Landing Page

Admin tạo landing page với:
- Hero section với form đăng ký
- Features grid
- Testimonials
- CTA banner

→ Không cần dev, chỉ config JSON

### Use Case 2: Blog Homepage

Template có sẵn:
- Hero banner
- Latest posts grid
- Categories sidebar
- Newsletter signup

→ Apply template, customize colors/text

### Use Case 3: Product Page

Sections:
- Product hero với images
- Description tabs
- Related products
- Reviews section

→ Mix & match sections từ library

---

## 🚀 Future Enhancements

### Phase 1 (MVP) ✅
- [x] JSON-based layout configuration
- [x] Section types definition
- [x] Layout templates
- [x] Theme customization
- [x] Frontend runtime engine

### Phase 2
- [ ] Visual drag & drop page builder
- [ ] Real-time preview
- [ ] Section component marketplace
- [ ] A/B testing layouts

### Phase 3
- [ ] Layout versioning & rollback
- [ ] AI-powered layout suggestions
- [ ] Performance analytics per layout
- [ ] Multi-language support per section

---

## 📝 Example Usage

See `layout-examples.json` for complete examples of:
- Homepage layout
- Landing page layout
- Section type definitions with config schemas
- Theme configurations

---

## 🛠️ Development Guide

### Adding New Section Type

1. **Backend: Create SectionType**
```http
POST /api/section-types
{
  "name": "Testimonials Slider",
  "typeKey": "testimonials",
  "configSchema": { ... },
  "defaultConfig": { ... }
}
```

2. **Frontend: Create Component**
```tsx
// sections/TestimonialsSection.tsx
export const TestimonialsSection = ({ config }) => {
  return <div>...</div>;
};
```

3. **Frontend: Register Component**
```tsx
// componentRegistry.ts
export const COMPONENT_REGISTRY = {
  ...
  'testimonials': TestimonialsSection
};
```

---

## 🔒 Security Considerations

1. **Input Validation**: Validate all JSON configs on backend
2. **XSS Prevention**: Sanitize custom CSS/JS inputs
3. **Authorization**: Only admins can modify layouts
4. **Rate Limiting**: Limit layout update frequency

---

## 📊 Performance Optimization

1. **Caching**: Cache layout configs in Redis
2. **Lazy Loading**: Load sections on scroll (React.lazy)
3. **Image Optimization**: Auto-optimize uploaded images
4. **CDN**: Serve static assets from CDN

---

Xem `layout-examples.json` để biết chi tiết cách sử dụng! 🎉
