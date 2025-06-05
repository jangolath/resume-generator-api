# Digital Ocean Deployment Guide

This guide walks through deploying the Resume Generator API to Digital Ocean App Platform with a managed PostgreSQL database.

## 🚀 Quick Deployment (Recommended)

### Step 1: Create Managed Database

1. **Log into Digital Ocean Console**
   - Navigate to [Digital Ocean Console](https://cloud.digitalocean.com/)

2. **Create PostgreSQL Database**
   ```bash
   # Via CLI (optional)
   doctl databases create resumedb \
     --engine postgres \
     --region nyc1 \
     --size db-s-1vcpu-1gb \
     --num-nodes 1
   ```

   Or via web console:
   - Go to **Databases** → **Create Database**
   - Choose **PostgreSQL 15**
   - Select region closest to your users
   - Choose **Basic** plan, **1 GB RAM, 1 vCPU**
   - Name: `resumedb`

3. **Configure Database Security**
   - Add your app platform to trusted sources
   - Note down the connection string

### Step 2: Prepare Your Repository

1. **Ensure your code is pushed to GitHub**
   ```bash
   git add .
   git commit -m "Ready for deployment"
   git push origin main
   ```

2. **Create production environment variables file**
   Create `.env.production` (don't commit this):
   ```bash
   DATABASE_URL="your-database-connection-string"
   CLAUDE_API_KEY="your-claude-api-key"
   OPENAI_API_KEY="your-openai-api-key"
   OPENAI_ORG_ID="your-openai-org-id"
   GOOGLE_SERVICE_ACCOUNT_JSON='{"type":"service_account",...}'
   ```

### Step 3: Deploy to App Platform

1. **Create App Platform App**
   - Go to **Apps** → **Create App**
   - Connect your GitHub repository
   - Select the repository and branch (`main`)

2. **Configure Build Settings**
   ```yaml
   # This will be auto-detected, but you can customize:
   name: resume-generator-api
   services:
   - name: api
     source_dir: /
     github:
       repo: your-username/resume-generator-api
       branch: main
     run_command: dotnet ResumeGenerator.API.dll
     environment_slug: dotnet
     instance_count: 1
     instance_size_slug: basic-xxs
     http_port: 8080
     routes:
     - path: /
   ```

3. **Set Environment Variables**
   In the App Platform console, add these environment variables:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   DATABASE_URL=${resumedb.DATABASE_URL}
   CLAUDE_API_KEY=your-claude-api-key
   OPENAI_API_KEY=your-openai-api-key
   OPENAI_ORG_ID=your-openai-org-id
   GOOGLE_SERVICE_ACCOUNT_JSON=your-service-account-json
   ```

4. **Configure Resources**
   - **Plan**: Basic ($5/month)
   - **Instance Size**: Basic XXS (0.5 vCPU, 0.5 GB RAM)
   - **Instance Count**: 1 (can scale later)

5. **Review and Create**
   - Review configuration
   - Click **Create Resources**
   - Wait for deployment (5-10 minutes)

## 🔧 Manual Deployment Steps

### Prerequisites

```bash
# Install Digital Ocean CLI
curl -sL https://github.com/digitalocean/doctl/releases/download/v1.94.0/doctl-1.94.0-linux-amd64.tar.gz | tar -xzv
sudo mv doctl /usr/local/bin

# Authenticate
doctl auth init
```

### Step 1: Create Database

```bash
# Create database cluster
doctl databases create resumedb \
  --engine postgres \
  --version 15 \
  --region nyc1 \
  --size db-s-1vcpu-1gb \
  --num-nodes 1

# Get connection details
doctl databases connection resumedb
```

### Step 2: Build and Push Docker Image

```bash
# Build the image
docker build -t resume-generator-api .

# Tag for Digital Ocean Container Registry
docker tag resume-generator-api registry.digitalocean.com/your-registry/resume-generator-api:latest

# Push to registry
docker push registry.digitalocean.com/your-registry/resume-generator-api:latest
```

### Step 3: Create App Spec

Create `app.yaml`:

```yaml
name: resume-generator-api
services:
- name: api
  image:
    registry_type: DOCR
    repository: resume-generator-api
    tag: latest
  instance_count: 1
  instance_size_slug: basic-xxs
  http_port: 8080
  routes:
  - path: /
  envs:
  - key: ASPNETCORE_ENVIRONMENT
    value: Production
  - key: DATABASE_URL
    value: ${resumedb.DATABASE_URL}
  - key: CLAUDE_API_KEY
    value: your-claude-api-key
    type: SECRET
  - key: OPENAI_API_KEY
    value: your-openai-api-key
    type: SECRET
  health_check:
    http_path: /health

databases:
- name: resumedb
  engine: PG
  version: "15"
  size: db-s-1vcpu-1gb
  num_nodes: 1
```

### Step 4: Deploy

```bash
# Create app from spec
doctl apps create --spec app.yaml

# Monitor deployment
doctl apps list
doctl apps logs <app-id> --follow
```

## 🌐 Custom Domain Setup

### Step 1: Add Domain to App

```bash
# Add domain
doctl apps update <app-id> --spec app-with-domain.yaml
```

Update `app.yaml`:
```yaml
domains:
- name: api.yourdomain.com
  type: PRIMARY
```

### Step 2: Configure DNS

Add these DNS records to your domain:
```
Type: CNAME
Name: api
Value: your-app-url.ondigitalocean.app
```

### Step 3: SSL Certificate

Digital Ocean automatically provisions SSL certificates for custom domains.

## 📊 Monitoring and Scaling

### Application Metrics

```bash
# View app metrics
doctl apps tier list
doctl apps logs <app-id>

# Scale instances
doctl apps update <app-id> --spec app-scaled.yaml
```

### Database Monitoring

```bash
# Database metrics
doctl databases pool list <database-id>
doctl databases metrics bandwidth <database-id>
```

### Scaling Configuration

Update `app.yaml` for scaling:
```yaml
services:
- name: api
  instance_count: 3  # Scale to 3 instances
  instance_size_slug: basic-xs  # Upgrade to more resources
  autoscaling:
    min_instance_count: 1
    max_instance_count: 5
```

## 🔒 Security Best Practices

### Environment Variables

Store sensitive data as secrets:
```bash
# Using doctl
doctl apps update <app-id> --spec secure-app.yaml
```

In `secure-app.yaml`:
```yaml
envs:
- key: CLAUDE_API_KEY
  value: your-claude-api-key
  type: SECRET
- key: OPENAI_API_KEY  
  value: your-openai-api-key
  type: SECRET
```

### Database Security

1. **Enable SSL**: Always use SSL connections
2. **Firewall Rules**: Restrict access to app platform only
3. **Regular Backups**: Enable automatic daily backups
4. **Monitoring**: Set up alerts for unusual activity

### Application Security

1. **Rate Limiting**: Configure in production settings
2. **CORS**: Restrict to your frontend domains
3. **Security Headers**: Already configured in middleware
4. **API Keys**: Use environment variables only

## 🚨 Troubleshooting

### Common Issues

#### Build Failures
```bash
# Check build logs
doctl apps logs <app-id> --type BUILD

# Common fixes:
# 1. Ensure Dockerfile is correct
# 2. Check .dockerignore file
# 3. Verify all dependencies in .csproj
```

#### Database Connection Issues
```bash
# Test database connection
doctl databases connection <db-id>

# Common fixes:
# 1. Check connection string format
# 2. Verify trusted sources in database settings
# 3. Ensure SSL mode is configured correctly
```

#### Runtime Errors
```bash
# Check runtime logs
doctl apps logs <app-id> --type RUN

# Common fixes:
# 1. Verify all environment variables are set
# 2. Check API key validity
# 3. Review middleware configuration
```

### Health Check Failures

If health checks fail:
1. Check `/health` endpoint manually
2. Verify database connectivity
3. Check external API availability
4. Review application logs

### Performance Issues

Monitor and optimize:
```bash
# Check app metrics
doctl monitoring metrics droplet <app-id>

# Scaling options:
# 1. Increase instance size
# 2. Add more instances
# 3. Enable autoscaling
# 4. Optimize database queries
```

## 💰 Cost Optimization

### Current Pricing (as of 2024)

- **App Platform Basic**: $5/month per component
- **Managed PostgreSQL**: $15/month (1GB RAM, 1 vCPU)
- **Bandwidth**: $0.01/GB after 1TB
- **Container Registry**: $5/month for private registries

### Cost-Saving Tips

1. **Right-size resources**: Start small and scale up
2. **Use shared databases**: For development/staging
3. **Optimize images**: Use multi-stage Docker builds
4. **Monitor usage**: Set up billing alerts

### Estimated Monthly Costs

- **Starter Setup**: ~$20/month (App + Database)
- **Production Setup**: ~$50/month (Scaled app + database + monitoring)
- **Enterprise Setup**: ~$200/month (Multi-region + backup + premium features)

## 📈 Production Checklist

- [ ] Database backup strategy configured
- [ ] SSL certificates in place
- [ ] Custom domain configured
- [ ] Environment variables secured
- [ ] Health checks passing
- [ ] Monitoring and alerting set up
- [ ] Log aggregation configured
- [ ] Performance baselines established
- [ ] Disaster recovery plan documented
- [ ] Security scan completed
- [ ] Load testing performed
- [ ] Documentation updated

## 📞 Support Resources

- **Digital Ocean Docs**: https://docs.digitalocean.com/products/app-platform/
- **Community Forum**: https://www.digitalocean.com/community/
- **Support Tickets**: Available for paid accounts
- **API Status**: https://status.digitalocean.com/

---

**Next Steps**: After deployment, consider setting up monitoring with tools like Datadog or New Relic for production workloads.