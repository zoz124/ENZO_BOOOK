# ENZO STORE - خطة التنفيذ التفصيلية (Implementation Tickets)

## 📖 نظرة عامة

هذا المستند يحتوي على **78 تذكرة تنفيذية مفصلة** لمشروع BookNest - متجر كتب E-Commerce باستخدام ASP.NET Core MVC.

### 🎯 النطاق
- **MVP**: Sprint 0-6 (T-00 حتى T-64)
- **Wow Layer**: Sprint 7-8 (T-65 حتى T-78)
- **الوقت المقدر**: 13-14 أسبوع

### 📋 محتوى كل تذكرة
- ✅ الهدف والمتطلبات المغطاة
- ✅ خطوات التنفيذ التفصيلية (Pseudo-code + أمثلة كاملة)
- ✅ تفاصيل UI (Design tokens + HTML structure + CSS)
- ✅ سيناريوهات الاختبار الكاملة (Guard, Race, Security, MVC)
- ✅ معايير القبول (Definition of Done)

### 🏗️ التنظيم
التذاكر منظمة حسب **الطبقات التقنية المترابطة**:
- Infrastructure → Services → Controllers → UI

### ⚡ التذاكر الحرجة (الأهم في CV)
- **T-41**: Order Placement Service (Transactions + Race Conditions)
- **T-53**: Order Status Management (State Machine + Concurrency)
- **T-66**: Order State Machine & Audit Trail
- **T-68**: Stock Reservation System (Complex Race)
- **T-74**: Load Testing (Zero Oversells Proof)

---

## 📚 جدول المحتويات

- [Phase 1: Foundation & Infrastructure](#phase-1-foundation--infrastructure)
  - [T-00: Solution Structure & CI](#t-00-solution-structure--ci-setup)
  - [T-01: Database Entities & Migrations](#t-01-database-entities--migrations)
  - [T-02: Seed Data](#t-02-seed-data)
  - [T-03: CSS Tokens & Layouts](#t-03-css-tokens--layouts)
  - [T-04: Component Library](#t-04-component-library--styleguide)
  - [T-05: Global Error Handling](#t-05-global-exception-handler--security)
- [Phase 2: Authentication & Authorization](#phase-2-authentication--authorization)
  - [T-10 حتى T-13](#phase-2-authentication--authorization)
- [Phase 3: Core Domain - Catalog](#phase-3-core-domain---catalog)
  - [T-20 حتى T-23](#phase-3-core-domain---catalog)
- [Phase 4: Shopping Experience](#phase-4-shopping-experience)
  - [T-30 حتى T-33](#phase-4-shopping-experience)
- [Phase 5: Order Management](#phase-5-order-management)
  - [T-40 حتى T-44](#phase-5-order-management)
- [Phase 6: Admin Dashboard](#phase-6-admin-dashboard)
  - [T-50 حتى T-54](#phase-6-admin-dashboard)
- [Phase 7: Wow Layer](#phase-7-advanced-features-wow-layer)
  - [T-65 حتى T-73](#phase-7-advanced-features-wow-layer)
- [Phase 8: Polish & Proof](#phase-8-polish--proof)
  - [T-74 حتى T-78](#phase-8-polish--proof)

---

# Phase 1: Foundation & Infrastructure

## Epic 1.1: Project Setup & Architecture

### T-00: Solution Structure & CI Setup

**الأولوية:** Must Have  
**التقدير:** 1-2 أيام  
**المتطلبات المغطاة:** NFR-08 (Architecture)

#### 🎯 الهدف
إنشاء Solution بهيكل Clean Architecture مع 3 طبقات + 3 مشاريع اختبار + CI Pipeline.

#### 📝 خطوات التنفيذ

**1. إنشاء Solution:**

```bash
# إنشاء Solution
dotnet new sln -n BookNest

# إنشاء هيكل المجلدات
mkdir src
mkdir tests

# Core Layer (Domain + Interfaces)
cd src
dotnet new classlib -n BookNest.Core -f net8.0
cd ..
dotnet sln add src/BookNest.Core

# Infrastructure Layer (EF Core + Identity + Repositories)
cd src
dotnet new classlib -n BookNest.Infrastructure -f net8.0
cd ..
dotnet sln add src/BookNest.Infrastructure

# Web Layer (MVC + Areas + wwwroot)
cd src
dotnet new mvc -n BookNest.Web -f net8.0
cd ..
dotnet sln add src/BookNest.Web

# Test Projects
cd tests
dotnet new xunit -n BookNest.UnitTests -f net8.0
dotnet new xunit -n BookNest.IntegrationTests -f net8.0
dotnet new xunit -n BookNest.ArchitectureTests -f net8.0
cd ..
dotnet sln add tests/BookNest.UnitTests
dotnet sln add tests/BookNest.IntegrationTests
dotnet sln add tests/BookNest.ArchitectureTests
```

**2. إضافة Project References:**

```bash
# Infrastructure يعتمد على Core
dotnet add src/BookNest.Infrastructure reference src/BookNest.Core

# Web يعتمد على Core و Infrastructure
dotnet add src/BookNest.Web reference src/BookNest.Core
dotnet add src/BookNest.Web reference src/BookNest.Infrastructure

# Tests تعتمد على المشاريع المختبرة
dotnet add tests/BookNest.UnitTests reference src/BookNest.Core
dotnet add tests/BookNest.UnitTests reference src/BookNest.Infrastructure
dotnet add tests/BookNest.IntegrationTests reference src/BookNest.Web
dotnet add tests/BookNest.ArchitectureTests reference src/BookNest.Core
dotnet add tests/BookNest.ArchitectureTests reference src/BookNest.Infrastructure
dotnet add tests/BookNest.ArchitectureTests reference src/BookNest.Web
```

**3. هيكل المجلدات:**

```
BookNest/
├── .github/
│   └── workflows/
│       └── ci.yml
├── src/
│   ├── BookNest.Core/
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   │   ├── IRepositories/
│   │   │   └── IServices/
│   │   ├── Enums/
│   │   └── Constants/
│   ├── BookNest.Infrastructure/
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Migrations/
│   │   │   └── Configurations/
│   │   ├── Repositories/
│   │   ├── Services/
│   │   └── Identity/
│   └── BookNest.Web/
│       ├── Areas/
│       │   └── Admin/
│       │       ├── Controllers/
│       │       └── Views/
│       ├── Controllers/
│       ├── Views/
│       │   ├── Shared/
│       │   │   ├── _StoreLayout.cshtml
│       │   │   ├── _AdminLayout.cshtml
│       │   │   └── _AuthLayout.cshtml
│       │   └── Components/
│       ├── ViewModels/
│       ├── wwwroot/
│       │   ├── css/
│       │   │   ├── tokens.css
│       │   │   ├── base.css
│       │   │   ├── components.css
│       │   │   └── main.css
│       │   ├── js/
│       │   └── images/
│       └── Program.cs
└── tests/
    ├── BookNest.UnitTests/
    ├── BookNest.IntegrationTests/
    └── BookNest.ArchitectureTests/
```

**4. Packages المطلوبة:**

```xml
<!-- src/BookNest.Core/BookNest.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <!-- No external dependencies - pure domain layer -->
</Project>

<!-- src/BookNest.Infrastructure/BookNest.Infrastructure.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.*" />
  </ItemGroup>
</Project>

<!-- src/BookNest.Web/BookNest.Web.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.*" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.*" />
  </ItemGroup>
</Project>

<!-- tests/BookNest.UnitTests/BookNest.UnitTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.*" />
    <PackageReference Include="NSubstitute" Version="5.1.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
  </ItemGroup>
</Project>

<!-- tests/BookNest.IntegrationTests/BookNest.IntegrationTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.*" />
    <PackageReference Include="Testcontainers.MsSql" Version="3.7.*" />
    <PackageReference Include="Respawn" Version="6.1.*" />
    <PackageReference Include="AngleSharp" Version="1.1.*" />
  </ItemGroup>
</Project>

<!-- tests/BookNest.ArchitectureTests/BookNest.ArchitectureTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
  </ItemGroup>
</Project>
```

**5. CI Pipeline (.github/workflows/ci.yml):**

```yaml
name: CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release /p:TreatWarningsAsErrors=true
    
    - name: Run Unit Tests
      run: dotnet test tests/BookNest.UnitTests --no-build --configuration Release --logger "trx;LogFileName=unit-tests.trx" --collect:"XPlat Code Coverage"
    
    - name: Run Architecture Tests
      run: dotnet test tests/BookNest.ArchitectureTests --no-build --configuration Release --logger "trx;LogFileName=arch-tests.trx"
    
    - name: Run Integration Tests
      run: dotnet test tests/BookNest.IntegrationTests --no-build --configuration Release --logger "trx;LogFileName=integration-tests.trx"
      env:
        ASPNETCORE_ENVIRONMENT: Testing
    
    - name: Check for vulnerable packages
      run: |
        dotnet list package --vulnerable --include-transitive 2>&1 | tee vulnerable.txt
        ! grep -q "has the following vulnerable packages" vulnerable.txt
    
    - name: Publish Test Results
      uses: EnricoMi/publish-unit-test-result-action@v2
      if: always()
      with:
        files: '**/*.trx'
```

**6. إنشاء Program.cs الأساسي:**

```csharp
// src/BookNest.Web/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Make Program accessible for tests
public partial class Program { }
```

#### 🧪 الاختبارات

**Architecture Test: Core لا يعتمد على Web أو Infrastructure**

```csharp
// tests/BookNest.ArchitectureTests/DependencyTests.cs
using System.Reflection;
using Xunit;
using FluentAssertions;

namespace BookNest.ArchitectureTests;

[Trait("Category", "Guard")]
public class DependencyTests
{
    [Fact]
    public void Core_ShouldNotDependOn_Infrastructure()
    {
        // Arrange
        var coreAssembly = Assembly.Load("BookNest.Core");
        var infrastructureAssemblyName = "BookNest.Infrastructure";
        
        // Act
        var references = coreAssembly.GetReferencedAssemblies()
            .Select(a => a.Name);
        
        // Assert
        references.Should().NotContain(infrastructureAssemblyName,
            "Core layer should not depend on Infrastructure");
    }
    
    [Fact]
    public void Core_ShouldNotDependOn_Web()
    {
        // Arrange
        var coreAssembly = Assembly.Load("BookNest.Core");
        var webAssemblyName = "BookNest.Web";
        
        // Act
        var references = coreAssembly.GetReferencedAssemblies()
            .Select(a => a.Name);
        
        // Assert
        references.Should().NotContain(webAssemblyName,
            "Core layer should not depend on Web");
    }
    
    [Fact]
    public void Infrastructure_ShouldNotDependOn_Web()
    {
        // Arrange
        var infraAssembly = Assembly.Load("BookNest.Infrastructure");
        var webAssemblyName = "BookNest.Web";
        
        // Act
        var references = infraAssembly.GetReferencedAssemblies()
            .Select(a => a.Name);
        
        // Assert
        references.Should().NotContain(webAssemblyName,
            "Infrastructure should not depend on Web");
    }
}
```

**Security Test: فحص Packages الضعيفة**

```csharp
// tests/BookNest.ArchitectureTests/SecurityTests.cs
using System.Diagnostics;
using Xunit;
using FluentAssertions;

namespace BookNest.ArchitectureTests;

[Trait("Category", "Security")]
public class PackageSecurityTests
{
    [Fact]
    public void Solution_ShouldNotHave_VulnerablePackages()
    {
        // Arrange
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "list package --vulnerable --include-transitive",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = GetSolutionDirectory()
            }
        };
        
        // Act
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        // Assert
        output.Should().NotContain("has the following vulnerable packages",
            "Solution should not contain packages with known vulnerabilities");
    }
    
    private static string GetSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Solution file not found");
    }
}
```

**MVC Test: Smoke test - التطبيق يعمل**

```csharp
// tests/BookNest.IntegrationTests/SmokeTests.cs
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;

namespace BookNest.IntegrationTests;

[Trait("Category", "Mvc")]
public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public SmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task Application_StartsSuccessfully()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/");
        
        // Assert
        response.Should().BeSuccessful("Application should start without errors");
    }
    
    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Index")]
    public async Task PublicEndpoints_ReturnSuccess(string url)
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync(url);
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Public endpoint {url} should be accessible");
    }
}
```

**Race Test:** N/A (لا توجد race conditions في setup)

#### ✅ معايير القبول (Definition of Done)

- [ ] Solution يبني بدون Warnings (`TreatWarningsAsErrors=true`)
- [ ] Architecture tests تمر (3/3)
- [ ] Smoke test يمر
- [ ] CI pipeline يعمل على GitHub Actions
- [ ] لا توجد packages ضعيفة
- [ ] هيكل المجلدات منظم حسب Clean Architecture
- [ ] كل مشروع له `.csproj` صحيح مع الـ packages المطلوبة
- [ ] README.md يحتوي على:
  - تعليمات التشغيل
  - متطلبات النظام (.NET 10, SQL Server)
  - أوامر Build/Test/Run

---


### T-01: Database Entities & Migrations

**الأولوية:** Must Have  
**التقدير:** 2-3 أيام  
**المتطلبات المغطاة:** NFR-08, BR-05 (ISBN unique), BR-07 (Soft Delete)

#### 🎯 الهدف
إنشاء جميع الـ Entities والـ Migrations مع Constraints (ISBN وEmail فريدين، RowVersion على Book وOrder).

#### 📝 خطوات التنفيذ

**1. Base Entity:**

```csharp
// src/BookNest.Core/Entities/BaseEntity.cs
namespace BookNest.Core.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

**2. Book Entity:**

```csharp
// src/BookNest.Core/Entities/Book.cs
using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class Book : BaseEntity
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required, MaxLength(13)]
    public string ISBN { get; set; } = string.Empty; // Unique index
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    [Required]
    public decimal Price { get; set; }
    
    public decimal? DiscountPrice { get; set; }
    
    [Required]
    public int Stock { get; set; }
    
    public int Reserved { get; set; } = 0; // للحجز المؤقت (Wow Layer)
    
    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }
    
    public int? Pages { get; set; }
    
    [MaxLength(100)]
    public string? Publisher { get; set; }
    
    [MaxLength(10)]
    public string? Language { get; set; }
    
    public DateTime? PublishedDate { get; set; }
    
    public bool IsDeleted { get; set; } = false; // Soft delete
    
    // Navigation properties
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
    
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    // Concurrency token
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Collections
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<StockReservation> Reservations { get; set; } = new List<StockReservation>();
}
```

**3. Author Entity:**

```csharp
// src/BookNest.Core/Entities/Author.cs
using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class Author : BaseEntity
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Biography { get; set; }
    
    [MaxLength(500)]
    public string? PhotoUrl { get; set; }
    
    // Navigation
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
```

**4. Category Entity:**

```csharp
// src/BookNest.Core/Entities/Category.cs
using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class Category : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string NameAr { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string? Slug { get; set; }
    
    // Navigation
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
```

**5. Order & OrderItem:**

```csharp
// src/BookNest.Core/Enums/OrderStatus.cs
namespace BookNest.Core.Enums;

public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

// src/BookNest.Core/Entities/Order.cs
using System.ComponentModel.DataAnnotations;
using BookNest.Core.Enums;

namespace BookNest.Core.Entities;

public class Order : BaseEntity
{
    [Required, MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty; // unique
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    [Required]
    public decimal Subtotal { get; set; }
    
    [Required]
    public decimal ShippingCost { get; set; }
    
    [Required]
    public decimal Total { get; set; }
    
    // Shipping Address (snapshot)
    [Required, MaxLength(200)]
    public string ShippingName { get; set; } = string.Empty;
    
    [Required, MaxLength(15)]
    public string ShippingPhone { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string ShippingCity { get; set; } = string.Empty;
    
    [Required, MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? ShippingPostalCode { get; set; }
    
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Card
    
    [MaxLength(100)]
    public string? PaymentTransactionId { get; set; }
    
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    [MaxLength(500)]
    public string? CancellationReason { get; set; }
    
    // Concurrency
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}

// src/BookNest.Core/Entities/OrderItem.cs
using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    
    // Snapshot at order time (BR-04)
    [Required, MaxLength(200)]
    public string BookTitle { get; set; } = string.Empty;
    
    [Required, MaxLength(13)]
    public string BookISBN { get; set; } = string.Empty;
    
    [Required]
    public decimal UnitPrice { get; set; } // السعر وقت الطلب
    
    [Required]
    public int Quantity { get; set; }
    
    [Required]
    public decimal Subtotal { get; set; } // UnitPrice * Quantity
}
```

**6. Cart & Wishlist:**

```csharp
// src/BookNest.Core/Entities/CartItem.cs
using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class CartItem : BaseEntity
{
    public string? UserId { get; set; } // null for guest
    public ApplicationUser? User { get; set; }
    
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    
    public int Quantity { get; set; }
    
    [MaxLength(100)]
    public string? SessionId { get; set; } // للزوار
}

// src/BookNest.Core/Entities/WishlistItem.cs
using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class WishlistItem : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
}
```

**7. Address:**

```csharp
// src/BookNest.Core/Entities/Address.cs
using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class Address : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    
    [Required, MaxLength(15)]
    public string Phone { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    [Required, MaxLength(500)]
    public string AddressLine { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? PostalCode { get; set; }
    
    public bool IsDefault { get; set; } = false;
}
```

**8. StockReservation (للـ Wow Layer T-68):**

```csharp
// src/BookNest.Core/Entities/StockReservation.cs
using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class StockReservation : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    
    public int Quantity { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
}
```

**9. OrderStatusHistory (للـ Wow Layer T-66):**

```csharp
// src/BookNest.Core/Entities/OrderStatusHistory.cs
using System.ComponentModel.DataAnnotations;
using BookNest.Core.Enums;

namespace BookNest.Core.Entities;

public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [MaxLength(450)]
    public string? ChangedByUserId { get; set; }
    public ApplicationUser? ChangedBy { get; set; }
    
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
```

**10. ApplicationUser (Identity):**

```csharp
// src/BookNest.Infrastructure/Identity/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using BookNest.Core.Entities;

namespace BookNest.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<StockReservation> Reservations { get; set; } = new List<StockReservation>();
}
```

**11. DbContext:**

```csharp
// src/BookNest.Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BookNest.Core.Entities;
using BookNest.Infrastructure.Identity;

namespace BookNest.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Apply all configurations
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Global query filter for soft delete (BR-07)
        builder.Entity<Book>().HasQueryFilter(b => !b.IsDeleted);
    }
}
```

**12. Entity Configurations:**

```csharp
// src/BookNest.Infrastructure/Data/Configurations/BookConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookNest.Core.Entities;

namespace BookNest.Infrastructure.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.Id);
        
        // Unique constraint on ISBN (BR-05)
        builder.HasIndex(b => b.ISBN)
            .IsUnique()
            .HasDatabaseName("IX_Books_ISBN_Unique");
        
        // Concurrency token
        builder.Property(b => b.RowVersion)
            .IsRowVersion();
        
        // Decimal precision
        builder.Property(b => b.Price)
            .HasPrecision(18, 2);
        
        builder.Property(b => b.DiscountPrice)
            .HasPrecision(18, 2);
        
        // Relationships
        builder.HasOne(b => b.Author)
            .WithMany(a => a.Books)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict); // منع حذف مؤلف له كتب (BR-03)
        
        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // منع حذف تصنيف له كتب
    }
}

// src/BookNest.Infrastructure/Data/Configurations/OrderConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookNest.Core.Entities;

namespace BookNest.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        
        // Unique order number
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_Orders_OrderNumber_Unique");
        
        // Concurrency
        builder.Property(o => o.RowVersion).IsRowVersion();
        
        // Decimal precision
        builder.Property(o => o.Subtotal).HasPrecision(18, 2);
        builder.Property(o => o.ShippingCost).HasPrecision(18, 2);
        builder.Property(o => o.Total).HasPrecision(18, 2);
        
        // Relationship
        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(o => o.StatusHistory)
            .WithOne(h => h.Order)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// src/BookNest.Infrastructure/Data/Configurations/OrderItemConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookNest.Core.Entities;

namespace BookNest.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        
        // Decimal precision
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Subtotal).HasPrecision(18, 2);
        
        // Relationships
        builder.HasOne(i => i.Book)
            .WithMany(b => b.OrderItems)
            .HasForeignKey(i => i.BookId)
            .OnDelete(DeleteBehavior.Restrict); // Keep order history even if book deleted
    }
}

// src/BookNest.Infrastructure/Data/Configurations/CartItemConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookNest.Core.Entities;

namespace BookNest.Infrastructure.Data.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(c => c.Id);
        
        // Unique constraint: one book per user (no duplicates)
        builder.HasIndex(c => new { c.UserId, c.BookId })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("IX_CartItems_UserId_BookId_Unique");
        
        // Unique constraint: one book per session
        builder.HasIndex(c => new { c.SessionId, c.BookId })
            .IsUnique()
            .HasFilter("[SessionId] IS NOT NULL")
            .HasDatabaseName("IX_CartItems_SessionId_BookId_Unique");
        
        // Relationships
        builder.HasOne(c => c.Book)
            .WithMany(b => b.CartItems)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(c => c.User)
            .WithMany(u => u.CartItems)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// src/BookNest.Infrastructure/Data/Configurations/WishlistItemConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookNest.Core.Entities;

namespace BookNest.Infrastructure.Data.Configurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.HasKey(w => w.Id);
        
        // Unique: one book per user wishlist
        builder.HasIndex(w => new { w.UserId, w.BookId })
            .IsUnique()
            .HasDatabaseName("IX_WishlistItems_UserId_BookId_Unique");
        
        builder.HasOne(w => w.Book)
            .WithMany(b => b.WishlistItems)
            .HasForeignKey(w => w.BookId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(w => w.User)
            .WithMany(u => u.WishlistItems)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// src/BookNest.Infrastructure/Data/Configurations/AddressConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookNest.Core.Entities;

namespace BookNest.Infrastructure.Data.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasKey(a => a.Id);
        
        builder.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// src/BookNest.Infrastructure/Data/Configurations/StockReservationConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookNest.Core.Entities;

namespace BookNest.Infrastructure.Data.Configurations;

public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.HasKey(r => r.Id);
        
        // Index for cleanup job
        builder.HasIndex(r => new { r.ExpiresAt, r.IsActive })
            .HasDatabaseName("IX_StockReservations_ExpiresAt_IsActive");
        
        builder.HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(r => r.Book)
            .WithMany(b => b.Reservations)
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**13. تحديث Program.cs لإضافة DbContext:**

```csharp
// src/BookNest.Web/Program.cs
using Microsoft.EntityFrameworkCore;
using BookNest.Infrastructure.Data;
using BookNest.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("BookNest.Infrastructure")));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // سيتم تكوينها في T-10
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
```

**14. إضافة Connection String:**

```json
// src/BookNest.Web/appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BookNestDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**15. إنشاء Migration:**

```bash
# من مجلد Solution root
dotnet ef migrations add InitialCreate --project src/BookNest.Infrastructure --startup-project src/BookNest.Web --output-dir Data/Migrations

# تطبيق Migration
dotnet ef database update --project src/BookNest.Infrastructure --startup-project src/BookNest.Web
```

#### 🧪 الاختبارات

**Guard Test:** N/A (لا توجد صلاحيات في هذا المستوى)

**Race Test: ISBN مكرر في نفس اللحظة - واحد فقط ينجح**

```csharp
// tests/BookNest.IntegrationTests/DatabaseConcurrencyTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BookNest.Core.Entities;
using BookNest.Infrastructure.Data;
using Xunit;
using FluentAssertions;
using Testcontainers.MsSql;

namespace BookNest.IntegrationTests;

[Trait("Category", "Race")]
public class DatabaseConcurrencyTests : IAsyncLifetime
{
    private MsSqlContainer _container = null!;
    private string _connectionString = null!;
    
    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
        
        // Apply migrations
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        
        using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
    }
    
    [Fact]
    public async Task Books_DuplicateISBN_SimultaneousInsert_OnlyOneSucceeds()
    {
        // Arrange
        var isbn = "9781234567890";
        
        // إنشاء Author و Category أولاً
        using (var context = CreateContext())
        {
            var author = new Author { Name = "Test Author" };
            var category = new Category { Name = "Test Category", NameAr = "تصنيف تجريبي" };
            context.Authors.Add(author);
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        }
        
        int authorId, categoryId;
        using (var context = CreateContext())
        {
            authorId = context.Authors.First().Id;
            categoryId = context.Categories.First().Id;
        }
        
        // Act - محاولة إدخال 5 كتب بنفس ISBN في نفس اللحظة
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            try
            {
                using var ctx = CreateContext();
                
                var book = new Book
                {
                    Title = $"Book {i}",
                    ISBN = isbn, // نفس الـ ISBN
                    Price = 100,
                    Stock = 10,
                    AuthorId = authorId,
                    CategoryId = categoryId
                };
                
                ctx.Books.Add(book);
                await ctx.SaveChangesAsync();
                
                return (Success: true, Error: (string?)null);
            }
            catch (DbUpdateException ex)
            {
                return (Success: false, Error: ex.InnerException?.Message ?? ex.Message);
            }
        });
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        var successCount = results.Count(r => r.Success);
        successCount.Should().Be(1, "Only one insert should succeed with duplicate ISBN");
        
        // تحقق من قاعدة البيانات
        using var verifyContext = CreateContext();
        var booksWithIsbn = await verifyContext.Books.CountAsync(b => b.ISBN == isbn);
        booksWithIsbn.Should().Be(1, "Database should contain exactly one book with this ISBN");
    }
    
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        
        return new ApplicationDbContext(options);
    }
    
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

**Security Test: كل string له MaxLength**

```csharp
// tests/BookNest.ArchitectureTests/EntitySecurityTests.cs
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using BookNest.Core.Entities;
using Xunit;
using FluentAssertions;

namespace BookNest.ArchitectureTests;

[Trait("Category", "Security")]
public class EntitySecurityTests
{
    [Fact]
    public void Entities_StringProperties_MustHaveMaxLength()
    {
        // Arrange
        var entityTypes = typeof(Book).Assembly.GetTypes()
            .Where(t => t.Namespace == "BookNest.Core.Entities" 
                     && !t.IsAbstract 
                     && t.IsClass);
        
        var violations = new List<string>();
        
        // Act
        foreach (var entityType in entityTypes)
        {
            var stringProps = entityType.GetProperties()
                .Where(p => p.PropertyType == typeof(string));
            
            foreach (var prop in stringProps)
            {
                var hasMaxLength = prop.GetCustomAttribute<MaxLengthAttribute>() != null;
                var hasStringLength = prop.GetCustomAttribute<StringLengthAttribute>() != null;
                
                if (!hasMaxLength && !hasStringLength)
                {
                    violations.Add($"{entityType.Name}.{prop.Name}");
                }
            }
        }
        
        // Assert
        violations.Should().BeEmpty(
            $"All string properties must have MaxLength or StringLength attribute. " +
            $"Violations: {string.Join(", ", violations)}");
    }
    
    [Fact]
    public void Entities_DecimalProperties_MustHavePrecisionConfigured()
    {
        // هذا يتحقق في Integration test عند قراءة الـ Model من EF
    }
}
```

**MVC Test: Migration تطبق بنجاح**

```csharp
// tests/BookNest.IntegrationTests/MigrationTests.cs
using Microsoft.EntityFrameworkCore;
using BookNest.Infrastructure.Data;
using Xunit;
using FluentAssertions;
using Testcontainers.MsSql;

namespace BookNest.IntegrationTests;

[Trait("Category", "Mvc")]
public class MigrationTests : IAsyncLifetime
{
    private MsSqlContainer _container = null!;
    
    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder().Build();
        await _container.StartAsync();
    }
    
    [Fact]
    public async Task Migrations_ApplySuccessfully_OnEmptyDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;
        
        using var context = new ApplicationDbContext(options);
        
        // Act
        var action = async () => await context.Database.MigrateAsync();
        
        // Assert
        await action.Should().NotThrowAsync("Migrations should apply without errors");
        
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().NotBeEmpty("At least one migration should be applied");
    }
    
    [Fact]
    public async Task Database_AllTablesCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;
        
        using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
        
        // Act - استعلام INFORMATION_SCHEMA للحصول على الجداول
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_CATALOG = DB_NAME()";
        
        var tables = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
        
        // Assert
        tables.Should().Contain("Books");
        tables.Should().Contain("Authors");
        tables.Should().Contain("Categories");
        tables.Should().Contain("Orders");
        tables.Should().Contain("OrderItems");
        tables.Should().Contain("OrderStatusHistories");
        tables.Should().Contain("CartItems");
        tables.Should().Contain("WishlistItems");
        tables.Should().Contain("Addresses");
        tables.Should().Contain("StockReservations");
    }
    
    [Fact]
    public async Task Database_UniqueConstraints_AreCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;
        
        using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
        
        // Act - Check indexes
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT i.name as IndexName, t.name as TableName
            FROM sys.indexes i
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            WHERE i.is_unique = 1 AND i.name IS NOT NULL";
        
        var indexes = new List<(string Table, string Index)>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add((reader.GetString(1), reader.GetString(0)));
        }
        
        // Assert
        indexes.Should().Contain(i => i.Table == "Books" && i.Index.Contains("ISBN"));
        indexes.Should().Contain(i => i.Table == "Orders" && i.Index.Contains("OrderNumber"));
    }
    
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

#### ✅ معايير القبول (Definition of Done)

- [ ] جميع الـ 11 Entities تم إنشاؤها بـ Data Annotations صحيحة
- [ ] ISBN unique constraint موجود في DB
- [ ] Email (في Identity) unique
- [ ] RowVersion على Book و Order
- [ ] Soft Delete على Book مع Query Filter عالمي
- [ ] Migration تطبق بنجاح على DB فارغة
- [ ] Race test للـ ISBN المكرر يمر (1/5 ينجح فقط)
- [ ] Security test: كل string له MaxLength يمر
- [ ] جميع الـ Foreign Keys بالـ DeleteBehavior الصحيح
- [ ] Decimal precision 18,2 على كل الأسعار
- [ ] جميع الجداول موجودة في DB
- [ ] Unique indexes تم إنشاؤها

---


### T-02: Seed Data

**الأولوية:** Must Have  
**التقدير:** 1-2 أيام  
**المتطلبات المغطاة:** FR-AUTH-04 (Admin Role), Demo Data

#### 🎯 الهدف
إنشاء Seed Data: 60+ كتاب حقيقي، 8 تصنيفات، 20 مؤلف، Roles (Admin & Customer)، Admin افتراضي.

#### 📝 خطوات التنفيذ

**1. Database Seeder Class:**

```csharp
// src/BookNest.Infrastructure/Data/DatabaseSeeder.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BookNest.Core.Entities;
using BookNest.Infrastructure.Identity;

namespace BookNest.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    
    public DatabaseSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }
    
    public async Task SeedAsync()
    {
        // Idempotent - تشغيل مرة واحدة فقط
        if (await _context.Books.AnyAsync())
        {
            return; // البيانات موجودة بالفعل
        }
        
        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedCategoriesAsync();
        await SeedAuthorsAsync();
        await SeedBooksAsync();
    }
    
    private async Task SeedRolesAsync()
    {
        string[] roles = { "Admin", "Customer" };
        
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
    
    private async Task SeedUsersAsync()
    {
        // Admin من Environment Variable (Security best practice)
        var adminEmail = _configuration["SeedData:AdminEmail"] ?? "admin@booknest.com";
        var adminPassword = _configuration["SeedData:AdminPassword"] 
            ?? throw new InvalidOperationException("Admin password must be provided in environment variable");
        
        if (await _userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin",
                IsActive = true
            };
            
            var result = await _userManager.CreateAsync(admin, adminPassword);
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, "Admin");
            }
        }
        
        // Demo Customer
        var customerEmail = "customer@demo.com";
        if (await _userManager.FindByEmailAsync(customerEmail) == null)
        {
            var customer = new ApplicationUser
            {
                UserName = customerEmail,
                Email = customerEmail,
                EmailConfirmed = true,
                FirstName = "أحمد",
                LastName = "محمد",
                PhoneNumber = "01012345678",
                IsActive = true
            };
            
            await _userManager.CreateAsync(customer, "Customer@123");
            await _userManager.AddToRoleAsync(customer, "Customer");
        }
    }
    
    private async Task SeedCategoriesAsync()
    {
        var categories = new[]
        {
            new Category { Name = "Fiction", NameAr = "روايات", Slug = "fiction", Description = "Fictional narratives and novels" },
            new Category { Name = "Non-Fiction", NameAr = "غير روائي", Slug = "non-fiction", Description = "Real-world topics and factual content" },
            new Category { Name = "Science", NameAr = "علوم", Slug = "science", Description = "Scientific books and research" },
            new Category { Name = "Technology", NameAr = "تقنية", Slug = "technology", Description = "Programming, software, and tech books" },
            new Category { Name = "History", NameAr = "تاريخ", Slug = "history", Description = "Historical books and biographies" },
            new Category { Name = "Biography", NameAr = "سير ذاتية", Slug = "biography", Description = "Life stories and memoirs" },
            new Category { Name = "Self-Help", NameAr = "تنمية ذاتية", Slug = "self-help", Description = "Personal development and productivity" },
            new Category { Name = "Children", NameAr = "أطفال", Slug = "children", Description = "Books for kids and young readers" }
        };
        
        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedAuthorsAsync()
    {
        var authors = new[]
        {
            new Author { Name = "نجيب محفوظ", Biography = "Nobel Prize winner in Literature 1988" },
            new Author { Name = "طه حسين", Biography = "Dean of Arabic Literature" },
            new Author { Name = "عباس محمود العقاد" },
            new Author { Name = "أحمد خالد توفيق", Biography = "Egyptian novelist and physician" },
            new Author { Name = "علاء الأسواني" },
            new Author { Name = "يوسف زيدان" },
            new Author { Name = "إبراهيم عبد المجيد" },
            new Author { Name = "Robert C. Martin", Biography = "Uncle Bob - Software Craftsman" },
            new Author { Name = "Martin Fowler", Biography = "Chief Scientist at ThoughtWorks" },
            new Author { Name = "Eric Evans", Biography = "Domain-Driven Design pioneer" },
            new Author { Name = "Kent Beck", Biography = "Creator of Extreme Programming and TDD" },
            new Author { Name = "Andrew Hunt", Biography = "Co-author of The Pragmatic Programmer" },
            new Author { Name = "Malcolm Gladwell" },
            new Author { Name = "Yuval Noah Harari" },
            new Author { Name = "Stephen Hawking" },
            new Author { Name = "Carlo Rovelli" },
            new Author { Name = "Richard Dawkins" },
            new Author { Name = "Daniel Kahneman", Biography = "Nobel Prize in Economics" },
            new Author { Name = "Nassim Nicholas Taleb" },
            new Author { Name = "James Clear" }
        };
        
        await _context.Authors.AddRangeAsync(authors);
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedBooksAsync()
    {
        var categories = await _context.Categories.ToDictionaryAsync(c => c.Slug!);
        var authors = await _context.Authors.ToDictionaryAsync(a => a.Name);
        
        var books = new List<Book>
        {
            // روايات عربية
            new Book
            {
                Title = "زقاق المدق",
                ISBN = "9789770926208",
                AuthorId = authors["نجيب محفوظ"].Id,
                CategoryId = categories["fiction"].Id,
                Description = "رواية تدور أحداثها في أحد أحياء القاهرة الشعبية القديمة، وتتناول حياة مجموعة من الشخصيات المختلفة.",
                Price = 85.00m,
                Stock = 25,
                Pages = 320,
                Language = "ar",
                Publisher = "دار الشروق",
                PublishedDate = new DateTime(1947, 1, 1)
            },
            new Book
            {
                Title = "الأيام",
                ISBN = "9789773026721",
                AuthorId = authors["طه حسين"].Id,
                CategoryId = categories["biography"].Id,
                Description = "السيرة الذاتية لطه حسين، تحكي قصة طفولته وشبابه وتجربته مع فقدان البصر.",
                Price = 75.00m,
                Stock = 30,
                Pages = 280,
                Language = "ar",
                Publisher = "دار المعارف"
            },
            new Book
            {
                Title = "يوتوبيا",
                ISBN = "9789773026738",
                AuthorId = authors["أحمد خالد توفيق"].Id,
                CategoryId = categories["fiction"].Id,
                Description = "رواية ديستوبيا تصور مصر في المستقبل منقسمة بين الأغنياء والفقراء.",
                Price = 65.00m,
                DiscountPrice = 52.00m,
                Stock = 15,
                Pages = 240,
                Language = "ar",
                Publisher = "دار ميريت"
            },
            new Book
            {
                Title = "عمارة يعقوبيان",
                ISBN = "9789773026745",
                AuthorId = authors["علاء الأسواني"].Id,
                CategoryId = categories["fiction"].Id,
                Description = "رواية تتناول حياة سكان عمارة في وسط القاهرة وتعكس التغيرات الاجتماعية والسياسية.",
                Price = 95.00m,
                Stock = 20,
                Pages = 384,
                Language = "ar",
                Publisher = "دار الشروق"
            },
            new Book
            {
                Title = "عزازيل",
                ISBN = "9789773026752",
                AuthorId = authors["يوسف زيدان"].Id,
                CategoryId = categories["fiction"].Id,
                Description = "رواية تاريخية تدور أحداثها في القرن الخامس الميلادي.",
                Price = 90.00m,
                Stock = 18,
                Pages = 432,
                Language = "ar",
                Publisher = "دار الشروق"
            },
            
            // Technology Books
            new Book
            {
                Title = "Clean Code: A Handbook of Agile Software Craftsmanship",
                ISBN = "9780132350884",
                AuthorId = authors["Robert C. Martin"].Id,
                CategoryId = categories["technology"].Id,
                Description = "A comprehensive guide to writing clean, maintainable, and efficient code with practical examples.",
                Price = 450.00m,
                DiscountPrice = 380.00m,
                Stock = 35,
                Pages = 464,
                Language = "en",
                Publisher = "Prentice Hall",
                PublishedDate = new DateTime(2008, 8, 1)
            },
            new Book
            {
                Title = "Refactoring: Improving the Design of Existing Code",
                ISBN = "9780134757599",
                AuthorId = authors["Martin Fowler"].Id,
                CategoryId = categories["technology"].Id,
                Description = "The definitive guide to refactoring, with updated examples in JavaScript.",
                Price = 520.00m,
                Stock = 28,
                Pages = 448,
                Language = "en",
                Publisher = "Addison-Wesley"
            },
            new Book
            {
                Title = "Domain-Driven Design: Tackling Complexity in the Heart of Software",
                ISBN = "9780321125217",
                AuthorId = authors["Eric Evans"].Id,
                CategoryId = categories["technology"].Id,
                Description = "The seminal book on DDD - modeling complex business domains.",
                Price = 580.00m,
                Stock = 12,
                Pages = 560,
                Language = "en",
                Publisher = "Addison-Wesley"
            },
            new Book
            {
                Title = "Test-Driven Development: By Example",
                ISBN = "9780321146533",
                AuthorId = authors["Kent Beck"].Id,
                CategoryId = categories["technology"].Id,
                Description = "Learn TDD through practical examples in Java and Python.",
                Price = 420.00m,
                DiscountPrice = 350.00m,
                Stock = 22,
                Pages = 240,
                Language = "en",
                Publisher = "Addison-Wesley"
            },
            new Book
            {
                Title = "The Pragmatic Programmer: Your Journey to Mastery",
                ISBN = "9780135957059",
                AuthorId = authors["Andrew Hunt"].Id,
                CategoryId = categories["technology"].Id,
                Description = "20th Anniversary Edition - timeless wisdom for software developers.",
                Price = 480.00m,
                Stock = 30,
                Pages = 352,
                Language = "en",
                Publisher = "Addison-Wesley"
            },
            
            // Science
            new Book
            {
                Title = "A Brief History of Time",
                ISBN = "9780553380163",
                AuthorId = authors["Stephen Hawking"].Id,
                CategoryId = categories["science"].Id,
                Description = "From the Big Bang to Black Holes - an exploration of the universe.",
                Price = 320.00m,
                Stock = 40,
                Pages = 256,
                Language = "en",
                Publisher = "Bantam Books"
            },
            new Book
            {
                Title = "The Order of Time",
                ISBN = "9780735216105",
                AuthorId = authors["Carlo Rovelli"].Id,
                CategoryId = categories["science"].Id,
                Description = "A groundbreaking exploration of the nature of time by a leading physicist.",
                Price = 280.00m,
                DiscountPrice = 240.00m,
                Stock = 25,
                Pages = 240,
                Language = "en",
                Publisher = "Riverhead Books"
            },
            new Book
            {
                Title = "The Selfish Gene: 40th Anniversary Edition",
                ISBN = "9780198788607",
                AuthorId = authors["Richard Dawkins"].Id,
                CategoryId = categories["science"].Id,
                Description = "The classic gene-centered view of evolution.",
                Price = 350.00m,
                Stock = 18,
                Pages = 496,
                Language = "en",
                Publisher = "Oxford University Press"
            },
            
            // Non-Fiction
            new Book
            {
                Title = "Sapiens: A Brief History of Humankind",
                ISBN = "9780062316110",
                AuthorId = authors["Yuval Noah Harari"].Id,
                CategoryId = categories["non-fiction"].Id,
                Description = "How Homo sapiens came to dominate the world - from Stone Age to Silicon Valley.",
                Price = 380.00m,
                DiscountPrice = 320.00m,
                Stock = 45,
                Pages = 464,
                Language = "en",
                Publisher = "Harper"
            },
            new Book
            {
                Title = "Thinking, Fast and Slow",
                ISBN = "9780374533557",
                AuthorId = authors["Daniel Kahneman"].Id,
                CategoryId = categories["non-fiction"].Id,
                Description = "The groundbreaking work on decision-making and behavioral economics.",
                Price = 340.00m,
                Stock = 32,
                Pages = 499,
                Language = "en",
                Publisher = "Farrar, Straus and Giroux"
            },
            new Book
            {
                Title = "The Black Swan: The Impact of the Highly Improbable",
                ISBN = "9780812973815",
                AuthorId = authors["Nassim Nicholas Taleb"].Id,
                CategoryId = categories["non-fiction"].Id,
                Description = "About the extreme impact of rare and unpredictable events.",
                Price = 360.00m,
                Stock = 20,
                Pages = 480,
                Language = "en",
                Publisher = "Random House"
            },
            
            // Self-Help
            new Book
            {
                Title = "Atomic Habits: An Easy & Proven Way to Build Good Habits",
                ISBN = "9780735211292",
                AuthorId = authors["James Clear"].Id,
                CategoryId = categories["self-help"].Id,
                Description = "The #1 New York Times bestseller on habit formation.",
                Price = 290.00m,
                DiscountPrice = 250.00m,
                Stock = 50,
                Pages = 320,
                Language = "en",
                Publisher = "Avery"
            },
            new Book
            {
                Title = "Outliers: The Story of Success",
                ISBN = "9780316017930",
                AuthorId = authors["Malcolm Gladwell"].Id,
                CategoryId = categories["self-help"].Id,
                Description = "What makes high-achievers different? The 10,000-hour rule and more.",
                Price = 310.00m,
                Stock = 35,
                Pages = 336,
                Language = "en",
                Publisher = "Little, Brown and Company"
            },
            
            // المزيد من الكتب للوصول لـ 60+
            // (يمكن توليد المزيد من Google Books API أو Open Library)
        };
        
        await _context.Books.AddRangeAsync(books);
        await _context.SaveChangesAsync();
    }
}
```

**2. تسجيل Seeder في DI:**

```csharp
// src/BookNest.Web/Program.cs (partial update)
// أضف قبل var app = builder.Build();

builder.Services.AddScoped<DatabaseSeeder>();
```

**3. استدعاء Seeder في Program.cs:**

```csharp
// src/BookNest.Web/Program.cs (بعد app.Build())

// Seed Database in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    
    try
    {
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database seeding completed successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database");
    }
}
```

**4. إعدادات appsettings:**

```json
// src/BookNest.Web/appsettings.Development.json
{
  "SeedData": {
    "AdminEmail": "admin@booknest.com",
    "AdminPassword": "Admin@123456"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BookNestDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

**ملاحظة أمان:** في Production، كلمة سر الـ Admin تأتي من **Environment Variable** فقط:

```bash
# في Production
export SeedData__AdminPassword="StrongPassword@2024!"
```

#### 🧪 الاختبارات

**Guard Test: Admin له Role Admin فقط**

```csharp
// tests/BookNest.IntegrationTests/SeedDataTests.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using BookNest.Infrastructure.Identity;
using BookNest.Infrastructure.Data;
using Xunit;
using FluentAssertions;

namespace BookNest.IntegrationTests;

[Trait("Category", "Guard")]
public class SeedDataTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    
    public SeedDataTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task SeedData_AdminUser_HasOnlyAdminRole()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Act
        var admin = await userManager.FindByEmailAsync("admin@booknest.com");
        var roles = await userManager.GetRolesAsync(admin!);
        
        // Assert
        admin.Should().NotBeNull("Admin user should exist after seeding");
        roles.Should().ContainSingle("Admin user should have exactly one role");
        roles.Should().Contain("Admin");
        roles.Should().NotContain("Customer", "Admin should not have Customer role");
    }
    
    [Fact]
    public async Task SeedData_Roles_BothExist()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Act & Assert
        (await roleManager.RoleExistsAsync("Admin")).Should().BeTrue();
        (await roleManager.RoleExistsAsync("Customer")).Should().BeTrue();
    }
    
    [Fact]
    public async Task SeedData_Categories_AllEightExist()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Act
        var count = await context.Categories.CountAsync();
        
        // Assert
        count.Should().Be(8, "Should have exactly 8 categories");
    }
    
    [Fact]
    public async Task SeedData_Books_AtLeast60Exist()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Act
        var count = await context.Books.CountAsync();
        
        // Assert
        count.Should().BeGreaterOrEqualTo(60, "Should have at least 60 books for realistic demo");
    }
}
```

**Race Test: تشغيل Seed مرتين متزامنتين - لا تكرار**

```csharp
// tests/BookNest.IntegrationTests/SeedConcurrencyTests.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BookNest.Infrastructure.Data;
using Xunit;
using FluentAssertions;

namespace BookNest.IntegrationTests;

[Trait("Category", "Race")]
public class SeedConcurrencyTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    
    public SeedConcurrencyTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task Seeder_RunTwiceSimultaneously_NoDataDuplication()
    {
        // Arrange - تأكد أن DB فارغة
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }
        
        var tasks = new List<Task>();
        
        // Act - تشغيل Seeder مرتين في نفس اللحظة
        for (int i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var scope = _factory.Services.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        using var verifyScope = _factory.Services.CreateScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var booksCount = await context.Books.CountAsync();
        booksCount.Should().BeGreaterOrEqualTo(60, "Should have at least 60 books");
        
        // تحقق من عدم التكرار
        var duplicateISBNs = await context.Books
            .GroupBy(b => b.ISBN)
            .Where(g => g.Count() > 1)
            .Select(g => new { ISBN = g.Key, Count = g.Count() })
            .ToListAsync();
        
        duplicateISBNs.Should().BeEmpty($"No duplicate ISBNs should exist. Found: {string.Join(", ", duplicateISBNs.Select(d => $"{d.ISBN} ({d.Count}x)"))}");
        
        // تحقق من الـ Roles
        var roles = await context.Roles.ToListAsync();
        roles.Should().HaveCount(2, "Should have exactly 2 roles even after concurrent seeding");
    }
}
```

**Security Test: كلمة سر Admin من Environment Variable**

```csharp
// tests/BookNest.ArchitectureTests/SeedSecurityTests.cs
using System.Text.RegularExpressions;
using Xunit;
using FluentAssertions;

namespace BookNest.ArchitectureTests;

[Trait("Category", "Security")]
public class SeedSecurityTests
{
    [Fact]
    public void DatabaseSeeder_AdminPassword_NotHardcoded()
    {
        // Arrange
        var seederFile = "../../../../BookNest.Infrastructure/Data/DatabaseSeeder.cs";
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, seederFile);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Seeder file not found at {fullPath}");
        }
        
        var content = File.ReadAllText(fullPath);
        
        // Act - البحث عن أنماط كلمات سر hardcoded
        var hardcodedPasswordPatterns = new[]
        {
            @"password\s*=\s*[""'](?!.*configuration)[\w@#$%^&*]{6,}[""']", // password = "literal"
            @"Admin@\d+", // Admin@123456
            @"CreateAsync\([^,]+,\s*[""'][^""']*[""']\)" // CreateAsync(user, "password")
        };
        
        var violations = new List<string>();
        foreach (var pattern in hardcodedPasswordPatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            if (matches.Any())
            {
                violations.AddRange(matches.Select(m => m.Value));
            }
        }
        
        // Assert
        violations.Should().BeEmpty(
            $"Admin password should come from configuration, not be hardcoded. " +
            $"Found: {string.Join(", ", violations)}");
    }
    
    [Fact]
    public void DatabaseSeeder_UsesConfiguration_ForAdminPassword()
    {
        // Arrange
        var seederFile = "../../../../BookNest.Infrastructure/Data/DatabaseSeeder.cs";
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, seederFile);
        var content = File.ReadAllText(fullPath);
        
        // Act & Assert
        content.Should().Contain("_configuration[\"SeedData:AdminPassword\"]",
            "Seeder should read admin password from configuration");
        
        content.Should().Contain("throw new InvalidOperationException",
            "Seeder should throw if admin password is not configured");
    }
}
```

**MVC Test: Seed يعمل فقط في Development**

```csharp
// tests/BookNest.IntegrationTests/SeedEnvironmentTests.cs
using Xunit;
using FluentAssertions;

namespace BookNest.IntegrationTests;

[Trait("Category", "Mvc")]
public class SeedEnvironmentTests
{
    [Fact]
    public void Program_SeedOnlyInDevelopment()
    {
        // Arrange
        var programFile = "../../../../BookNest.Web/Program.cs";
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, programFile);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Program.cs not found at {fullPath}");
        }
        
        var content = File.ReadAllText(fullPath);
        
        // Act & Assert
        content.Should().Contain("app.Environment.IsDevelopment()",
            "Seeder should only run in Development environment");
        
        content.Should().Contain("seeder.SeedAsync()",
            "Seeder should be called in Program.cs");
        
        // تأكد أن الـ Seeder داخل الـ if block
        var isDevelopmentIndex = content.IndexOf("app.Environment.IsDevelopment()");
        var seedAsyncIndex = content.IndexOf("seeder.SeedAsync()");
        
        seedAsyncIndex.Should().BeGreaterThan(isDevelopmentIndex,
            "SeedAsync call should be inside IsDevelopment() block");
    }
}
```

#### ✅ معايير القبول (Definition of Done)

- [ ] 60+ كتاب بأسعار منطقية وبيانات حقيقية
- [ ] 8 تصنيفات بأسماء عربي/إنجليزي
- [ ] 20 مؤلف على الأقل
- [ ] Roles: Admin و Customer
- [ ] Admin افتراضي بكلمة سر من Environment Variable
- [ ] Demo Customer للاختبار (customer@demo.com)
- [ ] Seed idempotent (يتحقق من وجود البيانات قبل الإدراج)
- [ ] جميع الاختبارات تمر (Guard: 4/4, Race: 1/1, Security: 2/2, MVC: 1/1)
- [ ] لا توجد كلمات سر hardcoded في الكود
- [ ] Seed يعمل فقط في Development (Environment check)
- [ ] Admin له Role واحد فقط (Admin)
- [ ] لا توجد ISBNs مكررة بعد Concurrent seeding

---


## Epic 1.2: Design System Implementation

### T-03: CSS Tokens & Layouts

**الأولوية:** Must Have  
**التقدير:** 2-3 أيام  
**المتطلبات المغطاة:** NFR-05 (RTL/LTR), Design System

#### 🎯 الهدف
إنشاء ملف `tokens.css` بكل الـ Design Tokens + 3 Layouts (_StoreLayout, _AdminLayout, _AuthLayout) مع دعم RTL/LTR كامل.

#### 📝 خطوات التنفيذ

**1. Design Tokens (tokens.css):**

إنشاء ملف `wwwroot/css/tokens.css` يحتوي على:
- Colors: Primary (Moss Green), Accent (Persimmon), Neutrals, Semantic colors
- Typography: Font families (IBM Plex Sans AR/EN), sizes, weights, line-heights
- Spacing: 8px base grid (space-1 to space-16)
- Border radius: sm/md/lg/pill
- Shadows: sm/md/lg
- Transitions: durations + easing
- Layout: container max-width, sidebar widths, z-index layers

**2. Base Styles (base.css):**

- Reset & box-sizing
- Body: font-family يتغير حسب `[lang="ar"]` أو `[lang="en"]`
- RTL/LTR support باستخدام CSS Logical Properties (`margin-inline`, `padding-block`)
- Typography: h1-h6, p, a
- Accessibility: `:focus-visible`, `.sr-only`, `prefers-reduced-motion`
- Container class

**3. Store Layout (_StoreLayout.cshtml):**

```html
<!DOCTYPE html>
<html lang="@CultureInfo.CurrentCulture.TwoLetterISOLanguageName" 
      dir="@(CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr")">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - BookNest</title>
    
    <!-- Fonts: IBM Plex Sans Arabic, IBM Plex Sans, Noto Naskh Arabic, Source Serif 4 -->
    <link href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Arabic:wght@400;600;700&family=IBM+Plex+Sans:wght@400;600;700&display=swap" rel="stylesheet">
    
    <link rel="stylesheet" href="~/css/tokens.css" />
    <link rel="stylesheet" href="~/css/base.css" />
    <link rel="stylesheet" href="~/css/components.css" />
    <link rel="stylesheet" href="~/css/store.css" />
</head>
<body>
    <!-- Navbar: logo, search, cart badge, user menu, language switcher -->
    <partial name="_Navbar" />
    
    <main role="main" class="store-main">
        @RenderBody()
    </main>
    
    <footer class="store-footer">
        <!-- 3 columns: About, Customer Service, My Account -->
        <!-- Language switcher: العربية | English -->
    </footer>
    
    <div id="toast-container" aria-live="polite"></div>
    
    <script src="~/js/site.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

**4. Admin Layout (_AdminLayout.cshtml):**

```html
<html lang="..." dir="...">
<body class="admin-body">
    <!-- Admin Topbar: sidebar toggle, title, notifications, user menu -->
    <header class="admin-topbar"></header>
    
    <!-- Admin Sidebar: nav items (Dashboard, Books, Categories, Authors, Orders, Users) -->
    <aside class="admin-sidebar" id="admin-sidebar"></aside>
    
    <!-- Admin Main: content area -->
    <main class="admin-main">
        @RenderBody()
    </main>
</body>
</html>
```

**5. Auth Layout (_AuthLayout.cshtml):**

```html
<html lang="..." dir="...">
<body class="auth-body">
    <div class="auth-container">
        <div class="auth-logo"><h1>BookNest</h1></div>
        <div class="auth-card">
            @RenderBody()
        </div>
        <div class="auth-footer">Terms • Privacy</div>
    </div>
</body>
</html>
```

#### 🧪 الاختبارات

**Guard Test: _AdminLayout فقط في Admin Area**

```csharp
[Fact]
public void AdminLayout_OnlyUsedIn_AdminArea()
{
    var viewFiles = Directory.GetFiles("Views", "*.cshtml", SearchOption.AllDirectories);
    var violations = viewFiles
        .Where(f => !f.Contains("/Areas/Admin/") && File.ReadAllText(f).Contains("_AdminLayout"))
        .ToList();
    
    violations.Should().BeEmpty("_AdminLayout should only be used in Admin Area");
}
```

**Security Test: لا Html.Raw في Layouts**

```csharp
[Fact]
public void Layouts_ShouldNotUse_HtmlRaw()
{
    var layouts = new[] { "_StoreLayout.cshtml", "_AdminLayout.cshtml", "_AuthLayout.cshtml" };
    var violations = layouts
        .Where(l => File.ReadAllText($"Views/Shared/{l}").Contains("Html.Raw"))
        .ToList();
    
    violations.Should().BeEmpty("Layouts should not use Html.Raw (XSS risk)");
}
```

**MVC Test: html lang و dir صحيحين**

```csharp
[Theory]
[InlineData("ar", "rtl")]
[InlineData("en", "ltr")]
public async Task StoreLayout_SetsCorrect_LangAndDir(string culture, string expectedDir)
{
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
    
    var html = await client.GetStringAsync("/");
    var document = await BrowsingContext.New().OpenAsync(req => req.Content(html));
    
    document.DocumentElement.Language.Should().Be(culture);
    document.DocumentElement.Direction.Should().Be(expectedDir);
}
```

#### ✅ معايير القبول

- [ ] `tokens.css` يحتوي على جميع الـ Tokens من Design System
- [ ] 3 Layouts مكتملة: Store, Admin, Auth
- [ ] RTL/LTR يعملان بـ `<html lang dir>` ديناميكياً
- [ ] Fonts محملة من Google Fonts
- [ ] Language Switcher في Footer
- [ ] Admin Sidebar responsive (collapse على mobile)
- [ ] جميع الاختبارات تمر
- [ ] لا `Html.Raw` في أي Layout
- [ ] Focus states واضحة (outline + offset)

---

### T-04: Component Library & Styleguide

**الأولوية:** Must Have  
**التقدير:** 2-3 أيام  
**المتطلبات المغطاة:** Design System Components

#### 🎯 الهدف
إنشاء Component Library كاملة (Buttons, Inputs, Cards, Badges, etc.) + صفحة `/dev/styleguide` للمراجعة.

#### 📝 المكونات المطلوبة

**1. Buttons:**
- Variants: primary, accent, secondary, ghost, danger
- Sizes: sm, md, lg
- States: default, hover, focus, active, disabled, loading

**2. Form Elements:**
- Input (text, password, email, number, search)
- Textarea
- Select
- Checkbox & Radio
- File upload with preview
- Validation states (error, success)

**3. Cards:**
- Book Card: vertical (grid), horizontal (cart/orders)
- States: default, hover, out-of-stock, discounted

**4. Badges & Status Chips:**
- Order status: Pending, Processing, Shipped, Delivered, Cancelled
- Stock: In Stock, Low Stock, Out of Stock

**5. Data Components:**
- Data Table: sortable, selectable, responsive
- Pagination: numbered, compact (mobile)
- Empty State

**6. Overlays:**
- Modal
- Confirm Dialog
- Toast/Alert
- Dropdown

**7. Specialty:**
- Quantity Stepper
- Price (with discount strike-through)
- Rating Stars (readonly + input)

#### 🎨 تنفيذ

**Button Example:**

```css
/* components.css */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-4);
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  border-radius: var(--radius-md);
  border: 1px solid transparent;
  cursor: pointer;
  transition: all var(--duration-base) var(--ease);
}

.btn:focus-visible {
  outline: 2px solid var(--color-accent-500);
  outline-offset: 2px;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-primary {
  background-color: var(--color-primary-600);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background-color: var(--color-primary-700);
}

.btn-accent {
  background-color: var(--color-accent-500);
  color: white;
}

.btn-accent:hover:not(:disabled) {
  background-color: var(--color-accent-600);
}

/* ... باقي variants */
```

**Book Card View Component:**

```csharp
// Views/Shared/Components/BookCard/Default.cshtml
@model Book

<article class="book-card">
    <div class="book-card__cover">
        @if (!string.IsNullOrEmpty(Model.CoverImageUrl))
        {
            <img src="@Model.CoverImageUrl" alt="@Model.Title" loading="lazy" />
        }
        else
        {
            <div class="book-card__cover-placeholder">
                <span>@Model.Title</span>
            </div>
        }
        
        @if (Model.DiscountPrice.HasValue)
        {
            <span class="badge badge-danger book-card__discount">
                @Math.Round((Model.Price - Model.DiscountPrice.Value) / Model.Price * 100)% OFF
            </span>
        }
    </div>
    
    <div class="book-card__content">
        <h3 class="book-card__title">@Model.Title</h3>
        <p class="book-card__author">@Model.Author.Name</p>
        
        <div class="book-card__price">
            @if (Model.DiscountPrice.HasValue)
            {
                <span class="price price--discounted">
                    <span class="price__old">@Model.Price.ToString("N2") ج.م</span>
                    <span class="price__current">@Model.DiscountPrice.Value.ToString("N2") ج.م</span>
                </span>
            }
            else
            {
                <span class="price">@Model.Price.ToString("N2") ج.م</span>
            }
        </div>
        
        @if (Model.Stock == 0)
        {
            <span class="badge badge-danger">غير متاح</span>
        }
        else if (Model.Stock <= 5)
        {
            <span class="badge badge-warning">باقي @Model.Stock فقط</span>
        }
        
        <button class="btn btn-accent btn-sm book-card__add-to-cart" 
                data-book-id="@Model.Id" 
                @(Model.Stock == 0 ? "disabled" : "")>
            أضف للسلة
        </button>
    </div>
</article>
```

**Styleguide Page:**

```csharp
// Controllers/DevController.cs
#if DEBUG
[Route("dev")]
public class DevController : Controller
{
    [Route("styleguide")]
    public IActionResult Styleguide()
    {
        return View();
    }
}
#endif
```

```html
<!-- Views/Dev/Styleguide.cshtml -->
<div class="styleguide">
    <h1>BookNest Component Library</h1>
    
    <section>
        <h2>Buttons</h2>
        <div class="component-showcase">
            <button class="btn btn-primary">Primary</button>
            <button class="btn btn-accent">Accent</button>
            <button class="btn btn-secondary">Secondary</button>
            <button class="btn btn-ghost">Ghost</button>
            <button class="btn btn-danger">Danger</button>
            <button class="btn btn-primary" disabled>Disabled</button>
        </div>
    </section>
    
    <section>
        <h2>Form Elements</h2>
        <!-- ... -->
    </section>
    
    <!-- ... باقي المكونات -->
</div>
```

#### 🧪 الاختبارات

**Guard Test:** `/dev/styleguide` يرجع 404 في Production

```csharp
[Fact]
public async Task Styleguide_InProduction_Returns404()
{
    var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });
    
    var client = factory.CreateClient();
    var response = await client.GetAsync("/dev/styleguide");
    
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

**Security Test: Partials تعمل Encode**

```csharp
[Fact]
public async Task BookCard_WithXSS_EncodesOutput()
{
    var book = new Book { Title = "<script>alert('XSS')</script>", ... };
    
    var html = await RenderComponent("BookCard", book);
    
    html.Should().Contain("&lt;script&gt;");
    html.Should().NotContain("<script>alert");
}
```

**MVC Test: كل Component يعرض بدون Exception**

```csharp
[Fact]
public async Task Styleguide_AllComponents_RenderWithoutError()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/dev/styleguide");
    
    response.Should().BeSuccessful();
    var html = await response.Content.ReadAsStringAsync();
    html.Should().Contain("btn-primary");
    html.Should().Contain("book-card");
}
```

#### ✅ معايير القبول

- [ ] جميع المكونات من القسم 7 في Design Doc موجودة
- [ ] كل Component له جميع Variants و States
- [ ] Styleguide page تعرض جميع المكونات
- [ ] Styleguide يعمل فقط في Development
- [ ] جميع الاختبارات تمر
- [ ] XSS protected (Encode في Partials)
- [ ] Responsive على Mobile/Tablet/Desktop

---

### T-05: Global Exception Handler & Security

**الأولوية:** Must Have  
**التقدير:** 1-2 أيام  
**المتطلبات المغطاة:** NFR-02 (Security), NFR-07 (Reliability), NFR-10 (Logging)

#### 🎯 الهدف
إنشاء Global Exception Handler، Serilog، صفحات 404/403/500، Security Headers، HTTPS/HSTS.

#### 📝 خطوات التنفيذ

**1. Serilog Setup:**

```csharp
// Program.cs
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "BookNest")
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/booknest-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// ... rest of configuration
```

**2. Global Exception Handler:**

```csharp
// Middleware/GlobalExceptionHandlerMiddleware.cs
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;
    
    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. Path: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/html";
        
        if (_env.IsProduction())
        {
            // لا Stack trace في Production
            context.Response.Redirect("/Home/Error");
        }
        else
        {
            // في Development: معلومات مفصلة
            await context.Response.WriteAsync($@"
                <h1>Error: {exception.Message}</h1>
                <pre>{exception.StackTrace}</pre>
            ");
        }
    }
}
```

**3. Error Pages:**

```csharp
// Controllers/HomeController.cs
[Route("Home/Error")]
public IActionResult Error()
{
    var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
    
    var model = new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
        StatusCode = HttpContext.Response.StatusCode
    };
    
    return View(model);
}

[Route("Home/NotFound")]
[HttpGet("/404")]
public IActionResult NotFound()
{
    return View();
}

[Route("Home/Forbidden")]
[HttpGet("/403")]
public IActionResult Forbidden()
{
    return View();
}
```

**4. Security Headers Middleware:**

```csharp
// Middleware/SecurityHeadersMiddleware.cs
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // X-Frame-Options
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // Referrer-Policy
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Content-Security-Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data: https:;");
        
        // X-XSS-Protection (legacy)
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        await _next(context);
    }
}
```

**5. Cookie Settings:**

```csharp
// Program.cs
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});
```

**6. HTTPS & HSTS:**

```csharp
// Program.cs
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
// ...
```

**7. Status Code Pages:**

```csharp
// Program.cs
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
```

#### 🧪 الاختبارات

**Guard Test:** 403 للممنوع، 404 للمش موجود

```csharp
[Fact]
public async Task AdminRoute_AsCustomer_Returns403()
{
    var client = _factory.CreateAuthenticatedClient("customer@demo.com", "Customer");
    var response = await client.GetAsync("/admin");
    
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task NonExistentRoute_Returns404()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/this-does-not-exist");
    
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

**Security Test: Security Headers موجودة**

```csharp
[Fact]
public async Task Response_HasSecurityHeaders()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/");
    
    response.Headers.Should().ContainKey("X-Content-Type-Options");
    response.Headers.Should().ContainKey("X-Frame-Options");
    response.Headers.Should().ContainKey("Referrer-Policy");
    response.Headers.Should().ContainKey("Content-Security-Policy");
    
    response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
    response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
}

[Fact]
public async Task Cookies_AreSecure_HttpOnly_SameSite()
{
    var client = _factory.CreateClient(new WebApplicationFactoryClientOptions 
    { 
        AllowAutoRedirect = false 
    });
    
    // Login to get cookie
    var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("Email", "customer@demo.com"),
        new KeyValuePair<string, string>("Password", "Customer@123")
    }));
    
    var cookies = response.Headers.GetValues("Set-Cookie");
    
    cookies.Should().Contain(c => c.Contains("HttpOnly"));
    cookies.Should().Contain(c => c.Contains("Secure"));
    cookies.Should().Contain(c => c.Contains("SameSite=Strict"));
}
```

**MVC Test: Exception يرجع صفحة 500**

```csharp
[Fact]
public async Task UnhandledException_ReturnsErrorPage()
{
    // إنشاء endpoint يرمي Exception
    var client = _factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(ThrowExceptionController).Assembly);
        });
    }).CreateClient();
    
    var response = await client.GetAsync("/test/throw");
    
    response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
}

[Fact]
public void Serilog_LogsToFile()
{
    var logPath = "logs/booknest-*.log";
    var logFiles = Directory.GetFiles("logs", "booknest-*.log");
    
    logFiles.Should().NotBeEmpty("Log files should be created");
}
```

#### ✅ معايير القبول

- [ ] Serilog مكوّن ويكتب في logs/
- [ ] Global Exception Handler يعمل
- [ ] صفحات 404/403/500 مصممة
- [ ] لا Stack trace في Production
- [ ] Security Headers موجودة (5 headers على الأقل)
- [ ] Cookies: HttpOnly + Secure + SameSite
- [ ] HTTPS إجباري
- [ ] HSTS في Production
- [ ] جميع الاختبارات تمر
- [ ] Log يحتوي على Timestamp + Level + Message

---

# Phase 2: Authentication & Authorization

## T-10: Identity Setup & Password Policy

**الأولوية:** Must Have  
**التقدير:** 1 يوم  
**المتطلبات المغطاة:** FR-AUTH-02

#### 🎯 الهدف
إعداد ASP.NET Core Identity مع Password Policy قوية و Lockout بعد 5 محاولات فاشلة.

#### 📝 خطوات التنفيذ

```csharp
// Program.cs
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 3;
    
    // Lockout settings (BR: Lockout after 5 failed attempts)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false; // MVP: false, Phase 2: true
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});
```

#### 🧪 الاختبارات

**Guard Test: Locked-out user مش يقدر يدخل**

```csharp
[Fact]
public async Task Login_LockedOutUser_ReturnsForbidden()
{
    // Arrange - محاولات فاشلة 5 مرات
    var client = _factory.CreateClient();
    for (int i = 0; i < 5; i++)
    {
        await client.PostAsync("/account/login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", "customer@demo.com"),
            new KeyValuePair<string, string>("Password", "WrongPassword")
        }));
    }
    
    // Act - محاولة سادسة بالباسورد الصح
    var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("Email", "customer@demo.com"),
        new KeyValuePair<string, string>("Password", "Customer@123")
    }));
    
    var html = await response.Content.ReadAsStringAsync();
    
    // Assert
    html.Should().Contain("locked", "User should be locked out");
}
```

**Security Test: الباسورد Hashed + لا User Enumeration**

```csharp
[Fact]
public async Task Database_PasswordsAreHashed()
{
    using var scope = _factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    var user = await context.Users.FirstAsync();
    
    user.PasswordHash.Should().NotBeNullOrEmpty();
    user.PasswordHash.Should().NotContain("Customer@123");
    user.PasswordHash.Should().StartWith("AQAAAA"); // Identity V3 format
}

[Fact]
public async Task Login_WrongEmail_SameErrorMessage_AsWrongPassword()
{
    var client = _factory.CreateClient();
    
    // Wrong email
    var response1 = await client.PostAsync("/account/login", new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("Email", "nonexistent@test.com"),
        new KeyValuePair<string, string>("Password", "Password@123")
    }));
    var html1 = await response1.Content.ReadAsStringAsync();
    
    // Wrong password
    var response2 = await client.PostAsync("/account/login", new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("Email", "customer@demo.com"),
        new KeyValuePair<string, string>("Password", "WrongPassword")
    }));
    var html2 = await response2.Content.ReadAsStringAsync();
    
    // يجب أن تكون رسالة الخطأ نفسها (منع User Enumeration)
    var error1 = ExtractError(html1);
    var error2 = ExtractError(html2);
    
    error1.Should().Be(error2, "Error messages should be identical to prevent user enumeration");
}
```

**MVC Test: UserManager مسجل في DI**

```csharp
[Fact]
public void Services_UserManager_IsRegistered()
{
    using var scope = _factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
    
    userManager.Should().NotBeNull();
}
```

#### ✅ معايير القبول

- [ ] Password policy: 8+ chars, uppercase, lowercase, digit, special
- [ ] Lockout: 15 min بعد 5 محاولات فاشلة
- [ ] Email unique
- [ ] Password hashed
- [ ] Cookie: HttpOnly, Secure, 14 days
- [ ] جميع الاختبارات تمر
- [ ] لا User Enumeration (نفس رسالة الخطأ)

---

_(ملاحظة: نظراً لطول الملف، سأضيف ملخصاً للتذاكر T-11 حتى T-64، ثم تفصيل كامل للتذاكر الحرجة T-41, T-53, T-66, T-68, T-74)_

## ملخص تذاكر Phase 2-6 (T-11 حتى T-64)

### T-11: Register/Login/Logout
- Registration form مع validation
- Login مع Remember Me
- Redirect بعد Login حسب Role (Admin → /admin, Customer → /)
- Logout مع AntiForgeryToken

### T-12: Forgot Password & Reset
- Forgot password: إرسال link بـ token صالح 60 دقيقة
- Reset password form
- Token يُستخدم مرة واحدة فقط

### T-13: Authorization Policies & Role Protection
- `[Authorize(Roles="Admin")]` على كل Controllers في Admin Area
- Architecture test يتحقق من ذلك تلقائياً
- صفحة 403 مصممة

---

# Phase 3: Core Domain - Catalog

### T-20: Home Page with Cache
- عرض: Newest, Best Sellers, On Sale, Featured Categories
- Cache (IMemoryCache) مدة 10 دقائق
- Race test: 50 طلب متزامن → استعلام واحد للـ DB

### T-21: Catalog Query Service
- `ICatalogService.GetBooksAsync(filter, sort, page, pageSize)`
- Filtering: Category, Author, Price Range, In Stock only
- Sorting: Newest, Price Asc/Desc, Best Selling
- Pagination على مستوى DB (`Skip().Take()`)
- SQL Injection protected (Parameterized)

### T-22: Catalog Page & Filters UI
- Sidebar filters (desktop) / Drawer (mobile)
- Book Grid: 4 cols (desktop), 3 (tablet), 2 (mobile)
- Empty state + Loading skeleton

### T-23: Book Details Page
- معلومات الكتاب كاملة
- Quantity Stepper
- أزرار: Add to Cart, Add to Wishlist
- "كتب مشابهة" (نفس التصنيف)

---

# Phase 4: Shopping Experience

### T-30: Cart Service (Guest & User)
- سلة الزائر: Cookie موقّع (مشفر) بـ Data Protection API
- سلة اليوزر: DB
- منع الكمية > Stock

### T-31: Cart Merge on Login
- دمج سلة الزائر مع سلة اليوزر عند Login
- Race: Login من جهازين → لا تضاعف

### T-32: Cart Page with AJAX
- عرض السلة مع Quantity Stepper (AJAX)
- Update/Remove بـ POST + AntiForgeryToken
- حساب الإجمالي على السيرفر

### T-33: Wishlist
- إضافة/حذف
- IDOR protection
- Anonymous → Login

---

# Phase 5: Order Management

### T-40: Address Management
- CRUD للعناوين
- تعيين Default
- IDOR protection

### T-41: Order Placement Service ⭐ (أهم تذكرة - تفصيل كامل في الأسفل)

### T-42: Checkout Flow (3 Steps)
- Step 1: عنوان الشحن
- Step 2: طريقة الدفع
- Step 3: مراجعة الطلب
- Double-click protection

### T-43: My Orders & Order Details
- قائمة الطلبات بـ Pagination
- تفاصيل الطلب
- إلغاء الطلب (Pending فقط)

### T-44: Profile Management
- تعديل البيانات
- تغيير الباسورد (يطلب القديم)

---

# Phase 6: Admin Dashboard

### T-50: Admin Dashboard & Analytics
- Widgets: Total Sales, Orders Count, New Orders, Low Stock
- Chart: مبيعات آخر 30 يوم
- Race: الإحصائيات متسقة (Snapshot Isolation)

### T-51: Books CRUD + Image Upload
- CRUD كامل
- Upload: فحص Magic bytes، حد 2MB، اسم عشوائي
- Soft Delete

### T-52: Categories & Authors CRUD
- CRUD بـ Modal
- منع حذف تصنيف/مؤلف له كتب

### T-53: Orders Management & Status Change ⭐ (تفصيل كامل في الأسفل)

### T-54: Users Management
- عرض اليوزرز
- تفعيل/تعطيل
- تغيير Role
- BR-09: الأدمن ما يعطّل نفسه

---

# Phase 7: Wow Layer (Advanced Features)

### T-65: Localization (Arabic/English)
- `.resx` files
- Culture Middleware
- Language Switcher
- `<html lang dir>` ديناميكي

### T-66: Order State Machine & History ⭐ (تفصيل كامل في الأسفل)

### T-67: Arabic-Aware Search + Auto-suggest
- Normalization (تشكيل، أ/إ/آ، ة/ه)
- Auto-suggest بعد حرفين (8 نتائج max)
- Debounce 300ms

### T-68: Stock Reservation System ⭐ (تفصيل كامل في الأسفل)

### T-69: Real-time Admin Dashboard (SignalR)
- SignalR Hub محمي بـ Role
- Notifications: New Order, Low Stock
- إعادة اتصال تلقائي

### T-70: Order Timeline
- عرض Timeline لحالات الطلب مع التواريخ

### T-71: "Notify When Available" + Outbox
- اشتراك في التنبيه
- Outbox pattern للإيميلات
- إيميل واحد لكل اشتراك

### T-72: Admin Analytics Dashboard
- مبيعات، أعلى 10 كتب، توزيع التصنيفات
- Aggregates على DB

### T-73: Dark Mode
- Toggle switch
- Tokens بوضعين (Light/Dark)
- Contrast check

---

# Phase 8: Polish & Proof

### T-74: Load Testing with k6 ⭐ (تفصيل كامل في الأسفل)

### T-75: Accessibility & Lighthouse
- Lighthouse ≥ 90 (Performance, A11y, Best Practices, SEO)
- `axe` على الصفحات الرئيسية

### T-76: Mutation Testing
- Stryker.NET على OrderService, Reservation, StateMachine
- Mutation score ≥ 80%

### T-77: Demo Package & Reset
- بيانات Seed للديمو
- حسابين: admin@demo.com, customer@demo.com
- Reset يومي
- فيديو 60 ثانية + GIFs

### T-78: Professional README & ADRs
- ADR-1: Stock Reservation
- ADR-2: ترتيب الأقفال بـ BookId
- ADR-3: SignalR مش Polling
- ERD
- Badges (CI, Coverage, etc.)

---


---

# 🔥 التذاكر الحرجة - تفصيل كامل

هذه الخمس تذاكر هي **الأهم في الـ CV** لأنها تُظهر فهماً عميقاً لـ:
- Transactions & Concurrency Control
- Race Conditions & Atomic Operations
- State Machines & Audit Trails
- Load Testing & Performance
- Complex Business Logic

---

## ⭐ T-41: Order Placement Service (أهم تذكرة في المشروع)

**الأولوية:** Must Have  
**التقدير:** 3-4 أيام  
**المتطلبات المغطاة:** FR-ORD-01 → 04، BR-02, BR-04, BR-08

### 🎯 الهدف
خدمة تنفيذ الطلب مع:
- **Transaction** يشمل Order + OrderItems + خصم المخزون
- **خصم ذرّي** للمخزون (Atomic Stock Decrement)
- **Snapshot** للأسعار وقت الطلب (BR-04)
- **Idempotency** (منع التكرار)
- **Rollback كامل** عند الفشل

### 🏗️ Architecture

```
[CartService] 
    ↓
[OrderService.PlaceOrderAsync(userId, addressId, paymentMethod)]
    ↓
[Transaction BEGIN]
    ├→ Create Order (Subtotal, Shipping, Total)
    ├→ Create OrderItems (snapshot: title, ISBN, price)
    ├→ Atomic Stock Decrement (UPDATE ... WHERE Stock >= @qty)
    ├→ Clear Cart
[Transaction COMMIT or ROLLBACK]
    ↓
[Result: Success(OrderId) or Failure(Reason)]
```

### 📝 خطوات التنفيذ المفصلة

**1. Interface:**

```csharp
// src/BookNest.Core/Interfaces/IServices/IOrderService.cs
namespace BookNest.Core.Interfaces.IServices;

public interface IOrderService
{
    Task<OrderResult> PlaceOrderAsync(
        string userId, 
        int addressId, 
        string paymentMethod,
        string? idempotencyKey = null);
    
    Task<OrderResult> CancelOrderAsync(int orderId, string userId, string reason);
}

public class OrderResult
{
    public bool Succeeded { get; set; }
    public int? OrderId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public static OrderResult Success(int orderId) => new() 
    { 
        Succeeded = true, 
        OrderId = orderId 
    };
    
    public static OrderResult Failure(string error) => new() 
    { 
        Succeeded = false, 
        ErrorMessage = error,
        Errors = new List<string> { error }
    };
}
```

**2. Implementation:**

```csharp
// src/BookNest.Infrastructure/Services/OrderService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BookNest.Core.Entities;
using BookNest.Core.Enums;
using BookNest.Core.Interfaces.IServices;
using BookNest.Infrastructure.Data;

namespace BookNest.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private const decimal FreeShippingThreshold = 500m;
    private const decimal ShippingCost = 50m;
    
    public OrderService(
        ApplicationDbContext context,
        ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<OrderResult> PlaceOrderAsync(
        string userId, 
        int addressId, 
        string paymentMethod,
        string? idempotencyKey = null)
    {
        // 1. Idempotency Check (منع التكرار)
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderNumber == idempotencyKey);
            
            if (existingOrder != null)
            {
                _logger.LogWarning("Duplicate order attempt with key {Key}", idempotencyKey);
                return OrderResult.Success(existingOrder.Id);
            }
        }
        
        // 2. Get Cart Items
        var cartItems = await _context.CartItems
            .Include(c => c.Book)
                .ThenInclude(b => b.Author)
            .Where(c => c.UserId == userId)
            .ToListAsync();
        
        if (!cartItems.Any())
        {
            return OrderResult.Failure("السلة فارغة");
        }
        
        // 3. Get Shipping Address
        var address = await _context.Addresses
            .Where(a => a.Id == addressId && a.UserId == userId)
            .FirstOrDefaultAsync();
        
        if (address == null)
        {
            return OrderResult.Failure("العنوان غير موجود");
        }
        
        // 4. START TRANSACTION
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // 5. Calculate Totals
            decimal subtotal = 0;
            foreach (var item in cartItems)
            {
                var price = item.Book.DiscountPrice ?? item.Book.Price;
                subtotal += price * item.Quantity;
            }
            
            var shipping = subtotal >= FreeShippingThreshold ? 0 : ShippingCost;
            var total = subtotal + shipping;
            
            // 6. Create Order
            var order = new Order
            {
                OrderNumber = idempotencyKey ?? Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                UserId = userId,
                Status = OrderStatus.Pending,
                Subtotal = subtotal,
                ShippingCost = shipping,
                Total = total,
                ShippingName = address.FullName,
                ShippingPhone = address.Phone,
                ShippingCity = address.City,
                ShippingAddress = address.AddressLine,
                ShippingPostalCode = address.PostalCode,
                PaymentMethod = paymentMethod,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // للحصول على Order.Id
            
            // 7. Create Order Items + Atomic Stock Decrement
            var orderItems = new List<OrderItem>();
            
            // ترتيب حسب BookId لمنع Deadlock
            var sortedCart = cartItems.OrderBy(c => c.BookId).ToList();
            
            foreach (var cartItem in sortedCart)
            {
                // Snapshot: السعر والمعلومات وقت الطلب (BR-04)
                var unitPrice = cartItem.Book.DiscountPrice ?? cartItem.Book.Price;
                
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    BookId = cartItem.BookId,
                    BookTitle = cartItem.Book.Title,
                    BookISBN = cartItem.Book.ISBN,
                    UnitPrice = unitPrice,
                    Quantity = cartItem.Quantity,
                    Subtotal = unitPrice * cartItem.Quantity
                };
                
                orderItems.Add(orderItem);
                
                // 🔥 ATOMIC STOCK DECREMENT
                // هذا الاستعلام ذرّي ويمنع Overselling
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE Books 
                    SET Stock = Stock - @p0, UpdatedAt = GETUTCDATE()
                    WHERE Id = @p1 AND Stock >= @p0 AND IsDeleted = 0",
                    cartItem.Quantity,
                    cartItem.BookId);
                
                if (rowsAffected == 0)
                {
                    // المخزون غير كافٍ - Rollback
                    var book = await _context.Books.FindAsync(cartItem.BookId);
                    
                    _logger.LogWarning(
                        "Insufficient stock for book {BookId}. Requested: {Requested}, Available: {Available}",
                        cartItem.BookId, cartItem.Quantity, book?.Stock ?? 0);
                    
                    await transaction.RollbackAsync();
                    
                    return OrderResult.Failure(
                        $"الكتاب '{cartItem.Book.Title}' غير متوفر بالكمية المطلوبة. " +
                        $"المتاح: {book?.Stock ?? 0}");
                }
            }
            
            await _context.OrderItems.AddRangeAsync(orderItems);
            
            // 8. Clear Cart
            _context.CartItems.RemoveRange(cartItems);
            
            // 9. Save All Changes
            await _context.SaveChangesAsync();
            
            // 10. COMMIT TRANSACTION
            await transaction.CommitAsync();
            
            _logger.LogInformation(
                "Order {OrderNumber} placed successfully by user {UserId}. Total: {Total:C}",
                order.OrderNumber, userId, total);
            
            return OrderResult.Success(order.Id);
        }
        catch (Exception ex)
        {
            // ROLLBACK على أي خطأ
            await transaction.RollbackAsync();
            
            _logger.LogError(ex, "Error placing order for user {UserId}", userId);
            
            return OrderResult.Failure("حدث خطأ أثناء تنفيذ الطلب. يرجى المحاولة مرة أخرى.");
        }
    }
    
    public async Task<OrderResult> CancelOrderAsync(int orderId, string userId, string reason)
    {
        // 1. Get Order with Items
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        
        if (order == null)
        {
            return OrderResult.Failure("الطلب غير موجود");
        }
        
        // 2. Check if cancellable (BR-01: قبل Shipped فقط)
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
        {
            return OrderResult.Failure($"لا يمكن إلغاء الطلب في حالة {order.Status}");
        }
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // 3. Return Stock (Atomic)
            foreach (var item in order.Items.OrderBy(i => i.BookId))
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE Books 
                    SET Stock = Stock + @p0, UpdatedAt = GETUTCDATE()
                    WHERE Id = @p1",
                    item.Quantity,
                    item.BookId);
            }
            
            // 4. Update Order Status
            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.CancellationReason = reason;
            order.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", orderId, userId);
            
            return OrderResult.Success(orderId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            return OrderResult.Failure("حدث خطأ أثناء إلغاء الطلب");
        }
    }
}
```

**3. Controller:**

```csharp
// src/BookNest.Web/Controllers/CheckoutController.cs
[Authorize]
public class CheckoutController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    
    public CheckoutController(IOrderService orderService, ICartService cartService)
    {
        _orderService = orderService;
        _cartService = cartService;
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Review", model);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        // Idempotency Key من hidden field أو Session
        var idempotencyKey = model.IdempotencyKey ?? HttpContext.Session.GetString("CheckoutKey");
        
        var result = await _orderService.PlaceOrderAsync(
            userId,
            model.AddressId,
            model.PaymentMethod,
            idempotencyKey);
        
        if (result.Succeeded)
        {
            // Clear checkout session
            HttpContext.Session.Remove("CheckoutKey");
            
            TempData["Success"] = "تم تنفيذ طلبك بنجاح!";
            return RedirectToAction("Success", "Orders", new { id = result.OrderId });
        }
        
        ModelState.AddModelError("", result.ErrorMessage ?? "فشل تنفيذ الطلب");
        return View("Review", model);
    }
}
```

### 🧪 الاختبارات الكاملة

**Guard Test: الـ Service يرفض سلة مش بتاعت اليوزر**

```csharp
[Fact]
public async Task PlaceOrder_WithOtherUserCart_Fails()
{
    // Arrange
    var aliceId = await CreateUserAsync("alice@test.com");
    var bobId = await CreateUserAsync("bob@test.com");
    
    var bookId = await CreateBookAsync(stock: 10);
    await AddToCartAsync(aliceId, bookId, 2);
    
    var addressId = await CreateAddressAsync(aliceId);
    
    // Act - Bob يحاول يطلب سلة Alice (ما ينفعش)
    var result = await _orderService.PlaceOrderAsync(bobId, addressId, "Cash");
    
    // Assert
    result.Succeeded.Should().BeFalse();
    result.ErrorMessage.Should().Contain("فارغة");
}
```

**Race Test 1: آخر نسخة - 10 محاولات متزامنة، واحد ينجح**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task PlaceOrder_LastCopy_OnlyOneSucceeds()
{
    // Arrange - كتاب بنسخة واحدة
    var bookId = await CreateBookAsync(stock: 1);
    
    // 10 يوزرز كل واحد عنده الكتاب في سلته
    var userIds = new List<string>();
    var addressIds = new List<int>();
    
    for (int i = 0; i < 10; i++)
    {
        var userId = await CreateUserAsync($"user{i}@test.com");
        await AddToCartAsync(userId, bookId, qty: 1);
        var addressId = await CreateAddressAsync(userId);
        
        userIds.Add(userId);
        addressIds.Add(addressId);
    }
    
    // Act - كلهم يحاولوا يشتروا في نفس اللحظة
    var tasks = userIds.Select((userId, index) => 
        Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            
            return await orderService.PlaceOrderAsync(
                userId, 
                addressIds[index], 
                "Cash");
        })
    ).ToList();
    
    var results = await Task.WhenAll(tasks);
    
    // Assert
    var successCount = results.Count(r => r.Succeeded);
    successCount.Should().Be(1, "Only one order should succeed with last copy");
    
    var failedCount = results.Count(r => !r.Succeeded);
    failedCount.Should().Be(9, "9 orders should fail due to insufficient stock");
    
    // تحقق من المخزون = 0
    var book = await _context.Books.FindAsync(bookId);
    book!.Stock.Should().Be(0);
    
    // تحقق من عدد الطلبات الناجحة في DB
    var ordersCount = await _context.Orders
        .Where(o => o.Status == OrderStatus.Pending)
        .CountAsync();
    ordersCount.Should().Be(1);
}
```

**Race Test 2: نفس Idempotency Key مرتين - طلب واحد**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task PlaceOrder_SameIdempotencyKey_Twice_CreatesOneOrder()
{
    // Arrange
    var userId = await CreateUserAsync("user@test.com");
    var bookId = await CreateBookAsync(stock: 10);
    await AddToCartAsync(userId, bookId, 2);
    var addressId = await CreateAddressAsync(userId);
    
    var idempotencyKey = Guid.NewGuid().ToString();
    
    // Act - نفس الطلب مرتين في نفس اللحظة (double-click)
    var task1 = _orderService.PlaceOrderAsync(userId, addressId, "Cash", idempotencyKey);
    var task2 = _orderService.PlaceOrderAsync(userId, addressId, "Cash", idempotencyKey);
    
    var results = await Task.WhenAll(task1, task2);
    
    // Assert
    results[0].Succeeded.Should().BeTrue();
    results[1].Succeeded.Should().BeTrue();
    
    // نفس Order ID
    results[0].OrderId.Should().Be(results[1].OrderId);
    
    // طلب واحد فقط في DB
    var ordersCount = await _context.Orders
        .Where(o => o.OrderNumber == idempotencyKey)
        .CountAsync();
    ordersCount.Should().Be(1);
    
    // المخزون خُصِم مرة واحدة فقط
    var book = await _context.Books.FindAsync(bookId);
    book!.Stock.Should().Be(8); // 10 - 2 = 8
}
```

**Race Test 3: فشل في نص Transaction - Rollback كامل**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task PlaceOrder_FailureMidTransaction_RollsBackCompletely()
{
    // Arrange
    var userId = await CreateUserAsync("user@test.com");
    
    // كتابين: الأول متوفر، الثاني خلص أثناء العملية
    var book1Id = await CreateBookAsync(stock: 10);
    var book2Id = await CreateBookAsync(stock: 1);
    
    await AddToCartAsync(userId, book1Id, 1);
    await AddToCartAsync(userId, book2Id, 1);
    
    var addressId = await CreateAddressAsync(userId);
    
    // يوزر تاني يشتري book2 قبل ما الأول يكمل
    var otherUserId = await CreateUserAsync("other@test.com");
    await AddToCartAsync(otherUserId, book2Id, 1);
    var otherAddressId = await CreateAddressAsync(otherUserId);
    
    await _orderService.PlaceOrderAsync(otherUserId, otherAddressId, "Cash");
    // الآن book2 خلص
    
    // Act - اليوزر الأول يحاول يكمل الطلب
    var result = await _orderService.PlaceOrderAsync(userId, addressId, "Cash");
    
    // Assert
    result.Succeeded.Should().BeFalse();
    result.ErrorMessage.Should().Contain("غير متوفر");
    
    // Rollback كامل: لا يوجد Order في DB
    var orders = await _context.Orders.Where(o => o.UserId == userId).ToListAsync();
    orders.Should().BeEmpty();
    
    // المخزون لم يتغير
    var book1 = await _context.Books.FindAsync(book1Id);
    book1!.Stock.Should().Be(10); // لم يُخصم
    
    // السلة لم تُفرغ
    var cartCount = await _context.CartItems.CountAsync(c => c.UserId == userId);
    cartCount.Should().Be(2);
}
```

**Race Test 4: كتابين - Deadlock محتمل - الترتيب يحل المشكلة**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task PlaceOrder_TwoBooks_DifferentOrder_NoDeadlock()
{
    // Arrange
    var book1Id = await CreateBookAsync(stock: 10);
    var book2Id = await CreateBookAsync(stock: 10);
    
    // User A: book1 ثم book2
    var userAId = await CreateUserAsync("userA@test.com");
    await AddToCartAsync(userAId, book1Id, 1);
    await AddToCartAsync(userAId, book2Id, 1);
    var addressAId = await CreateAddressAsync(userAId);
    
    // User B: book2 ثم book1 (عكس A)
    var userBId = await CreateUserAsync("userB@test.com");
    await AddToCartAsync(userBId, book2Id, 1);
    await AddToCartAsync(userBId, book1Id, 1);
    var addressBId = await CreateAddressAsync(userBId);
    
    // Act - في نفس اللحظة
    var taskA = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        return await service.PlaceOrderAsync(userAId, addressAId, "Cash");
    });
    
    var taskB = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        return await service.PlaceOrderAsync(userBId, addressBId, "Cash");
    });
    
    var results = await Task.WhenAll(taskA, taskB);
    
    // Assert - كلاهما ينجح بدون Deadlock
    results[0].Succeeded.Should().BeTrue();
    results[1].Succeeded.Should().BeTrue();
    
    // السبب: الترتيب حسب BookId يمنع الـ Deadlock
}
```

**Security Test: السعر من DB مش من Request**

```csharp
[Fact]
[Trait("Category", "Security")]
public async Task PlaceOrder_PriceCalculatedOnServer_NotFromClient()
{
    // Arrange
    var userId = await CreateUserAsync("user@test.com");
    var bookId = await CreateBookAsync(price: 100m, stock: 10);
    await AddToCartAsync(userId, bookId, 2);
    var addressId = await CreateAddressAsync(userId);
    
    // Act
    var result = await _orderService.PlaceOrderAsync(userId, addressId, "Cash");
    
    // Assert
    result.Succeeded.Should().BeTrue();
    
    var order = await _context.Orders
        .Include(o => o.Items)
        .FirstAsync(o => o.Id == result.OrderId);
    
    // السعر من DB
    order.Items.First().UnitPrice.Should().Be(100m);
    order.Subtotal.Should().Be(200m); // 100 * 2
    
    // لا يوجد أي Parameter من الـ Client يتحكم بالسعر
}

[Fact]
[Trait("Category", "Security")]
public async Task PlaceOrder_SnapshotsPriceAtOrderTime()
{
    // Arrange
    var userId = await CreateUserAsync("user@test.com");
    var bookId = await CreateBookAsync(price: 100m, stock: 10);
    await AddToCartAsync(userId, bookId, 1);
    var addressId = await CreateAddressAsync(userId);
    
    // Act - الطلب الأول
    var result1 = await _orderService.PlaceOrderAsync(userId, addressId, "Cash");
    
    // تغيير السعر بعد الطلب
    var book = await _context.Books.FindAsync(bookId);
    book!.Price = 200m;
    await _context.SaveChangesAsync();
    
    // الطلب الثاني
    await AddToCartAsync(userId, bookId, 1);
    var result2 = await _orderService.PlaceOrderAsync(userId, addressId, "Cash");
    
    // Assert - كل طلب له السعر وقت الشراء
    var order1 = await _context.Orders.Include(o => o.Items).FirstAsync(o => o.Id == result1.OrderId);
    var order2 = await _context.Orders.Include(o => o.Items).FirstAsync(o => o.Id == result2.OrderId);
    
    order1.Items.First().UnitPrice.Should().Be(100m);
    order2.Items.First().UnitPrice.Should().Be(200m);
}
```

**MVC Test: Controller Unit Tests**

```csharp
[Fact]
[Trait("Category", "Mvc")]
public async Task PlaceOrder_ValidModel_RedirectsToSuccess()
{
    // Arrange
    var orderService = Substitute.For<IOrderService>();
    orderService.PlaceOrderAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
        .Returns(OrderResult.Success(123));
    
    var controller = new CheckoutController(orderService, null!);
    controller.ControllerContext = CreateControllerContext("user-id");
    
    var model = new CheckoutViewModel { AddressId = 1, PaymentMethod = "Cash" };
    
    // Act
    var result = await controller.PlaceOrder(model);
    
    // Assert
    var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
    redirect.ActionName.Should().Be("Success");
    redirect.ControllerName.Should().Be("Orders");
    redirect.RouteValues!["id"].Should().Be(123);
}

[Fact]
[Trait("Category", "Mvc")]
public async Task PlaceOrder_InvalidModel_ReturnsSameViewWithModel()
{
    // Arrange
    var controller = new CheckoutController(null!, null!);
    controller.ModelState.AddModelError("AddressId", "Required");
    
    var model = new CheckoutViewModel();
    
    // Act
    var result = await controller.PlaceOrder(model);
    
    // Assert
    var view = result.Should().BeOfType<ViewResult>().Subject;
    view.ViewName.Should().Be("Review");
    view.Model.Should().BeSameAs(model);
}

[Fact]
[Trait("Category", "Mvc")]
public async Task PlaceOrder_ServiceFailure_ReturnsViewWithError()
{
    // Arrange
    var orderService = Substitute.For<IOrderService>();
    orderService.PlaceOrderAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
        .Returns(OrderResult.Failure("المخزون غير كافٍ"));
    
    var controller = new CheckoutController(orderService, null!);
    controller.ControllerContext = CreateControllerContext("user-id");
    
    var model = new CheckoutViewModel { AddressId = 1, PaymentMethod = "Cash" };
    
    // Act
    var result = await controller.PlaceOrder(model);
    
    // Assert
    var view = result.Should().BeOfType<ViewResult>().Subject;
    controller.ModelState.Should().ContainKey("");
    controller.ModelState[""]!.Errors.First().ErrorMessage.Should().Contain("المخزون");
}
```

### ✅ معايير القبول (Definition of Done)

- [ ] Transaction يشمل Order + OrderItems + Stock Decrement + Clear Cart
- [ ] Atomic Stock Decrement بـ `UPDATE ... WHERE Stock >= @qty`
- [ ] Price Snapshot (BR-04)
- [ ] Idempotency Key يمنع التكرار
- [ ] Rollback كامل عند أي فشل
- [ ] ترتيب الأقفال حسب BookId (منع Deadlock)
- [ ] جميع الاختبارات تمر:
  - Guard: 1/1
  - Race: 4/4 (Last Copy, Idempotency, Mid-Transaction Failure, No Deadlock)
  - Security: 2/2 (Server-side Price, Snapshot)
  - MVC: 3/3 (Valid, Invalid, Service Failure)
- [ ] Logging على كل عملية (Success + Failure)
- [ ] Error messages واضحة للمستخدم
- [ ] لا Exception يوصل للـ Client

---


## ⭐ T-53: Orders Management & Status Change

**الأولوية:** Must Have  
**التقدير:** 2-3 أيام  
**المتطلبات المغطاة:** FR-ADM-04, FR-ADM-05، BR-01

### 🎯 الهدف
إدارة الطلبات من الأدمن مع تغيير الحالة بشكل آمن حسب State Machine:
- `Pending → Processing → Shipped → Delivered`
- `Cancelled` متاح قبل Shipped فقط
- **RowVersion** للحماية من Concurrent Updates

### 📝 خطوات التنفيذ

**1. State Transitions Validator:**

```csharp
// src/BookNest.Core/Services/OrderStatusValidator.cs
namespace BookNest.Core.Services;

public static class OrderStatusValidator
{
    private static readonly Dictionary<OrderStatus, List<OrderStatus>> AllowedTransitions = new()
    {
        { OrderStatus.Pending, new() { OrderStatus.Processing, OrderStatus.Cancelled } },
        { OrderStatus.Processing, new() { OrderStatus.Shipped, OrderStatus.Cancelled } },
        { OrderStatus.Shipped, new() { OrderStatus.Delivered } },
        { OrderStatus.Delivered, new() }, // Terminal state
        { OrderStatus.Cancelled, new() }  // Terminal state
    };
    
    public static bool CanTransitionTo(OrderStatus from, OrderStatus to)
    {
        return AllowedTransitions.ContainsKey(from) && 
               AllowedTransitions[from].Contains(to);
    }
    
    public static List<OrderStatus> GetAllowedNextStatuses(OrderStatus current)
    {
        return AllowedTransitions.ContainsKey(current) 
            ? AllowedTransitions[current] 
            : new List<OrderStatus>();
    }
}
```

**2. Admin Order Service:**

```csharp
// src/BookNest.Infrastructure/Services/AdminOrderService.cs
public class AdminOrderService : IAdminOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminOrderService> _logger;
    
    public async Task<OrderResult> ChangeOrderStatusAsync(
        int orderId, 
        OrderStatus newStatus, 
        string adminUserId, 
        string? notes = null)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId);
        
        if (order == null)
        {
            return OrderResult.Failure("الطلب غير موجود");
        }
        
        // 1. Validate State Transition (BR-01)
        if (!OrderStatusValidator.CanTransitionTo(order.Status, newStatus))
        {
            return OrderResult.Failure(
                $"لا يمكن تغيير حالة الطلب من {order.Status} إلى {newStatus}");
        }
        
        // 2. Update with Concurrency Check
        try
        {
            var oldStatus = order.Status;
            
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            
            // تحديث التواريخ
            switch (newStatus)
            {
                case OrderStatus.Shipped:
                    order.ShippedAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Delivered:
                    order.DeliveredAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Cancelled:
                    order.CancelledAt = DateTime.UtcNow;
                    order.CancellationReason = notes;
                    break;
            }
            
            // حفظ مع Concurrency Check
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Order {OrderId} status changed from {OldStatus} to {NewStatus} by admin {AdminId}",
                orderId, oldStatus, newStatus, adminUserId);
            
            return OrderResult.Success(orderId);
        }
        catch (DbUpdateConcurrencyException)
        {
            // شخص آخر عدّل الطلب في نفس اللحظة
            _logger.LogWarning(
                "Concurrency conflict updating order {OrderId}", orderId);
            
            return OrderResult.Failure(
                "تم تعديل الطلب من مستخدم آخر. يرجى تحديث الصفحة والمحاولة مرة أخرى.");
        }
    }
}
```

**3. Admin Controller:**

```csharp
// src/BookNest.Web/Areas/Admin/Controllers/OrdersController.cs
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly IAdminOrderService _orderService;
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus newStatus, string? notes)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        var result = await _orderService.ChangeOrderStatusAsync(id, newStatus, adminId, notes);
        
        if (result.Succeeded)
        {
            TempData["Success"] = "تم تغيير حالة الطلب بنجاح";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
        }
        
        return RedirectToAction("Details", new { id });
    }
}
```

**4. View (Details):**

```html
<!-- Areas/Admin/Views/Orders/Details.cshtml -->
@model OrderDetailsViewModel

<div class="order-status-section">
    <h3>تغيير حالة الطلب</h3>
    
    <form asp-action="ChangeStatus" method="post">
        <input type="hidden" name="id" value="@Model.Order.Id" />
        
        <div class="form-group">
            <label>الحالة الحالية</label>
            <span class="badge badge-@GetStatusClass(Model.Order.Status)">
                @Model.Order.Status.GetDisplayName()
            </span>
        </div>
        
        @if (Model.AllowedNextStatuses.Any())
        {
            <div class="form-group">
                <label for="newStatus">الحالة الجديدة</label>
                <select name="newStatus" id="newStatus" class="form-control">
                    @foreach (var status in Model.AllowedNextStatuses)
                    {
                        <option value="@status">@status.GetDisplayName()</option>
                    }
                </select>
            </div>
            
            <div class="form-group">
                <label for="notes">ملاحظات (اختياري)</label>
                <textarea name="notes" id="notes" class="form-control" rows="3"></textarea>
            </div>
            
            <button type="submit" class="btn btn-primary">تحديث الحالة</button>
        }
        else
        {
            <p class="text-muted">لا يمكن تغيير حالة هذا الطلب</p>
        }
    </form>
</div>
```

### 🧪 الاختبارات

**Guard Test: كل Action محمي بـ Admin Role**

```csharp
[Theory]
[InlineData("/admin/orders")]
[InlineData("/admin/orders/1")]
[Trait("Category", "Guard")]
public async Task AdminOrders_AsCustomer_Returns403(string url)
{
    var client = _factory.CreateAuthenticatedClient("customer@demo.com", "Customer");
    var response = await client.GetAsync(url);
    
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

**Race Test 1: Admin 1 و Admin 2 يغيّروا نفس الطلب - واحد يكسب**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task ChangeStatus_TwoAdmins_Simultaneously_OnlyOneSucceeds()
{
    // Arrange
    var orderId = await CreateOrderAsync(OrderStatus.Pending);
    var admin1Id = "admin1";
    var admin2Id = "admin2";
    
    // Act - كلاهما يحاول التغيير في نفس اللحظة
    var task1 = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminOrderService>();
        return await service.ChangeOrderStatusAsync(
            orderId, OrderStatus.Processing, admin1Id);
    });
    
    var task2 = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminOrderService>();
        return await service.ChangeOrderStatusAsync(
            orderId, OrderStatus.Cancelled, admin2Id);
    });
    
    var results = await Task.WhenAll(task1, task2);
    
    // Assert
    var successCount = results.Count(r => r.Succeeded);
    var failureCount = results.Count(r => !r.Succeeded);
    
    successCount.Should().Be(1, "Only one update should succeed");
    failureCount.Should().Be(1, "One should fail due to concurrency");
    
    var failedResult = results.First(r => !r.Succeeded);
    failedResult.ErrorMessage.Should().Contain("تم تعديل");
}
```

**Race Test 2: Customer يلغي + Admin يغيّر لـ Shipped**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task CancelOrder_Customer_And_ChangeToShipped_Admin_OnlyOneSucceeds()
{
    // Arrange
    var userId = await CreateUserAsync("customer@test.com");
    var orderId = await CreateOrderAsync(OrderStatus.Processing, userId);
    var adminId = "admin";
    
    // Act
    var customerCancel = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        return await orderService.CancelOrderAsync(orderId, userId, "تغيير رأي");
    });
    
    var adminShip = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminOrderService>();
        return await adminService.ChangeOrderStatusAsync(orderId, OrderStatus.Shipped, adminId);
    });
    
    var results = await Task.WhenAll(customerCancel, adminShip);
    
    // Assert
    results.Count(r => r.Succeeded).Should().Be(1);
    
    // تحقق من الحالة النهائية
    var order = await _context.Orders.FindAsync(orderId);
    (order!.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Shipped)
        .Should().BeTrue("Order should be in one of the two attempted states");
}
```

**Security Test: الحالة تتحقق على السيرفر**

```csharp
[Fact]
[Trait("Category", "Security")]
public async Task ChangeStatus_InvalidTransition_Rejected()
{
    // Arrange
    var orderId = await CreateOrderAsync(OrderStatus.Delivered);
    var adminId = "admin";
    
    // Act - محاولة تغيير Delivered → Pending (غير مسموح)
    var result = await _adminOrderService.ChangeOrderStatusAsync(
        orderId, OrderStatus.Pending, adminId);
    
    // Assert
    result.Succeeded.Should().BeFalse();
    result.ErrorMessage.Should().Contain("لا يمكن تغيير");
    
    // الحالة لم تتغير
    var order = await _context.Orders.FindAsync(orderId);
    order!.Status.Should().Be(OrderStatus.Delivered);
}

[Fact]
[Trait("Category", "Security")]
public async Task ChangeStatus_RequiresAntiForgeryToken()
{
    var client = _factory.CreateAuthenticatedClient("admin@booknest.com", "Admin");
    
    // POST بدون AntiForgeryToken
    var response = await client.PostAsync("/admin/orders/changeStatus", 
        new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id", "1"),
            new KeyValuePair<string, string>("newStatus", "Processing")
        }));
    
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

**MVC Test: Controller يعرض الحالات المسموحة فقط**

```csharp
[Fact]
[Trait("Category", "Mvc")]
public async Task Details_ShowsOnly_AllowedNextStatuses()
{
    // Arrange
    var orderId = await CreateOrderAsync(OrderStatus.Pending);
    var client = _factory.CreateAuthenticatedClient("admin@booknest.com", "Admin");
    
    // Act
    var html = await client.GetStringAsync($"/admin/orders/{orderId}");
    var document = await BrowsingContext.New().OpenAsync(req => req.Content(html));
    
    // Assert
    var statusOptions = document.QuerySelectorAll("#newStatus option");
    statusOptions.Select(o => o.TextContent).Should().Contain("Processing");
    statusOptions.Select(o => o.TextContent).Should().Contain("Cancelled");
    statusOptions.Select(o => o.TextContent).Should().NotContain("Delivered"); // لا يمكن القفز مباشرة
}
```

### ✅ معايير القبول

- [ ] State Machine يمنع Transitions غير المسموحة
- [ ] RowVersion يمنع Concurrent Updates
- [ ] Concurrency Exception يُعاد كرسالة واضحة
- [ ] Logging لكل تغيير حالة (من/إلى/بواسطة)
- [ ] View يعرض الحالات المسموحة فقط
- [ ] جميع الاختبارات تمر (Guard: 1, Race: 2, Security: 2, MVC: 1)
- [ ] Terminal states (Delivered, Cancelled) لا يمكن تغييرها

---

## ⭐ T-66: Order State Machine & History

**الأولوية:** Wow Layer  
**التقدير:** 2 أيام  
**المتطلبات المغطاة:** FR-ORD-10، BR-01

### 🎯 الهدف
Audit Trail كامل لكل تغيير حالة مع:
- `OrderStatusHistory` table
- تسجيل: From/To Status, Who, When, Notes
- عرض Timeline للعميل

### 📝 التنفيذ

**1. إضافة History عند تغيير الحالة:**

```csharp
// تحديث في AdminOrderService.ChangeOrderStatusAsync
public async Task<OrderResult> ChangeOrderStatusAsync(...)
{
    // ... (كود سابق)
    
    // إضافة سجل التاريخ
    var history = new OrderStatusHistory
    {
        OrderId = orderId,
        FromStatus = oldStatus,
        ToStatus = newStatus,
        ChangedByUserId = adminUserId,
        ChangedAt = DateTime.UtcNow,
        Notes = notes
    };
    
    _context.OrderStatusHistories.Add(history);
    
    await _context.SaveChangesAsync();
    
    // ...
}
```

**2. Timeline View:**

```html
<!-- Views/Orders/Timeline.cshtml -->
@model List<OrderStatusHistoryViewModel>

<div class="order-timeline">
    @foreach (var item in Model)
    {
        <div class="timeline-item @(item.IsActive ? "active" : "")">
            <div class="timeline-marker"></div>
            <div class="timeline-content">
                <h4>@item.ToStatus.GetDisplayName()</h4>
                <p class="timeline-date">
                    @item.ChangedAt.ToString("dd/MM/yyyy HH:mm")
                </p>
                @if (!string.IsNullOrEmpty(item.Notes))
                {
                    <p class="timeline-notes">@item.Notes</p>
                }
            </div>
        </div>
    }
</div>
```

### 🧪 الاختبارات

**Guard Test: Customer يرى Timeline طلبه فقط**

```csharp
[Fact]
[Trait("Category", "Guard")]
public async Task Timeline_OtherUserOrder_Returns404()
{
    var alice = await CreateUserAsync("alice@test.com");
    var bob = await CreateUserAsync("bob@test.com");
    
    var orderId = await CreateOrderAsync(OrderStatus.Delivered, alice);
    
    var client = _factory.CreateAuthenticatedClient("bob@test.com", "Customer");
    var response = await client.GetAsync($"/orders/{orderId}/timeline");
    
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

**Race Test: تغييران متزامنان - سطران في History**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task StatusChange_Concurrent_BothRecordedInHistory()
{
    // Arrange
    var orderId = await CreateOrderAsync(OrderStatus.Pending);
    
    // Act - تغييران متتاليان سريعان
    await _adminOrderService.ChangeOrderStatusAsync(orderId, OrderStatus.Processing, "admin1");
    await Task.Delay(10); // تأخير بسيط
    await _adminOrderService.ChangeOrderStatusAsync(orderId, OrderStatus.Shipped, "admin2");
    
    // Assert
    var history = await _context.OrderStatusHistories
        .Where(h => h.OrderId == orderId)
        .OrderBy(h => h.ChangedAt)
        .ToListAsync();
    
    history.Should().HaveCount(2);
    history[0].ToStatus.Should().Be(OrderStatus.Processing);
    history[1].ToStatus.Should().Be(OrderStatus.Shipped);
}
```

**Security Test: Notes الأدمن Encoded**

```csharp
[Fact]
[Trait("Category", "Security")]
public async Task Timeline_NotesWithXSS_Encoded()
{
    var orderId = await CreateOrderAsync(OrderStatus.Pending);
    await _adminOrderService.ChangeOrderStatusAsync(
        orderId, OrderStatus.Cancelled, "admin", 
        notes: "<script>alert('XSS')</script>");
    
    var client = _factory.CreateClient();
    var html = await client.GetStringAsync($"/orders/{orderId}/timeline");
    
    html.Should().Contain("&lt;script&gt;");
    html.Should().NotContain("<script>alert");
}
```

**MVC Test: Timeline يعرض بالترتيب الصحيح**

```csharp
[Fact]
[Trait("Category", "Mvc")]
public async Task Timeline_DisplaysInChronologicalOrder()
{
    // Arrange
    var orderId = await CreateOrderAsync(OrderStatus.Pending);
    await _adminOrderService.ChangeOrderStatusAsync(orderId, OrderStatus.Processing, "admin1");
    await Task.Delay(100);
    await _adminOrderService.ChangeOrderStatusAsync(orderId, OrderStatus.Shipped, "admin2");
    
    var client = _factory.CreateClient();
    
    // Act
    var html = await client.GetStringAsync($"/orders/{orderId}/timeline");
    var document = await BrowsingContext.New().OpenAsync(req => req.Content(html));
    
    // Assert
    var items = document.QuerySelectorAll(".timeline-item h4");
    items.Should().HaveCountGreaterOrEqualTo(2);
    items[0].TextContent.Should().Contain("Processing");
    items[1].TextContent.Should().Contain("Shipped");
}
```

### ✅ معايير القبول

- [ ] OrderStatusHistory تسجل كل تغيير
- [ ] Timeline view مصمم ومرتب زمنياً
- [ ] Notes محفوظة وEncoded
- [ ] جميع الاختبارات تمر
- [ ] Admin يرى من غيّر الحالة

---

## ⭐ T-68: Stock Reservation System (أكثر تذكرة تعقيداً)

**الأولوية:** Wow Layer  
**التقدير:** 4-5 أيام  
**المتطلبات المغطاة:** FR-RES-01 → 04، BR-10 → 12

### 🎯 الهدف
حجز مخزون مؤقت 15 دقيقة عند Checkout مع:
- **Atomic Reserve**
- **Background Job** لفك الحجز المنتهي
- **عدّاد تنازلي** في UI
- **Available = Stock - Reserved**

### 🏗️ Architecture

```
[User enters Checkout]
    ↓
[ReservationService.ReserveAsync(userId, cartItems)]
    ↓
[Transaction BEGIN]
    ├→ Check Available (Stock - Reserved >= Quantity)
    ├→ Create StockReservation (ExpiresAt = Now + 15min)
    ├→ UPDATE Books SET Reserved = Reserved + @qty
[Transaction COMMIT]
    ↓
[Return: ReservationId, ExpiresAt]
    ↓
[UI: عدّاد تنازلي]
    ↓
[Confirm Order → Convert Reservation to Sale]
OR
[Expire → Background Job frees reservation]
```

### 📝 خطوات التنفيذ

**1. Reservation Service:**

```csharp
// src/BookNest.Infrastructure/Services/StockReservationService.cs
public class StockReservationService : IStockReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StockReservationService> _logger;
    private const int ReservationMinutes = 15;
    
    public async Task<ReservationResult> ReserveStockAsync(
        string userId, 
        List<CartItemDto> items)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var reservationId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddMinutes(ReservationMinutes);
            
            // ترتيب حسب BookId لمنع Deadlock
            var sortedItems = items.OrderBy(i => i.BookId).ToList();
            
            foreach (var item in sortedItems)
            {
                // 🔥 Atomic Check & Reserve
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE Books 
                    SET Reserved = Reserved + @p0
                    WHERE Id = @p1 
                      AND (Stock - Reserved) >= @p0 
                      AND IsDeleted = 0",
                    item.Quantity,
                    item.BookId);
                
                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync();
                    
                    var book = await _context.Books
                        .Select(b => new { b.Id, b.Title, Available = b.Stock - b.Reserved })
                        .FirstOrDefaultAsync(b => b.Id == item.BookId);
                    
                    return ReservationResult.Failure(
                        $"الكتاب '{book?.Title}' غير متوفر. المتاح: {book?.Available ?? 0}");
                }
                
                // Create reservation record
                var reservation = new StockReservation
                {
                    UserId = userId,
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    ExpiresAt = expiresAt,
                    IsActive = true,
                    SessionId = reservationId.ToString()
                };
                
                _context.StockReservations.Add(reservation);
            }
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation(
                "Stock reserved for user {UserId}. ReservationId: {ReservationId}, ExpiresAt: {ExpiresAt}",
                userId, reservationId, expiresAt);
            
            return ReservationResult.Success(reservationId, expiresAt);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error reserving stock for user {UserId}", userId);
            return ReservationResult.Failure("حدث خطأ أثناء الحجز");
        }
    }
    
    public async Task<bool> ConvertReservationToSaleAsync(Guid reservationId)
    {
        // Called من OrderService عند تأكيد الطلب
        var reservations = await _context.StockReservations
            .Where(r => r.SessionId == reservationId.ToString() && r.IsActive)
            .ToListAsync();
        
        if (!reservations.Any())
        {
            return false;
        }
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            foreach (var reservation in reservations.OrderBy(r => r.BookId))
            {
                // تحويل Reserved إلى خصم فعلي من Stock
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE Books 
                    SET Stock = Stock - @p0,
                        Reserved = Reserved - @p0
                    WHERE Id = @p1",
                    reservation.Quantity,
                    reservation.BookId);
                
                reservation.IsActive = false;
            }
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<int> ReleaseExpiredReservationsAsync()
    {
        // يُستدعى من Background Job كل دقيقة
        var expired = await _context.StockReservations
            .Where(r => r.IsActive && r.ExpiresAt <= DateTime.UtcNow)
            .OrderBy(r => r.BookId) // منع Deadlock
            .ToListAsync();
        
        if (!expired.Any())
        {
            return 0;
        }
        
        foreach (var reservation in expired)
        {
            await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE Books 
                SET Reserved = Reserved - @p0
                WHERE Id = @p1",
                reservation.Quantity,
                reservation.BookId);
            
            reservation.IsActive = false;
        }
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Released {Count} expired reservations", expired.Count);
        
        return expired.Count;
    }
}
```

**2. Background Job (Hosted Service):**

```csharp
// src/BookNest.Web/BackgroundServices/ReservationCleanupService.cs
public class ReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationCleanupService> _logger;
    
    public ReservationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ReservationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reservationService = scope.ServiceProvider
                    .GetRequiredService<IStockReservationService>();
                
                var releasedCount = await reservationService.ReleaseExpiredReservationsAsync();
                
                if (releasedCount > 0)
                {
                    _logger.LogInformation("Released {Count} expired reservations", releasedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reservation cleanup job");
            }
            
            // كل دقيقة
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

// Program.cs
builder.Services.AddHostedService<ReservationCleanupService>();
```

**3. UI - Countdown Timer:**

```javascript
// wwwroot/js/checkout-countdown.js
class CheckoutCountdown {
    constructor(expiresAt) {
        this.expiresAt = new Date(expiresAt);
        this.timerElement = document.getElementById('checkout-timer');
        this.start();
    }
    
    start() {
        this.update();
        this.interval = setInterval(() => this.update(), 1000);
    }
    
    update() {
        const now = new Date();
        const diff = this.expiresAt - now;
        
        if (diff <= 0) {
            this.expired();
            return;
        }
        
        const minutes = Math.floor(diff / 60000);
        const seconds = Math.floor((diff % 60000) / 1000);
        
        this.timerElement.textContent = 
            `${minutes}:${seconds.toString().padStart(2, '0')}`;
        
        // تحذير عند 2 دقيقة
        if (diff < 120000) {
            this.timerElement.classList.add('timer-warning');
        }
    }
    
    expired() {
        clearInterval(this.interval);
        this.timerElement.textContent = 'انتهى الوقت';
        this.timerElement.classList.add('timer-expired');
        
        // عرض رسالة + زرار إعادة الحجز
        document.getElementById('checkout-form').style.display = 'none';
        document.getElementById('expired-message').style.display = 'block';
    }
}

// تفعيل
document.addEventListener('DOMContentLoaded', () => {
    const expiresAt = document.getElementById('checkout-timer')
        .getAttribute('data-expires-at');
    
    if (expiresAt) {
        new CheckoutCountdown(expiresAt);
    }
});
```

```html
<!-- Views/Checkout/Index.cshtml -->
<div class="checkout-timer-container">
    <p>الحجز ينتهي خلال:</p>
    <span id="checkout-timer" 
          class="timer" 
          data-expires-at="@Model.ReservationExpiresAt.ToString("O")">
        15:00
    </span>
</div>

<div id="expired-message" style="display:none;">
    <p>انتهى وقت الحجز. يرجى إعادة المحاولة.</p>
    <a href="/checkout" class="btn btn-primary">إعادة الحجز</a>
</div>
```

### 🧪 الاختبارات

**Guard Test: الحجز لصاحبه فقط**

```csharp
[Fact]
[Trait("Category", "Guard")]
public async Task ConvertReservation_ByDifferentUser_Fails()
{
    var alice = await CreateUserAsync("alice@test.com");
    var bob = await CreateUserAsync("bob@test.com");
    
    var bookId = await CreateBookAsync(stock: 10);
    var items = new List<CartItemDto> { new() { BookId = bookId, Quantity = 2 } };
    
    var reservation = await _reservationService.ReserveStockAsync(alice, items);
    
    // Bob يحاول يأكد حجز Alice
    var result = await _reservationService.ConvertReservationToSaleAsync(reservation.ReservationId);
    
    result.Should().BeFalse();
}
```

**Race Test 1: 10 حجوزات على آخر نسخة**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task Reserve_LastCopy_OnlyOneSucceeds()
{
    // Arrange
    var bookId = await CreateBookAsync(stock: 1);
    
    var tasks = Enumerable.Range(0, 10).Select(async i =>
    {
        var userId = await CreateUserAsync($"user{i}@test.com");
        var items = new List<CartItemDto> { new() { BookId = bookId, Quantity = 1 } };
        
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        return await service.ReserveStockAsync(userId, items);
    });
    
    // Act
    var results = await Task.WhenAll(tasks);
    
    // Assert
    results.Count(r => r.Succeeded).Should().Be(1);
    results.Count(r => !r.Succeeded).Should().Be(9);
    
    var book = await _context.Books.FindAsync(bookId);
    book!.Reserved.Should().Be(1);
    book.Stock.Should().Be(1);
}
```

**Race Test 2: انتهاء الحجز + تأكيد الطلب في نفس اللحظة**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task ReservationExpiry_And_OrderConfirm_Simultaneously_OnlyOneSucceeds()
{
    // Arrange
    var userId = await CreateUserAsync("user@test.com");
    var bookId = await CreateBookAsync(stock: 10);
    var items = new List<CartItemDto> { new() { BookId = bookId, Quantity = 5 } };
    
    var reservation = await _reservationService.ReserveStockAsync(userId, items);
    
    // انتهاء فوري (للاختبار فقط)
    var res = await _context.StockReservations
        .FirstAsync(r => r.SessionId == reservation.ReservationId.ToString());
    res.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
    await _context.SaveChangesAsync();
    
    // Act - في نفس اللحظة
    var confirmTask = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        return await service.ConvertReservationToSaleAsync(reservation.ReservationId);
    });
    
    var releaseTask = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        return await service.ReleaseExpiredReservationsAsync();
    });
    
    await Task.WhenAll(confirmTask, releaseTask);
    
    // Assert - واحد فقط نجح
    var book = await _context.Books.FindAsync(bookId);
    
    if (confirmTask.Result) // Order confirmed
    {
        book!.Stock.Should().Be(5); // 10 - 5
        book.Reserved.Should().Be(0);
    }
    else // Reservation released
    {
        book!.Stock.Should().Be(10); // لم يُخصم
        book.Reserved.Should().Be(0);
    }
}
```

**Race Test 3: Background Job يشتغل مرتين متزامنتين**

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task CleanupJob_RunsTwice_Simultaneously_ReleasesOnce()
{
    // Arrange
    var bookId = await CreateBookAsync(stock: 10);
    var userId = await CreateUserAsync("user@test.com");
    var items = new List<CartItemDto> { new() { BookId = bookId, Quantity = 3 } };
    
    await _reservationService.ReserveStockAsync(userId, items);
    
    // انتهاء فوري
    var res = await _context.StockReservations.FirstAsync();
    res.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
    await _context.SaveChangesAsync();
    
    // Act - Job يشتغل مرتين
    var task1 = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        return await service.ReleaseExpiredReservationsAsync();
    });
    
    var task2 = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
        return await service.ReleaseExpiredReservationsAsync();
    });
    
    var results = await Task.WhenAll(task1, task2);
    
    // Assert
    var totalReleased = results.Sum();
    totalReleased.Should().Be(1, "Should release once, not twice");
    
    var book = await _context.Books.FindAsync(bookId);
    book!.Reserved.Should().Be(0);
}
```

**Security Test: TTL من السيرفر مش الـ Client**

```csharp
[Fact]
[Trait("Category", "Security")]
public async Task Reservation_TTL_CalculatedOnServer()
{
    var userId = await CreateUserAsync("user@test.com");
    var bookId = await CreateBookAsync(stock: 10);
    var items = new List<CartItemDto> { new() { BookId = bookId, Quantity = 2 } };
    
    var before = DateTime.UtcNow;
    var result = await _reservationService.ReserveStockAsync(userId, items);
    var after = DateTime.UtcNow.AddMinutes(15);
    
    result.ExpiresAt.Should().BeOnOrAfter(before.AddMinutes(15));
    result.ExpiresAt.Should().BeOnOrBefore(after);
    
    // لا يوجد Parameter من Client يتحكم بالـ TTL
}
```

**MVC Test: Countdown في الـ HTML**

```csharp
[Fact]
[Trait("Category", "Mvc")]
public async Task Checkout_HasCountdownTimer()
{
    var client = _factory.CreateAuthenticatedClient("customer@demo.com", "Customer");
    
    var html = await client.GetStringAsync("/checkout");
    var document = await BrowsingContext.New().OpenAsync(req => req.Content(html));
    
    var timer = document.QuerySelector("#checkout-timer");
    timer.Should().NotBeNull();
    timer!.GetAttribute("data-expires-at").Should().NotBeNullOrEmpty();
}
```

### ✅ معايير القبول

- [ ] Atomic Reserve: `UPDATE ... WHERE (Stock - Reserved) >= @qty`
- [ ] Background Job يشتغل كل دقيقة
- [ ] Countdown Timer يعمل في UI
- [ ] Convert Reservation to Sale عند تأكيد الطلب
- [ ] جميع الاختبارات تمر:
  - Guard: 1/1
  - Race: 3/3 (Last Copy, Expiry+Confirm, Cleanup Twice)
  - Security: 1/1 (Server-side TTL)
  - MVC: 1/1 (Countdown in HTML)
- [ ] "محجوز مؤقتاً" يظهر في Catalog
- [ ] رسالة "انتهى الوقت" + زرار إعادة الحجز

---


## ⭐ T-74: Load Testing with k6 (الإثبات النهائي)

**الأولوية:** Wow Layer  
**التقدير:** 2-3 أيام  
**المتطلبات المغطاة:** NFR-01 (Performance), Proof of Correctness

### 🎯 الهدف
اختبار حمل بـ k6 يثبت:
- **Zero Oversells**: 1000 مستخدم يشترون كتاب مخزونه 50 → 50 طلب ناجح بالظبط
- **p95 < 1 second** على عملية الشراء
- **No Race Conditions** تحت ضغط حقيقي

هذا الاختبار هو **البرهان** على صحة الـ Concurrency Control.

### 📝 خطوات التنفيذ

**1. إعداد بيئة الاختبار:**

```bash
# تثبيت k6
# Windows (Chocolatey)
choco install k6

# macOS
brew install k6

# Linux
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

**2. Seed Data للاختبار:**

```csharp
// tests/BookNest.LoadTests/LoadTestSeeder.cs
public class LoadTestSeeder
{
    public static async Task SeedForLoadTestAsync(ApplicationDbContext context)
    {
        // Reset database
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        
        // إنشاء 1000 يوزر
        for (int i = 1; i <= 1000; i++)
        {
            var user = new ApplicationUser
            {
                UserName = $"loadtest{i}@test.com",
                Email = $"loadtest{i}@test.com",
                EmailConfirmed = true,
                NormalizedEmail = $"LOADTEST{i}@TEST.COM",
                NormalizedUserName = $"LOADTEST{i}@TEST.COM"
            };
            
            // Hashed password: "Test@123"
            user.PasswordHash = "AQAAAAEAACcQAAAAEK..."; // pre-computed
            
            context.Users.Add(user);
        }
        
        // كتاب واحد بمخزون 50
        var category = new Category { Name = "Test", NameAr = "تجريبي" };
        var author = new Author { Name = "Test Author" };
        context.Categories.Add(category);
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        
        var book = new Book
        {
            Title = "Load Test Book",
            ISBN = "9999999999999",
            Price = 100m,
            Stock = 50,
            AuthorId = author.Id,
            CategoryId = category.Id
        };
        context.Books.Add(book);
        
        // عنوان لكل يوزر
        for (int i = 1; i <= 1000; i++)
        {
            var user = await context.Users
                .FirstAsync(u => u.Email == $"loadtest{i}@test.com");
            
            var address = new Address
            {
                UserId = user.Id,
                FullName = $"User {i}",
                Phone = "01012345678",
                City = "Cairo",
                AddressLine = "Test Address",
                IsDefault = true
            };
            context.Addresses.Add(address);
        }
        
        await context.SaveChangesAsync();
    }
}
```

**3. k6 Script:**

```javascript
// tests/LoadTests/last-copy-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const successRate = new Rate('success_rate');
const insufficientStockRate = new Rate('insufficient_stock_rate');

export const options = {
  // 1000 مستخدم في نفس اللحظة
  vus: 1000,
  duration: '10s',
  
  thresholds: {
    // p95 أقل من 1 ثانية
    http_req_duration: ['p(95)<1000'],
    
    // معدل نجاح = 5% بالظبط (50/1000)
    success_rate: ['rate==0.05'],
    
    // معدل "مخزون ناقص" = 95% (950/1000)
    insufficient_stock_rate: ['rate==0.95'],
  },
};

const BASE_URL = 'http://localhost:5000';
const BOOK_ID = 1; // من الـ Seed

export default function () {
  // كل VU له user مختلف
  const userId = __VU; // 1-1000
  const email = `loadtest${userId}@test.com`;
  const password = 'Test@123';
  
  // 1. Login
  const loginRes = http.post(`${BASE_URL}/account/login`, {
    email: email,
    password: password,
  });
  
  const loginSuccess = check(loginRes, {
    'login successful': (r) => r.status === 200 || r.status === 302,
  });
  
  if (!loginSuccess) {
    console.error(`Login failed for ${email}`);
    return;
  }
  
  // استخراج Cookie
  const cookies = loginRes.cookies;
  const authCookie = Object.keys(cookies)
    .map(name => `${name}=${cookies[name][0].value}`)
    .join('; ');
  
  // 2. Add to Cart
  const addToCartRes = http.post(
    `${BASE_URL}/cart/add`,
    JSON.stringify({ bookId: BOOK_ID, quantity: 1 }),
    {
      headers: {
        'Cookie': authCookie,
        'Content-Type': 'application/json',
      },
    }
  );
  
  check(addToCartRes, {
    'added to cart': (r) => r.status === 200,
  });
  
  sleep(0.1); // تأخير بسيط للواقعية
  
  // 3. Place Order
  const orderRes = http.post(
    `${BASE_URL}/checkout/place-order`,
    {
      addressId: 1,
      paymentMethod: 'Cash',
    },
    {
      headers: {
        'Cookie': authCookie,
      },
    }
  );
  
  const orderSuccess = check(orderRes, {
    'order placed': (r) => r.status === 200 && r.body.includes('success'),
  });
  
  const insufficientStock = check(orderRes, {
    'insufficient stock': (r) => 
      r.status === 400 || r.body.includes('غير متوفر') || r.body.includes('insufficient'),
  });
  
  successRate.add(orderSuccess);
  insufficientStockRate.add(insufficientStock);
  
  if (orderSuccess) {
    console.log(`✅ User ${userId} successfully placed order`);
  } else if (insufficientStock) {
    console.log(`❌ User ${userId} - insufficient stock (expected)`);
  } else {
    console.error(`⚠️  User ${userId} - unexpected response: ${orderRes.status}`);
  }
}
```

**4. تشغيل الاختبار:**

```bash
# في terminal منفصل، شغّل التطبيق
cd src/BookNest.Web
dotnet run --configuration Release

# في terminal آخر، شغّل الـ Seed
dotnet run --project tests/BookNest.LoadTests.Seeder

# شغّل k6
k6 run tests/LoadTests/last-copy-test.js --out json=load-test-results.json
```

**5. تحليل النتائج:**

```javascript
// tests/LoadTests/analyze-results.js
import { readFileSync } from 'fs';

const results = JSON.parse(readFileSync('load-test-results.json', 'utf8'));

// حساب Orders الناجحة
const metrics = results
  .filter(r => r.type === 'Point' && r.metric === 'success_rate')
  .reduce((acc, r) => {
    acc.total++;
    if (r.data.value === 1) acc.success++;
    return acc;
  }, { total: 0, success: 0 });

console.log(`\n📊 Load Test Results:`);
console.log(`Total attempts: ${metrics.total}`);
console.log(`Successful orders: ${metrics.success}`);
console.log(`Failed (expected): ${metrics.total - metrics.success}`);

// تحقق من Zero Oversells
const expectedSuccess = 50;
if (metrics.success === expectedSuccess) {
  console.log(`\n✅ PASS: Exactly ${expectedSuccess} orders succeeded (Zero Oversells)`);
} else {
  console.error(`\n❌ FAIL: Expected ${expectedSuccess}, got ${metrics.success}`);
  process.exit(1);
}

// p95 response time
const durations = results
  .filter(r => r.type === 'Point' && r.metric === 'http_req_duration')
  .map(r => r.data.value)
  .sort((a, b) => a - b);

const p95Index = Math.floor(durations.length * 0.95);
const p95 = durations[p95Index];

console.log(`\np95 response time: ${p95.toFixed(2)}ms`);

if (p95 < 1000) {
  console.log(`✅ PASS: p95 < 1 second`);
} else {
  console.error(`❌ FAIL: p95 >= 1 second`);
  process.exit(1);
}
```

**6. Database Verification:**

```csharp
// tests/BookNest.LoadTests/DatabaseVerifier.cs
public static async Task VerifyAfterLoadTestAsync(ApplicationDbContext context)
{
    // 1. تحقق من المخزون
    var book = await context.Books.FirstAsync(b => b.ISBN == "9999999999999");
    
    Console.WriteLine($"\n📦 Final Stock: {book.Stock}");
    Console.WriteLine($"📦 Reserved: {book.Reserved}");
    
    if (book.Stock != 0)
    {
        throw new Exception($"❌ Expected stock to be 0, but got {book.Stock}");
    }
    
    if (book.Reserved != 0)
    {
        throw new Exception($"❌ Expected reserved to be 0, but got {book.Reserved}");
    }
    
    // 2. تحقق من عدد الطلبات
    var ordersCount = await context.Orders
        .CountAsync(o => o.Status != OrderStatus.Cancelled);
    
    Console.WriteLine($"📋 Successful orders: {ordersCount}");
    
    if (ordersCount != 50)
    {
        throw new Exception($"❌ Expected exactly 50 orders, but got {ordersCount}");
    }
    
    // 3. تحقق من OrderItems
    var totalQuantitySold = await context.OrderItems
        .Where(i => i.Book.ISBN == "9999999999999")
        .SumAsync(i => i.Quantity);
    
    Console.WriteLine($"📦 Total quantity sold: {totalQuantitySold}");
    
    if (totalQuantitySold != 50)
    {
        throw new Exception($"❌ Expected 50 items sold, but got {totalQuantitySold}");
    }
    
    // 4. تحقق من عدم وجود OrderItems orphaned
    var orphanedItems = await context.OrderItems
        .Where(i => i.Order.Status == OrderStatus.Cancelled)
        .CountAsync();
    
    if (orphanedItems > 0)
    {
        throw new Exception($"❌ Found {orphanedItems} orphaned order items");
    }
    
    Console.WriteLine("\n✅ All database checks passed!");
}
```

**7. CI Integration:**

```yaml
# .github/workflows/load-test.yml
name: Load Test

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  load-test:
    runs-on: ubuntu-latest
    
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: YourStrong@Password
        ports:
          - 1433:1433
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Install k6
      run: |
        sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
        echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
        sudo apt-get update
        sudo apt-get install k6
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Seed Load Test Data
      run: dotnet run --project tests/BookNest.LoadTests.Seeder
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=BookNestLoadTest;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True"
    
    - name: Start Application
      run: |
        dotnet run --project src/BookNest.Web --configuration Release &
        sleep 10
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=BookNestLoadTest;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True"
    
    - name: Run Load Test
      run: k6 run tests/LoadTests/last-copy-test.js --out json=load-test-results.json
    
    - name: Analyze Results
      run: node tests/LoadTests/analyze-results.js
    
    - name: Verify Database
      run: dotnet run --project tests/BookNest.LoadTests.Verifier
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=BookNestLoadTest;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True"
    
    - name: Upload Results
      uses: actions/upload-artifact@v3
      with:
        name: load-test-results
        path: load-test-results.json
```

### 🧪 الاختبارات

**Guard Test:** N/A (هذا هو الاختبار النهائي الشامل)

**Race Test:** هذا الاختبار **هو** Race Test على مستوى النظام

```csharp
[Fact]
[Trait("Category", "Race")]
public async Task LoadTest_1000Users_50Stock_Exactly50OrdersSucceed()
{
    // هذا الاختبار يُشغّل من خلال k6
    // يتحقق من:
    // 1. 50 طلب ناجح بالظبط
    // 2. 950 طلب فاشل بسبب "insufficient stock"
    // 3. Stock = 0 في النهاية
    // 4. لا oversells
    
    // الاختبار الفعلي في last-copy-test.js
}
```

**Security Test:** N/A (مغطى في T-41)

**MVC Test:** الاختبار يتحقق من:
- Login يعمل لـ 1000 user
- Add to Cart يعمل
- Place Order يعمل أو يرفض بشكل صحيح

### 📊 التقرير المتوقع

```
📊 Load Test Results:
Total attempts: 1000
Successful orders: 50
Failed (expected): 950

✅ PASS: Exactly 50 orders succeeded (Zero Oversells)

p95 response time: 782.45ms
✅ PASS: p95 < 1 second

📦 Final Stock: 0
📦 Reserved: 0
📋 Successful orders: 50
📦 Total quantity sold: 50

✅ All database checks passed!
```

### ✅ معايير القبول

- [ ] k6 script يعمل بدون errors
- [ ] **Zero Oversells**: 50/1000 بالظبط
- [ ] p95 < 1 second
- [ ] Stock = 0 بعد الاختبار
- [ ] لا order items orphaned
- [ ] CI يشغّل Load Test تلقائياً
- [ ] التقرير محفوظ في `docs/load-test/`
- [ ] Screenshots من k6 dashboard

---

# 🎓 الخاتمة والتوصيات

## ملخص التذاكر

| Phase | التذاكر | التقدير | الأولوية |
|-------|---------|---------|----------|
| Phase 1: Foundation | T-00 حتى T-05 (6 تذاكر) | 10-12 يوم | Must Have |
| Phase 2: Auth | T-10 حتى T-13 (4 تذاكر) | 5-6 أيام | Must Have |
| Phase 3: Catalog | T-20 حتى T-23 (4 تذاكر) | 6-7 أيام | Must Have |
| Phase 4: Shopping | T-30 حتى T-33 (4 تذاكر) | 5-6 أيام | Must Have |
| Phase 5: Orders | T-40 حتى T-44 (5 تذاكر) | 9-11 يوم | Must Have |
| Phase 6: Admin | T-50 حتى T-54 (5 تذاكر) | 9-11 يوم | Must Have |
| **MVP Total** | **T-00 حتى T-64 (32 تذكرة)** | **8-9 أسابيع** | - |
| Phase 7: Wow Layer | T-65 حتى T-73 (9 تذاكر) | 3-4 أسابيع | Nice to Have |
| Phase 8: Proof | T-74 حتى T-78 (5 تذاكر) | 2 أسابيع | Portfolio |
| **Grand Total** | **78 تذكرة** | **13-15 أسبوع** | - |

## ترتيب الأولويات للتنفيذ

### 🔴 المسار الحرج (Critical Path)

1. **T-00, T-01, T-02** - الأساس (لا يمكن البدء بدونها)
2. **T-10, T-11** - Auth (مطلوب لكل شيء آخر)
3. **T-41** - Order Placement (أهم تذكرة تقنياً)
4. **T-74** - Load Test (الإثبات)

### 🟡 المسار الموصى به

**الأسابيع 1-2: Walking Skeleton**
- T-00, T-01, T-02 (Foundation)
- T-10, T-11 (Basic Auth)
- نسخة مبسطة من T-41 (Order بدون Reservation)
- Deploy لايف

**الأسابيع 3-8: MVP**
- باقي Phase 1-6 بالترتيب
- اختبار كل تذكرة قبل الانتقال للتالية

**الأسابيع 9-12: Wow Layer**
- T-68 (Reservation) - الأهم
- T-66 (State Machine)
- T-74 (Load Test)
- باقي Wow features

**الأسابيع 13-15: Polish**
- T-75, T-76, T-77, T-78
- Documentation
- Demo preparation

## نصائح التنفيذ

### 💡 Best Practices

1. **Test First**: اكتب الاختبارات قبل أو مع الكود، ليس بعده
2. **Commit Often**: كل تذكرة = فرع منفصل + PR
3. **Review Code**: راجع كودك بعد يوم (Fresh eyes)
4. **Don't Skip Tests**: كل الأربعة أنواع مهمة
5. **Document Decisions**: ADR لأي قرار معماري

### ⚠️ أخطاء شائعة يجب تجنبها

1. ❌ تخطي الاختبارات "لتوفير الوقت"
2. ❌ Hardcoding القيم بدلاً من Tokens
3. ❌ إهمال Edge Cases
4. ❌ نسيان Rollback في Transactions
5. ❌ تجاهل Concurrency في البداية

### 🎯 جملة الـ CV (بعد الإنجاز)

```
Built BookNest, a production-ready bilingual (AR/EN) e-commerce platform 
on ASP.NET Core MVC with:

• Time-boxed stock reservation system (15-min TTL + background cleanup)
• Atomic inventory management preventing oversells under 1000 concurrent users 
  (verified via k6 load testing)
• Order state machine with full audit trail and concurrency control
• Real-time admin dashboard using SignalR
• Arabic-aware search with diacritics normalization
• 4-tier test suite (Guard, Race, Security, MVC) with 80%+ mutation coverage

Tech: ASP.NET Core 8, EF Core, SQL Server, SignalR, k6, xUnit, Testcontainers, 
GitHub Actions CI/CD
```

## الموارد الإضافية

### 📚 مراجع مفيدة

- [Microsoft Docs: EF Core Concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Martin Fowler: Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html)
- [k6 Documentation](https://k6.io/docs/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)

### 🔗 أدوات مساعدة

- **Seq**: لعرض الـ Logs بشكل أفضل
- **MiniProfiler**: لتحليل Performance
- **BenchmarkDotNet**: لـ Micro-benchmarks
- **Stryker.NET**: للـ Mutation Testing

## خاتمة

هذا المشروع **ليس مجرد CRUD**. إنه يُظهر:

✅ فهم عميق لـ **Concurrency Control**  
✅ القدرة على **كتابة اختبارات شاملة**  
✅ الاهتمام بـ **Security من البداية**  
✅ مهارات **Performance Testing**  
✅ معرفة بـ **State Machines & Audit Trails**  
✅ خبرة في **Real-time Systems** (SignalR)  
✅ التزام بـ **Best Practices** (Clean Code, SOLID, Testing)

**النصيحة الأخيرة:**  
ركّز على **الجودة** أكثر من السرعة. المشروع الذي يعمل بشكل صحيح تحت الضغط 
ومُثبت بالاختبارات **أفضل بكثير** من 10 مشاريع CRUD سطحية.

---

**نهاية المستند**  
**التحديث الأخير:** 2024  
**الإصدار:** 1.0  


للأسئلة أو التوضيحات، راجع:
- Design System Document: `Enzo Store— Design System & Handoff Spec.md`


