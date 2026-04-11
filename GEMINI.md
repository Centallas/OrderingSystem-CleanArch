# GEMINI.md - Instructional Context for Ordering System

This file defines the mandates, architectural principles, and development workflows for the Ordering System project. All AI interactions within this workspace must strictly adhere to these guidelines.

## Senior Architect Mandates

**Role:** Senior .NET Architect.
**Project Goal:** Ordering System Practice Project.

### Tech Stack
- **Framework:** .NET 8
- **Language:** C# 12
- **Database:** PostgreSQL

### Architectural Pattern: Strict Clean Architecture
The project must be structured into the following layers with clear dependency boundaries:
1.  **Domain:** Core entities, value objects, exceptions, and domain logic. No dependencies on other layers.
2.  **Application:** Use cases, DTOs, interfaces (e.g., repository interfaces), and application logic. Depends only on the Domain layer.
3.  **Infrastructure:** Implementation of repository interfaces, database context (Entity Framework Core), and external services. Depends on Application and Domain layers.
4.  **WebAPI:** Controllers/Endoints, middleware, and dependency injection configuration. The entry point of the application.

### Principles & Patterns
- **SOLID:** Strictly follow SOLID principles.
- **DRY:** Avoid logic duplication.
- **Repository Pattern:** Use for data access abstraction.

### Coding Style (C# 12)
- **File-scoped namespaces:** Always use `namespace MyNamespace;`.
- **Primary Constructors:** Prefer primary constructors for classes and records where applicable.
- **Required Properties:** Utilize the `required` keyword for mandatory properties to ensure object integrity.

## Commands
- **Build:** `dotnet build`
- **Test:** `dotnet test`

## Workflow Mandates
Before writing any code for a task, you MUST:
1.  **Provide a 'Plan':** A detailed step-by-step implementation strategy.
2.  **Provide a Project Tree:** A visual representation of the proposed directory and file structure for the changes.

---

## Development Progress
- [ ] Initialize Solution and Projects
- [ ] Define Domain Entities (Order, Product, Customer)
- [ ] Implement Application Use Cases
- [ ] Set up PostgreSQL with EF Core
- [ ] Create WebAPI Endpoints
