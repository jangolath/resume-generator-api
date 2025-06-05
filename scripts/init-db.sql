-- Safer Database Initialization Script
-- Only creates extensions and users, lets EF handle tables

-- Create extensions if they don't exist
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create a dedicated user for the application (if not exists)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_user WHERE usename = 'resumeuser') THEN
        CREATE USER resumeuser WITH PASSWORD 'changeme_in_production';
    END IF;
END
$$;

-- Grant necessary permissions
GRANT CONNECT ON DATABASE resumegenerator TO resumeuser;
GRANT USAGE ON SCHEMA public TO resumeuser;
GRANT CREATE ON SCHEMA public TO resumeuser;

-- Grant permissions on all tables (for existing tables)
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO resumeuser;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO resumeuser;

-- Set default permissions for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO resumeuser;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO resumeuser;

-- Create a function to update the updated_at column automatically
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Grant execute permission on function
GRANT EXECUTE ON FUNCTION update_updated_at_column() TO resumeuser;

-- Final message
DO $$
BEGIN
    RAISE NOTICE 'Database initialization completed - ready for Entity Framework!';
    RAISE NOTICE 'Extensions created, user configured, permissions granted.';
    RAISE NOTICE 'Entity Framework will handle table creation and data seeding.';
END
$$;