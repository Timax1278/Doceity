using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Doceity.Areas.Identity.Pages.Account.Manage
{
    public static class ManageNavPages
    {
        public static string Index => "Index";
        public static string Email => "Email";
        public static string ChangePassword => "ChangePassword";
        public static string DownloadPersonalData => "DownloadPersonalData";
        public static string DeletePersonalData => "DeletePersonalData";
        public static string ExternalLogins => "ExternalLogins";
        public static string PersonalData => "PersonalData";
        public static string TwoFactorAuthentication => "TwoFactorAuthentication";

        // --- AGGIUNGI QUESTA COSTANTE ---
        public static string ExpertProfile => "ExpertProfile";
        // ---------------------------------


        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);
        public static string EmailNavClass(ViewContext viewContext) => PageNavClass(viewContext, Email);
        public static string ChangePasswordNavClass(ViewContext viewContext) => PageNavClass(viewContext, ChangePassword);
        public static string DownloadPersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DownloadPersonalData);
        public static string DeletePersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeletePersonalData);
        public static string ExternalLoginsNavClass(ViewContext viewContext) => PageNavClass(viewContext, ExternalLogins);
        public static string PersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, PersonalData);
        public static string TwoFactorAuthenticationNavClass(ViewContext viewContext) => PageNavClass(viewContext, TwoFactorAuthentication);


        // --- AGGIUNGI QUESTO METODO ---
        // Verifica se il controller corrente è ExpertProfileController
        public static string ExpertProfileNavClass(ViewContext viewContext) => ControllerNavClass(viewContext, "ExpertProfile");
        // ------------------------------

        // Metodo helper esistente per le pagine Razor
        private static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }

        // --- AGGIUNGI QUESTO METODO HELPER ---
        // Metodo helper per verificare il controller corrente
        private static string ControllerNavClass(ViewContext viewContext, string controller)
        {
            // Ottiene il nome del controller corrente dalla route data
            string currentController = viewContext.RouteData.Values["controller"] as string;
            return string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }
        // -------------------------------------
    }
}