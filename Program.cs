var builder = WebApplication.CreateBuilder(args);

// Adiciona serviços ao contêiner.
builder.Services.AddControllersWithViews();

// --- CONFIGURAÇÃO DA SESSÃO ATUALIZADA ---
// Necessário para que a sessão tenha um local para armazenar os dados temporários
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // O carrinho expira após 30 min de inatividade
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Essencial para o carrinho funcionar mesmo com políticas de cookies rígidas
});

builder.Services.AddHttpContextAccessor();

// Lê as configurações do banco NoSQL
builder.Services.Configure<ScarFood.Models.DatabaseSettings>(
    builder.Configuration.GetSection("DatabaseSettings"));

// ADICIONADO: Registra o serviço do MongoDB para o sistema inteiro poder usar
builder.Services.AddSingleton<ScarFood.Services.DatabaseService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// PIM: LINHA COMENTADA PARA EVITAR ERRO DE REDIRECIONAMENTO NO NGROK
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Ativa o uso de sessão - MANTIDO antes da autorização
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();