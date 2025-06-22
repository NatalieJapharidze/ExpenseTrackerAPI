# 💰 Expense Tracker API

A modern, scalable expense tracking REST API built with .NET 9, featuring advanced caching, background processing, and comprehensive analytics.

![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-blue)
![Redis](https://img.shields.io/badge/Redis-7-red)
![Docker](https://img.shields.io/badge/Docker-Ready-blue)

## 🚀 Features

### 💸 **Expense Management**
- ✅ CRUD operations for expenses
- ✅ Bulk CSV/Excel import with validation
- ✅ Advanced filtering and pagination
- ✅ Real-time expense tracking

### 📁 **Category & Budget Management**
- ✅ Dynamic category creation
- ✅ Monthly budget setting and tracking
- ✅ Smart budget alerts (80% threshold)
- ✅ Category-wise expense analytics

### 📊 **Reports & Analytics**
- ✅ Monthly and yearly expense reports
- ✅ Excel export with background processing
- ✅ Email reports with beautiful HTML templates
- ✅ Category breakdown analytics
- ✅ Trend analysis and insights

### ⚡ **Performance & Scalability**
- ✅ Two-level caching (Redis + In-Memory)
- ✅ Background job processing
- ✅ Async/await throughout
- ✅ Optimized database queries

### 🔧 **Technical Excellence**
- ✅ Clean Architecture with Repository Pattern
- ✅ Specification Pattern for complex queries
- ✅ Comprehensive error handling
- ✅ Structured logging
- ✅ Docker containerization

## 🏗️ **Tech Stack**

### **Backend**
- **Framework:** .NET 9 with Minimal APIs
- **Database:** PostgreSQL 15
- **Caching:** Redis 7 + In-Memory Cache
- **ORM:** Entity Framework Core 9
- **File Processing:** EPPlus (Excel), CsvHelper

### **Infrastructure**
- **Containerization:** Docker & Docker Compose
- **Background Jobs:** .NET Background Services
- **Email:** SMTP with HTML templates
- **API Documentation:** Swagger/OpenAPI with custom UI

### **Architecture Patterns**
- **Repository Pattern** with Generic Implementation
- **Specification Pattern** for complex queries
- **Unit of Work** pattern for transactions
- **Dependency Injection** throughout
- **Clean Architecture** principles

## 🚀 **Quick Start**

### **Option 1: Docker (Recommended)**

```bash
# Clone the repository
git clone <repository-url>
cd ExpenseTrackerApi

# Start all services
docker-compose up -d

# Verify services are running
docker-compose ps

# Access the API
open http://localhost:5000/api/docs
```

### **Option 2: Local Development**

```bash
# Prerequisites: .NET 9 SDK, PostgreSQL, Redis

# Clone and restore
git clone <repository-url>
cd ExpenseTrackerApi
dotnet restore

# Start databases (Docker)
docker-compose up postgres redis -d

# Update connection strings in appsettings.json
# Run the application
dotnet run

# Access the API
open http://localhost:5111/api/docs
```

## 📋 **Prerequisites**

- **Docker & Docker Compose** (for containerized setup)
- **.NET 9 SDK** (for local development)
- **PostgreSQL 15+** (if running locally)
- **Redis 7+** (for caching)

## ⚙️ **Configuration**

### **Environment Variables**

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Port=5300;Database=expense_tracker;Username=postgres;Password=password123` |
| `ConnectionStrings__Redis` | Redis connection string | `localhost:6379` |
| `EmailSettings__SmtpHost` | SMTP server host | `smtp.gmail.com` |
| `EmailSettings__SmtpPort` | SMTP server port | `587` |
| `EmailSettings__Username` | Email username | |
| `EmailSettings__Password` | Email password | |

### **Development Setup**

```bash
# Set user secrets for sensitive data
dotnet user-secrets set "EmailSettings:Username" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "your-app-password"
```

### **Production Setup**

```yaml
# docker-compose.prod.yml
environment:
  - EmailSettings__Username=${EMAIL_USERNAME}
  - EmailSettings__Password=${EMAIL_PASSWORD}
  - ConnectionStrings__DefaultConnection=${DATABASE_URL}
```

## 🌐 **API Documentation**

### **Interactive Documentation**
- **Local:** http://localhost:5111/api/docs
- **Docker:** http://localhost:5000/api/docs

### **Key Endpoints**

#### **👤 User Management**
```http
POST   /api/users              # Create user
GET    /api/users/{id}         # Get user
PUT    /api/users/{id}         # Update user
```

#### **💸 Expense Management**
```http
POST   /api/expenses           # Create expense
GET    /api/expenses           # List expenses (with filtering)
PUT    /api/expenses/{id}      # Update expense
DELETE /api/expenses/{id}      # Delete expense
POST   /api/expenses/import    # Bulk import from CSV/Excel
```

#### **📁 Category Management**
```http
GET    /api/categories         # List categories
POST   /api/categories         # Create category
PUT    /api/categories/{id}/budget  # Update budget
DELETE /api/categories/{id}    # Delete category
```

#### **📊 Reports & Analytics**
```http
GET    /api/reports/monthly/{year}/{month}  # Monthly report
GET    /api/reports/yearly/{year}           # Yearly trends
POST   /api/reports/excel                   # Generate Excel report
POST   /api/reports/email                   # Email report
GET    /api/analytics/category-breakdown    # Category analytics
```

## 🏛️ **Architecture Overview**

```
├── Features/                 # Feature-based organization
│   ├── Users/               # User management
│   ├── Expenses/            # Expense CRUD & import
│   ├── Categories/          # Category & budget management
│   ├── Reports/             # Report generation
│   └── Analytics/           # Analytics & insights
├── Infrastructure/          # Infrastructure concerns
│   ├── Database/            # EF Core, entities, configurations
│   ├── Repositories/        # Repository & Unit of Work
│   ├── Services/            # External services (Email, Excel, Cache)
│   └── BackgroundJobs/      # Background processing
├── Common/                  # Shared components
│   └── Specifications/      # Query specifications
└── Program.cs              # Application entry point
```

### **Design Patterns Used**

- **Repository Pattern:** Data access abstraction
- **Specification Pattern:** Complex query composition
- **Unit of Work:** Transaction management
- **Background Service:** Async processing
- **Options Pattern:** Configuration management
- **Dependency Injection:** Loose coupling

## 📦 **Database Schema**

### **Core Tables**
- **users** - User accounts and preferences
- **categories** - Expense categories with budgets
- **expenses** - Individual expense records
- **budget_alerts** - Budget threshold notifications
- **report_jobs** - Background report generation tracking

### **Migrations**

```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Reset database (development only)
dotnet ef database drop -f
dotnet ef database update
```

## 🚀 **Deployment**

### **Docker Production**

```bash
# Build production image
docker build -t expense-tracker:latest .

# Run with production compose
docker-compose -f docker-compose.prod.yml up -d
```

## 🛠️ **Development**

### **Prerequisites**
```bash
# Install .NET 9 SDK
# Install Docker Desktop
# Install your favorite IDE (VS Code, Visual Studio, Rider)
```

### **Setup**
```bash
# Clone repository
git clone <repository-url>
cd ExpenseTrackerApi

# Restore packages
dotnet restore

# Start dependencies
docker-compose up postgres redis -d

# Run application
dotnet run --launch-profile https
```

### **Code Style**
- C# 12 features with nullable reference types
- Async/await for all I/O operations
- Repository and Specification patterns
- Minimal APIs with feature organization
- Comprehensive error handling

### **Debugging**
```bash
# Debug with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Debug with specific URLs
dotnet run --urls="https://localhost:7015;http://localhost:5111"
```

## 📊 **Monitoring & Observability**

### **Logging**
- Structured logging with Microsoft.Extensions.Logging
- Request/response logging
- Performance logging for slow queries
- Error tracking with full stack traces

### **Health Checks**
```http
GET /health          # Application health
GET /health/ready    # Readiness probe
GET /health/live     # Liveness probe
```

### **Metrics**
- Background job execution times
- Cache hit/miss ratios
- Database query performance
- API response times

## 🔧 **Background Services**

### **ReportGenerationService**
- Processes report generation jobs
- Handles Excel/PDF creation
- File storage management

### **BudgetAlertService**
- Monitors budget thresholds
- Sends email notifications
- Tracks alert history

### **MonthlyEmailService**
- Automated monthly reports
- Bulk email processing
- User preference handling

## 📝 **Contributing**

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### **Code Guidelines**
- Follow C# naming conventions
- Write comprehensive unit tests
- Update documentation for new features
- Ensure Docker builds pass
- Follow repository patterns

## 🤝 **Support**

- **Documentation:** [API Docs](http://localhost:5000/api/docs)
- **Issues:** [GitHub Issues](https://github.com/your-repo/issues)
- **Email:** natalie.japharidze@gmail.com

## 🙏 **Acknowledgments**

- Built with [.NET 9](https://dotnet.microsoft.com/)
- Database by [PostgreSQL](https://www.postgresql.org/)
- Caching by [Redis](https://redis.io/)
- Containerization by [Docker](https://www.docker.com/)
- Excel processing by [EPPlus](https://epplussoftware.com/)

---

**⭐ If you find this project helpful, please give it a star!**

## 📈 **Project Stats**

- **Performance:** <100ms API response times
- **Scalability:** Horizontally scalable with Redis

