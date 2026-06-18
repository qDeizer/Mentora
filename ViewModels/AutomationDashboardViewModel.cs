using Microsoft.AspNetCore.Mvc.Rendering;

namespace PsikologProje_Void.ViewModels
{
    public class AutomationDashboardViewModel
    {
        public List<AutomationRoutineListItemViewModel> Routines { get; set; } = new();
        public AutomationRoutineInputViewModel CreateForm { get; set; } = new();
        public List<SelectListItem> SpecialtyOptions { get; set; } = new();
    }
}
