


using System.Windows.Controls;
using System.Windows;
using POSSystem.WPF.ViewModels.Cashier;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views.Components
{
    public partial class ProductSearchBar : UserControl
    {
        public ProductSearchBar()
        {
            // Use parent DataContext (CashierWindow provides CashierViewModel)
            // Do not override DataContext here
            InitializeComponent();
        }

        public event RoutedEventHandler? MenuToggleClick;
        public event RoutedEventHandler? CategoryButtonClick;
        public event RoutedEventHandler? DismantleClick;
        public event RoutedEventHandler? CookingBatchClick;
        public event RoutedEventHandler? LeftoverDisposalClick;

        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            MenuToggleClick?.Invoke(sender, e);
        }

        private void DismantleButton_Click(object sender, RoutedEventArgs e)
        {
            DismantleClick?.Invoke(sender, e);
        }

        private void CookingBatchButton_Click(object sender, RoutedEventArgs e)
        {
            CookingBatchClick?.Invoke(sender, e);
        }

        private void LeftoverDisposalButton_Click(object sender, RoutedEventArgs e)
        {
            if (LeftoverDisposalClick != null)
            {
                LeftoverDisposalClick.Invoke(sender, e);
            }
            else if (Window.GetWindow(this) is POSSystem.WPF.Views.CashierWindow cashierWindow)
            {
                cashierWindow.OpenLeftoverDisposalPopup();
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
        private  void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset all category buttons to normal style
            //AllCategoryButton.Style = FindResource("CategoryButtonStyle") as Style;
            //BreakfastCategoryButton.Style = FindResource("CategoryButtonStyle") as Style;
            //SoupsCategoryButton.Style = FindResource("CategoryButtonStyle") as Style;
            //PastaCategoryButton.Style = FindResource("CategoryButtonStyle") as Style;
            //MainCourseCategoryButton.Style = FindResource("CategoryButtonStyle") as Style;
            //BurgersCategoryButton.Style = FindResource("CategoryButtonStyle") as Style;

            //var selectcategry = CategoriesPanel.Items;


            //// ��� �� ������� �� Normal Style
            //foreach (var item in CategoriesPanel.Items)
            //{

            //    var container = (ContentPresenter)CategoriesPanel.ItemContainerGenerator.ContainerFromItem(item);
            //    if (container != null)
            //    {
            //        var btn = FindVisualChild<Button>(container);
            //        if (btn != null)
            //        {
            //            btn.Style = FindResource("CategoryButtonStyle") as Style;
            //           // var shoe = await _categoryService.GetActiveCategoriesAsync();
            //             _cashierViewModel?.FilterProductsByCategory(btn.Content.ToString());

            //           // var cc = shoe.Where(x=>x.Name == btn.Content.ToString());

            //        }

            //    }
            //}

            //// ��� ���� ������� Active
            //if (sender is Button clickedButton)
            //{
            //    clickedButton.Style = FindResource("ActiveCategoryButtonStyle") as Style;
            //}




            if (sender is Button clickedButton )
            {
                // Delegate to VM via command if available
                var vm = DataContext as CashierViewModel;
                var selectedCategoryName = clickedButton.Tag?.ToString();

                if (vm != null &&
                    !string.IsNullOrWhiteSpace(selectedCategoryName) &&
                    vm.Commands.CategorySelectedCommand != null &&
                    vm.Commands.CategorySelectedCommand.CanExecute(selectedCategoryName))
                {
                    vm.Commands.CategorySelectedCommand.Execute(selectedCategoryName);
                }
            }
        }




        //private void LoadCategoriesFromDatabase()
        //{
        //    using (var db = new POSDbContext())
        //    {
        //        var categories = db.Categories
        //                           .Select(c => new Category
        //                           {
        //                               Id = c.Id,
        //                               Name = c.Name,
        //                               Description = c.Description,
        //                               IsActive = c.IsActive,
        //                               Products = c.Products
        //                           })
        //                           .ToList();

        //        CategoriesPanel.ItemsSource = categories;
        //    }
        //}



        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void CategoryAllButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as CashierViewModel;
            if (vm != null)
            {
               
                vm.SelectAllCategories();

            }
        }
    }
}
