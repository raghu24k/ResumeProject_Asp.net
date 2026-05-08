# Software Requirements Specification (SRS)
## Project Name: ResumeAI

---

## 1. Introduction

### 1.1 Purpose
The purpose of this document is to define the requirements, functionality, and scope of **ResumeAI**, a web-based resume builder application. This document serves as a guide for developers, stakeholders, and testers to understand the system's architecture, features, and capabilities.

### 1.2 Scope
ResumeAI is a comprehensive ASP.NET Core MVC web application designed to help users create, manage, analyze, and export professional resumes. The system provides real-time live preview capabilities, multiple customizable design templates, AI-driven resume analysis, interview preparation modules, and PDF export functionality. It aims to simplify the resume creation process while providing intelligent feedback to improve users' job prospects.

---

## 2. Overall Description

### 2.1 Product Perspective
ResumeAI operates as a standalone web application built on the ASP.NET Core framework using the MVC (Model-View-Controller) architecture. It interfaces with an Entity Framework Core managed relational database to store user and resume data. It also interfaces with external AI APIs (such as Groq) to provide intelligent text analysis.

### 2.2 Product Functions
- **User Authentication:** Secure registration, login, logout, and password recovery functionality.
- **Dashboard Management:** Users can manage multiple tailored resumes within their account.
- **Dynamic Resume Builder:** Granular form sections to input Personal Details, Work Experience, Education, Skills, Projects, and Certifications.
- **Real-Time Live Preview:** As users enter their details, the resume preview pane visually updates in real time using JavaScript.
- **Template Selection:** Users can seamlessly toggle between "Professional", "Modern", and "Minimalist" resume templates.
- **PDF Generation:** Accurate, high-quality, client-side PDF downloads that strictly preserve template styling.
- **AI Tools:** Automated AI resume analysis to score and review resumes, alongside an interactive Interview Preparation module.

### 2.3 Operating Environment
- **Server:** Windows/Linux Server capable of hosting .NET 8 (or compatible) runtime.
- **Database:** SQL Server or SQLite via Entity Framework Core.
- **Client:** Modern web browsers (Chrome, Firefox, Safari, Edge) with JavaScript enabled.

---

## 3. System Features

### 3.1 User Authentication & Authorization
**Description:** A secure authentication system to identify users and protect their data.
- **Registration:** Users can create an account using a username, email, and password.
- **Login:** Cookie-based secure login mechanism.
- **Password Recovery:** Forgot password workflow allowing users to securely reset their credentials via email verification.
- **Authorization:** Middleware enforces that users can only access, edit, and export resumes they own.

### 3.2 Resume Dashboard
**Description:** The centralized hub for authenticated users.
- Displays all resumes created by the user, sorted chronologically.
- Users can spawn a new resume or delete existing ones.

### 3.3 Interactive Resume Builder
**Description:** The core module where data entry takes place. It is divided into isolated sections:
- **Personal Details:** First Name, Last Name, Email, Phone, Location, LinkedIn, Portfolio, and Professional Summary.
- **Experience:** Add, edit, or delete work history records (Company, Job Title, Dates, 'Currently Working' toggle, Description).
- **Education:** Record academic history (Institution, Degree, Field, Dates, GPA).
- **Skills:** Define technical or soft skills along with proficiency levels (Beginner, Intermediate, Expert).
- **Projects:** Highlight personal/academic projects with Tech Stack and GitHub/Project URLs.
- **Certifications:** Document professional licenses or certifications (Name, Issuer, Date, Credential Link).

### 3.4 Live Preview & Theming
**Description:** A side-by-side visual representation of the resume.
- Captures form input events using vanilla JavaScript to instantly update a visual DOM representation.
- Applies user-selected themes:
  - **Professional:** Traditional styling, serif fonts (Times New Roman), and classic dividers.
  - **Modern:** Contemporary sans-serif (Inter) fonts, bold headers, and colored timeline layouts.
  - **Minimalist:** High-whitespace, sans-serif (Helvetica), elegant and ultra-clean alignments.

### 3.5 PDF Export System
**Description:** Allows users to download a finalized copy of their resume.
- Renders the full resume in a dedicated print view.
- Utilizes `html2pdf.js` to parse the DOM and generate a high-fidelity, A4-sized PDF document.
- Retains exact CSS styling, web fonts, and layouts specified by the chosen template.

### 3.6 AI Capabilities
**Description:** Value-add features using LLM/AI integrations.
- **AI Resume Analyzer:** Analyzes the stored resume data (or uploaded PDFs) to provide a score, point out weaknesses, and suggest actionable improvements.
- **Interview Prep:** Context-aware interactive mock interview or Q&A module based on the user's documented skills and experience.

---

## 4. Database Schema (Entities)

The system manages data relationally using Entity Framework Core. Core entities include:

- **User (`User.cs`):**
  - `Id`, `Username`, `Email`, `PasswordHash`, `CreatedAt`
- **Resume (`Resume.cs`):**
  - `Id`, `UserId` (FK), `Template`, `FirstName`, `LastName`, `Email`, `Phone`, `Location`, `LinkedIn`, `Portfolio`, `ProfessionalSummary`, `CreatedAt`, `UpdatedAt`
- **WorkExperience (`WorkExperience.cs`):**
  - `Id`, `ResumeId` (FK), `Company`, `JobTitle`, `StartDate`, `EndDate`, `IsCurrent`, `Description`
- **Education (`Education.cs`):**
  - `Id`, `ResumeId` (FK), `Institution`, `Degree`, `Location`, `StartDate`, `EndDate`, `GradeGPA`
- **Skill (`Skill.cs`):**
  - `Id`, `ResumeId` (FK), `Name`, `Level`
- **Project (`Project.cs`):**
  - `Id`, `ResumeId` (FK), `Title`, `TechStack`, `ProjectUrl`, `StartDate`, `EndDate`, `Description`
- **Certification (`Certification.cs`):**
  - `Id`, `ResumeId` (FK), `CertificationName`, `IssuingOrganization`, `DateEarned`, `CredentialLink`

---

## 5. Non-Functional Requirements

### 5.1 Security
- Passwords must be heavily hashed and salted (e.g., BCrypt/PBKDF2 via ASP.NET Identity or equivalent secure custom hasher).
- Protection against Cross-Site Request Forgery (CSRF) via `ValidateAntiForgeryToken`.
- Prevention of Cross-Site Scripting (XSS) via Razor syntax HTML encoding and JavaScript sanitization.
- Hardcoded secrets and API keys must be externalized using environment variables or user secrets (`appsettings.json` excluded from repo).

### 5.2 Performance & Reliability
- The live preview engine must execute within milliseconds to prevent typing latency.
- Database queries must use `.Include()` selectively and efficiently to avoid N+1 query performance degradation.
- PDF Generation must scale properly and wait for all Web Fonts to resolve before capturing the canvas.

### 5.3 Usability
- The UI must be mobile-responsive (though resume editing is primarily optimized for desktop due to side-by-side preview).
- Clear and immediate feedback through Toast notifications or `TempData` alerts after CRUD operations.
- Dynamic error handling and validation summary messages on form submission failures.
