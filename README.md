Bước 1: Bật database
sqllocaldb start MSSQLLocalDB
Bật rồi làm xong thì nhớ đóng không nó chạy hoài (sqllocaldb stop MSSQLLocalDB) hoặc (net stop MSSQLSERVER)


Bước 2: Setup dự án
2.1 Tạo solution file bằng câu lệnh”dotnet new mvc -n BookStoreMVC” sau đó di chuyển vào thư mục bằng lệnh “cd BookStoreMWC”
2.2 Clone dự án từ github về bằng lệnh “git clone …”
2.3 Cài đặt các package bằng các lệnh sau:
# Entity Framework Core (câu lện comment không chạy)
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design

# Identity
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Identity.UI

# Additional packages
dotnet add package Serilog.AspNetCore
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
2.4 Tiếp tục setup với Tailwind bằng các câu lệnh sau:
# Cài đặt package.json
npm init -y

# Install Tailwind CSS và dependencies 
npm install -D tailwindcss @tailwindcss/forms @tailwindcss/typography @tailwindcss/aspect-ratio
npm install -D autoprefixer postcss 
npm install -D concurrently
npm install -D tailwindcss@3 postcss autoprefixer

# Generate Tailwind config
npx tailwindcss init
Bước 3: Cấu hình các file
# Tạo migration
dotnet ef migrations add InitialCreate


# Update database
dotnet ef database update


# Nếu cần xóa database và tạo lại
dotnet ef database drop --force
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update


