
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Freela.Data;
using Freela.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// =====================================
// SERVIÇOS
// =====================================

// Configuração dos controllers e das views.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Enum será enviado como texto no JSON.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

// Proteção contra CSRF.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// Conexão com o MySQL.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

// Serviço de interações e matches.
builder.Services.AddScoped<InteracaoService>();

// =====================================
// AUTENTICAÇÃO
// =====================================

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";

        options.AccessDeniedPath = "/Conta/AcessoNegado";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.SlidingExpiration = true;

        // Resposta das APIs quando não há login.
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                return context.Response.WriteAsJsonAsync(
                    new { mensagem = "Faça login para continuar." }
                );
            }

            context.Response.Redirect(context.RedirectUri);

            return Task.CompletedTask;
        };

        // Resposta das APIs quando não há autorização.
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                return context.Response.WriteAsJsonAsync(
                    new
                    {
                        mensagem = "Você não tem permissão para esta ação."
                    }
                );
            }

            context.Response.Redirect(context.RedirectUri);

            return Task.CompletedTask;
        };
    });

// =====================================
// APLICAÇÃO
// =====================================

var app = builder.Build();

// Configurações de produção.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

// Redirecionamento HTTPS.
app.UseHttpsRedirection();

// NOVO: permite carregar fotos adicionadas dinamicamente
// na pasta wwwroot/uploads/perfis.
app.UseStaticFiles();

// Roteamento.
app.UseRouting();

// Autenticação e autorização.
app.UseAuthentication();

app.UseAuthorization();

// Recursos estáticos do projeto.
app.MapStaticAssets();

// Rotas das APIs.
app.MapControllers();

// Rota padrão do sistema.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

// =====================================
// TESTE DO BANCO DE DADOS
// =====================================

if (app.Environment.IsDevelopment())
{
    app.MapGet("/teste-banco", async (AppDbContext db) =>
    {
        var testes = new (string Tabela, Func<Task<int>> Contar)[]
        {
            ("Usuarios", () => db.Usuarios.CountAsync()),
            ("Clientes", () => db.Clientes.CountAsync()),
            ("Freelancers", () => db.Freelancers.CountAsync()),
            ("Categorias", () => db.Categorias.CountAsync()),
            ("Habilidades", () => db.Habilidades.CountAsync()),

            ("Projetos", async () =>
                (await db.Projetos
                    .Include(p => p.Habilidades)
                    .ToListAsync()).Count),

            ("Candidaturas", async () =>
                (await db.Candidaturas.ToListAsync()).Count),

            ("Interesses", async () =>
                (await db.Interesses.ToListAsync()).Count),

            ("Matches", async () =>
                (await db.Matches.ToListAsync()).Count)
        };

        var linhas = new List<string>
        {
            "Teste do banco de dados",
            ""
        };

        var tudoCerto = true;

        foreach (var (tabela, contar) in testes)
        {
            try
            {
                linhas.Add(
                    $"OK    {tabela}: {await contar()} registro(s)"
                );
            }
            catch (Exception erro)
            {
                tudoCerto = false;

                linhas.Add(
                    $"ERRO  {tabela}: {(erro.InnerException ?? erro).Message}"
                );
            }
        }

        linhas.Add("");

        linhas.Add(
            tudoCerto
                ? "Banco pronto para o sistema."
                : "Banco desatualizado: confira o script freela.sql."
        );

        return string.Join("\n", linhas);
    });
}

app.Run();
