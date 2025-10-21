var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 添加控制器支持
builder.Services.AddControllers();

// 数据持久化：使用基于文件的存储（生产可替换为真实数据库）
var dataDir = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Data");
System.IO.Directory.CreateDirectory(dataDir);
var postsFile = System.IO.Path.Combine(dataDir, "posts.json");
builder.Services.AddSingleton<nekohub_core.Data.IPostRepository>(sp => new nekohub_core.Data.FilePostRepository(postsFile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

// 映射控制器路由
app.MapControllers();

app.Run();