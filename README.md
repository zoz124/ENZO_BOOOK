# Enzo Book — متجر كتب أونلاين (ASP.NET Core MVC)

متجر كتب بريميم بواجهة إنجليزية (LTR) وهوية بيضاء/أوف-وايت كريمي/أخضر غامق، تنفيذ لخطة
[BookNest-Implementation-Tickets.md](BookNest-Implementation-Tickets.md) والـ
[Design System & Handoff Spec](Enzo%20Store—%20Design%20System%20%26%20Handoff%20Spec.md).

> أسماء المشاريع والـ namespaces لسه `BookNest.*` (البراند الظاهر بس هو Enzo Book).

## المتطلبات

- .NET SDK 8.0 (`net8.0`؛ اشتغل على 8.0.418 و10.0.401)
- SQL Server — التطوير بيستخدم LocalDB: `(localdb)\mssqllocaldb`
- `dotnet tool install --global dotnet-ef`
- Docker — لتشغيل الـ compose، ولا لاختبارات `Race`/`Migration` (Testcontainers)

## التشغيل

```bash
dotnet restore
dotnet build -c Release -p:TreatWarningsAsErrors=true

# قاعدة البيانات (الـ migration بتتطبق تلقائيًا في Development كمان)
dotnet ef database update --project src/BookNest.Infrastructure --startup-project src/BookNest.Web
dotnet run --project src/BookNest.Web
```

### Docker

```bash
docker compose up --build
# المتجر: http://localhost:8080  ·  الأدمن: http://localhost:8080/admin
```

الـ compose بيشغّل SQL Server 2022 + الـ web، وبيحوّل `MigrateAndSeed=true` في Development،
وبيعمل volumes للسجلات والملفات المرفوعة. غيّر `SEED_ADMIN_PASSWORD` و`SQL_SA_PASSWORD`
قبل أي نشر.

### بيانات الـ Seed وحسابات الديمو

الـ Seed بيشتغل **في Development فقط** وعند `Database:MigrateAndSeed=true`، والباسوردات من متغيرات بيئة — مش من الكود:

```bash
export Seed__AdminEmail="admin@booknest.local"
export Seed__AdminPassword="<Strong-Password>"
# اختياري: عميل ديمو
export Seed__CustomerEmail="customer@booknest.local"
export Seed__CustomerPassword="<Strong-Password>"
```

بدون `Seed__AdminPassword` الـ Seeder بيتخطى إنشاء الأدمن ويكتب Warning.

### Seed Race (T-02)

تشغيل الـ Seed من أكتر من instance في نفس اللحظة كان بيعمل تكرار (check-then-act).
الحل المطبّق:

1. `DatabaseBootstrapper` بيمسك **Session app lock** على `master` (`sp_getapplock`) حوالين الـ Migrate + Seed.
2. `DatabaseSeeder` بيمسك **Transaction app lock** (`BookNest:DatabaseSeeder`) جوه transaction واحد بيعمل كل الخطوات.

التحقق: تم إسقاط القاعدة وتشغيل نسختين مع بعض → نسخة واحدة بس عملت Seed، والنتيجة 63 كتاب / 20 مؤلف / 8 تصنيفات / مستخدم واحد بدون أي أخطاء Unique.

## الهيكل

```
Enzo Book (BookNest.sln)
 ├─ src/BookNest.Core            Entities · Enums · Models (DTOs) · Interfaces — بدون أي اعتماد خارجي
 ├─ src/BookNest.Infrastructure  EF Core · Configurations · Migrations · Identity · Services · Seed
 ├─ src/BookNest.Web             MVC · Areas/Admin · ViewModels · wwwroot (tokens/base/components/pages)
 └─ tests/
     ├─ BookNest.UnitTests            (State machine + كتالوج الـ Seed)
     ├─ BookNest.IntegrationTests     (Smoke + Testcontainers للـ Race/Migration)
     └─ BookNest.ArchitectureTests    (الطبقات + Authorize + MaxLength + الثغرات)
```

## نظام التصميم

| الملف | المحتوى |
|---|---|
| `wwwroot/css/tokens.css` | ألوان (`--green #123C32`، `--green-deep #0D3028`، `--cream #F7F5EF`)، زوايا 0، مسافات، **Libre Caslon Display + DM Sans** (+ Amiri للعناوين العربية) |
| `wwwroot/css/base.css` | إعادة ضبط + تايبوجرافي + `container` + `eyebrow` |
| `wwwroot/css/components.css` | أزرار، حقول، badges، هيدر/فوتر المتجر، Auth card، **Admin shell** (sidebar + topbar + جداول) |
| `wwwroot/css/pages.css` | ستايلات الصفحات ولوحة التحكم |
| `wwwroot/css/figma.css` | طبقة مطابقة فيجما: الهيدر، الهيرو المنقسم، trust row، أغلفة الكتب الطباعية، manifesto، newsletter، الفوتر |
| `wwwroot/css/fonts.css` + `wwwroot/fonts/` | الخطوط self-hosted (بدون أي طلب خارجي) |

- الواجهة مطابقة لملف Figma Make (Enzo Store Bookshop): هيدر بـ ENZO + BOOK STORE، هيرو منقسم بصورة المكتبة وبطاقة Editor's Pick، trust row، 4 كروت بأغلفة طباعية، manifesto، newsletter، فوتر بـ 4 أعمدة.
- الـ 3 Layouts (`_StoreLayout`, `_AdminLayout`, `_AuthLayout`) كلها `<html lang="en" dir="ltr">`، والكلام كله إنجليزي.
- لوحة التحكم بنفس التوكنز: سايدبار أخضر متدرّج بمجموعات (Overview / Catalogue / Operations)، topbar أبيض بعنوان serif، صفحات بعناوين `admin-page-head`، جداول hairline، وstat cards بلمسة خضراء.
- `/dev/styleguide` فيها كل المكوّنات (بما فيها stat cards والجداول) وبترجع 404 خارج Development.
- مفيش Dark mode (T-73) — النظام أبيض/أخضر/أوف-وايت بس.

## التذاكر المنفّذة

| التذكرة | الحالة | ملاحظات |
|---|---|---|
| T-00 | ✅ | Solution + 3 طبقات + CI + Architecture/Security/Smoke tests |
| T-01 | ✅ | 11 Entity + Constraints + Migration (+ `IdempotencyKey` على Order) |
| T-02 | ✅ | Seed (63 كتاب/20 مؤلف/8 تصنيفات) + Roles + Admin من env + **race-safe** |
| T-03 | ✅ | نظام تصميم جديد كامل + 3 Layouts إنجليزي/LTR + Admin shell |
| T-04 | ✅ | Partials: BookCard · Pagination · StatusBadge · Toast + Skeleton/Modal + `/dev/styleguide` |
| T-05 | ✅ | Serilog (file+console) · Global exception · 403/404/500 · Security headers · HSTS |
| T-10 | ✅ | Password policy + Lockout 5/15د + Cookie 14 يوم |
| T-11 | ✅ | Register/Login/Logout + redirect حسب الدور + دمج سلة الزائر |
| T-12 | ⚠️ جزئي | Forgot/Reset شغالين بـ token، لكن مفيش SMTP — الرابط بيظهر/يتسجل مش يتبعت |
| T-13 | ✅ | `[Authorize(Roles="Admin")]` + Architecture test بيجبر كل Controller في Admin Area |
| T-20 | ✅ | Home: الأحدث/الأكثر مبيعًا/عليه خصم/التصنيفات + `IMemoryCache` 10 دقائق |
| T-21 | ✅ | `ICatalogService.GetBooksAsync` — فلترة/ترتيب/Pagination على الـ DB |
| T-22 | ✅ | Catalog + Sidebar filters + Grid + Empty state |
| T-23 | ✅ | Details + Quantity + Wishlist + كتب مشابهة + `/books/{id}/{slug}` |
| T-30 | ⚠️ | سلة الزائر في الـ DB عبر cookie موقّع (Data Protection) مش cart في الكوكيز |
| T-31 | ✅ | Merge عند الدخول مع Unique index + handling للتصادم |
| T-32 | ✅ | Update/Remove + POST + AntiForgery + الحساب على السيرفر |
| T-33 | ✅ | Wishlist + IDOR protection (scoped بـ UserId) |
| T-40 | ✅ | CRUD عناوين + Default + Ownership |
| T-41 | ✅ | Transaction + **خصم ذرّي** + Snapshot + Idempotency + Rollback |
| T-42 | ⚠️ | Checkout صفحة واحدة (عنوان/دفع/مراجعة) مش 3 خطوات + double-submit protection |
| T-43 | ✅ | My Orders + Details + Cancel (Pending فقط) |
| T-44 | ✅ | Profile + Change Password (بيطلب الحالي) |
| T-50 | ✅ | Dashboard: مبيعات/طلبات/جديد/مخزون قليل + Chart 30 يوم + Top 10 |
| T-51 | ✅ | Books CRUD + Upload (magic bytes + 2MB) + Soft Delete + Restore |
| T-52 | ✅ | Categories/Authors CRUD + منع حذف مرتبط بكتب |
| T-53 | ✅ | تغيير الحالة بمسارات مسموحة + History + RowVersion (concurrency) |
| T-54 | ✅ | Users list + تفعيل/تعطيل + Role + BR-09 (مايعدّيش نفسه) |
| T-65 | ⚠️ جزئي | الواجهة إنجليزي بالكامل + Culture middleware + `<html lang dir>`؛ `.resx` للنصوص مش معمول |
| T-66 | ✅ | State machine + Audit trail (`OrderStatusHistories`) + Timeline في الواجهة |
| T-67 | ✅ | بحث متطابق عربي/إنجليزي: `SearchText` مُطبَّع في الـ DB + migration + backfill (63 كتاب) |
| T-70 | ✅ | Timeline للحالات في Details (عميل + أدمن) |
| T-72 | ✅ | Category mix + low stock + top books في لوحة التحكم |
| T-73 | ❌ | Dark mode اتشال بقرار تصميمي — النظام أبيض/أخضر/أوف-وايت |
| T-68, T-69, T-71, T-74–T-78 | ❌ | Reservations · SignalR · Outbox/Notify · k6 · Lighthouse · Stryker · Demo package · ADRs |

**غير موجود (خارج نطاق MVP أو مؤجل):** Paymob · كوبونات · تقييمات ومراجعات · تقارير Excel/PDF · Inventory Log · إرسال إيميل.

## الاختبارات

```bash
dotnet test tests/BookNest.UnitTests            # 11/11 — State machine + Seed catalogue
dotnet test tests/BookNest.ArchitectureTests    # 7/7  — الطبقات + Authorize + MaxLength + ثغرات
dotnet test tests/BookNest.IntegrationTests --filter "Category=Mvc&FullyQualifiedName!~MigrationTests"  # 13/13
dotnet test tests/BookNest.IntegrationTests     # Race + Migration (محتاج Docker)
```

## أمان

- كل POST عليها AntiForgeryToken، وكل استعلام EF Parameterized.
- منع User Enumeration: رسالة واحدة للإيميل الغلط والباسورد الغلط.
- IDOR: كل استعلام للعميل scoped بـ `UserId`، والطلب بتاع يوزر تاني بيرجع 404.
- Security headers + CSP + HSTS في غير Development.
- Cookies: HttpOnly + SameSite=Lax، و`Secure` لما الطلب HTTPS.
- `/dev/styleguide` بترجع 404 خارج Development.

## قرارات معمارية

1. **`ApplicationUser` في Infrastructure** والـ Core Entities بتشيل `UserId` بدل Navigation — لو حطينا Navigation في Core كانت هتعتمد على Identity وتكسر Architecture test.
2. **خصم المخزون بـ `ExecuteUpdateAsync` شرطي** (`WHERE Stock >= qty`) مش قراءة ثم كتابة، ومع ترتيب حسب `BookId` لمنع الـ deadlock.
3. **`RowVersion` على Order** بيحوّل تغيير حالتين متزامنين لـ concurrency conflict مش lost update.
4. **سلة الزائر في الـ DB** بمفتاح session موقّع بالـ Data Protection — أنضف من حفظ السلة نفسها في الكوكي.
5. **`Book.SearchText`** عمود مُطبَّع بيتحدّث عند الحفظ/الـ seed، فالبحث العربي بيطابق «احمد» → «أحمد» بدون full scan على تطبيع في SQL.
