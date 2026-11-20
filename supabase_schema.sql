-- ============================================================================
-- BookNest — Supabase SQL Schema Migration
-- متجر كتب أونلاين ثنائي اللغة (عربي/إنجليزي)
-- ============================================================================
--
-- هذا الملف يحتوي على:
--   1. الجداول الأساسية (Entities) مع القيود والفهارس
--   2. سياسات أمان الصفوف (Row Level Security)
--   3. دوال المنطق التجاري (Business Logic Functions)
--   4. المشغلات (Triggers)
--   5. بيانات أولية (Seed Data)
--   6. تخزين الملفات (Storage Buckets)
--
-- الكيانات:
--   ApplicationUser, Category, Author, Book, CartItem, WishlistItem,
--   Address, Order, OrderItem, OrderStatusHistory, StockReservation,
--   NotifyMeSubscription
--
-- ملاحظة: يعتمد على Supabase Auth (auth.uid(), auth.jwt())
-- ============================================================================

-- ============================================================================
-- 0. EXTENSIONS — الإضافات المطلوبة
-- ============================================================================
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "pg_trgm";    -- Trigram index for search / فهارس البحث النصي


-- ============================================================================
-- 1. CUSTOM TYPES — أنواع مخصصة
-- ============================================================================

-- حالات الطلب — Order status enum
-- مسار الحالة: Pending → Processing → Shipped → Delivered
-- الإلغاء مسموح قبل Shipped فقط (BR-01)
CREATE TYPE order_status AS ENUM (
  'Pending',
  'Processing',
  'Shipped',
  'Delivered',
  'Cancelled'
);

-- أدوار المستخدمين
CREATE TYPE user_role AS ENUM ('Customer', 'Admin');


-- ============================================================================
-- 2. TABLES — الجداول
-- ============================================================================

-- -------------------------------------------------------
-- 2.1 ApplicationUser — المستخدمون
-- مرتبط بـ auth.users عبر id (UUID)
-- -------------------------------------------------------
CREATE TABLE public.application_users (
  id            UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  full_name     TEXT        NOT NULL CHECK (char_length(full_name) BETWEEN 2 AND 100),
  email         TEXT        NOT NULL UNIQUE CHECK (char_length(email) <= 254),
  phone         TEXT        CHECK (phone IS NULL OR phone ~ '^01[0125][0-9]{8}$'),  -- تليفون مصري
  role          user_role   NOT NULL DEFAULT 'Customer',
  is_active     BOOLEAN     NOT NULL DEFAULT TRUE,
  avatar_url    TEXT        CHECK (avatar_url IS NULL OR char_length(avatar_url) <= 500),
  preferred_lang TEXT       NOT NULL DEFAULT 'ar' CHECK (preferred_lang IN ('ar', 'en')),
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE public.application_users IS 'جدول المستخدمين — Users table linked to Supabase Auth';
COMMENT ON COLUMN public.application_users.phone IS 'رقم تليفون مصري — Egyptian phone: 01[0125]XXXXXXXX';


-- -------------------------------------------------------
-- 2.2 Category — التصنيفات
-- -------------------------------------------------------
CREATE TABLE public.categories (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name_ar       TEXT        NOT NULL CHECK (char_length(name_ar) BETWEEN 1 AND 100),
  name_en       TEXT        NOT NULL CHECK (char_length(name_en) BETWEEN 1 AND 100),
  slug          TEXT        NOT NULL UNIQUE CHECK (char_length(slug) BETWEEN 1 AND 120),
  description_ar TEXT       CHECK (description_ar IS NULL OR char_length(description_ar) <= 500),
  description_en TEXT       CHECK (description_en IS NULL OR char_length(description_en) <= 500),
  display_order INT         NOT NULL DEFAULT 0,
  is_active     BOOLEAN     NOT NULL DEFAULT TRUE,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE public.categories IS 'تصنيفات الكتب — Book categories';


-- -------------------------------------------------------
-- 2.3 Author — المؤلفون
-- -------------------------------------------------------
CREATE TABLE public.authors (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name_ar       TEXT        NOT NULL CHECK (char_length(name_ar) BETWEEN 1 AND 150),
  name_en       TEXT        NOT NULL CHECK (char_length(name_en) BETWEEN 1 AND 150),
  slug          TEXT        NOT NULL UNIQUE CHECK (char_length(slug) BETWEEN 1 AND 170),
  bio_ar        TEXT        CHECK (bio_ar IS NULL OR char_length(bio_ar) <= 2000),
  bio_en        TEXT        CHECK (bio_en IS NULL OR char_length(bio_en) <= 2000),
  avatar_url    TEXT        CHECK (avatar_url IS NULL OR char_length(avatar_url) <= 500),
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE public.authors IS 'المؤلفون — Authors';


-- -------------------------------------------------------
-- 2.4 Book — الكتب
-- BR-03: سعر الخصم أقل من الأصلي
-- BR-05: ISBN فريد
-- BR-07: Soft Delete
-- BR-10: المتاح = Stock - Reserved
-- -------------------------------------------------------
CREATE TABLE public.books (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  title_ar        TEXT        NOT NULL CHECK (char_length(title_ar) BETWEEN 1 AND 200),
  title_en        TEXT        NOT NULL CHECK (char_length(title_en) BETWEEN 1 AND 200),
  slug            TEXT        NOT NULL UNIQUE CHECK (char_length(slug) BETWEEN 1 AND 220),
  isbn            TEXT        NOT NULL UNIQUE CHECK (char_length(isbn) BETWEEN 10 AND 17),  -- BR-05
  description_ar  TEXT        CHECK (description_ar IS NULL OR char_length(description_ar) <= 2000),
  description_en  TEXT        CHECK (description_en IS NULL OR char_length(description_en) <= 2000),
  price           NUMERIC(10,2) NOT NULL CHECK (price > 0),
  discount_price  NUMERIC(10,2) CHECK (discount_price IS NULL OR discount_price > 0),
  stock           INT         NOT NULL DEFAULT 0 CHECK (stock >= 0),
  reserved_stock  INT         NOT NULL DEFAULT 0 CHECK (reserved_stock >= 0),
  pages           INT         CHECK (pages IS NULL OR pages > 0),
  language        TEXT        NOT NULL DEFAULT 'ar' CHECK (language IN ('ar', 'en', 'ar-en')),
  publisher       TEXT        CHECK (publisher IS NULL OR char_length(publisher) <= 200),
  publication_year INT        CHECK (publication_year IS NULL OR publication_year BETWEEN 1000 AND 2100),
  cover_url       TEXT        CHECK (cover_url IS NULL OR char_length(cover_url) <= 500),
  category_id     UUID        NOT NULL REFERENCES public.categories(id) ON DELETE RESTRICT,
  author_id       UUID        NOT NULL REFERENCES public.authors(id) ON DELETE RESTRICT,
  is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,  -- BR-07: Soft Delete
  row_version     INT         NOT NULL DEFAULT 1,       -- Optimistic concurrency
  -- نص مُعرَّب مُطبَّع للبحث — Normalized Arabic text for search
  title_normalized TEXT       GENERATED ALWAYS AS (
    regexp_replace(
      translate(
        regexp_replace(lower(title_ar || ' ' || title_en), '[\u064B-\u065F\u0670]', '', 'g'),  -- إزالة التشكيل
        'أإآةى', 'اااهي'  -- توحيد الحروف
      ),
      '\s+', ' ', 'g'
    )
  ) STORED,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

  -- BR-03: سعر الخصم لازم يكون أقل من السعر الأصلي
  CONSTRAINT chk_discount_less_than_price
    CHECK (discount_price IS NULL OR discount_price < price),

  -- BR-10: المحجوز لا يتعدى المخزون
  CONSTRAINT chk_reserved_not_exceeding_stock
    CHECK (reserved_stock <= stock)
);

COMMENT ON TABLE public.books IS 'الكتب — Books catalog with soft delete and stock reservation';
COMMENT ON COLUMN public.books.reserved_stock IS 'الكمية المحجوزة مؤقتاً — Temporarily reserved quantity (BR-10)';
COMMENT ON COLUMN public.books.row_version IS 'نسخة الصف للتحكم بالتزامن — Optimistic concurrency version';


-- -------------------------------------------------------
-- 2.5 CartItem — عناصر السلة
-- -------------------------------------------------------
CREATE TABLE public.cart_items (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id     UUID        NOT NULL REFERENCES public.application_users(id) ON DELETE CASCADE,
  book_id     UUID        NOT NULL REFERENCES public.books(id) ON DELETE CASCADE,
  quantity    INT         NOT NULL CHECK (quantity > 0 AND quantity <= 99),
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),

  UNIQUE (user_id, book_id)
);

COMMENT ON TABLE public.cart_items IS 'سلة التسوق — Shopping cart items';


-- -------------------------------------------------------
-- 2.6 WishlistItem — قائمة الأمنيات
-- -------------------------------------------------------
CREATE TABLE public.wishlist_items (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id     UUID        NOT NULL REFERENCES public.application_users(id) ON DELETE CASCADE,
  book_id     UUID        NOT NULL REFERENCES public.books(id) ON DELETE CASCADE,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),

  UNIQUE (user_id, book_id)
);

COMMENT ON TABLE public.wishlist_items IS 'قائمة الأمنيات — Wishlist';


-- -------------------------------------------------------
-- 2.7 Address — العناوين
-- -------------------------------------------------------
CREATE TABLE public.addresses (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id         UUID        NOT NULL REFERENCES public.application_users(id) ON DELETE CASCADE,
  label           TEXT        NOT NULL DEFAULT 'المنزل' CHECK (char_length(label) BETWEEN 1 AND 50),
  recipient_name  TEXT        NOT NULL CHECK (char_length(recipient_name) BETWEEN 2 AND 100),
  phone           TEXT        NOT NULL CHECK (phone ~ '^01[0125][0-9]{8}$'),  -- تليفون مصري
  governorate     TEXT        NOT NULL CHECK (char_length(governorate) BETWEEN 2 AND 50),
  city            TEXT        NOT NULL CHECK (char_length(city) BETWEEN 2 AND 100),
  street_address  TEXT        NOT NULL CHECK (char_length(street_address) BETWEEN 10 AND 200),
  postal_code     TEXT        CHECK (postal_code IS NULL OR char_length(postal_code) BETWEEN 3 AND 10),
  is_default      BOOLEAN     NOT NULL DEFAULT FALSE,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE public.addresses IS 'عناوين الشحن — Shipping addresses';


-- -------------------------------------------------------
-- 2.8 Order — الطلبات
-- -------------------------------------------------------
CREATE TABLE public.orders (
  id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_number      TEXT        NOT NULL UNIQUE,  -- رقم طلب فريد مقروء
  user_id           UUID        NOT NULL REFERENCES public.application_users(id) ON DELETE RESTRICT,
  status            order_status NOT NULL DEFAULT 'Pending',
  subtotal          NUMERIC(10,2) NOT NULL CHECK (subtotal >= 0),
  shipping_cost     NUMERIC(10,2) NOT NULL DEFAULT 0 CHECK (shipping_cost >= 0),
  total             NUMERIC(10,2) NOT NULL CHECK (total >= 0),
  payment_method    TEXT        NOT NULL DEFAULT 'cod' CHECK (payment_method IN ('cod', 'card')),
  idempotency_key   TEXT        UNIQUE,  -- منع تكرار الطلب
  -- بيانات الشحن (Snapshot من العنوان وقت الطلب)
  shipping_name     TEXT        NOT NULL CHECK (char_length(shipping_name) BETWEEN 2 AND 100),
  shipping_phone    TEXT        NOT NULL CHECK (shipping_phone ~ '^01[0125][0-9]{8}$'),
  shipping_governorate TEXT     NOT NULL CHECK (char_length(shipping_governorate) BETWEEN 2 AND 50),
  shipping_city     TEXT        NOT NULL CHECK (char_length(shipping_city) BETWEEN 2 AND 100),
  shipping_address  TEXT        NOT NULL CHECK (char_length(shipping_address) BETWEEN 10 AND 200),
  notes             TEXT        CHECK (notes IS NULL OR char_length(notes) <= 500),
  row_version       INT         NOT NULL DEFAULT 1,  -- Optimistic concurrency
  created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE public.orders IS 'الطلبات — Orders with status tracking';
COMMENT ON COLUMN public.orders.idempotency_key IS 'مفتاح لمنع تكرار الطلب — Prevents duplicate order submission';


-- -------------------------------------------------------
-- 2.9 OrderItem — عناصر الطلب
-- BR-04: سعر الكتاب وقت الطلب (Snapshot)
-- -------------------------------------------------------
CREATE TABLE public.order_items (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id      UUID        NOT NULL REFERENCES public.orders(id) ON DELETE CASCADE,
  book_id       UUID        NOT NULL REFERENCES public.books(id) ON DELETE RESTRICT,
  quantity      INT         NOT NULL CHECK (quantity > 0 AND quantity <= 99),
  unit_price    NUMERIC(10,2) NOT NULL CHECK (unit_price > 0),   -- BR-04: Snapshot price
  total_price   NUMERIC(10,2) NOT NULL CHECK (total_price > 0),
  book_title    TEXT        NOT NULL CHECK (char_length(book_title) BETWEEN 1 AND 200),  -- Snapshot
  book_cover_url TEXT       CHECK (book_cover_url IS NULL OR char_length(book_cover_url) <= 500),

  CONSTRAINT chk_total_matches CHECK (total_price = unit_price * quantity)
);

COMMENT ON TABLE public.order_items IS 'عناصر الطلب بأسعار مُثبتة — Order items with price snapshot (BR-04)';


-- -------------------------------------------------------
-- 2.10 OrderStatusHistory — سجل تغييرات حالة الطلب
-- FR-ORD-10: مين غيّر الحالة وإمتى وليه
-- -------------------------------------------------------
CREATE TABLE public.order_status_history (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id      UUID         NOT NULL REFERENCES public.orders(id) ON DELETE CASCADE,
  old_status    order_status,
  new_status    order_status NOT NULL,
  changed_by    UUID         NOT NULL REFERENCES public.application_users(id) ON DELETE SET NULL,
  reason        TEXT         CHECK (reason IS NULL OR char_length(reason) <= 500),
  created_at    TIMESTAMPTZ  NOT NULL DEFAULT now()
);

COMMENT ON TABLE public.order_status_history IS 'سجل تغييرات حالة الطلب — Order status audit trail (FR-ORD-10)';


-- -------------------------------------------------------
-- 2.11 StockReservation — حجز المخزون المؤقت
-- FR-RES-01: حجز 15 دقيقة عند بدء الـ Checkout
-- -------------------------------------------------------
CREATE TABLE public.stock_reservations (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id       UUID        NOT NULL REFERENCES public.application_users(id) ON DELETE CASCADE,
  book_id       UUID        NOT NULL REFERENCES public.books(id) ON DELETE CASCADE,
  quantity      INT         NOT NULL CHECK (quantity > 0 AND quantity <= 99),
  expires_at    TIMESTAMPTZ NOT NULL,
  is_confirmed  BOOLEAN     NOT NULL DEFAULT FALSE,
  is_released   BOOLEAN     NOT NULL DEFAULT FALSE,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),

  UNIQUE (user_id, book_id)
);

COMMENT ON TABLE public.stock_reservations IS 'حجز مخزون مؤقت 15 دقيقة — Temporary stock reservation with TTL (FR-RES-01)';


-- -------------------------------------------------------
-- 2.12 NotifyMeSubscription — نبّهني لما يتوفر
-- FR-NOTIF-01: إيميل واحد لكل اشتراك
-- BR-13: اشتراك واحد لكل (يوزر، كتاب)
-- -------------------------------------------------------
CREATE TABLE public.notify_me_subscriptions (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id       UUID        NOT NULL REFERENCES public.application_users(id) ON DELETE CASCADE,
  book_id       UUID        NOT NULL REFERENCES public.books(id) ON DELETE CASCADE,
  is_notified   BOOLEAN     NOT NULL DEFAULT FALSE,
  notified_at   TIMESTAMPTZ,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),

  UNIQUE (user_id, book_id)
);

COMMENT ON TABLE public.notify_me_subscriptions IS 'اشتراكات الإشعار عند التوفر — Notify me when available (FR-NOTIF-01)';


-- ============================================================================
-- 3. INDEXES — الفهارس للأداء
-- ============================================================================

-- Books — البحث والفلترة والترتيب
CREATE INDEX idx_books_category       ON public.books(category_id) WHERE NOT is_deleted;
CREATE INDEX idx_books_author         ON public.books(author_id) WHERE NOT is_deleted;
CREATE INDEX idx_books_price          ON public.books(price) WHERE NOT is_deleted;
CREATE INDEX idx_books_created_at     ON public.books(created_at DESC) WHERE NOT is_deleted;
CREATE INDEX idx_books_stock          ON public.books(stock) WHERE NOT is_deleted AND stock > 0;
CREATE INDEX idx_books_is_deleted     ON public.books(is_deleted);
CREATE INDEX idx_books_discount       ON public.books(id) WHERE NOT is_deleted AND discount_price IS NOT NULL;
-- بحث نصي بالعنوان المُعرَّب — Trigram search on normalized title
CREATE INDEX idx_books_title_trgm     ON public.books USING gin(title_normalized gin_trgm_ops);
CREATE INDEX idx_books_isbn           ON public.books(isbn);

-- Cart
CREATE INDEX idx_cart_items_user      ON public.cart_items(user_id);

-- Wishlist
CREATE INDEX idx_wishlist_user        ON public.wishlist_items(user_id);

-- Orders
CREATE INDEX idx_orders_user          ON public.orders(user_id);
CREATE INDEX idx_orders_status        ON public.orders(status);
CREATE INDEX idx_orders_created_at    ON public.orders(created_at DESC);
CREATE INDEX idx_orders_number        ON public.orders(order_number);

-- Order Items
CREATE INDEX idx_order_items_order    ON public.order_items(order_id);
CREATE INDEX idx_order_items_book     ON public.order_items(book_id);

-- Status History
CREATE INDEX idx_status_history_order ON public.order_status_history(order_id, created_at DESC);

-- Reservations
CREATE INDEX idx_reservations_user    ON public.stock_reservations(user_id);
CREATE INDEX idx_reservations_book    ON public.stock_reservations(book_id);
CREATE INDEX idx_reservations_expires ON public.stock_reservations(expires_at) WHERE NOT is_confirmed AND NOT is_released;

-- Addresses
CREATE INDEX idx_addresses_user       ON public.addresses(user_id);

-- NotifyMe
CREATE INDEX idx_notify_book          ON public.notify_me_subscriptions(book_id) WHERE NOT is_notified;
CREATE INDEX idx_notify_user          ON public.notify_me_subscriptions(user_id);

-- Categories / Authors
CREATE INDEX idx_categories_slug      ON public.categories(slug);
CREATE INDEX idx_authors_slug         ON public.authors(slug);


-- ============================================================================
-- 4. HELPER FUNCTIONS — دوال مساعدة
-- ============================================================================

-- -------------------------------------------------------
-- 4.1 arabic_normalize() — تطبيع النص العربي
-- يوحّد: أ/إ/آ→ا، ة→ه، ى→ي، ويزيل التشكيل
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.arabic_normalize(input_text TEXT)
RETURNS TEXT
LANGUAGE plpgsql IMMUTABLE STRICT
AS $$
BEGIN
  RETURN regexp_replace(
    translate(
      regexp_replace(
        lower(COALESCE(input_text, '')),
        '[\u064B-\u065F\u0670]', '', 'g'  -- إزالة التشكيل — Remove tashkeel
      ),
      'أإآةى',   -- الحروف الأصلية — Source chars
      'اااهي'    -- الحروف البديلة — Replacement chars
    ),
    '\s+', ' ', 'g'  -- توحيد المسافات — Normalize whitespace
  );
END;
$$;

COMMENT ON FUNCTION public.arabic_normalize IS 'تطبيع النص العربي للبحث — Normalize Arabic text for search (أ/إ/آ→ا, ة→ه, ى→ي, remove tashkeel)';


-- -------------------------------------------------------
-- 4.2 generate_order_number() — توليد رقم طلب فريد مقروء
-- الصيغة: BN-YYYYMMDD-XXXXX (5 أرقام عشوائية)
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.generate_order_number()
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE
  v_number TEXT;
  v_exists BOOLEAN;
BEGIN
  LOOP
    v_number := 'BN-' || to_char(now(), 'YYYYMMDD') || '-' || lpad(floor(random() * 100000)::TEXT, 5, '0');
    SELECT EXISTS(SELECT 1 FROM public.orders WHERE order_number = v_number) INTO v_exists;
    EXIT WHEN NOT v_exists;
  END LOOP;
  RETURN v_number;
END;
$$;


-- -------------------------------------------------------
-- 4.3 calculate_shipping() — حساب تكلفة الشحن
-- BR-06: 50 ج.م ثابت، مجاني فوق 500 ج.م
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.calculate_shipping(subtotal NUMERIC)
RETURNS NUMERIC
LANGUAGE plpgsql IMMUTABLE
AS $$
DECLARE
  v_shipping_cost NUMERIC := 50.00;
  v_free_threshold NUMERIC := 500.00;
BEGIN
  IF subtotal >= v_free_threshold THEN
    RETURN 0;
  ELSE
    RETURN v_shipping_cost;
  END IF;
END;
$$;

COMMENT ON FUNCTION public.calculate_shipping IS 'حساب الشحن: 50 ج.م، مجاني فوق 500 ج.م — Shipping: 50 EGP, free above 500 EGP (BR-06)';


-- -------------------------------------------------------
-- 4.4 get_available_stock() — المخزون المتاح
-- BR-10: المتاح = Stock - Reserved
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.get_available_stock(p_book_id UUID)
RETURNS INT
LANGUAGE plpgsql STABLE
AS $$
DECLARE
  v_stock INT;
  v_reserved INT;
BEGIN
  SELECT stock, reserved_stock INTO v_stock, v_reserved
  FROM public.books
  WHERE id = p_book_id AND NOT is_deleted;

  IF NOT FOUND THEN
    RETURN 0;
  END IF;

  RETURN GREATEST(v_stock - v_reserved, 0);
END;
$$;

COMMENT ON FUNCTION public.get_available_stock IS 'المخزون المتاح = المخزون - المحجوز — Available = Stock - Reserved (BR-10)';


-- ============================================================================
-- 5. BUSINESS LOGIC FUNCTIONS — دوال المنطق التجاري
-- ============================================================================

-- -------------------------------------------------------
-- 5.1 update_order_status() — تغيير حالة الطلب
-- BR-01: مسار الحالة المسموح مع State Machine validation
-- FR-ORD-10: تسجيل في OrderStatusHistory
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.update_order_status(
  p_order_id    UUID,
  p_new_status  order_status,
  p_changed_by  UUID,
  p_reason      TEXT DEFAULT NULL
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
  v_current_status order_status;
  v_row_version INT;
  v_valid BOOLEAN := FALSE;
BEGIN
  -- قفل الصف — Lock the order row
  SELECT status, row_version INTO v_current_status, v_row_version
  FROM public.orders
  WHERE id = p_order_id
  FOR UPDATE;

  IF NOT FOUND THEN
    RETURN jsonb_build_object('success', false, 'error', 'Order not found');
  END IF;

  -- BR-01: التحقق من صحة الانتقال — Validate state transition
  v_valid := CASE
    WHEN v_current_status = 'Pending'    AND p_new_status IN ('Processing', 'Cancelled') THEN TRUE
    WHEN v_current_status = 'Processing' AND p_new_status IN ('Shipped', 'Cancelled')    THEN TRUE
    WHEN v_current_status = 'Shipped'    AND p_new_status = 'Delivered'                  THEN TRUE
    ELSE FALSE
  END;

  IF NOT v_valid THEN
    RETURN jsonb_build_object(
      'success', false,
      'error', format('Invalid transition from %s to %s', v_current_status, p_new_status)
    );
  END IF;

  -- تحديث الحالة — Update status
  UPDATE public.orders
  SET status = p_new_status,
      row_version = row_version + 1,
      updated_at = now()
  WHERE id = p_order_id;

  -- تسجيل التغيير — Log to history (FR-ORD-10)
  INSERT INTO public.order_status_history (order_id, old_status, new_status, changed_by, reason)
  VALUES (p_order_id, v_current_status, p_new_status, p_changed_by, p_reason);

  RETURN jsonb_build_object('success', true, 'old_status', v_current_status::TEXT, 'new_status', p_new_status::TEXT);
END;
$$;

COMMENT ON FUNCTION public.update_order_status IS 'تغيير حالة الطلب مع التحقق من المسار المسموح — Update order status with state machine validation (BR-01)';


-- -------------------------------------------------------
-- 5.2 place_order() — تأكيد الطلب
-- FR-ORD-04: خصم المخزون الذرّي داخل Transaction
-- BR-02: لا يمكن طلب أكتر من المتاح
-- BR-04: حفظ السعر (Snapshot) في OrderItem
-- BR-06: حساب الشحن
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.place_order(
  p_user_id           UUID,
  p_address_id        UUID,
  p_payment_method    TEXT DEFAULT 'cod',
  p_idempotency_key   TEXT DEFAULT NULL,
  p_notes             TEXT DEFAULT NULL
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
  v_cart_item    RECORD;
  v_order_id     UUID;
  v_order_number TEXT;
  v_subtotal     NUMERIC := 0;
  v_shipping     NUMERIC;
  v_total        NUMERIC;
  v_unit_price   NUMERIC;
  v_available    INT;
  v_address      RECORD;
  v_items_count  INT := 0;
  v_rows_updated INT;
BEGIN
  -- التحقق من عدم تكرار الطلب — Idempotency check
  IF p_idempotency_key IS NOT NULL THEN
    SELECT id INTO v_order_id FROM public.orders WHERE idempotency_key = p_idempotency_key;
    IF FOUND THEN
      RETURN jsonb_build_object('success', true, 'order_id', v_order_id, 'duplicate', true);
    END IF;
  END IF;

  -- جلب بيانات العنوان — Get address data (Snapshot)
  SELECT recipient_name, phone, governorate, city, street_address
  INTO v_address
  FROM public.addresses
  WHERE id = p_address_id AND user_id = p_user_id;

  IF NOT FOUND THEN
    RETURN jsonb_build_object('success', false, 'error', 'Address not found or does not belong to user');
  END IF;

  -- توليد رقم الطلب — Generate order number
  v_order_number := public.generate_order_number();
  v_order_id := gen_random_uuid();

  -- معالجة كل عنصر في السلة — Process each cart item
  FOR v_cart_item IN
    SELECT ci.book_id, ci.quantity, b.title_ar, b.price, b.discount_price, b.cover_url, b.stock, b.reserved_stock
    FROM public.cart_items ci
    JOIN public.books b ON b.id = ci.book_id AND NOT b.is_deleted
    WHERE ci.user_id = p_user_id
    ORDER BY ci.book_id  -- ترتيب بالـ ID لمنع الـ Deadlock
  LOOP
    v_items_count := v_items_count + 1;

    -- BR-04: السعر الفعلي (خصم أو أصلي)
    v_unit_price := COALESCE(v_cart_item.discount_price, v_cart_item.price);

    -- BR-02: التحقق من المخزون المتاح (Stock - Reserved)
    v_available := v_cart_item.stock - v_cart_item.reserved_stock;
    IF v_cart_item.quantity > v_available THEN
      RAISE EXCEPTION 'Insufficient stock for book %. Available: %, Requested: %',
        v_cart_item.book_id, v_available, v_cart_item.quantity;
    END IF;

    -- خصم المخزون الذرّي — Atomic stock deduction (FR-ORD-04)
    UPDATE public.books
    SET stock = stock - v_cart_item.quantity,
        row_version = row_version + 1,
        updated_at = now()
    WHERE id = v_cart_item.book_id
      AND stock >= v_cart_item.quantity
      AND NOT is_deleted;

    GET DIAGNOSTICS v_rows_updated = ROW_COUNT;
    IF v_rows_updated = 0 THEN
      RAISE EXCEPTION 'Failed to deduct stock for book %. Concurrent modification detected.', v_cart_item.book_id;
    END IF;

    -- حساب المجموع الفرعي
    v_subtotal := v_subtotal + (v_unit_price * v_cart_item.quantity);

    -- إضافة عنصر الطلب — Insert order item
    INSERT INTO public.order_items (order_id, book_id, quantity, unit_price, total_price, book_title, book_cover_url)
    VALUES (v_order_id, v_cart_item.book_id, v_cart_item.quantity, v_unit_price,
            v_unit_price * v_cart_item.quantity, v_cart_item.title_ar, v_cart_item.cover_url);
  END LOOP;

  -- التأكد من وجود عناصر — Make sure cart is not empty
  IF v_items_count = 0 THEN
    RETURN jsonb_build_object('success', false, 'error', 'Cart is empty');
  END IF;

  -- BR-06: حساب الشحن
  v_shipping := public.calculate_shipping(v_subtotal);
  v_total := v_subtotal + v_shipping;

  -- إنشاء الطلب — Create the order
  INSERT INTO public.orders (
    id, order_number, user_id, status, subtotal, shipping_cost, total,
    payment_method, idempotency_key,
    shipping_name, shipping_phone, shipping_governorate, shipping_city, shipping_address, notes
  ) VALUES (
    v_order_id, v_order_number, p_user_id, 'Pending', v_subtotal, v_shipping, v_total,
    p_payment_method, p_idempotency_key,
    v_address.recipient_name, v_address.phone, v_address.governorate,
    v_address.city, v_address.street_address, p_notes
  );

  -- تسجيل الحالة الأولية — Log initial status
  INSERT INTO public.order_status_history (order_id, old_status, new_status, changed_by, reason)
  VALUES (v_order_id, NULL, 'Pending', p_user_id, 'Order placed');

  -- تفريغ السلة — Clear cart
  DELETE FROM public.cart_items WHERE user_id = p_user_id;

  -- تحرير الحجوزات المؤكدة — Release confirmed reservations
  UPDATE public.stock_reservations
  SET is_confirmed = TRUE
  WHERE user_id = p_user_id AND NOT is_released;

  RETURN jsonb_build_object(
    'success', true,
    'order_id', v_order_id,
    'order_number', v_order_number,
    'total', v_total,
    'duplicate', false
  );
END;
$$;

COMMENT ON FUNCTION public.place_order IS 'تأكيد الطلب بخصم ذرّي للمخزون — Atomically place order with stock deduction (FR-ORD-04)';


-- -------------------------------------------------------
-- 5.3 cancel_order() — إلغاء الطلب
-- FR-ORD-06: إلغاء الطلب وإرجاع المخزون
-- BR-01: مسموح قبل Shipped فقط
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.cancel_order(
  p_order_id    UUID,
  p_cancelled_by UUID,
  p_reason      TEXT DEFAULT 'Cancelled by customer'
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
  v_order       RECORD;
  v_item        RECORD;
BEGIN
  -- قفل الطلب — Lock the order
  SELECT id, status, user_id, row_version
  INTO v_order
  FROM public.orders
  WHERE id = p_order_id
  FOR UPDATE;

  IF NOT FOUND THEN
    RETURN jsonb_build_object('success', false, 'error', 'Order not found');
  END IF;

  -- BR-01: الإلغاء مسموح قبل Shipped فقط
  IF v_order.status NOT IN ('Pending', 'Processing') THEN
    RETURN jsonb_build_object('success', false, 'error',
      format('Cannot cancel order with status %s. Only Pending or Processing orders can be cancelled.', v_order.status));
  END IF;

  -- إرجاع المخزون — Restore stock for each item
  FOR v_item IN
    SELECT book_id, quantity FROM public.order_items WHERE order_id = p_order_id
    ORDER BY book_id  -- ترتيب لمنع الـ Deadlock
  LOOP
    UPDATE public.books
    SET stock = stock + v_item.quantity,
        row_version = row_version + 1,
        updated_at = now()
    WHERE id = v_item.book_id;
  END LOOP;

  -- تحديث حالة الطلب — Update order status
  UPDATE public.orders
  SET status = 'Cancelled',
      row_version = row_version + 1,
      updated_at = now()
  WHERE id = p_order_id;

  -- تسجيل التغيير — Log to history
  INSERT INTO public.order_status_history (order_id, old_status, new_status, changed_by, reason)
  VALUES (p_order_id, v_order.status, 'Cancelled', p_cancelled_by, p_reason);

  RETURN jsonb_build_object('success', true, 'old_status', v_order.status::TEXT);
END;
$$;

COMMENT ON FUNCTION public.cancel_order IS 'إلغاء الطلب وإرجاع المخزون — Cancel order and restore stock (FR-ORD-06, BR-01)';


-- -------------------------------------------------------
-- 5.4 reserve_stock() — حجز مخزون مؤقت
-- FR-RES-01: حجز 15 دقيقة عند بدء الـ Checkout
-- BR-10: المتاح = Stock - Reserved
-- BR-11: مدة 15 دقيقة
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.reserve_stock(
  p_user_id  UUID,
  p_book_id  UUID,
  p_quantity INT
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
  v_available     INT;
  v_expires_at    TIMESTAMPTZ;
  v_reservation_id UUID;
  v_existing      RECORD;
  v_rows_updated  INT;
BEGIN
  -- التحقق من الكمية — Validate quantity
  IF p_quantity <= 0 OR p_quantity > 99 THEN
    RETURN jsonb_build_object('success', false, 'error', 'Invalid quantity');
  END IF;

  -- BR-11: مدة الحجز 15 دقيقة
  v_expires_at := now() + INTERVAL '15 minutes';

  -- التحقق من حجز قائم — Check for existing reservation
  SELECT id, quantity INTO v_existing
  FROM public.stock_reservations
  WHERE user_id = p_user_id AND book_id = p_book_id
    AND NOT is_confirmed AND NOT is_released AND expires_at > now()
  FOR UPDATE;

  IF FOUND THEN
    -- تجديد الحجز — Renew existing reservation (BR-11)
    UPDATE public.stock_reservations
    SET expires_at = v_expires_at
    WHERE id = v_existing.id;

    RETURN jsonb_build_object('success', true, 'reservation_id', v_existing.id, 'expires_at', v_expires_at, 'renewed', true);
  END IF;

  -- قفل الكتاب — Lock the book row
  SELECT stock - reserved_stock INTO v_available
  FROM public.books
  WHERE id = p_book_id AND NOT is_deleted
  FOR UPDATE;

  IF NOT FOUND THEN
    RETURN jsonb_build_object('success', false, 'error', 'Book not found');
  END IF;

  -- BR-10: التحقق من المتاح
  IF p_quantity > v_available THEN
    RETURN jsonb_build_object('success', false, 'error',
      format('Insufficient available stock. Available: %s, Requested: %s', v_available, p_quantity));
  END IF;

  -- حجز المخزون — Reserve the stock
  UPDATE public.books
  SET reserved_stock = reserved_stock + p_quantity,
      updated_at = now()
  WHERE id = p_book_id AND (stock - reserved_stock) >= p_quantity;

  GET DIAGNOSTICS v_rows_updated = ROW_COUNT;
  IF v_rows_updated = 0 THEN
    RETURN jsonb_build_object('success', false, 'error', 'Concurrent reservation conflict');
  END IF;

  -- إنشاء سجل الحجز — Create reservation record
  v_reservation_id := gen_random_uuid();
  INSERT INTO public.stock_reservations (id, user_id, book_id, quantity, expires_at)
  VALUES (v_reservation_id, p_user_id, p_book_id, p_quantity, v_expires_at);

  RETURN jsonb_build_object(
    'success', true,
    'reservation_id', v_reservation_id,
    'expires_at', v_expires_at,
    'renewed', false
  );
END;
$$;

COMMENT ON FUNCTION public.reserve_stock IS 'حجز مخزون مؤقت 15 دقيقة — Reserve stock for 15 minutes (FR-RES-01, BR-10, BR-11)';


-- -------------------------------------------------------
-- 5.5 release_expired_reservations() — تحرير الحجوزات المنتهية
-- FR-RES-03: انتهاء الحجز يرجّع الكمية تلقائيًا
-- يُستدعى من Supabase Edge Function / cron
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.release_expired_reservations()
RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
  v_reservation RECORD;
  v_count INT := 0;
BEGIN
  -- قفل ومعالجة كل حجز منتهي — Lock and process expired reservations
  FOR v_reservation IN
    SELECT id, book_id, quantity
    FROM public.stock_reservations
    WHERE expires_at <= now()
      AND NOT is_confirmed
      AND NOT is_released
    ORDER BY book_id  -- ترتيب لمنع الـ Deadlock
    FOR UPDATE SKIP LOCKED
  LOOP
    -- إرجاع الكمية المحجوزة — Return reserved quantity
    UPDATE public.books
    SET reserved_stock = GREATEST(reserved_stock - v_reservation.quantity, 0),
        updated_at = now()
    WHERE id = v_reservation.book_id;

    -- تعليم الحجز كمُحرَّر — Mark reservation as released
    UPDATE public.stock_reservations
    SET is_released = TRUE
    WHERE id = v_reservation.id;

    v_count := v_count + 1;
  END LOOP;

  RETURN v_count;
END;
$$;

COMMENT ON FUNCTION public.release_expired_reservations IS
  'تحرير الحجوزات المنتهية وإرجاع المخزون — Release expired reservations and restore stock (FR-RES-03)';

-- -------------------------------------------------------
-- ملاحظة: يمكن استدعاء هذه الدالة من Supabase Edge Function على cron كل دقيقة:
-- Note: Call via Supabase Edge Function on a 1-minute cron:
--
--   import { createClient } from '@supabase/supabase-js'
--   const supabase = createClient(url, serviceRoleKey)
--   const { data } = await supabase.rpc('release_expired_reservations')
-- -------------------------------------------------------


-- -------------------------------------------------------
-- 5.6 search_books() — بحث بالكتب مع تطبيع عربي
-- FR-SRCH-01: بحث ذكي يتجاهل التشكيل ويوحّد الحروف
-- FR-CAT-01-04: فلترة وترتيب وPagination
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.search_books(
  p_query       TEXT DEFAULT NULL,
  p_category_id UUID DEFAULT NULL,
  p_author_id   UUID DEFAULT NULL,
  p_min_price   NUMERIC DEFAULT NULL,
  p_max_price   NUMERIC DEFAULT NULL,
  p_in_stock    BOOLEAN DEFAULT FALSE,
  p_sort        TEXT DEFAULT 'newest',       -- newest, price_asc, price_desc
  p_page        INT DEFAULT 1,
  p_page_size   INT DEFAULT 12
)
RETURNS TABLE (
  id UUID,
  title_ar TEXT,
  title_en TEXT,
  slug TEXT,
  price NUMERIC,
  discount_price NUMERIC,
  stock INT,
  available_stock INT,
  cover_url TEXT,
  author_name_ar TEXT,
  author_name_en TEXT,
  category_name_ar TEXT,
  category_name_en TEXT,
  total_count BIGINT
)
LANGUAGE plpgsql STABLE SECURITY DEFINER
AS $$
DECLARE
  v_normalized_query TEXT;
  v_offset INT;
  v_safe_page INT;
  v_safe_size INT;
BEGIN
  -- حدود آمنة للصفحات — Safe pagination bounds
  v_safe_page := GREATEST(p_page, 1);
  v_safe_size := LEAST(GREATEST(p_page_size, 1), 50);
  v_offset := (v_safe_page - 1) * v_safe_size;

  -- تطبيع نص البحث — Normalize search query
  IF p_query IS NOT NULL AND char_length(trim(p_query)) > 0 THEN
    v_normalized_query := public.arabic_normalize(trim(p_query));
  END IF;

  RETURN QUERY
  SELECT
    b.id,
    b.title_ar,
    b.title_en,
    b.slug,
    b.price,
    b.discount_price,
    b.stock,
    (b.stock - b.reserved_stock)::INT AS available_stock,
    b.cover_url,
    a.name_ar AS author_name_ar,
    a.name_en AS author_name_en,
    c.name_ar AS category_name_ar,
    c.name_en AS category_name_en,
    COUNT(*) OVER() AS total_count
  FROM public.books b
  JOIN public.authors a ON a.id = b.author_id
  JOIN public.categories c ON c.id = b.category_id
  WHERE NOT b.is_deleted
    AND (v_normalized_query IS NULL OR b.title_normalized % v_normalized_query
         OR b.isbn = p_query)  -- بحث بالـ ISBN أيضاً
    AND (p_category_id IS NULL OR b.category_id = p_category_id)
    AND (p_author_id IS NULL OR b.author_id = p_author_id)
    AND (p_min_price IS NULL OR COALESCE(b.discount_price, b.price) >= p_min_price)
    AND (p_max_price IS NULL OR COALESCE(b.discount_price, b.price) <= p_max_price)
    AND (NOT p_in_stock OR (b.stock - b.reserved_stock) > 0)
  ORDER BY
    CASE WHEN p_sort = 'price_asc'  THEN COALESCE(b.discount_price, b.price) END ASC NULLS LAST,
    CASE WHEN p_sort = 'price_desc' THEN COALESCE(b.discount_price, b.price) END DESC NULLS LAST,
    CASE WHEN p_sort = 'newest' OR p_sort IS NULL THEN b.created_at END DESC NULLS LAST
  LIMIT v_safe_size
  OFFSET v_offset;
END;
$$;

COMMENT ON FUNCTION public.search_books IS 'بحث بالكتب مع تطبيع عربي وفلترة وPagination — Search books with Arabic normalization (FR-SRCH-01, FR-CAT-01-04)';


-- ============================================================================
-- 6. TRIGGERS — المشغلات
-- ============================================================================

-- -------------------------------------------------------
-- 6.1 تحديث updated_at تلقائياً — Auto-update timestamps
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.trg_set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$;

-- تطبيق على كل الجداول اللي فيها updated_at
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.application_users
  FOR EACH ROW EXECUTE FUNCTION public.trg_set_updated_at();

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.categories
  FOR EACH ROW EXECUTE FUNCTION public.trg_set_updated_at();

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.authors
  FOR EACH ROW EXECUTE FUNCTION public.trg_set_updated_at();

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.books
  FOR EACH ROW EXECUTE FUNCTION public.trg_set_updated_at();

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.cart_items
  FOR EACH ROW EXECUTE FUNCTION public.trg_set_updated_at();

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.addresses
  FOR EACH ROW EXECUTE FUNCTION public.trg_set_updated_at();

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.orders
  FOR EACH ROW EXECUTE FUNCTION public.trg_set_updated_at();


-- -------------------------------------------------------
-- 6.2 منع تعطيل الأدمن لنفسه — Prevent admin self-deactivation
-- BR-09: الأدمن ما يقدرش يعطّل أو يشيل Role نفسه
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.trg_prevent_admin_self_deactivation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  -- منع تعطيل النفس — Prevent self-deactivation
  IF OLD.id = auth.uid() THEN
    -- منع تغيير is_active من true لـ false
    IF OLD.is_active = TRUE AND NEW.is_active = FALSE THEN
      RAISE EXCEPTION 'Admin cannot deactivate themselves (BR-09)';
    END IF;
    -- منع تغيير الدور من Admin لـ Customer
    IF OLD.role = 'Admin' AND NEW.role = 'Customer' THEN
      RAISE EXCEPTION 'Admin cannot remove their own Admin role (BR-09)';
    END IF;
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER prevent_admin_self_deactivation
  BEFORE UPDATE ON public.application_users
  FOR EACH ROW
  WHEN (OLD.role = 'Admin')
  EXECUTE FUNCTION public.trg_prevent_admin_self_deactivation();


-- -------------------------------------------------------
-- 6.3 منع حذف تصنيف/مؤلف مرتبط بكتب — Prevent deletion of category/author with books
-- FR-ADM-03
-- -------------------------------------------------------
-- (يتم التحكم بهذا عبر ON DELETE RESTRICT في FK، لكن نضيف رسالة واضحة)
CREATE OR REPLACE FUNCTION public.trg_prevent_delete_with_books()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
  v_count INT;
BEGIN
  IF TG_TABLE_NAME = 'categories' THEN
    SELECT COUNT(*) INTO v_count FROM public.books WHERE category_id = OLD.id AND NOT is_deleted;
  ELSIF TG_TABLE_NAME = 'authors' THEN
    SELECT COUNT(*) INTO v_count FROM public.books WHERE author_id = OLD.id AND NOT is_deleted;
  END IF;

  IF v_count > 0 THEN
    RAISE EXCEPTION 'Cannot delete % with % active book(s). Remove or reassign books first.',
      TG_TABLE_NAME, v_count;
  END IF;

  RETURN OLD;
END;
$$;

CREATE TRIGGER prevent_category_delete_with_books
  BEFORE DELETE ON public.categories
  FOR EACH ROW EXECUTE FUNCTION public.trg_prevent_delete_with_books();

CREATE TRIGGER prevent_author_delete_with_books
  BEFORE DELETE ON public.authors
  FOR EACH ROW EXECUTE FUNCTION public.trg_prevent_delete_with_books();


-- -------------------------------------------------------
-- 6.4 تسجيل حالة الطلب الأولية — Log initial order status
-- (للطلبات المنشأة مباشرة بدون الدالة place_order)
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.trg_log_order_status_change()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  IF TG_OP = 'UPDATE' AND OLD.status != NEW.status THEN
    INSERT INTO public.order_status_history (order_id, old_status, new_status, changed_by, reason)
    VALUES (NEW.id, OLD.status, NEW.status, auth.uid(), 'Status updated via trigger');
  END IF;
  RETURN NEW;
END;
$$;

CREATE TRIGGER log_order_status_change
  AFTER UPDATE OF status ON public.orders
  FOR EACH ROW
  WHEN (OLD.status IS DISTINCT FROM NEW.status)
  EXECUTE FUNCTION public.trg_log_order_status_change();


-- -------------------------------------------------------
-- 6.5 التحقق من انتقال حالة الطلب — Validate order status transition
-- BR-01 enforcement at trigger level
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.trg_validate_order_status_transition()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
  v_valid BOOLEAN;
BEGIN
  v_valid := CASE
    WHEN OLD.status = 'Pending'    AND NEW.status IN ('Processing', 'Cancelled') THEN TRUE
    WHEN OLD.status = 'Processing' AND NEW.status IN ('Shipped', 'Cancelled')    THEN TRUE
    WHEN OLD.status = 'Shipped'    AND NEW.status = 'Delivered'                  THEN TRUE
    ELSE FALSE
  END;

  IF NOT v_valid THEN
    RAISE EXCEPTION 'Invalid order status transition: % → % (BR-01)', OLD.status, NEW.status;
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER validate_order_status_transition
  BEFORE UPDATE OF status ON public.orders
  FOR EACH ROW
  WHEN (OLD.status IS DISTINCT FROM NEW.status)
  EXECUTE FUNCTION public.trg_validate_order_status_transition();


-- ============================================================================
-- 7. ROW LEVEL SECURITY (RLS) — سياسات أمان الصفوف
-- ============================================================================
-- هذا هو الجزء الأهم للأمان في Supabase
-- كل جدول يجب أن يكون عليه RLS مفعّل
-- ============================================================================

-- Helper: دالة للتحقق من دور الأدمن — Check if current user is admin
CREATE OR REPLACE FUNCTION public.is_admin()
RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER
AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.application_users
    WHERE id = auth.uid() AND role = 'Admin' AND is_active = TRUE
  );
$$;

-- Helper: دالة للتحقق من إن المستخدم نشط — Check if user is active
CREATE OR REPLACE FUNCTION public.is_active_user()
RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER
AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.application_users
    WHERE id = auth.uid() AND is_active = TRUE
  );
$$;


-- -------------------------------------------------------
-- 7.1 application_users RLS
-- -------------------------------------------------------
ALTER TABLE public.application_users ENABLE ROW LEVEL SECURITY;

-- المستخدم يشوف بياناته بس — User can see own profile
CREATE POLICY "users_select_own" ON public.application_users
  FOR SELECT USING (id = auth.uid());

-- الأدمن يشوف كل المستخدمين — Admin can see all users
CREATE POLICY "users_select_admin" ON public.application_users
  FOR SELECT USING (public.is_admin());

-- المستخدم يعدّل بياناته بس (ما عدا الدور) — User can update own profile (not role)
CREATE POLICY "users_update_own" ON public.application_users
  FOR UPDATE USING (id = auth.uid())
  WITH CHECK (id = auth.uid() AND role = (SELECT role FROM public.application_users WHERE id = auth.uid()));

-- الأدمن يعدّل أي مستخدم — Admin can update any user
CREATE POLICY "users_update_admin" ON public.application_users
  FOR UPDATE USING (public.is_admin());

-- إنشاء حساب جديد (عند التسجيل) — Insert on signup
CREATE POLICY "users_insert_own" ON public.application_users
  FOR INSERT WITH CHECK (id = auth.uid());


-- -------------------------------------------------------
-- 7.2 categories RLS
-- -------------------------------------------------------
ALTER TABLE public.categories ENABLE ROW LEVEL SECURITY;

-- الكل يقرأ التصنيفات — Everyone can read categories
CREATE POLICY "categories_select_all" ON public.categories
  FOR SELECT USING (TRUE);

-- الأدمن فقط يعدّل — Admin only for mutations
CREATE POLICY "categories_insert_admin" ON public.categories
  FOR INSERT WITH CHECK (public.is_admin());

CREATE POLICY "categories_update_admin" ON public.categories
  FOR UPDATE USING (public.is_admin());

CREATE POLICY "categories_delete_admin" ON public.categories
  FOR DELETE USING (public.is_admin());


-- -------------------------------------------------------
-- 7.3 authors RLS
-- -------------------------------------------------------
ALTER TABLE public.authors ENABLE ROW LEVEL SECURITY;

-- الكل يقرأ المؤلفين — Everyone can read authors
CREATE POLICY "authors_select_all" ON public.authors
  FOR SELECT USING (TRUE);

-- الأدمن فقط يعدّل — Admin only for mutations
CREATE POLICY "authors_insert_admin" ON public.authors
  FOR INSERT WITH CHECK (public.is_admin());

CREATE POLICY "authors_update_admin" ON public.authors
  FOR UPDATE USING (public.is_admin());

CREATE POLICY "authors_delete_admin" ON public.authors
  FOR DELETE USING (public.is_admin());


-- -------------------------------------------------------
-- 7.4 books RLS
-- -------------------------------------------------------
ALTER TABLE public.books ENABLE ROW LEVEL SECURITY;

-- الكل يقرأ الكتب الغير محذوفة — Everyone reads non-deleted books
CREATE POLICY "books_select_active" ON public.books
  FOR SELECT USING (NOT is_deleted);

-- الأدمن يقرأ كل الكتب (حتى المحذوفة) — Admin reads all books
CREATE POLICY "books_select_admin" ON public.books
  FOR SELECT USING (public.is_admin());

-- الأدمن فقط يعدّل — Admin only for mutations
CREATE POLICY "books_insert_admin" ON public.books
  FOR INSERT WITH CHECK (public.is_admin());

CREATE POLICY "books_update_admin" ON public.books
  FOR UPDATE USING (public.is_admin());

-- لا حذف فعلي — No hard delete (use soft delete / is_deleted)
CREATE POLICY "books_delete_admin" ON public.books
  FOR DELETE USING (FALSE);


-- -------------------------------------------------------
-- 7.5 cart_items RLS
-- -------------------------------------------------------
ALTER TABLE public.cart_items ENABLE ROW LEVEL SECURITY;

-- المستخدم يتعامل مع سلته فقط — User manages own cart only
CREATE POLICY "cart_select_own" ON public.cart_items
  FOR SELECT USING (user_id = auth.uid());

CREATE POLICY "cart_insert_own" ON public.cart_items
  FOR INSERT WITH CHECK (user_id = auth.uid() AND public.is_active_user());

CREATE POLICY "cart_update_own" ON public.cart_items
  FOR UPDATE USING (user_id = auth.uid());

CREATE POLICY "cart_delete_own" ON public.cart_items
  FOR DELETE USING (user_id = auth.uid());


-- -------------------------------------------------------
-- 7.6 wishlist_items RLS
-- -------------------------------------------------------
ALTER TABLE public.wishlist_items ENABLE ROW LEVEL SECURITY;

-- المستخدم يتعامل مع قائمته فقط — User manages own wishlist only
CREATE POLICY "wishlist_select_own" ON public.wishlist_items
  FOR SELECT USING (user_id = auth.uid());

CREATE POLICY "wishlist_insert_own" ON public.wishlist_items
  FOR INSERT WITH CHECK (user_id = auth.uid() AND public.is_active_user());

CREATE POLICY "wishlist_delete_own" ON public.wishlist_items
  FOR DELETE USING (user_id = auth.uid());


-- -------------------------------------------------------
-- 7.7 addresses RLS
-- -------------------------------------------------------
ALTER TABLE public.addresses ENABLE ROW LEVEL SECURITY;

-- المستخدم يتعامل مع عناوينه فقط — User manages own addresses only
CREATE POLICY "addresses_select_own" ON public.addresses
  FOR SELECT USING (user_id = auth.uid());

CREATE POLICY "addresses_insert_own" ON public.addresses
  FOR INSERT WITH CHECK (user_id = auth.uid() AND public.is_active_user());

CREATE POLICY "addresses_update_own" ON public.addresses
  FOR UPDATE USING (user_id = auth.uid());

CREATE POLICY "addresses_delete_own" ON public.addresses
  FOR DELETE USING (user_id = auth.uid());


-- -------------------------------------------------------
-- 7.8 orders RLS
-- -------------------------------------------------------
ALTER TABLE public.orders ENABLE ROW LEVEL SECURITY;

-- المستخدم يشوف طلباته فقط — User sees own orders
CREATE POLICY "orders_select_own" ON public.orders
  FOR SELECT USING (user_id = auth.uid());

-- الأدمن يشوف كل الطلبات — Admin sees all orders
CREATE POLICY "orders_select_admin" ON public.orders
  FOR SELECT USING (public.is_admin());

-- الطلب يُنشأ عبر place_order() فقط — Orders created via function only
CREATE POLICY "orders_insert_none" ON public.orders
  FOR INSERT WITH CHECK (FALSE);  -- يتم عبر SECURITY DEFINER function

-- الأدمن يعدّل الطلب — Admin updates orders (status changes)
CREATE POLICY "orders_update_admin" ON public.orders
  FOR UPDATE USING (public.is_admin());

-- لا حذف — No delete
CREATE POLICY "orders_delete_none" ON public.orders
  FOR DELETE USING (FALSE);


-- -------------------------------------------------------
-- 7.9 order_items RLS
-- -------------------------------------------------------
ALTER TABLE public.order_items ENABLE ROW LEVEL SECURITY;

-- المستخدم يشوف عناصر طلباته — User sees own order items
CREATE POLICY "order_items_select_own" ON public.order_items
  FOR SELECT USING (
    EXISTS (SELECT 1 FROM public.orders WHERE orders.id = order_items.order_id AND orders.user_id = auth.uid())
  );

-- الأدمن يشوف كل عناصر الطلبات — Admin sees all order items
CREATE POLICY "order_items_select_admin" ON public.order_items
  FOR SELECT USING (public.is_admin());

-- لا إدخال مباشر — No direct insert (via place_order function)
CREATE POLICY "order_items_insert_none" ON public.order_items
  FOR INSERT WITH CHECK (FALSE);

-- لا تعديل أو حذف — No update or delete
CREATE POLICY "order_items_update_none" ON public.order_items
  FOR UPDATE USING (FALSE);

CREATE POLICY "order_items_delete_none" ON public.order_items
  FOR DELETE USING (FALSE);


-- -------------------------------------------------------
-- 7.10 order_status_history RLS
-- -------------------------------------------------------
ALTER TABLE public.order_status_history ENABLE ROW LEVEL SECURITY;

-- المستخدم يشوف سجل طلباته — User sees own order history
CREATE POLICY "status_history_select_own" ON public.order_status_history
  FOR SELECT USING (
    EXISTS (SELECT 1 FROM public.orders WHERE orders.id = order_status_history.order_id AND orders.user_id = auth.uid())
  );

-- الأدمن يشوف كل السجلات — Admin sees all history
CREATE POLICY "status_history_select_admin" ON public.order_status_history
  FOR SELECT USING (public.is_admin());

-- لا إدخال أو تعديل أو حذف مباشر — No direct DML (via functions only)
CREATE POLICY "status_history_insert_none" ON public.order_status_history
  FOR INSERT WITH CHECK (FALSE);

CREATE POLICY "status_history_update_none" ON public.order_status_history
  FOR UPDATE USING (FALSE);

CREATE POLICY "status_history_delete_none" ON public.order_status_history
  FOR DELETE USING (FALSE);


-- -------------------------------------------------------
-- 7.11 stock_reservations RLS
-- -------------------------------------------------------
ALTER TABLE public.stock_reservations ENABLE ROW LEVEL SECURITY;

-- المستخدم يشوف حجوزاته فقط — User sees own reservations
CREATE POLICY "reservations_select_own" ON public.stock_reservations
  FOR SELECT USING (user_id = auth.uid());

-- الأدمن يشوف كل الحجوزات — Admin sees all reservations
CREATE POLICY "reservations_select_admin" ON public.stock_reservations
  FOR SELECT USING (public.is_admin());

-- لا إدخال أو تعديل مباشر — No direct DML (via reserve_stock function)
CREATE POLICY "reservations_insert_none" ON public.stock_reservations
  FOR INSERT WITH CHECK (FALSE);

CREATE POLICY "reservations_update_none" ON public.stock_reservations
  FOR UPDATE USING (FALSE);

CREATE POLICY "reservations_delete_none" ON public.stock_reservations
  FOR DELETE USING (FALSE);


-- -------------------------------------------------------
-- 7.12 notify_me_subscriptions RLS
-- -------------------------------------------------------
ALTER TABLE public.notify_me_subscriptions ENABLE ROW LEVEL SECURITY;

-- المستخدم يشوف اشتراكاته فقط — User sees own subscriptions
CREATE POLICY "notify_select_own" ON public.notify_me_subscriptions
  FOR SELECT USING (user_id = auth.uid());

-- المستخدم يشترك — User can subscribe
CREATE POLICY "notify_insert_own" ON public.notify_me_subscriptions
  FOR INSERT WITH CHECK (user_id = auth.uid() AND public.is_active_user());

-- المستخدم يلغي اشتراكه (حذف) — User can unsubscribe
CREATE POLICY "notify_delete_own" ON public.notify_me_subscriptions
  FOR DELETE USING (user_id = auth.uid());

-- الأدمن يشوف كل الاشتراكات — Admin sees all subscriptions
CREATE POLICY "notify_select_admin" ON public.notify_me_subscriptions
  FOR SELECT USING (public.is_admin());


-- ============================================================================
-- 8. STORAGE — تخزين الملفات (أغلفة الكتب)
-- ============================================================================

-- إنشاء Bucket لأغلفة الكتب — Create storage bucket for book covers
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'book-covers',
  'book-covers',
  TRUE,  -- متاح للعامة للقراءة
  2097152,  -- 2MB حد أقصى (NFR-12)
  ARRAY['image/jpeg', 'image/png', 'image/webp']
)
ON CONFLICT (id) DO NOTHING;

-- سياسة القراءة: الكل يقرأ — Public read
CREATE POLICY "book_covers_read" ON storage.objects
  FOR SELECT
  USING (bucket_id = 'book-covers');

-- سياسة الرفع: الأدمن فقط — Admin only upload
CREATE POLICY "book_covers_upload" ON storage.objects
  FOR INSERT
  WITH CHECK (
    bucket_id = 'book-covers'
    AND public.is_admin()
  );

-- سياسة التعديل: الأدمن فقط — Admin only update
CREATE POLICY "book_covers_update" ON storage.objects
  FOR UPDATE
  USING (bucket_id = 'book-covers' AND public.is_admin());

-- سياسة الحذف: الأدمن فقط — Admin only delete
CREATE POLICY "book_covers_delete" ON storage.objects
  FOR DELETE
  USING (bucket_id = 'book-covers' AND public.is_admin());


-- ============================================================================
-- 9. SEED DATA — بيانات أولية
-- ============================================================================

-- -------------------------------------------------------
-- 9.1 التصنيفات (8 تصنيفات) — Categories
-- -------------------------------------------------------
INSERT INTO public.categories (id, name_ar, name_en, slug, description_ar, description_en, display_order) VALUES
  (gen_random_uuid(), 'روايات وأدب',       'Fiction & Literature',   'fiction',         'روايات عربية وعالمية مترجمة',              'Arabic and translated world fiction',      1),
  (gen_random_uuid(), 'تنمية بشرية',       'Self Development',       'self-development','كتب تطوير الذات والمهارات',                'Self-help and personal development',       2),
  (gen_random_uuid(), 'تاريخ وسياسة',      'History & Politics',     'history',         'تاريخ وعلوم سياسية',                       'History and political science',            3),
  (gen_random_uuid(), 'علوم وتكنولوجيا',   'Science & Technology',   'science-tech',    'علوم طبيعية وتكنولوجيا وبرمجة',            'Natural sciences, technology and coding',  4),
  (gen_random_uuid(), 'فلسفة وفكر',        'Philosophy & Thought',   'philosophy',      'فلسفة وفكر ونقد',                          'Philosophy, thought and criticism',        5),
  (gen_random_uuid(), 'دين وإسلاميات',     'Religion & Islamic',     'religion',        'علوم إسلامية وفقه وسيرة',                  'Islamic studies, jurisprudence and seerah', 6),
  (gen_random_uuid(), 'أطفال وناشئة',      'Children & Young Adult', 'children',        'قصص وكتب تعليمية للأطفال والمراهقين',      'Stories and educational books for kids',   7),
  (gen_random_uuid(), 'أعمال واقتصاد',     'Business & Economics',   'business',        'ريادة أعمال واقتصاد وتسويق',               'Entrepreneurship, economics and marketing', 8)
ON CONFLICT DO NOTHING;


-- -------------------------------------------------------
-- 9.2 المؤلفون (20 مؤلف) — Authors
-- -------------------------------------------------------
INSERT INTO public.authors (id, name_ar, name_en, slug) VALUES
  (gen_random_uuid(), 'نجيب محفوظ',          'Naguib Mahfouz',          'naguib-mahfouz'),
  (gen_random_uuid(), 'أحمد خالد توفيق',      'Ahmed Khaled Tawfik',     'ahmed-khaled-tawfik'),
  (gen_random_uuid(), 'يوسف إدريس',          'Yusuf Idris',             'yusuf-idris'),
  (gen_random_uuid(), 'طه حسين',             'Taha Hussein',            'taha-hussein'),
  (gen_random_uuid(), 'عباس العقاد',          'Abbas El-Akkad',          'abbas-el-akkad'),
  (gen_random_uuid(), 'محمد المنسي قنديل',    'Mohamed El-Mansi Qandil', 'mansi-qandil'),
  (gen_random_uuid(), 'أحلام مستغانمي',       'Ahlam Mosteghanemi',      'ahlam-mosteghanemi'),
  (gen_random_uuid(), 'إبراهيم عبد المجيد',   'Ibrahim Abdel Meguid',    'ibrahim-abdel-meguid'),
  (gen_random_uuid(), 'علاء الأسواني',        'Alaa Al Aswany',          'alaa-al-aswany'),
  (gen_random_uuid(), 'بهاء طاهر',           'Bahaa Taher',             'bahaa-taher'),
  (gen_random_uuid(), 'صنع الله إبراهيم',     'Sonallah Ibrahim',        'sonallah-ibrahim'),
  (gen_random_uuid(), 'أحمد مراد',           'Ahmed Mourad',            'ahmed-mourad'),
  (gen_random_uuid(), 'هشام الخشن',          'Hesham El-Kheshen',       'hesham-el-kheshen'),
  (gen_random_uuid(), 'عمرو عبد الحميد',     'Amr Abdel Hamid',         'amr-abdel-hamid'),
  (gen_random_uuid(), 'محمد صادق',           'Mohamed Sadek',           'mohamed-sadek'),
  (gen_random_uuid(), 'مصطفى محمود',         'Mustafa Mahmoud',         'mustafa-mahmoud'),
  (gen_random_uuid(), 'أنيس منصور',          'Anis Mansour',            'anis-mansour'),
  (gen_random_uuid(), 'روبرت كيوساكي',       'Robert Kiyosaki',         'robert-kiyosaki'),
  (gen_random_uuid(), 'ستيفن هوكينج',        'Stephen Hawking',         'stephen-hawking'),
  (gen_random_uuid(), 'يوفال نوح هراري',      'Yuval Noah Harari',       'yuval-noah-harari')
ON CONFLICT DO NOTHING;


-- -------------------------------------------------------
-- 9.3 أدمن افتراضي — Default admin user
-- ملاحظة: يجب إنشاء المستخدم أولاً في auth.users عبر Supabase Dashboard أو API
-- ثم تُضاف البيانات هنا. المثال أدناه يفترض أن الأدمن موجود.
-- -------------------------------------------------------
-- Note: The admin user must first be created in auth.users via Supabase Dashboard/API.
-- After that, insert the profile record here.
-- Example:
--
-- INSERT INTO public.application_users (id, full_name, email, phone, role)
-- VALUES (
--   '<AUTH_USER_UUID>',
--   'مدير النظام',
--   'admin@booknest.com',
--   '01000000000',
--   'Admin'
-- );
--
-- -------------------------------------------------------
-- Edge Function hint for admin setup:
-- يمكن إنشاء Edge Function لتسجيل الأدمن الأول:
--
--   // supabase/functions/setup-admin/index.ts
--   import { createClient } from '@supabase/supabase-js'
--   const supabase = createClient(url, serviceRoleKey)
--   const { data: authUser } = await supabase.auth.admin.createUser({
--     email: 'admin@booknest.com',
--     password: process.env.ADMIN_PASSWORD, // من Environment Variable
--     email_confirm: true,
--   })
--   await supabase.from('application_users').insert({
--     id: authUser.user.id,
--     full_name: 'مدير النظام',
--     email: 'admin@booknest.com',
--     phone: '01000000000',
--     role: 'Admin',
--   })
-- -------------------------------------------------------


-- ============================================================================
-- 10. UTILITY FUNCTIONS — دوال مساعدة إضافية
-- ============================================================================

-- -------------------------------------------------------
-- 10.1 دالة إنشاء ملف شخصي عند التسجيل — Auto-create profile on signup
-- تُربط بـ auth.users عبر trigger في Supabase
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER SET search_path = public
AS $$
BEGIN
  INSERT INTO public.application_users (id, full_name, email, phone, role)
  VALUES (
    NEW.id,
    COALESCE(NEW.raw_user_meta_data->>'full_name', 'مستخدم جديد'),
    NEW.email,
    NEW.raw_user_meta_data->>'phone',
    'Customer'
  );
  RETURN NEW;
END;
$$;

-- ربط بـ auth.users — Connect to auth.users
CREATE OR REPLACE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();


-- -------------------------------------------------------
-- 10.2 Dashboard stats for admin — إحصائيات لوحة التحكم
-- FR-ADM-01
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.get_admin_dashboard_stats()
RETURNS JSONB
LANGUAGE plpgsql STABLE SECURITY DEFINER
AS $$
DECLARE
  v_result JSONB;
BEGIN
  -- التحقق من صلاحية الأدمن
  IF NOT public.is_admin() THEN
    RAISE EXCEPTION 'Unauthorized: Admin access required';
  END IF;

  SELECT jsonb_build_object(
    'total_revenue', COALESCE(SUM(CASE WHEN status = 'Delivered' THEN total ELSE 0 END), 0),
    'total_orders', COUNT(*),
    'pending_orders', COUNT(*) FILTER (WHERE status = 'Pending'),
    'processing_orders', COUNT(*) FILTER (WHERE status = 'Processing'),
    'shipped_orders', COUNT(*) FILTER (WHERE status = 'Shipped'),
    'delivered_orders', COUNT(*) FILTER (WHERE status = 'Delivered'),
    'cancelled_orders', COUNT(*) FILTER (WHERE status = 'Cancelled'),
    'recent_orders_count', COUNT(*) FILTER (WHERE created_at >= now() - INTERVAL '24 hours'),
    'revenue_last_30_days', COALESCE(SUM(CASE WHEN status = 'Delivered' AND created_at >= now() - INTERVAL '30 days' THEN total ELSE 0 END), 0)
  ) INTO v_result
  FROM public.orders;

  -- كتب قربت تخلص — Low stock books
  v_result := v_result || jsonb_build_object(
    'low_stock_books', (
      SELECT COALESCE(jsonb_agg(jsonb_build_object(
        'id', b.id, 'title', b.title_ar, 'stock', b.stock
      )), '[]'::jsonb)
      FROM public.books b
      WHERE b.stock BETWEEN 1 AND 10 AND NOT b.is_deleted
      ORDER BY b.stock ASC
      LIMIT 10
    )
  );

  -- إجمالي الكتب والمستخدمين — Total books & users
  v_result := v_result || jsonb_build_object(
    'total_books', (SELECT COUNT(*) FROM public.books WHERE NOT is_deleted),
    'total_users', (SELECT COUNT(*) FROM public.application_users WHERE role = 'Customer')
  );

  RETURN v_result;
END;
$$;

COMMENT ON FUNCTION public.get_admin_dashboard_stats IS 'إحصائيات لوحة تحكم الأدمن — Admin dashboard statistics (FR-ADM-01)';


-- -------------------------------------------------------
-- 10.3 Top selling books — أعلى الكتب مبيعاً
-- FR-ADM-10
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.get_top_selling_books(p_limit INT DEFAULT 10)
RETURNS TABLE (
  book_id UUID,
  title_ar TEXT,
  title_en TEXT,
  cover_url TEXT,
  total_sold BIGINT,
  total_revenue NUMERIC
)
LANGUAGE plpgsql STABLE SECURITY DEFINER
AS $$
BEGIN
  IF NOT public.is_admin() THEN
    RAISE EXCEPTION 'Unauthorized: Admin access required';
  END IF;

  RETURN QUERY
  SELECT
    oi.book_id,
    b.title_ar,
    b.title_en,
    b.cover_url,
    SUM(oi.quantity)::BIGINT AS total_sold,
    SUM(oi.total_price) AS total_revenue
  FROM public.order_items oi
  JOIN public.orders o ON o.id = oi.order_id AND o.status = 'Delivered'
  JOIN public.books b ON b.id = oi.book_id
  GROUP BY oi.book_id, b.title_ar, b.title_en, b.cover_url
  ORDER BY total_sold DESC
  LIMIT LEAST(p_limit, 50);
END;
$$;

COMMENT ON FUNCTION public.get_top_selling_books IS 'أعلى الكتب مبيعاً — Top selling books (FR-ADM-10)';


-- -------------------------------------------------------
-- 10.4 Sales by category — مبيعات حسب التصنيف
-- FR-ADM-10
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.get_sales_by_category()
RETURNS TABLE (
  category_id UUID,
  category_name_ar TEXT,
  category_name_en TEXT,
  total_sold BIGINT,
  total_revenue NUMERIC
)
LANGUAGE plpgsql STABLE SECURITY DEFINER
AS $$
BEGIN
  IF NOT public.is_admin() THEN
    RAISE EXCEPTION 'Unauthorized: Admin access required';
  END IF;

  RETURN QUERY
  SELECT
    c.id AS category_id,
    c.name_ar AS category_name_ar,
    c.name_en AS category_name_en,
    COALESCE(SUM(oi.quantity), 0)::BIGINT AS total_sold,
    COALESCE(SUM(oi.total_price), 0) AS total_revenue
  FROM public.categories c
  LEFT JOIN public.books b ON b.category_id = c.id
  LEFT JOIN public.order_items oi ON oi.book_id = b.id
  LEFT JOIN public.orders o ON o.id = oi.order_id AND o.status = 'Delivered'
  GROUP BY c.id, c.name_ar, c.name_en
  ORDER BY total_revenue DESC;
END;
$$;

COMMENT ON FUNCTION public.get_sales_by_category IS 'مبيعات حسب التصنيف — Sales by category (FR-ADM-10)';


-- ============================================================================
-- 11. GRANTS — صلاحيات الوصول
-- ============================================================================
-- Supabase يستخدم دور anon للزوار و authenticated للمسجلين
-- الدوال SECURITY DEFINER تعمل بصلاحيات المُنشئ
-- ============================================================================

-- الزوار — Anonymous access
GRANT SELECT ON public.books TO anon;
GRANT SELECT ON public.categories TO anon;
GRANT SELECT ON public.authors TO anon;

-- المسجلون — Authenticated access
GRANT SELECT, INSERT, UPDATE, DELETE ON public.cart_items TO authenticated;
GRANT SELECT, INSERT, DELETE ON public.wishlist_items TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON public.addresses TO authenticated;
GRANT SELECT ON public.orders TO authenticated;
GRANT SELECT ON public.order_items TO authenticated;
GRANT SELECT ON public.order_status_history TO authenticated;
GRANT SELECT ON public.stock_reservations TO authenticated;
GRANT SELECT, INSERT, DELETE ON public.notify_me_subscriptions TO authenticated;
GRANT SELECT, INSERT, UPDATE ON public.application_users TO authenticated;
GRANT SELECT ON public.books TO authenticated;
GRANT SELECT ON public.categories TO authenticated;
GRANT SELECT ON public.authors TO authenticated;

-- الدوال — Function execution
GRANT EXECUTE ON FUNCTION public.place_order TO authenticated;
GRANT EXECUTE ON FUNCTION public.cancel_order TO authenticated;
GRANT EXECUTE ON FUNCTION public.reserve_stock TO authenticated;
GRANT EXECUTE ON FUNCTION public.search_books TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.update_order_status TO authenticated;
GRANT EXECUTE ON FUNCTION public.get_available_stock TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.calculate_shipping TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.arabic_normalize TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.get_admin_dashboard_stats TO authenticated;
GRANT EXECUTE ON FUNCTION public.get_top_selling_books TO authenticated;
GRANT EXECUTE ON FUNCTION public.get_sales_by_category TO authenticated;
-- release_expired_reservations يُنفذ بـ service_role فقط — Only via service_role
GRANT EXECUTE ON FUNCTION public.release_expired_reservations TO service_role;


-- ============================================================================
-- END OF MIGRATION
-- ============================================================================
-- ملخص:
-- • 12 جدول مع قيود كاملة وفهارس للأداء
-- • RLS مفعّل على كل جدول بسياسات محددة
-- • 11 دالة (بحث، طلب، إلغاء، حجز، تحرير، إحصائيات...)
-- • 7 مشغلات (timestamps, state machine, audit, protection)
-- • بيانات أولية (8 تصنيفات، 20 مؤلف)
-- • Storage bucket مع سياسات أمان
--
-- Edge Functions المقترحة (تُنشأ في supabase/functions/):
--   1. cleanup-reservations — cron كل دقيقة لتحرير الحجوزات المنتهية
--   2. setup-admin — إنشاء الأدمن الأول
--   3. notify-restock — إرسال إيميلات "نبّهني لما يتوفر" عند زيادة المخزون
--   4. daily-sales-report — تقرير مبيعات يومي
-- ============================================================================
