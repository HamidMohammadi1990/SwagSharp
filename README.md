SwagSharp
یک ابزار قدرتمند برای تولید خودکار سرویس‌ها، مدل‌ها و کلاینت‌های #C از فایل  Swagger/OpenAPI
🚀 معرفی
SwagSharp  یک کتابخانه .NET 8.0 است که با تحلیل فایل Swagger/OpenAPI، به صورت خودکار کدهای #C شامل مدل‌های داده، اینترفیس‌های سرویس، پیاده‌سازی سرویس‌ها و کلاینت HTTP را تولید می‌کند.
✨ ویژگی‌های اصلی
🎯 تولید هوشمند کد
•	تشخیص خودکار انواع داده - تبدیل انواع Swagger به انواع #C مناسب
•	مدیریت Dictionary و لیست‌ها - پشتیبانی از Dictionary<string, T> و List<T>
•	حل تعارض نام‌ها - مدیریت خودکار نام‌های تکراری
•	دسته‌بندی خودکار مدل‌ها - سازماندهی مدل‌ها در پوشه‌های منطقی
🛠 پشتیبانی کامل از REST APIs
•	متدهای HTTP - GET, POST, PUT, DELETE, PATCH
•	انواع پارامترها - Path, Query, Body parameters
•	ساختار پاسخ یکپارچه - پشتیبانی از Response<T>
•	Nullable Types - پشتیبانی از انواع nullable
⚡ بهینه‌سازی‌های پیشرفته
•	Dependency Injection - تنظیمات خودکار DI
•	HttpClient Factory - پیاده‌سازی بهینه کلاینت HTTP
•	Generic Types - تولید انواع generic با محدودیت‌های مناسب




📦 ساختار پروژه
SwagSharp/
├── SwagSharp.Core/          # هسته اصلی پروژه
├── SwagSharp.Application/   # لایه کاربردی
├── SwagSharp.Web/          # وب API
├── SwagSharp.Cli/          # Command Line Interface
└── GeneratedCode/          # خروجی تولید شده


🎯 موارد استفاده ایده‌آل
این پروژه برای سرویس‌هایی مناسب است که:
✅ الگوهای یکسان دارند
•	CRUD Operations - Create, Read, Update, Delete
•	پایان‌نامه‌های استاندارد REST
•	ساختار پاسخ یکپارچه
•	الگوهای نامگذاری ثابت




📁 ساختار خروجی
GeneratedCode/
├── Models/
│   ├── Account/
│   │   ├── FinancialAccountDto.cs
│   │   └── AccountTransactionDto.cs
│   └── Agreement/
│       ├── AgreementDto.cs
│       └── AgreementTypeDto.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IAccountService.cs
│   │   └── IAgreementService.cs
│   └── Implementations/
│       ├── AccountService.cs
│       └── AgreementService.cs
└── Clients/
    ├── IApiClient.cs
    └── ApiClient.cs


مدل تولید شده
csharp
namespace GeneratedCode.Models.Account
{
    public class FinancialAccountDto
    {
        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; } = string.Empty;
        
        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }
        
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
    }
}
اینترفیس سرویس
csharp
namespace GeneratedCode.Services.Interfaces
{
    public interface IAccountService
    {
        Task<Response<FinancialAccountDto>> GetFinancialAccountAsync(string accountNumber);
        Task<Response<List<AccountTransactionDto>>> GetAccountTransactionsAsync(string accountNumber);
        Task<Response<bool>> CreateAccountAsync(CreateAccountRequest request);
    }
}
پیاده‌سازی سرویس
csharp
namespace GeneratedCode.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IApiClient _apiClient;
        
        public AccountService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }
        
        public async Task<Response<FinancialAccountDto>> GetFinancialAccountAsync(string accountNumber)
        {
            return await _apiClient.GetAsync<FinancialAccountDto>($"api/v2/account/{accountNumber}");
        }
    }
}


