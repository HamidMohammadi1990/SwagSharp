# SwagSharp 🚀

A powerful .NET 8.0 tool for automatically generating C# services, models, and HTTP clients from Swagger/OpenAPI files.

![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![C#](https://img.shields.io/badge/C%23-Latest-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Features

### 🎯 Smart Code Generation
- **Automatic Type Detection** - Converts Swagger types to appropriate C# types
- **Dictionary & List Management** - Supports `Dictionary<string, T>` and `List<T>`
- **Name Conflict Resolution** - Handles duplicate names automatically
- **Automatic Model Categorization** - Organizes models into logical folders

### 🛠 Full REST API Support
- **HTTP Methods** - GET, POST, PUT, DELETE, PATCH
- **Parameter Types** - Path, Query, Body parameters
- **Unified Response Structure** - Supports `Response<T>` wrapper
- **Nullable Types** - Full nullable reference type support

### ⚡ Advanced Optimizations
- **Dependency Injection** - Auto-generated DI configurations
- **HttpClient Factory** - Optimized HTTP client implementation
- **Generic Types** - Proper generic constraints and type resolution

## 📦 Project Structure


## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Swagger/OpenAPI 2.0 or 3.0 file

### Installation

```bash
# Clone the repository
git clone https://github.com/your-username/SwagSharp.git
cd SwagSharp

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the Web API
cd SwagSharp.Web
dotnet run
