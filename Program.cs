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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Ativa o uso de sessão - MANTIDO antes da autorização
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();