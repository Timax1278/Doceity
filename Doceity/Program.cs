// File: Program.cs

// --- Using Necessari ---
using Doceity.Services;             // Per EmailSender, AuthMessageSenderOptions (e TwilioSettings se è lì)
using Microsoft.AspNetCore.Identity.UI.Services; // Per IEmailSender
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Doceity.Data;                 // Per ApplicationDbContext
using Doceity.Models;               // Per ApplicationUser
using Doceity.Constants;            // Per Roles
using Doceity.Configuration;        // Per TwilioSettings
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; // Per CreateScope, GetRequiredService
using System;                       // Per Exception, DateTime, TimeSpan
using System.Linq;                  // Per Select nei log seeding
using System.Threading.Tasks;       // Per Task nei seeding
using System.Net.Http.Headers;      // Per MediaTypeWithQualityHeaderValue

var builder = WebApplication.CreateBuilder(args);

// --- Configurazione Servizi (Dependency Injection Container) ---

// 1. Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter(); // Utile in sviluppo per errori DB

// 2. ASP.NET Core Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Impostazioni di Sicurezza e Registrazione
    options.SignIn.RequireConfirmedAccount = true; // Richiede conferma email per il login
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false; // Puoi cambiarlo a true se vuoi simboli
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Tempo di blocco dopo tentativi falliti
    options.Lockout.MaxFailedAccessAttempts = 5; // Numero massimo di tentativi di login falliti
    options.User.RequireUniqueEmail = true; // Ogni utente deve avere un'email unica
})
    .AddRoles<IdentityRole>() // Abilita la gestione dei ruoli (Admin, Expert, User)
    .AddEntityFrameworkStores<ApplicationDbContext>(); // Usa EF Core per salvare dati Identity

// 3. Servizi MVC e Razor Pages
builder.Services.AddControllersWithViews(); // Per architettura MVC
builder.Services.AddRazorPages();         // Per Identity UI e altre pagine Razor

// 4. Configurazione Email Sender (Es: SendGrid)
builder.Services.Configure<AuthMessageSenderOptions>(options =>
{
    // Legge le impostazioni da appsettings.json o user secrets
    options.SendGridKey = builder.Configuration["SendGrid:ApiKey"];
    options.FromEmail = builder.Configuration["EmailSender:FromEmail"];
    options.FromName = builder.Configuration["EmailSender:FromName"];
});
builder.Services.AddTransient<IEmailSender, EmailSender>(); // Registra il servizio EmailSender

// 5. Configurazione Twilio (per SMS o altre funzionalità Twilio)
// Assicurati che la classe TwilioSettings esista e abbia la costante SectionName
builder.Services.Configure<TwilioSettings>(
    builder.Configuration.GetSection(TwilioSettings.SectionName) // Lega la sezione "Twilio" alla classe TwilioSettings
);

// 6. Configurazione HttpClient Factory per API esterne (Es: Gemini)
builder.Services.AddHttpClient("GeminiApiClient", client =>
{
    // Impostazioni di default per questo client specifico
    // L'URL base potrebbe non essere necessario qui se l'URL completo viene costruito nella chiamata API
    // client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/"); // Esempio
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    // Potresti aggiungere altri header di default qui, come User-Agent
    // client.DefaultRequestHeaders.Add("User-Agent", "DoceityApp");
});

// --- Fine Configurazione Servizi ---


// Costruisce l'applicazione web
var app = builder.Build();


// --- Seeding Database (Eseguito dopo Build e prima di Run) ---
// Questo blocco esegue operazioni iniziali sul database come migrazioni e creazione dati di default
using (var scope = app.Services.CreateScope()) // Crea un nuovo scope per risolvere i servizi
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var seedingLogger = loggerFactory.CreateLogger("DatabaseSeeding"); // Logger specifico per il seeding

    try
    {
        seedingLogger.LogInformation("Starting database setup...");

        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Applica le migrazioni del database in sospeso
        seedingLogger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        seedingLogger.LogInformation("Database migrations applied successfully.");

        // Esegue il seeding dei ruoli e dell'utente admin di default
        await SeedRolesAsync(roleManager, loggerFactory.CreateLogger("RoleSeeding"));
        await SeedDefaultAdminUserAsync(userManager, roleManager, builder.Configuration, loggerFactory.CreateLogger("AdminUserSeeding"));

        seedingLogger.LogInformation("Database setup completed successfully.");
    }
    catch (Exception ex)
    {
        // Logga eventuali errori critici durante il setup del database
        seedingLogger.LogError(ex, "An error occurred during database seeding or migration. The application might not start correctly.");
        // Potresti considerare di terminare l'applicazione qui se il DB è fondamentale
        // throw; // Rilancia l'eccezione per fermare l'avvio se necessario
    }
}
// --- Fine Seeding ---


// --- Configurazione Pipeline HTTP Request (Middleware) ---
// L'ordine dei middleware è importante!

// Gestione Errori
if (app.Environment.IsDevelopment())
{
    // In sviluppo, mostra pagine di errore dettagliate
    app.UseDeveloperExceptionPage(); // Informazioni dettagliate sull'eccezione
    app.UseMigrationsEndPoint();     // Endpoint per applicare migrazioni via UI (se usato)
}
else
{
    // In produzione, mostra una pagina di errore generica e usa HSTS
    app.UseExceptionHandler("/Home/Error"); // Pagina di errore per utenti finali
    app.UseHsts(); // Aggiunge header Strict-Transport-Security per forzare HTTPS
}

// Middleware comuni
app.UseHttpsRedirection(); // Reindirizza richieste HTTP a HTTPS
app.UseStaticFiles();      // Abilita la servitura di file statici (CSS, JS, immagini da wwwroot)

app.UseRouting(); // Aggiunge il middleware di routing

// Middleware di Autenticazione e Autorizzazione (DEVONO essere tra UseRouting e UseEndpoints/Map*)
app.UseAuthentication(); // Identifica l'utente basato sui cookie o altri schemi
app.UseAuthorization();  // Verifica se l'utente identificato ha i permessi per accedere alla risorsa

// Mappatura degli Endpoint
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Route MVC di default
app.MapRazorPages(); // Mappa le richieste alle Razor Pages (incluse quelle di Identity)

// --- Fine Configurazione Pipeline ---


// Avvia l'applicazione
app.Logger.LogInformation("Starting Doceity application on {Environment} environment...", app.Environment.EnvironmentName);
app.Run();


// --- Metodi Helper per Seeding (spostati alla fine per leggibilità) ---

// Popola i ruoli di base se non esistono
async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
{
    logger.LogInformation("--- Role Seeding Started ---");
    string[] roleNames = { Roles.Admin, Roles.Expert, Roles.User }; // Usa le costanti definite
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            logger.LogInformation("Creating role '{RoleName}'...", roleName);
            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (roleResult.Succeeded)
            {
                logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
            }
            else
            {
                // Logga gli errori specifici restituiti da Identity
                logger.LogError("Error creating role '{RoleName}': {Errors}", roleName, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Role '{RoleName}' already exists.", roleName);
        }
    }
    logger.LogInformation("--- Role Seeding Finished ---");
}

// Crea un utente amministratore di default se non esiste
async Task SeedDefaultAdminUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, ILogger logger)
{
    logger.LogInformation("--- Default Admin User Seeding Started ---");

    // Leggi le credenziali dell'admin dalla configurazione (user secrets o appsettings.json)
    // Fornisci valori di default se non trovati nella configurazione
    var defaultAdminEmail = configuration["AdminUser:Email"] ?? "admin@doceity.com";
    var defaultAdminPassword = configuration["AdminUser:Password"] ?? "Password123!"; // CAMBIARE IN PRODUZIONE!
    var defaultAdminFirstName = configuration["AdminUser:FirstName"] ?? "Admin";
    var defaultAdminLastName = configuration["AdminUser:LastName"] ?? "Doceity";

    // Verifica se l'utente admin esiste già tramite email
    var defaultAdminUser = await userManager.FindByEmailAsync(defaultAdminEmail);
    if (defaultAdminUser == null)
    {
        logger.LogInformation("Default admin user '{AdminEmail}' not found. Creating...", defaultAdminEmail);
        var adminUser = new ApplicationUser
        {
            UserName = defaultAdminEmail, // UserName è richiesto da Identity, spesso uguale all'Email
            Email = defaultAdminEmail,
            FirstName = defaultAdminFirstName,
            LastName = defaultAdminLastName,
            EmailConfirmed = true, // Conferma l'email automaticamente per l'admin di default
            RegistrationDate = DateTime.UtcNow
        };

        // Crea l'utente con la password specificata
        var result = await userManager.CreateAsync(adminUser, defaultAdminPassword);
        if (result.Succeeded)
        {
            logger.LogInformation("Admin user '{AdminEmail}' created successfully.", defaultAdminEmail);

            // Assegna il ruolo di Admin all'utente appena creato
            if (await roleManager.RoleExistsAsync(Roles.Admin))
            {
                logger.LogInformation("Assigning '{AdminRoleName}' role to '{AdminEmail}'...", Roles.Admin, defaultAdminEmail);
                var roleResult = await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("'{AdminRoleName}' role assigned successfully to '{AdminEmail}'.", Roles.Admin, defaultAdminEmail);
                }
                else
                {
                    logger.LogError("Error assigning '{AdminRoleName}' role to '{AdminEmail}': {Errors}", Roles.Admin, defaultAdminEmail, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Questo non dovrebbe accadere se SeedRolesAsync viene eseguito prima
                logger.LogError("'{AdminRoleName}' role not found. Cannot assign role to '{AdminEmail}'. Ensure roles are seeded first.", Roles.Admin, defaultAdminEmail);
            }
        }
        else
        {
            // Logga gli errori durante la creazione dell'utente
            logger.LogError("Error creating admin user '{AdminEmail}': {Errors}", defaultAdminEmail, string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        logger.LogInformation("Default admin user '{AdminEmail}' already exists.", defaultAdminEmail);
        // Opzionale: Verifica e assegna il ruolo Admin se l'utente esiste ma non ha il ruolo
        if (!await userManager.IsInRoleAsync(defaultAdminUser, Roles.Admin))
        {
            logger.LogWarning("Admin user '{AdminEmail}' exists but lacks '{AdminRoleName}' role. Attempting to assign...", defaultAdminEmail, Roles.Admin);
            if (await roleManager.RoleExistsAsync(Roles.Admin))
            {
                var roleResult = await userManager.AddToRoleAsync(defaultAdminUser, Roles.Admin);
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("'{AdminRoleName}' role assigned successfully to existing user '{AdminEmail}'.", Roles.Admin, defaultAdminEmail);
                }
                else
                {
                    logger.LogError("Error assigning '{AdminRoleName}' role to existing user '{AdminEmail}': {Errors}", Roles.Admin, defaultAdminEmail, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogError("'{AdminRoleName}' role not found. Cannot assign role to existing user '{AdminEmail}'.", Roles.Admin, defaultAdminEmail);
            }
        }
    }
    logger.LogInformation("--- Default Admin User Seeding Finished ---");
}
// --- FINE Metodi Helper per Seeding ---