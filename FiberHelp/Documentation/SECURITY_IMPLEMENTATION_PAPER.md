# Security Implementation in the FiberHelp CRM System

In partial fulfillment of the requirements in **IT16/L – Information Assurance and Security 1**  
Presented by: **[Your Name]**  
Presented to: **[Instructor Name]**  
Date: **March 2026**

---

## TABLE OF CONTENTS

I. Project Overview  
II. Secure Coding Practices  
III. Authentication and Authorization  
IV. Data Encryption  
V. Input Validation and Sanitization  
VI. Error Handling and Logging  
VII. Access Control  
VIII. Code Auditing Tools  
IX. Testing  
X. Security Policies  
XI. Incident Response Plan  
XII. Conclusion

---

## LIST OF TABLES

- Table 1. Intended Users and Roles  
- Table 2. Platform and Technology Stack  
- Table 3. User Roles and RBAC  
- Table 4. Encrypted / Protected Data  
- Table 5. Validated User Inputs  
- Table 6. Logged Security Events  
- Table 7. Access Control List  
- Table 8. Code Auditing Tools  
- Table 9. Test Categories  
- Table 10. Security Policy Summary  
- Table 11. Incident Severity Levels

---

## LIST OF FIGURES

- Figure 1. Environment Variable Configuration (`.env.example`)  
- Figure 2. Connection String Loading (`MauiProgram.cs`)  
- Figure 3. Password Hashing Implementation (`PasswordHasher.cs`)  
- Figure 4. Forgot Password OTP Flow (`Login.razor`, `AuthService.cs`)  
- Figure 5. Input Validation Rules (`InputValidator.cs`)  
- Figure 6. Audit Logging Service (`AuditLoggingService.cs`)  
- Figure 7. Role Guard / Restricted Access UI  
- Figure 8. Sync Status Indicator (Offline/Online)  
- Figure 9. Security Audit Command Results  
- Figure 10. Test Execution Evidence

---

## I. PROJECT OVERVIEW

### System Description
FiberHelp is a **.NET MAUI Blazor Hybrid** CRM system designed for fiber internet service operations. It consolidates customer management, ticketing, technician assignment, billing, analytics, and auditing into one secure platform. The system supports both online cloud operation and offline local operation through synchronized data flows.

### Purpose of the System
The system aims to:
1. Securely manage customer, operational, and transaction records.
2. Enforce strong authentication and role-based authorization.
3. Prevent common threats such as SQL injection, brute-force attacks, and unauthorized access.
4. Provide resilient operations using offline mode with automatic resynchronization.
5. Maintain audit-ready logs of security and user events.

### Intended Users

| Role | Description | Main Access |
|---|---|---|
| Administrator | Full control of system and users | User management, logs, configuration, all modules |
| Support Agent / Supervisor | Handles operations and customer service flow | Tickets, clients, billing views, assignments |
| Technician | Service execution role | Assigned tickets, service updates |
| Authorized Staff User | Basic operational access based on permissions | Limited data and role-allowed features |

**Table 1. Intended Users and Roles**

### Platform and Technology Used

| Component | Technology |
|---|---|
| Programming Language | C# (.NET 9) |
| Framework / Environment | .NET MAUI + Blazor Hybrid |
| ORM / Data Layer | Entity Framework Core |
| Databases | SQL Server (online/local dev), SQLite (offline) |
| Security Utilities | PBKDF2 hasher, AES helper, audit logging service |
| Platform Type | Desktop Hybrid Application |

**Table 2. Platform and Technology Stack**

---

## II. SECURE CODING PRACTICES

### Environment Variables and Credential Management
Sensitive values are not hardcoded in source code. Instead, runtime values are loaded from environment variables (`.env`) such as:
- `FIBERHELP_LOCAL_CONNECTION`
- `FIBERHELP_ONLINE_CONNECTION`
- `FIBERHELP_AES_KEY`
- `FIBERHELP_SMTP_HOST`, `FIBERHELP_SMTP_USER`, `FIBERHELP_SMTP_PASS`, `FIBERHELP_SMTP_FROM`

The `.env` file is excluded from repository tracking via `.gitignore`.

### Configuration Security
- Configuration is read through `MauiProgram.cs` using secure environment loading.
- Safe fallback behavior is implemented for development only.
- Secrets are never placed in public templates (`.env.example` only contains placeholders).

### Secure Data Access Patterns
- Database operations use EF Core LINQ and tracked entities.
- Queries are parameterized by framework design, reducing SQL injection risk.
- Service layer enforces validation and role checks before writes.

### Credential-Safety Controls Applied
- OTP sending uses SMTP credentials from environment variables.
- OTP code values are not stored in plaintext; only hashes are persisted.
- Exposed credentials are rotated as part of security policy.

---

## III. AUTHENTICATION AND AUTHORIZATION

### Login Process
The login flow validates credentials against `Users`, `Agents`, and `Technicians` with account-state checks:
1. Email/password validation
2. Account state check (`IsActive`, archived status, lock state)
3. Password verification via PBKDF2 hash matching
4. Session/auth state update and role assignment
5. Login audit logging

### Password Security
Passwords are never stored as plaintext.
- Hashing algorithm: **PBKDF2** (`PasswordHasher.cs`)
- Legacy hash upgrade support is implemented on successful login.

### Forgot Password and OTP Security
Forgot-password flow includes:
- OTP generation (6-digit)
- OTP hash storage (`PasswordResetTokens`)
- expiration window, retry cap, and cooldown/rate-limiting
- one-time token usage and invalidation of older active tokens
- SMTP delivery with secure config loading

### Role-Based Access Control

| Role | Access Scope |
|---|---|
| Administrator | Full system access |
| Support Agent / Supervisor | Operational modules with restrictions |
| Technician | Assigned service and resolution functions |
| Staff User | Limited role-specific capabilities |

**Table 3. User Roles and RBAC**

---

## IV. DATA ENCRYPTION

### Data Protection Mechanisms
- **Passwords:** PBKDF2 hash
- **Sensitive payloads:** AES utility support (`AesEncryptor.cs`)
- **Connection security:** encrypted SQL connection string options
- **Transport guidance:** secure endpoints and TLS-capable configurations for integrated services

### Encrypted / Protected Data Categories

| Data Type | Protection Method |
|---|---|
| User/Agent/Technician passwords | PBKDF2 hashing |
| OTP codes | SHA-256 hash storage |
| Sensitive sync payload fields | AES-based encryption helper |
| Credentials and keys | Environment-variable secrets |

**Table 4. Encrypted / Protected Data**

### Data Masking Support
The project includes data masking SQL scripts (`apply_data_masking.sql`) for controlled exposure scenarios.

---

## V. INPUT VALIDATION AND SANITIZATION

### Validated Inputs

| Input Category | Validation Applied |
|---|---|
| Login credentials | Required, email format check |
| Forgot-password email/OTP/new password | Email rule, OTP length, password policy |
| Client/account forms | Required fields, format and type checks |
| Billing/transaction data | Numeric/date and business-rule checks |
| Search/filter input | Sanitized and constrained |

**Table 5. Validated User Inputs**

### Validation Techniques
- Central validator utility: `InputValidator.cs`
- Regex-backed email/phone format checks
- Server-side validation in service methods
- String sanitization methods for safe display/storage

### Injection and XSS Prevention
- EF Core query parameterization prevents raw SQL concatenation issues.
- Blazor HTML-encodes output by default, reducing reflected/stored XSS risks when output is bound safely.

---

## VI. ERROR HANDLING AND LOGGING

### Secure Error Handling
- End-user messages are user-friendly and non-technical.
- Internal exception details are not exposed in UI.
- `ErrorHandlingService` provides sanitized messaging.

### Comprehensive Logging
`AuditLoggingService` records:
- login success/failure
- logout events
- password reset requests and failures
- security and system errors
- user activity actions (create/update/delete/access)

| Event Type | Logged |
|---|---|
| Authentication attempts | Yes |
| Password reset lifecycle | Yes |
| Access violations | Yes |
| CRUD activities | Yes |
| System exceptions | Yes |

**Table 6. Logged Security Events**

---

## VII. ACCESS CONTROL

### Protected Resources
Critical pages and actions are role-gated and authentication-guarded (dashboard modules, management pages, audit views, and privileged operations).

### Access Control List (ACL)

| Feature / Resource | Guest | Staff User | Agent/Supervisor | Technician | Administrator |
|---|---|---|---|---|---|
| Login | Allowed | Allowed | Allowed | Allowed | Allowed |
| Dashboard | Denied | Allowed | Allowed | Allowed | Allowed |
| User/Role Management | Denied | Denied | Denied | Denied | Allowed |
| Full Audit Logs | Denied | Denied | Denied | Denied | Allowed |
| Technician Task Pages | Denied | Denied | Limited | Allowed | Allowed |
| System Config | Denied | Denied | Denied | Denied | Allowed |

**Table 7. Access Control List**

### Enforcement Method
- Role checks at service layer and page guards
- Unauthorized access attempts are logged
- Authentication state required for protected routes

---

## VIII. CODE AUDITING TOOLS

| Tool | Purpose | Status |
|---|---|---|
| `dotnet list package --vulnerable` | Vulnerability scan for dependencies | Executed |
| `dotnet list package --outdated` | Update visibility for package hygiene | Executed |
| SonarQube for Visual Studio | Static analysis integration | Installed |
| `dotnet-sonarscanner` | CLI scanner for SonarQube pipeline | Installed |

**Table 8. Code Auditing Tools**

### Findings Summary
- Vulnerable packages: none detected in executed scan
- Outdated packages: identified and documented for upgrade planning
- Sonar scan pipeline prepared (requires running server URL/token)

---

## IX. TESTING

### Test Coverage Areas

| Test Category | Description |
|---|---|
| Authentication testing | valid/invalid login, lockout handling |
| OTP reset testing | request, verify, reset success/failure paths |
| Authorization testing | role-based page and action restrictions |
| Input validation testing | invalid form/input rejection |
| CRUD functional testing | entities and transaction flows |
| Offline sync testing | create/update offline then cloud sync |

**Table 9. Test Categories**

### Functional Status
- Build verification: successful
- Core pages and modules: operational
- Security flows: implemented and testable with evidence screenshots

---

## X. SECURITY POLICIES

| Policy Area | Implemented Rules |
|---|---|
| Password Policy | complexity checks, hashed storage, secure reset |
| Login Attempt Policy | failed-attempt tracking, lockout and suspension |
| Data Handling Policy | encrypted/hashed sensitive fields, secret isolation |
| Access Control Policy | RBAC, admin-only controls, denied access logging |
| Logging Policy | authentication/activity/error logs retained for review |
| Backup & Recovery Policy | scheduled backup recommendation with secure storage |

**Table 10. Security Policy Summary**

### Key Policy Statements
- Secrets must remain outside source code and public documentation.
- Unauthorized access attempts must be logged and reviewed.
- Security-relevant events are retained for audit and incident investigation.

---

## XI. INCIDENT RESPONSE PLAN

### Detection Phase
Incidents are detected via authentication anomalies, repeated failures, unauthorized access attempts, and security-oriented log events.

### Reporting Phase
Detected incidents are recorded in logs and escalated to administrators/security handlers for assessment.

### Containment Phase
Immediate controls include account lockout, token invalidation, credential rotation, and temporary restriction of suspicious access paths.

### Recovery Phase
Recovery includes restoring normal service, rotating affected credentials, verifying data integrity, and documenting lessons learned.

| Severity | Examples | Response Priority |
|---|---|---|
| Critical | repeated account compromise indicators, broad auth failures | Immediate |
| High | brute-force patterns, repeated unauthorized actions | Urgent |
| Medium | suspicious input anomalies | Prompt review |
| Low | minor unusual behavior | Monitor |

**Table 11. Incident Severity Levels**

---

## XII. CONCLUSION

FiberHelp demonstrates a layered security implementation appropriate for an academic and operational CRM project. The system integrates secure credential handling, PBKDF2 password protection, OTP-based recovery controls, role-based authorization, validation/sanitization mechanisms, and centralized auditing. Combined with offline resilience and cloud synchronization, these controls provide both practical functionality and strong alignment with IT16/L information assurance requirements.

For final submission, attach section-aligned screenshots for each figure reference and include command outputs for package audits and SonarQube analysis evidence.
