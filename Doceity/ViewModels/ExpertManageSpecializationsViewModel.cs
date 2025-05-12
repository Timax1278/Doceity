// File: ViewModels/ExpertManageSpecializationsViewModel.cs
using Doceity.Models; // Per Specialization
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Doceity.ViewModels
{
    public class SpecializationSelectionViewModel
    {
        public int SpecializationId { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; } // True se l'esperto ha questa specializzazione
    }

    public class ExpertManageSpecializationsViewModel
    {
        public string ExpertUserId { get; set; } // Non mostrato nel form, ma utile per il POST
        public string ExpertFullName { get; set; } // Per mostrare a chi si riferisce la pagina

        [Display(Name = "Le Mie Specializzazioni")]
        public List<SpecializationSelectionViewModel> AvailableSpecializations { get; set; }

        public ExpertManageSpecializationsViewModel()
        {
            AvailableSpecializations = new List<SpecializationSelectionViewModel>();
        }
    }
}