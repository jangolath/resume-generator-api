# Entity Framework Migration Commands

This document contains the Entity Framework Core commands for managing database migrations in the Resume Generator API.

## 🔧 Prerequisites

Ensure you have the following installed:
```bash
# Install EF Core CLI tools globally
dotnet tool install --global dotnet-ef

# Or update if already installed
dotnet tool update --global dotnet-ef

# Verify installation
dotnet ef --version
```

## 🗃️ Initial Migration Setup

### Create Initial Migration

```bash
# Navigate to project directory
cd ResumeGenerator.API

# Create initial migration
dotnet ef migrations add InitialCreate --context ResumeGeneratorContext

# Review generated migration files in Migrations/ folder
ls Migrations/
```

### Apply Migration to Database

```bash
# Apply migrations to database (development)
dotnet ef database update --context ResumeGeneratorContext

# Apply to specific environment
dotnet ef database update --environment Development
dotnet ef database update --environment Production
```

## 📊 Managing Migrations

### Add New Migration

```bash
# After making model changes, create new migration
dotnet ef migrations add AddNewFeature --context ResumeGeneratorContext

# Examples of common migrations:
dotnet ef migrations add AddJobLogging
dotnet ef migrations add AddTemplateVersioning
dotnet ef migrations add AddUserAuthentication
dotnet ef migrations add AddApiUsageTracking
```

### Remove Last Migration

```bash
# Remove the last migration (if not applied to database)
dotnet ef migrations remove --context ResumeGeneratorContext

# Remove and revert database changes
dotnet ef database update PreviousMigrationName --context ResumeGeneratorContext
dotnet ef migrations remove --context ResumeGeneratorContext
```

### List Migrations

```bash
# List all migrations
dotnet ef migrations list --context ResumeGeneratorContext

# Check migration status
dotnet ef migrations has-pending-model-changes --context ResumeGeneratorContext
```

## 🏭 Production Deployment

### Generate SQL Scripts

```bash
# Generate SQL script for all migrations
dotnet ef migrations script --context ResumeGeneratorContext --output migrations.sql

# Generate script for specific range
dotnet ef migrations script InitialCreate AddJobLogging --context ResumeGeneratorContext --output update.sql

# Generate idempotent script (safe to run multiple times)
dotnet ef migrations script --idempotent --context ResumeGeneratorContext --output production-migration.sql
```

### Bundle Migrations

```bash
# Create migration bundle for deployment
dotnet ef migrations bundle --context ResumeGeneratorContext --output migrate-bundle

# Create self-contained bundle
dotnet ef migrations bundle --self-contained --runtime linux-x64 --context ResumeGeneratorContext
```

## 🐳 Docker Migration Commands

### Migration in Docker Container

```bash
# Run migrations in Docker container
docker run --rm -it \
  -e ConnectionStrings__DefaultConnection="your-connection-string" \
  your-api-image \
  dotnet ef database update

# Using docker-compose
docker-compose run api dotnet ef database update
```

### Migration Init Container

Add to `docker-compose.yml`:
```yaml
services:
  migrate:
    build: .
    command: dotnet ef database update
    environment:
      - ConnectionStrings__DefaultConnection=${DATABASE_URL}
    depends_on:
      - db
    
  api:
    build: .
    depends_on:
      - migrate
      - db
```

## 🔧 Troubleshooting

### Common Issues and Solutions

#### 1. **Migration Lock Issues**
```bash
# Error: Database is locked by another process
# Solution: Kill existing connections or wait

# Check active connections (PostgreSQL)
SELECT pid, usename, application_name, state 
FROM pg_stat_activity 
WHERE datname = 'resumegenerator';

# Force disconnect (if necessary)
SELECT pg_terminate_backend(pid) 
FROM pg_stat_activity 
WHERE datname = 'resumegenerator' AND pid <> pg_backend_pid();
```

#### 2. **Context Issues**
```bash
# Error: No DbContext was found
# Solution: Ensure proper context specification

dotnet ef migrations add MigrationName --context ResumeGeneratorContext --project ResumeGenerator.API
```

#### 3. **Connection String Issues**
```bash
# Error: Connection string not found
# Solution: Set environment variable or use --connection flag

export ConnectionStrings__DefaultConnection="Host=localhost;Database=resumegenerator;Username=user;Password=pass"
dotnet ef database update

# Or specify directly
dotnet ef database update --connection "Host=localhost;Database=resumegenerator;Username=user;Password=pass"
```

#### 4. **Pending Model Changes**
```bash
# Check for pending changes
dotnet ef migrations has-pending-model-changes

# If changes detected, create new migration
dotnet ef migrations add PendingChanges
```

## 📋 Migration Best Practices

### 1. **Before Creating Migrations**

```bash
# Always check current state
dotnet ef migrations list
dotnet ef migrations has-pending-model-changes

# Review model changes
git diff HEAD -- Models/
```

### 2. **Migration Naming Conventions**

```bash
# Good migration names:
dotnet ef migrations add InitialCreate
dotnet ef migrations add AddJobStatusIndex
dotnet ef migrations add UpdateTemplateSchema
dotnet ef migrations add AddUserAuthTables

# Avoid generic names:
# dotnet ef migrations add Update
# dotnet ef migrations add Fix
# dotnet ef migrations add Changes
```

### 3. **Testing Migrations**

```bash
# Test migration on development database
dotnet ef database update --environment Development

# Create test database for migration testing
dotnet ef database update --connection "Host=localhost;Database=resumegenerator_test;..."

# Rollback test
dotnet ef database update PreviousMigration --connection "test-connection"
```

### 4. **Production Migration Strategy**

```bash
# 1. Generate and review SQL script
dotnet ef migrations script --idempotent --output production-migration.sql

# 2. Backup production database
pg_dump resumegenerator > backup_$(date +%Y%m%d_%H%M%S).sql

# 3. Apply migration during maintenance window
psql -d resumegenerator -f production-migration.sql

# 4. Verify migration success
dotnet ef migrations list --connection "production-connection"
```

## 🚀 Automated Migration Scripts

### Development Auto-Migration

Create `scripts/dev-migrate.sh`:
```bash
#!/bin/bash
set -e

echo "🔄 Checking for pending migrations..."
if dotnet ef migrations has-pending-model-changes --context ResumeGeneratorContext; then
    echo "📋 Pending model changes detected!"
    read -p "Migration name: " migration_name
    
    if [ -n "$migration_name" ]; then
        echo "🆕 Creating migration: $migration_name"
        dotnet ef migrations add "$migration_name" --context ResumeGeneratorContext
        
        echo "⬆️ Applying migration to database..."
        dotnet ef database update --context ResumeGeneratorContext
        
        echo "✅ Migration completed successfully!"
    else
        echo "❌ Migration name cannot be empty"
        exit 1
    fi
else
    echo "✅ No pending changes detected"
fi
```

### Production Migration Script

Create `scripts/prod-migrate.sh`:
```bash
#!/bin/bash
set -e

BACKUP_DIR="backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
DATABASE_NAME="resumegenerator"

echo "🔄 Starting production migration process..."

# Create backup
echo "💾 Creating database backup..."
mkdir -p $BACKUP_DIR
pg_dump $DATABASE_NAME > "$BACKUP_DIR/backup_$TIMESTAMP.sql"

# Generate migration script
echo "📜 Generating migration script..."
dotnet ef migrations script --idempotent --context ResumeGeneratorContext --output "migration_$TIMESTAMP.sql"

# Apply migration
echo "⬆️ Applying migration..."
psql -d $DATABASE_NAME -f "migration_$TIMESTAMP.sql"

# Verify migration
echo "✅ Verifying migration..."
dotnet ef migrations list --context ResumeGeneratorContext

echo "🎉 Production migration completed successfully!"
echo "📁 Backup saved to: $BACKUP_DIR/backup_$TIMESTAMP.sql"
```

### CI/CD Migration Integration

Add to `.github/workflows/ci-cd.yml`:
```yaml
- name: Run Database Migrations
  run: |
    dotnet ef migrations bundle --context ResumeGeneratorContext --output migrate-bundle
    ./migrate-bundle --connection "${{ secrets.DATABASE_URL }}"
```

## 📊 Migration Monitoring

### Track Migration Performance

```sql
-- Query to monitor migration history
SELECT 
    migration_id,
    product_version,
    applied_at
FROM __efmigrationshistory
ORDER BY applied_at DESC;

-- Check database size after migrations
SELECT 
    pg_size_pretty(pg_database_size('resumegenerator')) as database_size;
```

### Migration Health Check

Create custom health check:
```csharp
public class MigrationHealthCheck : IHealthCheck
{
    private readonly ResumeGeneratorContext _context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingMigrations = await _context.Database
                .GetPendingMigrationsAsync(cancellationToken);
                
            if (pendingMigrations.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Pending migrations: {string.Join(", ", pendingMigrations)}");
            }

            return HealthCheckResult.Healthy("All migrations applied");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Migration check failed", ex);
        }
    }
}
```

---

**Remember**: Always backup your production database before applying migrations and test all migrations in a staging environment first!