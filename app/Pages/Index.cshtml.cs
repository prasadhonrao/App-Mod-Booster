using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseMgmt.Pages
{
    public class IndexModel : PageModel
    {
        public string? ErrorMessage { get; private set; }

        public void OnGet()
        {
            // Error messages are surfaced via the API endpoints and JS;
            // this page model is kept minimal.
        }
    }
}
