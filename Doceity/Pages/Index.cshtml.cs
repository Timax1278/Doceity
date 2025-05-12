using Microsoft.AspNetCore.Mvc.RazorPages; // Rimosso using non necessario di Identity qui
using Microsoft.Extensions.Logging;       // Mantenuto per ILogger

namespace Doceity.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        // Non è necessario iniettare SignInManager e UserManager qui
        // se la logica che li usa è interamente nel file .cshtml con @inject
        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Puoi mettere qui logica se serve caricare dati specifici per la homepage
            // da passare alla vista tramite proprietà di questo PageModel.
            // Per ora, la nostra homepage non ne ha bisogno dal PageModel.
        }
    }
}