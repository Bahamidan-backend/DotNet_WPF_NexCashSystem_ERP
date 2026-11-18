namespace POSSystem.WPF.ViewModels.Admin
{
    public partial class AdminDashboardViewModel
    {
        private void ToggleSidebar()
        {
            if (IsSidebarVisible)
            {
                SidebarWidth = 0;
                IsSidebarVisible = false;
            }
            else
            {
                SidebarWidth = 280;
                IsSidebarVisible = true;
            }
        }
    }
}
