# 🛒 Lasamify — Local Marketplace System

A marketplace web application where users can post products for sale and buy from other sellers.

Built with **ASP.NET Core MVC (.NET 10)** using **Entity Framework Core + SQLite**.

---

## 👥 Group Members & Roles

| Member | Role |
|--------|------|
| Edriane Diaz | Project Manager |
| Xavier Halasan | Lead Programmer |
| Benedict | UI/UX Manager |
| JohnLloyd Sy | UI/UX |
| James Galvez | Programmer |

**Project Name:** Lasamify  

---

## ✅ Features Implemented

### Interaction
- [x] **Transactions** — Users can buy products; transaction history tracked
- [x] **Account Management** — Register, Login, Logout with cookie auth

### UI/UX 
- [x] **Profile Picture Upload** — Users can upload and update their profile photo
- [x] **Search Bar** — Search products by name/description; filter by category

---

## 🚀 How to Run

### Requirements
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- No database setup needed yet — SQLite is used automatically

### Steps

```bash
# Clone the repo
git clone https://github.com/jiagalvezabella/Lasamify.git
cd Lasamify

# Restore packages
dotnet restore

# Run the app
dotnet run
```

Then open your browser at: https://localhost:64864

---

## 📁 Project Structure

```
Lasamify/
├── Controllers/
│   ├── AccountController.cs    # Register, Login, Profile, Upload
│   ├── HomeController.cs       # Marketplace homepage + search
│   └── ProductsController.cs   # CRUD Products + Buy (Transaction)
├── Data/
│   └── ApplicationDbContext.cs # EF Core DbContext (SQLite/MySQL)
├── Models/                     # Data Entities and ViewModels
├── Properties/
│   └── launchSettings.json     # Port and hosting configurations
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml        # User Login page
│   │   ├── Profile.cshtml      # User Profile & Picture Upload
│   │   └── Register.cshtml     # Account Registration
│   ├── Home/
│   │   ├── Index.cshtml        # Main Marketplace grid
│   │   └── Landing.cshtml      # Landing/Welcome page
│   ├── Products/
│   │   ├── Create.cshtml       # Post new product form
│   │   └── Details.cshtml      # Product info & Buy button
│   └── Shared/
│       ├── _Layout.cshtml      # Main navigation & footer
│       ├── _ViewImports.cshtml # Global Razor directives
│       └── _ViewStart.cshtml   # Default layout configuration
├── wwwroot/                    # CSS, JS, and Uploaded Images
├── appsettings.json            # Connection strings (SQLite)
└── .gitignore                  # Files to exclude from GitHub
```

---

## 🛠️ Tech Stack

- ASP.NET Core MVC 8
- Entity Framework Core (SQLite)
- Bootstrap 5.3
- Font Awesome 6
- Cookie Authentication
