# Enzo Book — test suites

All commands run from the repository root.

## 1. Build

```bash
dotnet build BookNest.sln -c Release -p:TreatWarningsAsErrors=true
```

## 2. Fast suites (no database container needed)

```bash
dotnet test tests/BookNest.UnitTests        -c Release --no-build
dotnet test tests/BookNest.ArchitectureTests -c Release --no-build
dotnet test tests/BookNest.IntegrationTests -c Release --no-build \
  --filter "Category=Mvc&FullyQualifiedName!~MigrationTests"
```

The MVC smoke tests replace the database-backed services with stubs, so they run anywhere.

## 3. Race-condition + data-integrity suite (Windows / LocalDB)

`tests/BookStore.Tests/RaceConditionTests.cs` creates a throwaway
`BookNestRace_<guid>` LocalDB database, applies every migration, then runs the real
services concurrently:

- 2 buyers, last copy in stock → exactly one order
- 20 simultaneous buyers, one copy → exactly one order, stock 0
- same idempotency key submitted 10× → one order, every submit gets the same confirmation
- 8 simultaneous reviews by one user → one row
- 6 simultaneous registrations with one email → one account (unique index)
- DB CHECK constraints reject negative stock/price, zero quantity, rating 0
- 5 simultaneous cancellations → stock restored once

```bash
dotnet test tests/BookStore.Tests -c Release --no-build --filter "Category=LocalDb"
```

`tests/BookStore.Tests` also covers the production connection guard
(`DatabaseConnectionGuardTests.cs`) and runs the order flow with the same
`EnableRetryOnFailure` execution strategy the web app uses.

Run every non-Docker suite at once:

```bash
dotnet test BookNest.sln -c Release --no-build --filter "FullyQualifiedName!~MigrationTests"
```

> LocalDB only exists on Windows developer machines. The Docker/Testcontainers
> suite (`Category=Race`, `MigrationTests`) remains the portable CI path and needs Docker.

## 4. Black-box security audit (live HTTP)

Start the site (development database), then:

```bash
node tests/e2e/http-audit.mjs http://127.0.0.1:5102
```

It checks security headers, anonymous guards, CSRF, guest-cart merge, mass assignment,
HTML escaping of review comments, negative cart quantities, server-side order pricing,
double-submit idempotency, order IDOR between two customers, and the login rate limiter.
Exit code is non-zero if any check fails.

The script creates disposable `qa.*@example.test` accounts and one order. To clean them
out of the development database:

```sql
-- run with sqlcmd -S "(localdb)\mssqllocaldb" -d BookNestDb
SET QUOTED_IDENTIFIER ON;
-- restore stock first if you want the catalogue untouched:
-- UPDATE b SET b.Stock = b.Stock + x.Qty
-- FROM Books b JOIN (SELECT i.BookId, SUM(i.Quantity) Qty FROM OrderItems i
--   JOIN Orders o ON o.Id = i.OrderId JOIN AspNetUsers u ON u.Id = o.UserId
--   WHERE u.Email LIKE 'qa.%@example.test' GROUP BY i.BookId) x ON x.BookId = b.Id;
DELETE h FROM OrderStatusHistories h JOIN Orders o ON o.Id = h.OrderId
  JOIN AspNetUsers u ON u.Id = o.UserId WHERE u.Email LIKE 'qa.%@example.test';
DELETE i FROM OrderItems i JOIN Orders o ON o.Id = i.OrderId
  JOIN AspNetUsers u ON u.Id = o.UserId WHERE u.Email LIKE 'qa.%@example.test';
DELETE o FROM Orders o JOIN AspNetUsers u ON u.Id = o.UserId WHERE u.Email LIKE 'qa.%@example.test';
DELETE r FROM BookReviews r JOIN AspNetUsers u ON u.Id = r.UserId WHERE u.Email LIKE 'qa.%@example.test';
DELETE a FROM Addresses a JOIN AspNetUsers u ON u.Id = a.UserId WHERE u.Email LIKE 'qa.%@example.test';
DELETE c FROM CartItems c JOIN AspNetUsers u ON u.Id = c.UserId WHERE u.Email LIKE 'qa.%@example.test';
DELETE ur FROM AspNetUserRoles ur JOIN AspNetUsers u ON u.Id = ur.UserId WHERE u.Email LIKE 'qa.%@example.test';
DELETE FROM AspNetUsers WHERE Email LIKE 'qa.%@example.test';
```
