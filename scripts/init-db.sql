-- Resume Generator Database Initialization Script
-- This script sets up the database with proper permissions and initial data

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

-- Create indexes for better performance (these will be created by EF migrations, but having them here as reference)
-- These are examples of what EF will create

/*
-- Performance indexes that will be created by Entity Framework
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_templates_name ON resume_templates(name);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_templates_is_public ON resume_templates(is_public);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_templates_created_at ON resume_templates(created_at);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_templates_is_active ON resume_templates(is_active);

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_jobs_status ON resume_jobs(status);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_jobs_created_at ON resume_jobs(created_at);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_jobs_template_id ON resume_jobs(template_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_jobs_status_created_at ON resume_jobs(status, created_at);

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_job_logs_job_id ON resume_job_logs(job_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_job_logs_timestamp ON resume_job_logs(timestamp);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_job_logs_step ON resume_job_logs(step);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_resume_job_logs_is_error ON resume_job_logs(is_error);

CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS ix_api_usage_stats_date ON api_usage_stats(date);
*/

-- Insert sample templates (these will also be seeded by Entity Framework)
-- This is a backup/reference of the default templates

INSERT INTO resume_templates (
    id, 
    name, 
    description, 
    content, 
    format, 
    tags, 
    is_public, 
    created_at, 
    updated_at, 
    usage_count,
    is_active
) VALUES 
(
    'a1b2c3d4-e5f6-7890-1234-567890abcdef'::uuid,
    'Modern Professional',
    'A clean, modern template suitable for most professional roles',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset=''UTF-8''>
    <title>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}} - Resume</title>
    <style>
        body { font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 40px; background: #f8f9fa; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 20px rgba(0,0,0,0.1); }
        .header { text-align: center; border-bottom: 3px solid #007acc; padding-bottom: 20px; margin-bottom: 30px; }
        .name { font-size: 2.5em; font-weight: bold; color: #333; margin-bottom: 10px; }
        .contact { color: #666; font-size: 1.1em; }
        .section { margin-bottom: 30px; }
        .section-title { font-size: 1.4em; font-weight: bold; color: #007acc; border-bottom: 2px solid #007acc; padding-bottom: 5px; margin-bottom: 15px; }
    </style>
</head>
<body>
    <div class=''container''>
        <div class=''header''>
            <div class=''name''>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}}</div>
            <div class=''contact''>{{PersonalInfo.Email}} • {{PersonalInfo.Phone}}</div>
        </div>
        <!-- Template content continues... -->
    </div>
</body>
</html>',
    'Html',
    '["professional", "modern", "clean"]',
    true,
    NOW(),
    NOW(),
    0,
    true
),
(
    'b2c3d4e5-f6g7-8901-2345-678901bcdefg'::uuid,
    'Creative Portfolio',
    'A creative template for designers and creative professionals',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset=''UTF-8''>
    <title>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}} - Creative Resume</title>
    <style>
        body { font-family: ''Arial'', sans-serif; margin: 0; padding: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        .container { max-width: 850px; margin: 0 auto; background: white; border-radius: 15px; overflow: hidden; box-shadow: 0 20px 40px rgba(0,0,0,0.2); }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px; text-align: center; }
        .name { font-size: 3em; font-weight: 300; margin-bottom: 10px; text-shadow: 2px 2px 4px rgba(0,0,0,0.3); }
    </style>
</head>
<body>
    <div class=''container''>
        <div class=''header''>
            <div class=''name''>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}}</div>
        </div>
        <!-- Template content continues... -->
    </div>
</body>
</html>',
    'Html',
    '["creative", "portfolio", "design"]',
    true,
    NOW(),
    NOW(),
    0,
    true
)
ON CONFLICT (id) DO NOTHING;

-- Create a function to update the updated_at column automatically
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for automatic updated_at updates (if tables exist)
-- These will be created by Entity Framework, but having them here for reference

/*
DROP TRIGGER IF EXISTS update_resume_templates_updated_at ON resume_templates;
CREATE TRIGGER update_resume_templates_updated_at 
    BEFORE UPDATE ON resume_templates 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_resume_jobs_updated_at ON resume_jobs;
CREATE TRIGGER update_resume_jobs_updated_at 
    BEFORE UPDATE ON resume_jobs 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_api_usage_stats_updated_at ON api_usage_stats;
CREATE TRIGGER update_api_usage_stats_updated_at 
    BEFORE UPDATE ON api_usage_stats 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
*/

-- Insert initial API usage stats record for today
INSERT INTO api_usage_stats (
    id,
    date,
    total_jobs,
    successful_jobs,
    failed_jobs,
    total_claude_tokens,
    total_openai_tokens,
    average_processing_time_ms,
    created_at,
    updated_at
) VALUES (
    uuid_generate_v4(),
    CURRENT_DATE,
    0,
    0,
    0,
    0,
    0,
    0,
    NOW(),
    NOW()
) ON CONFLICT (date) DO NOTHING;

-- Create a view for easy job monitoring
CREATE OR REPLACE VIEW job_status_summary AS
SELECT 
    status,
    COUNT(*) as count,
    AVG(processing_time_ms) as avg_processing_time_ms,
    MIN(created_at) as oldest_job,
    MAX(created_at) as newest_job
FROM resume_jobs 
GROUP BY status;

-- Create a view for template usage statistics
CREATE OR REPLACE VIEW template_usage_stats AS
SELECT 
    t.id,
    t.name,
    t.usage_count,
    COUNT(j.id) as total_jobs,
    COUNT(CASE WHEN j.status = 'Completed' THEN 1 END) as successful_jobs,
    COUNT(CASE WHEN j.status = 'Failed' THEN 1 END) as failed_jobs,
    AVG(CASE WHEN j.processing_time_ms IS NOT NULL THEN j.processing_time_ms END) as avg_processing_time_ms
FROM resume_templates t
LEFT JOIN resume_jobs j ON t.id = j.template_id
WHERE t.is_active = true
GROUP BY t.id, t.name, t.usage_count
ORDER BY t.usage_count DESC;

-- Grant permissions on views
GRANT SELECT ON job_status_summary TO resumeuser;
GRANT SELECT ON template_usage_stats TO resumeuser;

-- Add comments for documentation
COMMENT ON TABLE resume_templates IS 'Resume templates with HTML/CSS content and placeholders';
COMMENT ON TABLE resume_jobs IS 'Resume generation jobs with status tracking';
COMMENT ON TABLE resume_job_logs IS 'Detailed logs for each step of resume generation';
COMMENT ON TABLE api_usage_stats IS 'Daily statistics for API usage and performance monitoring';

COMMENT ON VIEW job_status_summary IS 'Summary of job statuses for monitoring';
COMMENT ON VIEW template_usage_stats IS 'Template usage statistics for analytics';

-- Performance optimization settings
-- These are recommendations for production deployment

/*
-- Recommended PostgreSQL settings for production:
-- In postgresql.conf:

-- Memory settings (adjust based on available RAM)
shared_buffers = 256MB                  -- 25% of RAM for smaller instances
effective_cache_size = 1GB              -- 75% of RAM
work_mem = 4MB                          -- For complex queries
maintenance_work_mem = 64MB             -- For maintenance operations

-- Connection settings
max_connections = 100                   -- Adjust based on application needs
shared_preload_libraries = 'pg_stat_statements'

-- WAL settings for better performance
wal_buffers = 16MB
checkpoint_completion_target = 0.9
checkpoint_timeout = 10min

-- Vacuum settings
autovacuum = on
autovacuum_max_workers = 3
autovacuum_vacuum_scale_factor = 0.1
autovacuum_analyze_scale_factor = 0.05
*/

-- Create maintenance procedures
CREATE OR REPLACE FUNCTION cleanup_old_jobs(retention_days INTEGER DEFAULT 30)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM resume_jobs 
    WHERE completed_at < (NOW() - (retention_days || ' days')::INTERVAL)
    AND status IN ('Completed', 'Failed', 'Cancelled');
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    -- Log the cleanup
    INSERT INTO resume_job_logs (id, job_id, step, message, timestamp, is_error)
    VALUES (
        uuid_generate_v4(),
        '00000000-0000-0000-0000-000000000000'::uuid,
        'Finalizing',
        'Cleanup completed: ' || deleted_count || ' jobs removed',
        NOW(),
        false
    );
    
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- Grant execute permission on cleanup function
GRANT EXECUTE ON FUNCTION cleanup_old_jobs(INTEGER) TO resumeuser;

-- Final message
DO $$
BEGIN
    RAISE NOTICE 'Resume Generator database initialization completed successfully!';
    RAISE NOTICE 'Default templates have been created.';
    RAISE NOTICE 'Performance views are available for monitoring.';
    RAISE NOTICE 'Remember to update the resumeuser password in production!';
END
$$;