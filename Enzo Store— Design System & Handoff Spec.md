# Enzo Store— Design System & Handoff Spec

*مشروع E-Commerce لبيع الكتب (ASP.NET Core MVC) · نوعين يوزر: Customer و Admin · الإصدار 1.0*

**خريطة الدوك**

- **المنتج:** [1. نظرة عامة](#m5wkajw6z7c.132) · [2. اليوزرز والصلاحيات](#m5wkajw6z7c.678) · [3. Requirements](#m5wkajw6z7c.11301) · [4. الصفحات (23)](#m5wkajw6z7c.1100)
- **التصميم:** [5. Design Tokens](#m5wkajw6z7c.2337) · [6. الاتجاه البصري](#m5wkajw6z7c.18598) · [7. Components](#m5wkajw6z7c.4291) · [8. Patterns](#m5wkajw6z7c.5957) · [9. Handoff](#m5wkajw6z7c.6384) · [10. Accessibility](#m5wkajw6z7c.9716)
- **الهندسة والاختبار:** [11. الربط مع MVC](#m5wkajw6z7c.9121) · [12. التاسكات والاختبارات](#m5wkajw6z7c.22220)
- **الإبهار:** [13. Wow Layer وحزمة الإثبات](#m5wkajw6z7c.39272)
- **الأمان والإطلاق:** [14. Threat Model](#m5wkajw6z7c.48626) · [15. جاهزية الـ Production](#m5wkajw6z7c.50458)
- **التنفيذ والعرض:** [16. طريقة التنفيذ والجدول](#m5wkajw6z7c.52528) · [17. العرض في الـ CV](#m5wkajw6z7c.10035)

## 1. نظرة عامة

**BookNest** متجر كتب أونلاين. الهدف إن المشروع يبان كمنتج حقيقي مش تمرين: ديزاين سيستم موحّد، صفحات محددة بعدد، وصلاحيات واضحة، وقابل للتوسع من غير ما يتكسر.

**مبادئ الديزاين**

- **Editorial**: تصميم تحريري Editorial: الطباعة هي البطل، لوحة ألوان محدودة (Moss + Persimmon على رمادي محايد)، بدون تدرجات ولا ظلال تقيلة.
- **Consistency over creativity**: كل حاجة من Tokens، مفيش قيمة مكتوبة بإيدك في الـ CSS.
- **Bilingual by default**: عربي RTL وإنجليزي LTR من أول يوم (باستخدام CSS logical properties).
- **Two surfaces, one system**: واجهة المتجر والـ Admin بيشاركوا نفس الـ Tokens والـ Components بس Layout مختلف.

## 2. أنواع اليوزر والصلاحيات

| الدور | يعمل إيه | الـ Layout |
| --- | --- | --- |
| **Guest** (زائر) | يتصفح، يبحث، يشوف التفاصيل، يضيف للسلة | `_StoreLayout` |
| **Customer** | كل اللي فوق + شراء، طلبات، Wishlist، بروفايل | `_StoreLayout` |
| **Admin** | إدارة الكتب والتصنيفات والمؤلفين والطلبات واليوزرز | `_AdminLayout` (Area: Admin) |

التنفيذ: ASP.NET Core Identity + Roles، و`[Authorize(Roles = "Admin")]` على كل Controllers الـ Area، و`[Authorize]` على صفحات الـ Customer.

## 3. Requirements (SRS)

### 3.1 النطاق والافتراضات

- **الهدف:** متجر كتب أونلاين بيخلي العميل يلاقي كتاب ويشتريه ويتابع طلبه، والأدمن يدير المحتوى والطلبات.
- **الأولويات:** **M** = Must (لازم في الـ MVP) · **S** = Should · **C** = Could (Phase 2).
- **افتراضات:** العملة EGP، الشحن داخل مصر، الدفع كاش عند الاستلام في الـ MVP والكارت في Phase 2، كتب مادية فقط (مفيش كتب إلكترونية).

### 3.2 Functional Requirements

**Authentication & Accounts**

| ID | المتطلب | الدور | أولوية |
| --- | --- | --- | --- |
| FR-AUTH-01 | تسجيل حساب بالاسم والإيميل والباسورد والتليفون | Guest | M |
| FR-AUTH-02 | تسجيل دخول وخروج، مع Remember me | Guest/Customer/Admin | M |
| FR-AUTH-03 | استرجاع الباسورد بلينك على الإيميل صالح 60 دقيقة | Guest | S |
| FR-AUTH-04 | الدخول يوجّه كل دور لصفحته (Customer للمتجر، Admin للوحة التحكم) | الكل | M |
| FR-AUTH-05 | تفعيل الإيميل Email Confirmation | Customer | C |

**Catalog & Search**

| ID | المتطلب | الدور | أولوية |
| --- | --- | --- | --- |
| FR-CAT-01 | عرض الكتب في Grid مع Pagination (12 في الصفحة) | الكل | M |
| FR-CAT-02 | بحث بالعنوان أو المؤلف أو ISBN | الكل | M |
| FR-CAT-03 | فلترة بالتصنيف والمؤلف ونطاق السعر والمتاح فقط | الكل | M |
| FR-CAT-04 | ترتيب بالأحدث أو السعر أو التقييم | الكل | M |
| FR-CAT-05 | صفحة تفاصيل الكتاب مع كتب مشابهة | الكل | M |
| FR-CAT-06 | تقييمات ومراجعات من العملاء اللي اشتروا الكتاب | Customer | C |

**Cart & Wishlist**

| ID | المتطلب | الدور | أولوية |
| --- | --- | --- | --- |
| FR-CART-01 | إضافة كتاب للسلسلة وتعديل الكمية وحذفه | Guest/Customer | M |
| FR-CART-02 | سلة الزائر تتحفظ (Session/Cookie) وتتدمج مع سلة الحساب عند الدخول | Guest→Customer | S |
| FR-CART-03 | عرض الإجمالي الفرعي والشحن والإجمالي الكلي | الكل | M |
| FR-WISH-01 | إضافة وإزالة كتاب من Wishlist (يتطلب تسجيل دخول) | Customer | S |

**Checkout & Orders**

| ID | المتطلب | الدور | أولوية |
| --- | --- | --- | --- |
| FR-ORD-01 | إدخال أو اختيار عنوان شحن محفوظ | Customer | M |
| FR-ORD-02 | اختيار طريقة الدفع (كاش عند الاستلام) | Customer | M |
| FR-ORD-03 | مراجعة الطلب ثم تأكيده، ويتولد رقم طلب فريد | Customer | M |
| FR-ORD-04 | خصم المخزون عند تأكيد الطلب داخل Transaction | System | M |
| FR-ORD-05 | عرض كل طلباتي بحالتها وتفاصيل كل طلب | Customer | M |
| FR-ORD-06 | إلغاء الطلب طالما حالته Pending | Customer | S |
| FR-ORD-07 | إيميل تأكيد الطلب وتغيير الحالة | System | C |
| FR-ORD-08 | الدفع بالكارت عبر Paymob | Customer | C |
| FR-ORD-09 | كوبونات الخصم | Customer | C |

**Profile**

| ID | المتطلب | الدور | أولوية |
| --- | --- | --- | --- |
| FR-PRO-01 | تعديل البيانات الشخصية وتغيير الباسورد | Customer | M |
| FR-PRO-02 | إدارة العناوين (إضافة/تعديل/حذف/افتراضي) | Customer | S |

**Admin**

| ID | المتطلب | الدور | أولوية |
| --- | --- | --- | --- |
| FR-ADM-01 | Dashboard فيها: إجمالي المبيعات، عدد الطلبات، طلبات جديدة، كتب قربت تخلص، وChart مبيعات آخر 30 يوم | Admin | M |
| FR-ADM-02 | CRUD للكتب مع رفع غلاف، وSoft Delete | Admin | M |
| FR-ADM-03 | CRUD للتصنيفات والمؤلفين (مع منع حذف تصنيف/مؤلف مرتبط بكتب) | Admin | M |
| FR-ADM-04 | قائمة الطلبات بفلترة وبحث | Admin | M |
| FR-ADM-05 | تغيير حالة الطلب حسب المسار المسموح | Admin | M |
| FR-ADM-06 | قائمة اليوزرز: عرض، تفعيل/تعطيل، تغيير Role | Admin | S |
| FR-ADM-07 | تعديل المخزون مع سجل بالتغييرات Inventory Log | Admin | C |
| FR-ADM-08 | تقارير Excel/PDF للمبيعات | Admin | C |
| FR-ADM-09 | مراجعة التقييمات والموافقة أو الحذف | Admin | C |

### 3.3 Business Rules

- **BR-01:** مسار حالة الطلب: `Pending → Processing → Shipped → Delivered`، والإلغاء `Cancelled` مسموح قبل Shipped فقط، ولا رجوع لحالة سابقة.
- **BR-02:** لا يمكن طلب كمية أكبر من المخزون المتاح.
- **BR-03:** سعر الخصم لازم يكون أقل من السعر الأصلي.
- **BR-04:** سعر الكتاب وقت الطلب يتخزن في `OrderItem` (Snapshot)، فتغيير السعر بعدها ما يأثرش على الطلبات القديمة.
- **BR-05:** ISBN فريد، والإيميل فريد.
- **BR-06:** الشحن ثابت (مثلًا 50 ج) ومجاني فوق 500 ج، والقيم تتغير من إعدادات في الكود.
- **BR-07:** الكتب المحذوفة (Soft Delete) تختفي من المتجر وتفضل ظاهرة في الطلبات القديمة.
- **BR-08:** لو الكتاب خلص أثناء الـ Checkout، يتوقف الطلب مع رسالة واضحة وتتحدث الكمية.
- **BR-09:** الأدمن ما يقدرش يعطّل أو يشيل Role نفسه (حماية من القفل).

### 3.4 Non-Functional Requirements

| ID | الفئة | المتطلب |
| --- | --- | --- |
| NFR-01 | Performance | تحميل الصفحة الرئيسية وCatalog أقل من 2 ثانية على اتصال عادي، وPagination والفلترة على مستوى الـ DB |
| NFR-02 | Security | Identity بباسورد Hashed، `AntiForgeryToken` على كل POST، حماية من XSS وSQL Injection (EF Core Parameterized)، وHTTPS إجباري |
| NFR-03 | Authorization | منع وصول Customer لأي Route تحت `/admin` (403)، واختبار ده بتست |
| NFR-04 | Validation | Validation على الطرفين (Client + Server)، والسيرفر هو المرجع |
| NFR-05 | Usability | Responsive على Mobile وTablet وDesktop، وRTL/LTR كاملين |
| NFR-06 | Accessibility | WCAG 2.1 AA حسب قسم 10 |
| NFR-07 | Reliability | Global Exception Handling مع صفحات 404/403/500، وTransaction لعملية الطلب |
| NFR-08 | Maintainability | Layered/Clean Architecture، وViewModels، وSeparation of Concerns، وCode style موحد |
| NFR-09 | Testability | Unit Tests لخدمات الطلبات والسلة والمخزون (تغطية 60%+ للـ Services) |
| NFR-10 | Logging | Serilog لتسجيل الأخطاء وعمليات الأدمن الحساسة |
| NFR-11 | Scalability | Caching للصفحة الرئيسية والتصنيفات (`IMemoryCache`)، وجاهزية لتحويل لـ API لاحقًا |
| NFR-12 | Uploads | الغلاف JPG/PNG/WebP بحد أقصى 2MB مع فحص الـ Content-Type |

### 3.5 User Stories وAcceptance Criteria (أهم 6)

**US-01 — كعميل أبحث عن كتاب**

- **Given** أنا في `/books` **When** كتبت جزء من اسم كتاب أو مؤلف **Then** تظهر النتائج المطابقة فقط مع عددها.
- **Given** مفيش نتائج **Then** تظهر رسالة "مفيش نتائج" وزرار مسح الفلاتر.

**US-02 — كعميل أضيف كتاب للسلة**

- **Given** الكتاب متاح **When** ضغطت أضف **Then** العداد يزيد وتظهر رسالة نجاح.
- **Given** الكمية المطلوبة أكبر من المخزون **Then** الإضافة تترفض برسالة توضح المتاح.

**US-03 — كعميل أتم الشراء**

- **Given** سلتي فيها كتب وأنا مسجل دخول **When** أكدت الطلب بعنوان صحيح **Then** يتسجل الطلب بحالة Pending ويتخصم المخزون وأروح لصفحة النجاح برقم الطلب.
- **Given** مش مسجل دخول **Then** أتحول للـ Login ثم أرجع للـ Checkout بعد الدخول.

**US-04 — كأدمن أضيف كتاب**

- **Given** أنا Admin **When** ملأت الحقول المطلوبة ورفعت غلاف صحيح **Then** الكتاب يظهر في المتجر فورًا.
- **Given** ISBN مكرر **Then** يظهر خطأ تحت الحقل ولا يتم الحفظ.

**US-05 — كأدمن أغيّر حالة طلب**

- **Given** طلب Pending **When** غيّرته لـ Processing **Then** الحالة تتحدث ويتسجل التاريخ ومين غيّرها.
- **Given** طلب Delivered **Then** أي تغيير حالة مرفوض.

**US-06 — كزائر أحاول أدخل صفحة أدمن**

- **Given** أنا Customer أو Guest **When** فتحت `/admin` **Then** أتحول للـ Login (Guest) أو أشوف 403 (Customer).

### 3.6 خارج النطاق (Out of Scope للـ MVP)

كتب إلكترونية · شحن دولي · تعدد العملات · تعدد البائعين Marketplace · تطبيق موبايل · توصيات بالـ AI · نظام نقاط ولاء.

### 3.7 Traceability (الصفحات ↔ المتطلبات)

| الصفحة | أهم المتطلبات |
| --- | --- |
| Catalog (2) | FR-CAT-01 → 04 |
| Book Details (3) | FR-CAT-05، FR-CART-01، FR-WISH-01 |
| Cart (7) | FR-CART-01 → 03 |
| Checkout (8) + Success (9) | FR-ORD-01 → 04، BR-02، BR-08 |
| My Orders (10, 11) | FR-ORD-05، FR-ORD-06 |
| Books List/Form (15, 16) | FR-ADM-02، BR-03، BR-05، NFR-12 |
| Orders Admin (19, 20) | FR-ADM-04، FR-ADM-05، BR-01 |
| Users (21) | FR-ADM-06، BR-09 |

### 3.8 أسئلة مفتوحة

- الشحن ثابت ولا حسب المحافظة؟
- هل الـ MVP فيه تقييمات ولا Phase 2؟
- المشروع بلغتين فعليًا (محتوى الكتب عربي وإنجليزي) ولا الواجهة بس؟

### 3.9 مراجعة التوافق: Requirements ↔ Pages

راجعت الـ 23 صفحة على المتطلبات، وكل متطلب Must له صفحة تغطيه، ومفيش صفحة من غير متطلب، **إلا الـ Home**. ضفنا لها **FR-HOME-01 (M):** الصفحة الرئيسية تعرض الأحدث والأكثر مبيعًا والتصنيفات وكتب عليها خصم. من هنا القاعدة: أي صفحة أو Component جديد يترجع لمتطلب قبل ما يتصمم.

## 4. عدد الصفحات: 23 صفحة (MVP)

**Public / Auth (6)**

| # | الصفحة | Route |
| --- | --- | --- |
| 1 | Home | `/` |
| 2 | Catalog (بحث + فلاتر + ترتيب + Pagination) | `/books` |
| 3 | Book Details | `/books/{id}/{slug}` |
| 4 | Login | `/account/login` |
| 5 | Register | `/account/register` |
| 6 | Forgot / Reset Password | `/account/forgot-password` |

**Customer (7)**

| # | الصفحة | Route |
| --- | --- | --- |
| 7 | Cart | `/cart` |
| 8 | Checkout (عنوان + دفع + مراجعة) | `/checkout` |
| 9 | Order Success | `/orders/success/{id}` |
| 10 | My Orders | `/orders` |
| 11 | Order Details + Tracking | `/orders/{id}` |
| 12 | Profile (بيانات + عناوين + باسورد) | `/profile` |
| 13 | Wishlist | `/wishlist` |

**Admin (8)**

| # | الصفحة | Route |
| --- | --- | --- |
| 14 | Dashboard (إحصائيات + Chart) | `/admin` |
| 15 | Books List | `/admin/books` |
| 16 | Book Create / Edit (نفس الـ View) | `/admin/books/edit/{id?}` |
| 17 | Categories (CRUD بـ Modal) | `/admin/categories` |
| 18 | Authors (CRUD بـ Modal) | `/admin/authors` |
| 19 | Orders List | `/admin/orders` |
| 20 | Order Details + تغيير الحالة | `/admin/orders/{id}` |
| 21 | Users List (تفعيل / تعطيل / Role) | `/admin/users` |

**System (2)**

| # | الصفحة |
| --- | --- |
| 22 | 404 Not Found |
| 23 | 403 Access Denied / 500 Error |

**Phase 2 (لو عاوزه يكبر):** Reviews & Ratings + Moderation، Coupons، Inventory Logs، Reports (Excel/PDF)، Publishers، Settings، Payment Gateway (Paymob)، Email Notifications.

## 5. Design Tokens

### 5.1 الألوان

| Token | Value | الاستخدام |
| --- | --- | --- |
| `color-primary-700` | `#24443A` | Navbar، Sidebar الأدمن، العناوين |
| `color-primary-600` | `#2F5849` | Primary Button |
| `color-primary-100` | `#E1EAE4` | خلفية خفيفة، Selected |
| `color-accent-500` | `#C9461F` | CTA «أضف للسلة»، Badges، النجوم |
| `color-accent-600` | `#A93A17` | Hover للـ Accent |
| `color-bg-paper` | `#F3F3EF` | خلفية المتجر |
| `color-bg-admin` | `#ECEDE9` | خلفية الأدمن |
| `color-surface` | `#FFFFFF` | Cards، Tables |
| `color-text-primary` | `#16181A` | النص الأساسي |
| `color-text-secondary` | `#5A5F5B` | نص ثانوي |
| `color-border` | `#D9DAD3` | حدود |
| `color-success` | `#3B7A57` | Delivered، نجاح |
| `color-warning` | `#B7791F` | Pending، مخزون قليل |
| `color-danger` | `#B3261E` | Cancelled، أخطاء، حذف |
| `color-info` | `#2F6F9F` | Processing، معلومات |

> **Accessibility:** نص `#16181A` على `#F3F3EF` يعدّي WCAG AA (≈15:1). أي نص على Accent يكون أبيض بحجم 16px Bold فما فوق أو يُستبدل بـ `primary-700`.

### 5.2 الخطوط

| Token | القيمة | الاستخدام |
| --- | --- | --- |
| `font-family-ar` | IBM Plex Sans Arabic | واجهة عربي |
| `font-family-en` | IBM Plex Sans | واجهة إنجليزي |
| `font-family-display` | Noto Naskh Arabic / Source Serif 4 | عناوين الهيرو وأسماء الكتب (اختياري) |
| `font-size-xs / sm / md / lg` | 12 / 14 / 16 / 18 | نص |
| `font-size-h3 / h2 / h1` | 22 / 28 / 36 | عناوين |
| `font-weight` | 400 / 600 / 700 | Regular / Semibold / Bold |
| `line-height` | 1.7 عربي · 1.5 إنجليزي | العربي محتاج مساحة أكبر |

### 5.3 Spacing / Radius / Shadow / Motion / Breakpoints

| الفئة | Tokens |
| --- | --- |
| Spacing (4px base) | `space-1`=4 · `2`=8 · `3`=12 · `4`=16 · `6`=24 · `8`=32 · `12`=48 · `16`=64 |
| Radius | `radius-sm`=2 · `md`=4 · `lg`=8 · `pill`=999 |
| Shadow | `shadow-sm` (Cards) · `shadow-md` (Hover/Dropdown) · `shadow-lg` (Modal) |
| Motion | `duration-fast`=120ms · `base`=200ms · `slow`=320ms · easing `cubic-bezier(.2,.8,.2,1)` |
| Breakpoints | Mobile `<768` · Tablet `768–1024` · Desktop `>1024` · Wide `>1440` |
| Grid | Container أقصى عرض 1200px · 12 عمود · gutter = `space-6` |

**التنفيذ:** كل الـ Tokens في ملف واحد `wwwroot/css/tokens.css` كـ CSS Variables على `:root`، وتقدر تبني فوقها Bootstrap 5 (Override للـ variables) أو Tailwind.

## 6. Design Direction v2: UI احترافي ما يبانش عليه AI

غيّرت الـ Palette لأن النسخة الأولى (كحلي + ذهبي + كريمي) هي بالظبط اللي بتتكرر في التصاميم الـ AI. الجديدة مبنية على **Moss** أخضر غامق (الـ Primary) و**Persimmon** برتقالي محروق (الـ Accent) على خلفية **رمادي محايد**، وده مش من الـ Defaults الشائعة (أزرق/بنفسجي/Teal) ومعمول للكتب: لون قماش تجليد وشريط علامة.

**نسب الاستخدام:** 70% محايد (خلفية وأسطح ونص) · 20% Moss (Navbar، Sidebar، أزرار أساسية) · 10% Persimmon (CTA واحد بارز في كل شاشة، الخصم، التقييم). لو الشاشة فيها أكتر من CTA Persimmon يبقى في خطأ.

**Contrast تم حسابه:** أبيض على `color-primary-700` ≈ 10.7:1 · أبيض على `color-accent-500` ≈ 4.8:1 (AA) · نص أساسي على الخلفية ≈ 16:1.

### 6.1 علامات الـ AI اللي لازم نتجنبها

| بنتجنب | البديل |
| --- | --- |
| تدرجات بنفسجي/أزرق، Glassmorphism، Glow | ألوان مسطحة Flat، وفصل بـ Border بسمك 1px |
| ظل ناعم تحت كل Card ومساحات دائرية كبيرة | `radius` من 2 لـ 8 بس، والـ Cards بـ Border بدون ظل، والظل للـ Dropdown والـ Modal فقط |
| إيموجي أو أيقونات متنوعة الأسلوب | مجموعة أيقونات واحدة (Lucide أو Phosphor) بـ stroke 1.5px |
| Lorem ipsum وصور Stock وغلافات وهمية | بيانات حقيقية: 60+ كتاب بأغلفة وأسعار منطقية (Seed من Open Library أو Google Books) |
| نصوص تسويقية عامة ("Discover the magic of reading") | نص مصري مباشر: "ابدأ من الأكثر مبيعًا" · "باقي 3 نسخ" |
| Hero ضخم بشعار وزرارين | Hero صغير بمحتوى: كتاب الأسبوع + بحث + تصنيفات |
| Animations كتير (Fade/Bounce على كل حاجة) | انتقالات 120–200ms فقط على Hover وFocus والفتح والإغلاق |
| كل الصفحات بنفس شكل Cards متراصة | تنويع حسب المحتوى: قوائم، جداول، تقسيمات بحدود، وعناوين قوية |

### 6.2 قواعد الـ UI

- **الطباعة هي الهوية:** عناوين الكتب والأقسام بخط Serif (`Source Serif 4` / `Noto Naskh Arabic`)، وكل الواجهة بـ `IBM Plex Sans` / `IBM Plex Sans Arabic`. سلم واضح: 36 / 28 / 22 / 16 / 14، وعرض السطر الأقصى 65 حرف.
- **Grid صارم:** 12 عمود، وأساس 8px، وكل العناصر محاذية على نفس الخطوط. مسافات كبيرة بين الأقسام (`space-12`) وصغيرة داخل المجموعات.
- **Hierarchy:** في كل شاشة عنصر واحد بارز (CTA Persimmon)، وباقي الأزرار Secondary أو Ghost.
- **المتجر مقابل الأدمن:** المتجر مريح وفيه مساحات (Spacious)، والأدمن مضغوط (Dense): صفوف جدول 44px، وSidebar داكن `ink-900` والعنصر النشط بخط Moss.
- **الأغلفة:** نسبة 2:3 ثابتة، ظل خفيف جدًا أسفل الغلاف فقط، وPlaceholder مصمم لما الغلاف يكون مفقود (لون + عنوان الكتاب بخط Serif).
- **الحالات:** كل شاشة ليها Empty/Loading/Error مصممة فعليًا، مش بس الحالة الجميلة.
- **RTL حقيقي:** الأيقونات اللي ليها اتجاه (أسهم، Pagination) تتعكس، والأرقام الإنجليزية في الأسعار مع "ج.م" بعدها.

### 6.3 Tokens جاهزة للتنفيذ

```css
:root {
  --color-primary-700: #24443A;
  --color-primary-600: #2F5849;
  --color-primary-100: #E1EAE4;
  --color-accent-500:  #C9461F;
  --color-accent-600:  #A93A17;
  --color-bg:          #F3F3EF;
  --color-bg-admin:    #ECEDE9;
  --color-surface:     #FFFFFF;
  --color-text:        #16181A;
  --color-text-muted:  #5A5F5B;
  --color-border:      #D9DAD3;
  --color-success:     #3B7A57;
  --color-warning:     #B7791F;
  --color-danger:      #B3261E;
  --color-info:        #2F6F9F;

  --radius-sm: 2px; --radius-md: 4px; --radius-lg: 8px;
  --space-1: 4px; --space-2: 8px; --space-3: 12px; --space-4: 16px;
  --space-6: 24px; --space-8: 32px; --space-12: 48px; --space-16: 64px;

  --font-ui: "IBM Plex Sans", "IBM Plex Sans Arabic", system-ui, sans-serif;
  --font-display: "Source Serif 4", "Noto Naskh Arabic", Georgia, serif;
  --dur-fast: 120ms; --dur-base: 200ms;
  --ease: cubic-bezier(.2,.8,.2,1);
}
```

### 6.4 الخطوة اللي بعد كده

قبل الكود: نرسم في Figma أو HTML الشاشات الأهم بالترتيب: Home → Catalog → Book Details → Cart/Checkout → Admin Dashboard → Admin Books. كل شاشة تتراجع على متطلباتها في 3.2 والـ Acceptance Criteria في 3.5.

## 7. Components

| Component | Variants | States | يتنفذ كـ |
| --- | --- | --- | --- |
| **Button** | primary · accent · secondary · ghost · danger · sizes sm/md/lg | default · hover · focus · active · disabled · loading | Tag Helper / CSS class |
| **Input / Textarea / Select** | text · password · search · number | default · focus · error · disabled · readonly | `_FormField` Partial |
| **Book Card** | vertical (Grid) · horizontal (Cart/Orders) | default · hover · out-of-stock · discounted | `BookCard` View Component |
| **Price** | regular · discounted (قبل/بعد) | — | Partial |
| **Rating Stars** | readonly · input | hover (input) | Partial |
| **Badge / Status Chip** | Pending · Processing · Shipped · Delivered · Cancelled · Low Stock | — | Tag Helper |
| **Navbar** | guest · customer · admin-topbar | sticky · scrolled · mobile-collapsed | `_Navbar` Partial |
| **Sidebar (Admin)** | expanded · collapsed | active-item · hover | `_AdminSidebar` |
| **Quantity Stepper** | default | min=1 · max=stock · loading | Partial + JS |
| **Data Table** | sortable · selectable | loading · empty · row-hover · error | `_DataTable` Partial |
| **Pagination** | numbered · compact (mobile) | active · disabled | `PagedList` View Component |
| **Filters Panel** | sidebar (desktop) · drawer (mobile) | applied · reset | Partial |
| **Modal / Confirm Dialog** | form · confirm-delete | open · submitting | Bootstrap Modal |
| **Toast / Alert** | success · error · warning · info | auto-dismiss 4s | `TempData` + JS |
| **Stepper (Checkout)** | 3 خطوات | current · done · upcoming | Partial |
| **Stat Card (Admin)** | with trend ↑↓ | loading skeleton | Partial |
| **Empty State** | cart · orders · wishlist · search | — | Partial |
| **Skeleton** | card · table-row · text | pulse animation | CSS |
| **Image Upload** | cover preview | uploading · error · success | Partial + JS |

## 8. Patterns

- **Forms:** Label فوق الحقل، رسالة الخطأ تحته بلون `danger` مع أيقونة، Validation على السيرفر (DataAnnotations / FluentValidation) + Client-side (jQuery Validation Unobtrusive).
- **Feedback:** نجاح/فشل العمليات → Toast. تأكيد الحذف → Confirm Modal دايمًا (مفيش حذف مباشر).
- **Data display:** كل قوائم الأدمن = Search + Filter + Sort + Pagination (10/25/50).
- **Loading:** Skeleton للصفحات والجداول، Spinner داخل الزرار للـ Actions.

## 9. Handoff Specs (الصفحات الأهم)

### 9.1 Catalog `/books`

- **Layout:** Desktop: Filters Sidebar (280px) + Grid 4 أعمدة. Tablet: 3 أعمدة والفلاتر Drawer. Mobile: عمودين والفلاتر Bottom Sheet.
- **Filters:** التصنيف، المؤلف، نطاق السعر، التقييم، متاح فقط. السبب: المستخدم بيحدد قراره بالسعر والتصنيف أكتر حاجة.
- **Sort:** الأحدث · الأعلى تقييمًا · السعر ↑ · السعر ↓.
- **Book Card:** غلاف نسبة 2:3، اسم الكتاب سطرين بحد أقصى ثم `…`، المؤلف سطر واحد، السعر `font-size-lg` Bold.
- **Hover (Desktop):** رفع 4px + `shadow-md` خلال `duration-base`، ويظهر زرار «أضف للسلة».
- **Empty:** «مفيش نتايج» + زرار «مسح الفلاتر».
- **Loading:** 8 Skeleton Cards.
- **Edge cases:** اسم كتاب 100+ حرف (Truncate)، غلاف مفقود (Placeholder)، 0 نتايج، كتاب Out-of-stock (Badge + زرار Disabled).
- **A11y:** Filters داخل `<form role="search">`، الكروت `<article>` بعنوان `<h3>`، الصور `alt` = اسم الكتاب.

### 9.2 Book Details `/books/{id}/{slug}`

- **Layout:** Desktop: الغلاف (40%) + المعلومات (60%). Mobile: عمود واحد، وزرار «أضف للسلة» Sticky أسفل الشاشة (استخدام بإيد واحدة).
- **المحتوى:** العنوان، المؤلف (رابط)، التقييم، السعر، حالة المخزون، Quantity Stepper، زرار Wishlist، الوصف (Read more بعد 6 سطور)، تفاصيل (ISBN، ناشر، صفحات، لغة)، كتب مشابهة.
- **States لزرار الإضافة:** default → loading (Spinner) → success (Toast + تحديث عداد السلة) / error.
- **المخزون:** `>10` «متاح» أخضر · `1–10` «باقي X فقط» برتقالي · `0` «غير متاح» وDisabled.
- **Edge cases:** وصف فاضي، خصم (سعر قديم مشطوب)، Guest بيضغط Wishlist → Redirect للـ Login مع `returnUrl`.

### 9.3 Checkout `/checkout`

- **3 خطوات:** العنوان ← الدفع (كاش عند الاستلام / كارت) ← المراجعة. ملخص الطلب Sticky على اليمين/الشمال حسب الاتجاه.
- **Validation:** تليفون مصري `^01[0125][0-9]{8}$`، المحافظة من قائمة، العنوان 10–200 حرف.
- **Submit:** الزرار يتقفل (Loading) لمنع Double-submit + `AntiForgeryToken`.
- **Edge cases:** كتاب خلص من المخزون أثناء الـ Checkout → رسالة واضحة وتحديث الكمية، السلة فاضية → Redirect `/cart`.
- **Motion:** انتقال بين الخطوات `duration-base`.

### 9.4 Admin — Orders List `/admin/orders`

- **Layout:** Topbar + Sidebar (240px، يتطوي لـ 72px). Tablet/Mobile: Sidebar يتحول Drawer.
- **Table:** رقم الطلب · العميل · التاريخ · الإجمالي · الحالة (Chip) · Actions. فلتر بالحالة والتاريخ + بحث برقم الطلب أو اسم العميل.
- **Mobile:** الجدول يتحول لـ Cards أو Scroll أفقي داخل الـ Container.
- **تغيير الحالة:** Dropdown في صفحة التفاصيل مع Confirm. الحالات المسموحة: `Pending → Processing → Shipped → Delivered` و`Cancelled` متاح قبل Shipped بس.
- **Empty:** «مفيش طلبات لسه». **Error:** Banner أعلى الجدول + زرار «إعادة المحاولة».

### 9.5 Admin — Book Form

- **الحقول:** العنوان (≤150) · المؤلف (Select) · التصنيف (Select) · السعر · سعر الخصم (< السعر) · المخزون (≥0) · ISBN (فريد) · الوصف (≤2000) · الغلاف (JPG/PNG/WebP ≤2MB).
- **Behavior:** معاينة الغلاف فورًا، Validation Inline، Soft Delete بدل الحذف النهائي.
- **بعد الحفظ:** Redirect للقائمة + Toast نجاح.

## 10. Accessibility Checklist

- Contrast ≥ AA، وFocus Ring واضح (`outline: 2px solid color-accent-500`).
- كل الـ Interactions شغالة بالكيبورد (Tab · Enter · Esc للـ Modal مع Focus Trap).
- `lang` و`dir` على `<html>` حسب اللغة، وLabels مرتبطة بالحقول، وToast بـ `role="status"`.
- Touch targets ≥ 44px، واحترام `prefers-reduced-motion`.

## 11. الربط مع MVC

| الطبقة | التنفيذ |
| --- | --- |
| **Layouts** | `_StoreLayout` · `_AdminLayout` · `_AuthLayout` |
| **Areas** | `Admin` (Controllers: Dashboard, Books, Categories, Authors, Orders, Users) |
| **Controllers (Store)** | Home · Books · Cart · Checkout · Orders · Account · Profile · Wishlist |
| **ViewModels** | لكل View ViewModel خاص (مفيش Entities في الـ Views) |
| **Partials / View Components** | BookCard · Navbar · Pagination · DataTable · StatCard |
| **Data** | EF Core + Repository + Unit of Work + Migrations + Seed Data |
| **Entities** | Book · Author · Category · CartItem · Order · OrderItem · Address · WishlistItem · ApplicationUser |

## 12. Task Breakdown + Test-per-Task

المشروع مقسّم لـ **33 تاسك للـ MVP (Sprint 0–6)**، وكل تاسك مش بيتقفل غير لما يعدّي 4 أنواع اختبارات على نفسه: **Guard** و**Race** و**Security** و**MVC**. التاسك اللي نوع منهم مش مناسب له بنكتب جنبه "N/A" وسببه في الـ PR، مش بنسيبه فاضي.

### 12.1 أنواع الاختبارات الأربعة

| النوع | بيثبت إيه | أداته |
| --- | --- | --- |
| **Guard** | الصلاحيات: مين يدخل وإيه اللي يتمنع (Anonymous / Customer / Admin) وحماية الحالات (مثلًا مينفعش Delivered ترجع Pending) | `WebApplicationFactory` + Architecture test |
| **Race** | سلوك العمليات المتزامنة: آخر نسخة من كتاب، ضغطتين على Submit، تعديلين في نفس الوقت | xUnit + `Task.WhenAll` على **SQL Server حقيقي** (Testcontainers) |
| **Security** | IDOR وCSRF وXSS وSQLi ورفع ملفات وOpen Redirect وRate limit وHeaders | Integration tests + `AngleSharp` لفحص الـ HTML |
| **MVC** | الـ Controller يرجع الـ View والـ Model الصح، وModelState الغلط، وRedirect بعد النجاح، والـ Razor بيعرض الصح | Unit (NSubstitute) + Integration (HTML) |

> **مهم:** اختبارات الـ Race ما تتعملش على `InMemory` ولا SQLite لأنهم ما بيحاكوش الـ Locking ولا الـ Isolation. لازم قاعدة حقيقية.

### 12.2 هيكل الـ Solution والاختبارات

```text
BookNest.sln
 ├─ src/BookNest.Core            (Entities, Interfaces, Services)
 ├─ src/BookNest.Infrastructure  (EF Core, Repositories, Identity)
 ├─ src/BookNest.Web             (MVC, Areas/Admin, ViewModels, wwwroot)
 └─ tests/
     ├─ BookNest.UnitTests         (Services + Controllers)
     ├─ BookNest.IntegrationTests  (WebApplicationFactory + Testcontainers)
     └─ BookNest.ArchitectureTests (قواعد الطبقات والـ Authorize)
```

- **التسمية:** `Method_Scenario_ExpectedResult`.
- **Trait لكل تيست:** `[Trait("Category","Guard")]` أو `Race` أو `Security` أو `Mvc`، عشان الـ CI يشغّلهم منفصلين ويظهر تقرير لكل نوع.
- **عزل البيانات:** `Respawn` يمسح الـ DB بين كل تيست.

### 12.3 أمثلة جاهزة لكل نوع

**Guard: كل Route في الـ Admin محمي (Architecture test، بيمسك أي Controller جديد ينسى الـ Authorize):**

```csharp
[Fact, Trait("Category","Guard")]
public void AdminControllers_MustRequireAdminRole()
{
    var controllers = typeof(Program).Assembly.GetTypes()
        .Where(t => typeof(Controller).IsAssignableFrom(t) && t.Namespace!.Contains(".Areas.Admin"));

    foreach (var c in controllers)
    {
        var attr = c.GetCustomAttribute<AuthorizeAttribute>();
        Assert.True(attr?.Roles?.Contains("Admin") == true, $"{c.Name} missing [Authorize(Roles=\"Admin\")]");
    }
}
```

**Guard: Anonymous يتحول للـ Login وCustomer يتمنع:**

```csharp
[Theory, Trait("Category","Guard")]
[InlineData("/admin")] [InlineData("/admin/books")] [InlineData("/admin/orders")]
public async Task AdminRoutes_Anonymous_RedirectToLogin(string url)
{
    var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
    var res = await client.GetAsync(url);
    Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
    Assert.Contains("/account/login", res.Headers.Location!.OriginalString);
}
```

**Race: آخر نسخة من كتاب، 10 طلبات في نفس اللحظة، واحد بس ينجح:**

```csharp
[Fact, Trait("Category","Race")]
public async Task PlaceOrder_LastCopy_OnlyOneSucceeds()
{
    var bookId = await _seed.BookWithStockAsync(1);
    var attempts = Enumerable.Range(0, 10).Select(async _ =>
    {
        using var scope = _factory.Services.CreateScope();       // DbContext مستقل لكل محاولة
        var svc = scope.ServiceProvider.GetRequiredService<IOrderService>();
        return await svc.PlaceAsync(await _seed.UserWithCartAsync(bookId, qty: 1));
    });

    var results = await Task.WhenAll(attempts);

    Assert.Equal(1, results.Count(r => r.Succeeded));
    Assert.Equal(0, await _db.Books.Where(b => b.Id == bookId).Select(b => b.Stock).SingleAsync());
}
```

والتنفيذ اللي بيعدّي التيست ده: `ExecuteUpdateAsync` ذرّي (`SET Stock = Stock - @q WHERE Id = @id AND Stock >= @q`) جوه Transaction، ونفحص `rowsAffected == 1`.

**Security: IDOR، يوزر مينفعش يفتح طلب يوزر تاني:**

```csharp
[Fact, Trait("Category","Security")]
public async Task OrderDetails_OtherUsersOrder_Returns404()
{
    var orderId = await _seed.OrderForUserAsync("alice@test.com");
    var bob = _factory.CreateClientFor("bob@test.com");

    var res = await bob.GetAsync($"/orders/{orderId}");

    Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);   // 404 مش 403 عشان ما نكشفش إن الطلب موجود
}
```

**MVC: Controller + ModelState + Redirect:**

```csharp
[Fact, Trait("Category","Mvc")]
public async Task Checkout_InvalidModel_ReturnsSameViewWithModel()
{
    var controller = new CheckoutController(_orderService, _cartService);
    controller.ModelState.AddModelError("Phone", "Required");
    var vm = new CheckoutViewModel();

    var result = await controller.Index(vm);

    var view = Assert.IsType<ViewResult>(result);
    Assert.Same(vm, view.Model);
    await _orderService.DidNotReceive().PlaceAsync(Arg.Any<PlaceOrderRequest>());
}
```

### 12.4 Definition of Done لكل تاسك

- [ ] الكود متراجع (PR) ومعدّي Build من غير Warnings.
- [ ] تيست **Guard** مكتوب (أو N/A بسبب).
- [ ] تيست **Race** مكتوب لأي عملية بتغيّر Stock أو Status أو بتنشئ سجل فريد (أو N/A بسبب).
- [ ] تيست **Security** مكتوب (IDOR/XSS/CSRF/Validation/Upload حسب التاسك).
- [ ] تيست **MVC** مكتوب (Controller Unit + Integration على الـ HTML للصفحة).
- [ ] الـ Validation على السيرفر ومتغطي بتيست، مش على الـ Client بس.
- [ ] الـ Empty / Loading / Error في الـ UI مطابقين للـ Design System.
- [ ] الـ README أو الـ ADR اتحدّث لو في قرار معماري.

### 12.5 Sprint 0: الأساس

| ID | المهمة · يغطي | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- |
| T-00 | Solution بالطبقات الثلاث + 3 مشاريع اختبار + CI (Build + Test) | Architecture test: Core مايعتمدش على Web أو Infrastructure | N/A | `dotnet list package --vulnerable` بيفشل الـ CI | `Program` بيشتغل في `WebApplicationFactory` (Smoke) |
| T-01 | الـ Entities والـ Migrations والـ Constraints (ISBN وEmail فريدين، `RowVersion` على Book وOrder) · NFR-08 | N/A | إدخال كتابين بنفس الـ ISBN في نفس اللحظة: واحد بس ينجح (Unique Index) | لا يوجد `string` بدون `MaxLength` في الـ Entities (Architecture test) | Migration تتطبق على DB فاضية وتتعمل Rollback |
| T-02 | Seed: 60+ كتاب حقيقي، 8 تصنيفات، 20 مؤلف، Role-ين، Admin افتراضي | Admin الافتراضي بياخد Role Admin وبس | تشغيل الـ Seed مرتين في نفس الوقت مايعملش تكرار (Idempotent) | كلمة سر الـ Admin من Environment Variable مش من الكود | Seed بيتشغل في Development فقط (تيست على `Environment`) |
| T-03 | Tokens CSS (`tokens.css`) + `_StoreLayout` و`_AdminLayout` و`_AuthLayout` + RTL/LTR · NFR-05 | `_AdminLayout` مايتعرضش غير في Area Admin | N/A | كل `Html.Raw` في الـ Layouts ممنوع (Architecture test على الـ Views) | `<html lang dir>` بيتغير حسب الثقافة، وLayout الصح بيتحمل لكل Area |
| T-04 | مكتبة الـ Components (Button، Input، Badge، Toast، Modal، Pagination، DataTable) + صفحة `/dev/styleguide` · قسم 7 | `/dev/styleguide` بترجع 404 في Production | N/A | الـ Partials بتعمل Encode للمدخلات (تيست بـ `<script>` في الـ Label) | كل Component بيتعرض من غير Exception بكل الـ Variants (Render test) |
| T-05 | Global Exception Handler، Serilog، صفحات 404/403/500، Security Headers، HTTPS/HSTS · NFR-02, 07, 10 | 403 للممنوع و404 للمش موجود، ومفيش Stack trace في Production | N/A | الـ Response فيه `X-Content-Type-Options` و`Referrer-Policy` و`CSP` و`X-Frame-Options`، والـ Cookies `HttpOnly` و`Secure` و`SameSite` | Exception في Controller بيرجع صفحة 500 بالـ Layout الصح، والـ Log بيتكتب |

### 12.6 Sprint 1: الـ Auth والصلاحيات

| ID | المهمة · يغطي | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- |
| T-10 | Identity + Roles + Password Policy + Lockout بعد 5 محاولات · FR-AUTH-02 | يوزر Locked-out مايدخلش حتى بالباسورد الصح | N/A | الباسورد Hashed، والـ Lockout بيشتغل، ورسالة الخطأ واحدة سواء الإيميل غلط أو الباسورد غلط (مفيش User Enumeration) | `UserManager` متظبط في الـ DI، وSign-in بيرجع Cookie |
| T-11 | Register / Login / Logout · FR-AUTH-01, 02, 04 | Authenticated user يفتح `/account/login` يتحول للصفحة الرئيسية، وLogin يوجّه Admin للوحة والـ Customer للمتجر | تسجيلين بنفس الإيميل في نفس اللحظة: واحد ينجح والتاني يرجع "الإيميل مستخدم" (مش 500) | `returnUrl` خارجي مرفوض (Open Redirect)، وPOST من غير `AntiForgeryToken` مرفوض 400، والباسورد مش بيظهر في الـ Log | ModelState غلط يرجع نفس الـ View، ونجاح التسجيل Redirect، والـ HTML فيه Labels مرتبطة بالحقول |
| T-12 | نسيت الباسورد / Reset · FR-AUTH-03 | Token منتهي (60 دقيقة) مرفوض | استخدام نفس الـ Token مرتين في نفس اللحظة: مرة واحدة بس تنجح | الرد على الإيميل المش موجود نفس رد الموجود (مفيش Enumeration)، وRate limit على الطلب | الـ View بتعرض رسالة موحدة، وReset ناجح بيعمل Redirect لـ Login |
| T-13 | Authorization Policies وحماية الـ Admin Area وصفحات 403/404 · NFR-03, BR-09 | **المصفوفة الكاملة:** كل Route × (Anonymous / Customer / Admin) في Theory واحدة، وArchitecture test إن كل Controller في Admin عليه `[Authorize(Roles="Admin")]` | تعطيل Admin وتغيير Role في نفس اللحظة مايسيبش النظام من غير Admin | مفيش Endpoint بيعتمد على إخفاء الزرار بس (الصلاحية على السيرفر) | Customer على `/admin` يشوف صفحة 403 بالـ `_StoreLayout` |

### 12.7 Sprint 2: الـ Catalog

| ID | المهمة · يغطي | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- |
| T-20 | الصفحة الرئيسية + Cache · FR-HOME-01 | متاحة للـ Anonymous بدون Redirect | 50 طلب متزامن على Cache فاضي: استعلام واحد بس للـ DB (مفيش Cache stampede) | الكتب المحذوفة Soft Delete مابتظهرش | الـ Model فيه الأقسام الأربعة، والـ HTML فيه كروت بعدد صح |
| T-21 | Catalog Query Service: بحث وفلترة وترتيب وPagination على الـ DB · FR-CAT-01 → 04 | `page` و`pageSize` برا النطاق (سالب أو 10,000) بيتصحح مش بيوقع | N/A | SQL Injection في البحث (`'; DROP TABLE--`) بيتعامل كنص عادي، وترتيب بـ Whitelist مش اسم عمود من اليوزر | تيست أن الـ Query بيتحول لـ SQL واحد (مفيش N+1، بفحص عدد الـ Queries)، والنتايج الصح لكل Filter |
| T-22 | صفحة Catalog + Filters + BookCard View Component · FR-CAT-01 → 04 | N/A | N/A | عنوان كتاب فيه `<script>` يظهر Encoded في الـ HTML | حالة Empty وحالة Out-of-stock وPagination في الـ HTML، والفلاتر بتتحفظ في الـ Query string |
| T-23 | Book Details + كتب مشابهة · FR-CAT-05 | Soft-deleted أو Id مش موجود يرجع 404 | N/A | الـ Slug في الـ URL مايأثرش على الـ Authorization، وحقل الوصف Encoded | `Model` فيه كل الحقول، وحالة المخزون بتتحول لـ Badge الصح (متاح / باقي X / غير متاح) |

### 12.8 Sprint 3: السلة والـ Wishlist

| ID | المهمة · يغطي | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- |
| T-30 | Cart Service: سلة الزائر (Cookie موقّع) وسلة اليوزر (DB) · FR-CART-01, 03، BR-02 | سلة يوزر مش بتتفتح بـ Id يوزر تاني | ضغطتين "أضف" في نفس اللحظة لنفس الكتاب: سطر واحد بكمية 2 (مش سطرين ولا Exception)، والكمية مايتعداش المخزون | الـ Cookie موقّع ومحدود الحجم، والكمية سالبة أو صفرية أو عشرية مرفوضة، وسعر الكتاب بيتقرا من الـ DB مش من الـ Request (Price tampering) | الـ Service بيرجع نتيجة موحدة (نجاح / مخزون ناقص / مش موجود) والـ Controller بيترجمها لـ Status Code |
| T-31 | دمج سلة الزائر مع سلة اليوزر عند الـ Login · FR-CART-02 | الدمج بيحصل بعد نجاح الـ Login فقط | Login من جهازين في نفس اللحظة مايضاعفش الكميات | Cookie السلة القديم بيتمسح بعد الدمج، وCookie متلاعب بيه يتجاهل | بعد الـ Login الـ Header بيعرض عدد صح |
| T-32 | صفحة Cart + Quantity Stepper (AJAX) · FR-CART-01, 03 | Endpoints الـ AJAX بتقبل POST فقط وبـ `AntiForgeryToken` | تحديث الكمية من تابين مفتوحين: آخر تحديث يكسب والإجمالي يتحسب من الـ DB | الإجمالي بيتحسب على السيرفر مش من الـ Client، وXSS في اسم الكتاب | Empty State لما السلة فاضية، والـ Partial بيرجع HTML بإجمالي صح، وحالة Out-of-stock بتظهر |
| T-33 | Wishlist · FR-WISH-01 | Anonymous يتحول للـ Login ويرجع بـ `returnUrl` آمن، ومينفعش يشوف Wishlist غيره | إضافة نفس الكتاب مرتين: سجل واحد (Unique على UserId + BookId) | IDOR على حذف عنصر من Wishlist يوزر تاني | الصفحة بتعرض Empty State، وزرار القلب بيتبدل في الـ HTML |

### 12.9 Sprint 4: Checkout والطلبات

| ID | المهمة · يغطي | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- |
| T-40 | إدارة العناوين في الـ Profile · FR-PRO-02 | عنوان يوزر تاني مينفعش يتعدّل أو يتحذف | تعيين عنوانين كـ Default في نفس اللحظة: واحد بس Default | IDOR، وMass assignment (الـ ViewModel مافيهوش `UserId`)، وتليفون مصري بـ Regex | CRUD كامل بـ Redirect وToast، والـ Validation بيرجع نفس الـ View |
| T-41 | **Order Placement Service:** Transaction، خصم مخزون ذرّي، Snapshot للسعر، Idempotency Key · FR-ORD-01 → 04، BR-02, 04, 08 | الـ Service بيرفض سلة مش بتاعت اليوزر الحالي | **(1)** آخر نسخة: 10 محاولات متزامنة، واحد ينجح والمخزون صفر. **(2)** نفس الـ Idempotency Key مرتين: طلب واحد. **(3)** فشل في نص العملية: Rollback كامل ومخزون ما اتغيرش. **(4)** كتابين مختلفين Deadlock محتمل: بنرتب الخصم بـ BookId | السعر والإجمالي بيتحسبوا على السيرفر، ومفيش تعديل على سعر طلب قديم، وحدود الكمية | تيست Unit على Controller (Valid / Invalid / نقص مخزون)، والنتيجة Redirect لصفحة النجاح |
| T-42 | Checkout (3 خطوات) + صفحة النجاح · FR-ORD-01 → 03 | Checkout بسلة فاضية يتحول لـ `/cart`، وصفحة النجاح مفتوحة لصاحب الطلب بس | Double-click على Submit: طلب واحد (الزرار بيتقفل + الـ Idempotency على السيرفر) | `AntiForgeryToken`، والإجمالي المعروض مش هو اللي بيتخزن، وPOST من يوزر تاني على نفس الـ Draft مرفوض | الخطوات الثلاث في الـ HTML، ورسائل Validation للتليفون والعنوان |
| T-43 | طلباتي وتفاصيل الطلب وإلغاء الطلب · FR-ORD-05, 06، BR-01 | **IDOR:** طلب يوزر تاني = 404، والإلغاء مسموح لـ Pending فقط | إلغاء طلب وفي نفس اللحظة Admin بيحوله لـ Shipped: واحد يكسب والتاني يترفض (`RowVersion`)، والمخزون بيرجع مرة واحدة بس | الإلغاء POST وبـ AntiForgery | قائمة بـ Pagination وStatus Chip صح، وEmpty State، وزرار الإلغاء مخفي لو الحالة مش Pending |
| T-44 | Profile: بيانات وتغيير باسورد · FR-PRO-01 | يوزر مايعدّلش بيانات غيره عن طريق تغيير `id` في الـ Form | تعديلين متزامنين: `RowVersion` أو آخر كتابة بشكل متفق عليه | تغيير الباسورد بيطلب القديم، وبيعمل Invalidate للـ Sessions الأخرى | حفظ ناجح بـ Toast وحفظ فاشل بـ Errors تحت الحقول |

### 12.10 Sprint 5: لوحة الأدمن

| ID | المهمة · يغطي | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- |
| T-50 | Admin Shell + Dashboard · FR-ADM-01 | Customer = 403، Anonymous = Login (من مصفوفة T-13) | الإحصائيات بتتحسب بـ Query واحد على Snapshot متسق (مش أرقام متناقضة أثناء طلب جديد) | الأرقام ما بتتعرضش في الـ HTML لغير الأدمن (حتى في Partials) | الـ ViewModel بيتحسب من بيانات Seed معروفة، والـ Chart بياخد Data صحيحة |
| T-51 | Books CRUD + رفع غلاف · FR-ADM-02، BR-03, 05, 07 | كل Action (Create / Edit / Delete) متجربة بـ 3 أدوار | تعديل نفس الكتاب من أدمنين: `RowVersion` يرفض التاني برسالة "اتعدّل من حد تاني"، وISBN مكرر في نفس اللحظة | **Upload:** فحص الـ Magic bytes مش الامتداد فقط، حد 2MB، اسم ملف عشوائي (مفيش Path traversal)، ورفض SVG وملفات تنفيذية | Create ناجح = Redirect + Toast، وISBN مكرر = نفس الـ View بالخطأ، والخصم أكبر من السعر مرفوض، والحذف Soft |
| T-52 | Categories + Authors CRUD (Modal) · FR-ADM-03 | زي T-51 | حذف تصنيف وفي نفس اللحظة إضافة كتاب فيه: واحد يكسب، ومفيش كتب يتيمة | منع حذف تصنيف/مؤلف مرتبط بكتب (Server-side)، وXSS في الاسم | الـ Modal بيفتح من Partial ويرجع JSON/HTML صح، والأخطاء بتظهر في الـ Modal |
| T-53 | Orders List + Details + تغيير الحالة · FR-ADM-04, 05، BR-01 | Admin فقط، وتغيير الحالة بيتحقق من المسار المسموح (Pending → Processing → Shipped → Delivered) | **(1)** أدمنين يغيّروا نفس الطلب في نفس اللحظة. **(2)** إلغاء العميل وShipped من الأدمن. **(3)** إلغاء مرتين: المخزون يرجع مرة واحدة | الحالة بتتحقق على السيرفر (مش Dropdown فقط)، وPOST بـ AntiForgery، وLog لمين غيّر الحالة | فلترة بالحالة والتاريخ، وChip بلون الحالة، وPagination، وحالة Delivered بدون Dropdown |
| T-54 | Users Management · FR-ADM-06، BR-09 | الأدمن مايعطّلش نفسه ولا يشيل Role نفسه، وآخر Admin مايتشالش | تعطيل يوزر وهو بيعمل Checkout: الطلب إما يكمل أو يترفض كامل | تغيير Role محتاج Re-authentication، ويتسجل في الـ Log | الجدول بيعرض Roles والحالة، والـ Toggle بيرجع الحالة الجديدة |

### 12.11 Sprint 6: التقفيل (Hardening وRelease)

| ID | المهمة · يغطي | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- |
| T-60 | Rate Limiting وحماية Brute-force وSecurity audit · NFR-02 | الـ Limiter على Login وRegister وForgot وCheckout بس | 100 طلب متزامن: بعد الحد بيرجع 429 والعدّاد دقيق | فحص الـ Headers كلها، OWASP ZAP baseline في الـ CI، وSecrets مش في الـ Repo (gitleaks) | صفحة 429 بتظهر بالـ Layout ورسالة مفهومة |
| T-61 | Performance: Indexes وAsNoTracking وكشف N+1 وCaching · NFR-01, 11 | N/A | الـ Cache بيتحدّث بعد تعديل أدمن (Invalidation) من غير قيمة قديمة | الـ Cache مابيخزنش بيانات يوزر (Cache Poisoning) | تيست بيعدّ الـ Queries لكل صفحة رئيسية (حد أقصى محدد) |
| T-62 | E2E Smoke بـ Playwright للمسارات الأساسية | مسار Admin ومسار Customer منفصلين | 2 Browsers يشتروا آخر نسخة: واحد ينجح | المسار كامل على HTTPS | Register → Browse → Cart → Checkout → My Orders، وAdmin: إضافة كتاب → تغيير حالة طلب |
| T-63 | Docs: README وERD وScreenshots وADRs وTest report | N/A | N/A | قسم Security في الـ README (الحماية المطبقة) | N/A |
| T-64 | Docker + CI/CD + Deploy لايف | الـ Admin الافتراضي بيتغيّر باسورده أول تشغيل | N/A | Secrets من Environment، وHTTPS إجباري، وContainer بـ non-root | `/health` بيرجع 200 وبيفحص الـ DB |

### 12.12 بوابات الجودة (Quality Gates في الـ CI)

| البوابة | الشرط |
| --- | --- |
| Build | بدون Warnings (`TreatWarningsAsErrors`) |
| Unit + MVC | 100% ناجحين، وتغطية Services ≥ 70% |
| Guard | 100% ناجحين، ومفيش Controller بدون `[Authorize]` أو `[AllowAnonymous]` صريح |
| Race | تشغيلهم 5 مرات متتالية والكل ينجح (عشان مايبقوش Flaky) |
| Security | 100% ناجحين + `dotnet list package --vulnerable` نظيف + gitleaks نظيف |
| E2E | المسارات الأساسية ناجحة قبل أي Deploy |

### 12.13 ترتيب الشغل والتقدير

| Sprint | المحتوى | التقدير |
| --- | --- | --- |
| 0 | الأساس (T-00 → T-05) | 5–6 أيام |
| 1 | Auth والصلاحيات (T-10 → T-13) | 4–5 أيام |
| 2 | Catalog (T-20 → T-23) | 5 أيام |
| 3 | السلة والـ Wishlist (T-30 → T-33) | 4 أيام |
| 4 | Checkout والطلبات (T-40 → T-44) | 6–7 أيام |
| 5 | الأدمن (T-50 → T-54) | 6–7 أيام |
| 6 | التقفيل (T-60 → T-64) | 5 أيام |

الإجمالي حوالي **8 أسابيع** لشخص واحد بدوام جزئي مريح. التاسكات **T-41 و T-43 و T-53** هي الأخطر والأهم في الـ CV لأنها فيها الـ Transactions والـ Race Conditions، فمتستعجلش فيهم.

## 13. طبقة الإبهار (Wow Layer)

القاعدة: **كل ميزة هنا لازم تتشاف في ديمو مدته 3 دقائق، ولازم يكون وراها هندسة حقيقية تتختبر.** لو الميزة شكلها حلو بس مفيش وراها Transaction أو Race أو Security، مش بتدخل. الطبقة دي بتتبني **بعد** ما الـ MVP (Sprint 0–6) يخلص ويتختبر.

### 13.1 الميزات ولحظة الإبهار في الديمو

| الميزة | اللحظة اللي بتبهر | الهندسة اللي وراها |
| --- | --- | --- |
| **حجز مخزون مؤقت (Signature)** | عميل بيدخل الـ Checkout فتظهر له "النسخة محجوزة ليك 14:59" بعدّاد حي، وعميل تاني يشوف "محجوز الآن" | Reservation بـ TTL 15 دقيقة، حجز ذرّي، Background Job بيفك الحجز، وRace بين الانتهاء والتأكيد |
| **لوحة أدمن لحظية** | الأدمن في نافذة، والعميل بيأكد طلب في نافذة تانية، والـ Toast والعدّاد بيتحدّثوا قدامك من غير Refresh | SignalR Hub محمي بـ Role، وGroups، وإعادة اتصال |
| **بحث عربي ذكي + اقتراحات فورية** | تكتب "احمد" فيظهر كتاب "أحمد" و"إحمد"، والنتايج بتظهر وأنت بتكتب مع تمييز الكلمة | Normalization للعربي (التشكيل، أ/إ/آ، ة/ه، ى/ي)، Debounce، Escape للـ LIKE، تمييز آمن من XSS |
| **تتبع الطلب (Timeline)** | الطلب بيظهر كخط زمني: تم الطلب ← قيد التجهيز ← اتشحن ← اتسلم، بتواريخ حقيقية | Order State Machine + `OrderStatusHistory` |
| **عربي وإنجليزي بضغطة** | الواجهة بتنقلب RTL/LTR فورًا بالأرقام والعملة والتواريخ | `.resx` + Culture Middleware |
| **نبّهني لما يتوفر** | كتاب خلص، العميل سابِ إيميله، الأدمن يزوّد المخزون فيوصله إيميل بتصميم محترم | Subscription فريد، Outbox، Job غير مكرر |
| **Dark Mode** | تبديل بضغطة بدون وميض | Tokens فقط (مفيش ألوان مكتوبة في الـ CSS) + فحص Contrast |
| **لوحة تحليلات للأدمن** | مبيعات آخر 30 يوم، أعلى 10 كتب، توزيع التصنيفات | Aggregates على الـ DB مش في الذاكرة |

### 13.2 متطلبات جديدة

| ID | المتطلب | أولوية |
| --- | --- | --- |
| FR-RES-01 | عند بدء الـ Checkout تتحجز الكميات 15 دقيقة، والمتاح = المخزون − المحجوز | M |
| FR-RES-02 | الـ Checkout بيعرض عدّاد تنازلي، وعند الانتهاء يتحول لرسالة "انتهى الحجز" مع زرار إعادة الحجز | M |
| FR-RES-03 | تأكيد الطلب بيحوّل الحجز لخصم فعلي، والانتهاء بيرجّع الكمية تلقائيًا | M |
| FR-RES-04 | الكتاب المحجوز بالكامل بيظهر "محجوز مؤقتًا" مش "غير متاح" | S |
| FR-LIVE-01 | الأدمن يشوف الطلبات الجديدة وتحذيرات المخزون القليل لحظيًا | S |
| FR-SRCH-01 | البحث بيتجاهل التشكيل ويوحّد الحروف المتشابهة العربية | M |
| FR-SRCH-02 | اقتراحات فورية بعد حرفين، بحد أقصى 8 نتايج | S |
| FR-ORD-10 | `OrderStatusHistory`: مين غيّر الحالة وإمتى وليه | M |
| FR-ORD-11 | العميل يشوف Timeline للطلب | S |
| FR-I18N-01 | عربي/إنجليزي، والاختيار بيتحفظ، والأرقام والعملة حسب اللغة | M |
| FR-NOTIF-01 | "نبّهني لما يتوفر" بإيميل واحد لكل اشتراك | S |
| FR-ADM-10 | لوحة تحليلات: مبيعات، أعلى الكتب، التصنيفات | S |
| FR-UX-01 | Dark Mode | C |

**قواعد عمل جديدة:**

- **BR-10:** المتاح للشراء = `Stock − Reserved`، ومفيش حجز بكمية أكبر من المتاح.
- **BR-11:** مدة الحجز 15 دقيقة، وبتتجدد مرة واحدة فقط لو العميل لسه في الـ Checkout.
- **BR-12:** الحجز المنتهي ما ينفعش يتأكد، وانتهاء الحجز وتأكيده في نفس اللحظة: واحد بس يكسب.
- **BR-13:** اشتراك "نبّهني" واحد لكل (يوزر، كتاب)، وإيميل واحد لكل مرة بيرجع فيها المخزون.

### 13.3 تاسكات Sprint 7: الـ Wow Layer

| ID | المهمة · يغطي | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- |
| T-65 | عربي/إنجليزي: `.resx` وCulture Middleware وتبديل اللغة · FR-I18N-01 | الـ Cookie بيتغيّر لقيم مسموحة فقط (`ar` أو `en`) | N/A | لغة غير مدعومة أو `returnUrl` خارجي مرفوض، وقيم الـ Culture من Whitelist | `<html lang dir>` صح، ومفيش مفتاح ترجمة ناقص (تيست يقارن ملفين الـ resx)، وتنسيق الأسعار صح |
| T-66 | **Order State Machine** + `OrderStatusHistory` + Audit Log · FR-ORD-10، BR-01 | كل انتقال × كل دور، والانتقال الغلط يترفض | تغيير حالة من اتنين أدمن في نفس اللحظة: واحد يكسب والتاني يترفض، والـ History فيه سطر واحد | مفيش تعديل مباشر لعمود `Status` خارج الـ State Machine (Architecture test)، والـ History للقراءة فقط | الـ Controller بيعرض سبب الرفض بوضوح، وTheory بكل الانتقالات المسموحة والممنوعة |
| T-67 | بحث عربي بـ Normalization + اقتراحات فورية · FR-SRCH-01, 02 | الـ Endpoint متاح للزائر بـ Rate limit | الاقتراحات أثناء تعديل أدمن لكتاب: مفيش نتيجة قديمة بعد التحديث (Invalidation) | Escape لـ `%` و`_` في الـ LIKE، SQLi، والتمييز بيعمل Encode (XSS في عنوان كتاب فيه `<b>`)، وحد أقصى لطول النص | Theory بحالات عربية (أحمد/احمد/إحمد، مدرسة/مدرسه، على/علي) والـ Partial بيرجع 8 كحد أقصى |
| T-68 | **حجز المخزون المؤقت** + Job فك الحجز + عدّاد الـ Checkout · FR-RES-01 → 04، BR-10 → 12 | الحجز لصاحبه فقط، ومحدش يأكد حجز غيره | **(1)** 10 حجوزات متزامنة على آخر نسخة: واحد ينجح. **(2)** انتهاء الحجز + تأكيده في نفس اللحظة: واحد يكسب والمخزون سليم. **(3)** الـ Job شغال مرتين متزامنتين: الفك مرة واحدة. **(4)** تجديد الحجز مرتين متزامنتين: مرة واحدة | الـ TTL بيتحسب على وقت السيرفر مش الـ Client، وعدّاد الـ Client للعرض فقط، ومفيش حجز بكمية سالبة أو أكبر من حد أقصى | عدّاد الـ Checkout في الـ HTML، ورسالة انتهاء الحجز، وحالة "محجوز مؤقتًا" في Catalog وDetails، والـ Job بيتشغل بـ `TimeProvider` مزيّف في التيست |
| T-69 | لوحة الأدمن اللحظية بـ SignalR · FR-LIVE-01 | الـ Hub بيرفض Anonymous وCustomer عند الاتصال | 100 طلب متزامن: كل Admin بيوصله كل الأحداث من غير تكرار، وإعادة الاتصال بتجيب اللي فاته | مفيش بيانات عملاء حساسة في الرسايل (Id ومبلغ بس)، والـ Origin محدد | تيست بـ `HubConnection` في الـ Integration، والـ Toast والعدّاد بيتحدّثوا |
| T-70 | Timeline للطلب · FR-ORD-11 | IDOR: طلب غيرك = 404 | N/A | الأسباب والملاحظات اللي كتبها الأدمن Encoded، وملاحظات الأدمن الداخلية مابتظهرش للعميل | الخط الزمني بعدد الخطوات وترتيبها الصح لكل حالة |
| T-71 | "نبّهني لما يتوفر" + Outbox للإيميلات · FR-NOTIF-01، BR-13 | الاشتراك لمسجل دخول فقط ولصاحبه | زيادة المخزون من أدمنين متزامنة: إيميل واحد لكل مشترك (Idempotency Key) | رابط إلغاء الاشتراك بـ Token موقّع، ومفيش Header Injection في الإيميل | القالب بيتعرض عربي وإنجليزي، والإيميل بيطلع في الـ Outbox مرة واحدة |
| T-72 | تحليلات الأدمن: مبيعات وأعلى كتب وتصنيفات · FR-ADM-10 | Admin فقط | الأرقام متسقة أثناء طلبات جديدة (Snapshot Isolation) | مفيش Endpoint بيرجع Data خام للأدمن من غير Authorize | الأرقام صح على Seed معروف، وChart بياخد بيانات صح |
| T-73 | Dark Mode · FR-UX-01 | N/A | N/A | الاختيار بيتخزن في Cookie بقيم محددة فقط | فحص Contrast آلي لكل Token في الوضعين (AA)، ومفيش ألوان مكتوبة (Hex) خارج `tokens.css` (Architecture test) |

### 13.4 تاسكات Sprint 8: حزمة الإثبات (Proof Pack)

الإبهار الحقيقي هو الإثبات. التاسكات دي هي اللي بتحوّل المشروع من "كود" لـ "منتج متثبت".

| ID | المهمة | معيار القبول | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- | --- |
| T-74 | **اختبار حمل بـ k6:** 1000 مستخدم افتراضي بيشتروا كتاب مخزونه 50 | **0 Oversell**، 50 طلب ناجح بالظبط، والباقي رُفض برسالة واضحة، وp95 أقل من ثانية على الـ Demo | السيناريو بيعمل Login حقيقي بحسابات Customer | دي هي اختبار الـ Race على مستوى النظام، بتتشغل 3 مرات والنتيجة ثابتة | Rate limit متعطّل بس على بيئة الاختبار، ومش على الـ Production | تقرير k6 محفوظ في `docs/load-test` |
| T-75 | Accessibility وLighthouse في الـ CI | Lighthouse ≥ 90 (Performance وAccessibility وBest Practices وSEO) على Home وCatalog وBook Details، وصفر Violations من `axe` | N/A | N/A | فحص الـ Best Practices بيشمل الـ HTTPS والـ Headers | `axe` جوه Playwright على الصفحات الأساسية بالوضعين (Light وDark) والاتجاهين (RTL وLTR) |
| T-76 | Mutation Testing بـ Stryker.NET على `OrderService` وReservation وStateMachine | Mutation score ≥ 80% على الـ 3 Services دول | N/A | بيثبت إن اختبارات الـ Race مش شكلية | بيثبت إن اختبارات الـ Guard بتفشل لو شلت الـ Check | بيثبت إن اختبارات الـ Controller بتفشل لو غيّرت النتيجة |
| T-77 | Demo جاهز: بيانات Seed للديمو + حسابين تجريبيين + فيديو 60 ثانية + GIFs | ديمو أونلاين شغال و`reset` للبيانات كل 24 ساعة | حسابات الديمو صلاحياتها محدودة (الأدمن مايحذفش بيانات الـ Seed الأساسية) | Reset الليلي مايتشغلش أثناء طلب شغال | حسابات الديمو بباسوردات مختلفة عن الـ Production، ومفيش بيانات حقيقية | `/health` وصفحة `/about-demo` بتشرح السيناريو |
| T-78 | README احترافي + 3 ADRs + ERD + Badges | ADR-1: ليه Reservation بدل الخصم عند التأكيد. ADR-2: ليه ترتيب الأقفال بـ BookId. ADR-3: ليه SignalR مش Polling | N/A | N/A | قسم "Security Controls" بيذكر كل حماية ومكان اختبارها | رابط لكل نوع تيست وتقريره |

### 13.5 سيناريو الديمو (3 دقائق)

1. **(0:00)** الصفحة الرئيسية، تبديل عربي ← إنجليزي ← Dark Mode في 5 ثواني.
2. **(0:20)** بحث "احمد" والاقتراحات بتظهر، والنتيجة فيها "أحمد".
3. **(0:40)** نافذتين جنب بعض: **عميل أ** و**الأدمن**. كتاب مخزونه نسخة واحدة.
4. **(1:00)** العميل أ يدخل الـ Checkout: العدّاد 14:59، والأدمن يشوف التنبيه لحظيًا.
5. **(1:30)** **عميل ب** يفتح نفس الكتاب فيلاقيه "محجوز مؤقتًا"، ويضغط "نبّهني".
6. **(2:00)** العميل أ يأكد الطلب، والأدمن يغيّر الحالة، والـ Timeline عند العميل بتتحدث.
7. **(2:30)** عرض سريع: تقرير k6 ("0 oversells")، وشارة CI بأنواع الاختبارات الأربعة، وMutation score.

### 13.6 خط القطع (لو الوقت ضاق)

| الأولوية | المهام | لو اتأخرت |
| --- | --- | --- |
| **لازم** (بتصنع الإبهار) | T-68, T-69, T-66, T-67, T-74, T-77, T-78 | لا تُحذف |
| **مهم** | T-65, T-70, T-71, T-75 | تتأجل لبعد أول نشر |
| **لو فاضل وقت** | T-72, T-73, T-76 | تتحذف بدون ضرر |

### 13.7 الجدول المحدّث

| المرحلة | المحتوى | التقدير |
| --- | --- | --- |
| Sprint 0–6 | الـ MVP الكامل (T-00 → T-64) | حوالي 8 أسابيع |
| Sprint 7 | الـ Wow Layer (T-65 → T-73) | 3 أسابيع |
| Sprint 8 | حزمة الإثبات (T-74 → T-78) | 1.5 أسبوع |
| **الإجمالي** | **51 تاسك** | **حوالي 13–14 أسبوع** |

### 13.8 جملة الـ CV بعد التطوير

> Built **BookNest**, a bilingual (AR/EN) bookstore on ASP.NET Core MVC, featuring time-boxed **stock reservation** with background expiry, a **real-time SignalR admin dashboard**, an order **state machine** with full audit history, and **Arabic-aware search**. Verified with 4 test suites (guard, race-condition, security, MVC) plus mutation testing (≥ 80%) and a **k6 load test showing zero oversells at 1,000 concurrent users**.

**ملحوظة:** الجملة دي مش بتبقى صحيحة غير لما الأرقام اتحققت فعلًا. لو النتيجة طلعت مختلفة (مثلًا Mutation score 74%) اكتب الرقم الحقيقي، لأن المقابلة التقنية هتسأل عنه.

## 14. Threat Model (STRIDE مختصر)

كل خطر هنا له حماية ومعاه اختبار بيثبتها، فالـ Security tests اللي في التاسكات مش عشوائية. **حدود النموذج:** بيانات الكروت عمرها ما بتتخزن عندنا (لو اتضاف دفع بالكارت يبقى عبر صفحة الدفع المستضافة عند المزوّد).

| الأصل | التهديد | الحماية | الإثبات |
| --- | --- | --- | --- |
| حسابات اليوزرز | **Spoofing:** تخمين الباسورد وCredential stuffing | Hashing قوي، Lockout بعد 5 محاولات، Rate limit على Login | T-10, T-60 |
| حسابات اليوزرز | **Info disclosure:** معرفة إن الإيميل مسجل (User Enumeration) | رد موحد في Login وForgot Password | T-10, T-12 |
| الطلبات | **Info disclosure / Tampering:** فتح أو تعديل طلب يوزر تاني (IDOR) | فحص الملكية على السيرفر، و404 مش 403 | T-43, T-70 |
| الأسعار والإجماليات | **Tampering:** تغيير السعر أو الكمية من الـ Client | السعر والإجمالي من الـ DB، وSnapshot في `OrderItem` | T-30, T-41 |
| المخزون | **Tampering / Race:** بيع نسخة أكتر من الموجود | خصم ذرّي، حجز مؤقت، Idempotency Key | T-41, T-68, T-74 |
| لوحة الأدمن | **Elevation of privilege:** Customer يوصل لصفحات الأدمن | `[Authorize(Roles)]` + Architecture test + اختبار على كل Endpoint | T-13, T-82 |
| الـ Sessions والـ Cookies | **Spoofing / CSRF** | `AntiForgeryToken`، `HttpOnly` و`Secure` و`SameSite` | T-05, T-11 |
| رفع الأغلفة | **Tampering:** ملف خبيث أو Path traversal | فحص Magic bytes، حد 2MB، اسم عشوائي، رفض SVG | T-51 |
| إجراءات الأدمن | **Repudiation:** إنكار إن حد غيّر حالة أو حذف | `OrderStatusHistory` وAudit Log بمين وإمتى | T-66 |
| البحث والـ Checkout | **DoS:** استعلامات تقيلة أو ضغط متعمد | حدود `pageSize` ونص البحث، Rate limiting، Timeouts | T-21, T-60 |
| الأخطاء والـ Logs | **Info disclosure:** Stack trace أو Secrets في الرد أو اللوج | صفحات خطأ عامة، وفلتر على اللوج | T-05, T-79 |
| قناة SignalR | **Spoofing / Info disclosure:** اشتراك غير مصرح | Hub بـ Role، وOrigin محدد، وبيانات الرسائل محدودة | T-69 |
| الأسرار | **Info disclosure:** مفاتيح أو كلمات سر في الـ Repo | Environment variables، وgitleaks في الـ CI | T-60, T-64 |

## 15. جاهزية الـ Production

دي الحاجات اللي بتفرّق مشروع "شغّال عندي" عن مشروع تقدر تسلّمه لناس. ماتتفتحش غير بعد الـ MVP.

### 15.1 متطلبات

| ID | المتطلب | أولوية |
| --- | --- | --- |
| NFR-13 | كل Request ليه `X-Correlation-Id` بيظهر في الـ Logs والرد، والـ Logs منظمة (JSON) | S |
| NFR-14 | `/health` بيفحص الـ DB ويرجع حالة واضحة بدون بيانات حساسة | M |
| NFR-15 | نسخ احتياطي يومي للـ DB، وتمرين Restore موثّق مرة على الأقل | S |
| NFR-16 | الـ Migrations بتتطبق من الـ CI/CD فقط، وتكون متوافقة مع الإصدار القديم (بدون كسر) | S |
| FR-PRIV-01 | صفحتا الشروط والخصوصية بالعربي والإنجليزي | M |
| FR-PRIV-02 | اليوزر يقدر يحذف حسابه (الطلبات القديمة بتتحول لبيانات مجهولة) ويصدّر بياناته JSON | S |

> **خصوصية البيانات:** لو المشروع هيشتغل على ناس حقيقيين في مصر، راجع **قانون حماية البيانات الشخصية رقم 151 لسنة 2020** مع مختص قبل الإطلاق. ده تنبيه مش استشارة قانونية.

### 15.2 تاسكات Sprint 9

| ID | المهمة · يغطي | Guard | Race | Security | MVC |
| --- | --- | --- | --- | --- | --- |
| T-79 | Correlation Id + Structured Logs + `/health` · NFR-13, 14 | `/health` متاح بدون بيانات حساسة، والتفاصيل للأدمن فقط | 100 طلب متزامن: كل Correlation Id مختلف ومفيش تداخل في اللوج | الـ Logs مفيهاش باسورد أو Token أو تليفون كامل (تيست على الـ Log sink) | الـ Header راجع في الرد، و`/health` بيرجع Unhealthy لو الـ DB واقعة |
| T-80 | حذف الحساب وتصدير البيانات + صفحات الخصوصية · FR-PRIV-01, 02 | اليوزر بيحذف ويصدّر حسابه هو بس، والأدمن الأخير مايتحذفش (BR-09) | حذف الحساب أثناء Checkout: الطلب يكمل أو يترفض كامل ومفيش بيانات يتيمة | Re-authentication قبل الحذف أو التصدير، والتصدير فيه بيانات صاحبه فقط | الصفحات بالعربي والإنجليزي، وتأكيد قبل الحذف، وملف JSON صالح |
| T-81 | نسخ احتياطي وتمرين Restore واستراتيجية Migrations · NFR-15, 16 | الـ Migrations بتتطبق من الـ CI فقط وليس من التطبيق في Production | تشغيل Migration من نسختين في نفس اللحظة: واحدة بس تشتغل (Lock) | النسخ مشفّرة وغير متاحة للعامة | تمرين Restore على DB جديدة والتطبيق بيعدّي Smoke test |
| T-82 | مراجعة نهائية للـ Threat Model قبل الإصدار | تيست بيمشي على كل Endpoint ويفشل لو فيه واحد من غير `[Authorize]` أو `[AllowAnonymous]` صريح | N/A (مغطى بـ T-41 و T-68 و T-74) | OWASP ZAP Full Scan على Staging، ومراجعة جدول القسم 14 صف بصف | N/A |

## 16. طريقة التنفيذ والتنظيم

### 16.1 ابني شريحة رأسية الأول (Walking Skeleton)

قبل ما توسّع، خلّي أقل مسار كامل يشتغل ومنشور أونلاين وبالاختبارات الأربعة: **كتاب ← سلة ← Checkout ← طلب في الـ DB.** كده تكتشف مشاكل الربط والنشر من الأسبوع الأول مش من الآخر.

1. T-00 و T-01 و T-02 (الأساس والـ DB والـ Seed).
2. T-05 (الأخطاء والـ Headers) و T-10 و T-11 (Login فقط).
3. نسخة مبسطة من T-23 (تفاصيل كتاب) و T-30 (سلة) و T-41 و T-42 (طلب بخصم ذرّي).
4. T-64 (نشر لايف) بالـ CI.
5. بعدها توسّع حسب الـ Sprints.

### 16.2 الشاشات قبل الـ Views

مفيش Razor View بيتكتب قبل ما شاشته تتصمم (Figma أو HTML) بالـ Tokens الجديدة، بكل حالاتها.

| الشاشة | المتطلبات | الحالات المطلوبة |
| --- | --- | --- |
| Home | FR-HOME-01 | Loading، فاضية (مفيش كتب)، خصم |
| Catalog | FR-CAT-01 → 04 | نتايج، Empty، Out-of-stock، Filters على الموبايل |
| Book Details | FR-CAT-05، FR-CART-01 | متاح، باقي X، محجوز مؤقتًا، غير متاح |
| Checkout | FR-ORD-01 → 03، FR-RES-02 | 3 خطوات، العدّاد، انتهاء الحجز، خطأ Validation |
| Admin Dashboard | FR-ADM-01، FR-ADM-10 | Loading، Empty، تنبيه لحظي |
| Admin Books | FR-ADM-02 | قائمة، نموذج بخطأ، رفع غلاف، حذف بتأكيد |

### 16.3 تنظيم الـ Repo (GitHub)

- **Issue لكل تاسك** (T-xx) من قالب فيه الـ Requirements المغطاة، وChecklist الاختبارات الأربعة والـ DoD.
- **Project Board:** Backlog ← In progress ← In review ← Done، وLabels للـ Sprint والنوع (feature / test / security / docs).
- **Branch protection على `main`:** ممنوع Push مباشر، والـ CI لازم ينجح، وPR واحد على الأقل.
- **PR صغير** (تاسك أو جزء منه) بقالب فيه: إيه اتغيّر، الاختبارات الأربعة (أو N/A + سبب)، Screenshots للـ UI.
- **Commits بصيغة موحّدة:** `feat:` و`fix:` و`test:` و`docs:` و`chore:`.
- **Release Tag** مع نهاية كل Sprint (`v0.1`، `v0.2`...) وCHANGELOG بسيط.

### 16.4 الجدول الرئيسي للمراحل

| المرحلة | المحتوى | التقدير |
| --- | --- | --- |
| 0 | تصميم الشاشات الست (16.2) | 1 أسبوع |
| 1 | الشريحة الرأسية (16.1) ومنشورة | 2 أسبوع |
| 2 | باقي الـ MVP (Sprint 0–6، T-00 → T-64) | حوالي 6–7 أسابيع إضافية |
| 3 | Wow Layer (T-65 → T-73) | 3 أسابيع |
| 4 | حزمة الإثبات (T-74 → T-78) | 1.5 أسبوع |
| 5 | جاهزية الـ Production (T-79 → T-82) | 1 أسبوع |
| **الإجمالي** | **51 تاسك** | **حوالي 14–15 أسبوع** |

### 16.5 اختياري بعد كل ده: الدفع بالكارت (Paymob)

بيخلي المتجر "حقيقي"، لكنه بيزوّد المخاطرة (Webhook وSignature وتكرار الإشعارات). لو اتعمل، يتعمل كـ T-83 بنفس الأربع اختبارات: Webhook بدون Signature مرفوض (Guard وSecurity)، ونفس الإشعار مرتين يأكد الطلب مرة واحدة (Race)، والمبلغ من الـ DB مش من الـ Request (Security).

## 17. إزاي تخليه يبان قوي في الـ CV

**قبل الكود:** ابني الـ Design System ده في Figma (Tokens + Components + أهم 6 شاشات) وحط اللينك في الـ README.

**في الكود (يفرّق عن المشاريع العادية):**

- Clean Architecture أو Layered (Core · Infrastructure · Web) + Repository/UoW.
- Identity + Roles + Policy-based Authorization.
- FluentValidation، AutoMapper، Serilog، Global Exception Middleware.
- Pagination/Filtering/Sorting على مستوى الـ Database (IQueryable) مش في الذاكرة.
- Unit Tests (xUnit) لـ Services المهمة، وSeed Data جاهزة.
- Docker + CI بسيط (GitHub Actions) + Deployment Live (Azure / Somee / Railway).

**في الـ README:** Screenshots، ERD، Roles & Pages table، Tech stack، وطريقة التشغيل بأمر واحد.
