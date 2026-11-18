using System;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Warehouses;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddEditWarehouseControl : UserControl
    {
        private WarehouseManagementViewModel _viewModel;
        private Warehouse _warehouse;
        private bool _isEditMode;

        public AddEditWarehouseControl(WarehouseManagementViewModel viewModel, Warehouse? warehouse = null)
        {
            InitializeComponent();
            _viewModel = viewModel;

            if (warehouse != null)
            {
                _isEditMode = true;
                _warehouse = warehouse;
                LoadWarehouseData();
                HeaderText.Text = "تعديل المخزن";
                HeaderSubText.Text = "Edit Warehouse";
                SaveButton.Content = "حفظ التعديلات";
            }
            else
            {
                _isEditMode = false;
                _warehouse = new Warehouse();
            }
        }

        private void LoadWarehouseData()
        {
            NameTextBox.Text = _warehouse.Name;
            DescriptionTextBox.Text = _warehouse.Description;
            LocationTextBox.Text = _warehouse.Location;
            ManagerNameTextBox.Text = _warehouse.ManagerName;
            ContactInfoTextBox.Text = _warehouse.ContactInfo;
            IsActiveCheckBox.IsChecked = _warehouse.IsActive;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                _warehouse.Name = NameTextBox.Text.Trim();
                _warehouse.Description = DescriptionTextBox.Text.Trim();
                _warehouse.Location = LocationTextBox.Text.Trim();
                _warehouse.ManagerName = ManagerNameTextBox.Text.Trim();
                _warehouse.ContactInfo = ContactInfoTextBox.Text.Trim();
                _warehouse.IsActive = IsActiveCheckBox.IsChecked ?? false;

                if (!_isEditMode)
                {
                    _warehouse.CreatedAt = DateTime.Now;
                }
                _warehouse.UpdatedAt = DateTime.Now;

                // Save using ViewModel
                _ = _viewModel.SaveWarehouseAsync(_warehouse);

                // Close Overlay
                CloseOverlay();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.CloseOverlay();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال اسم المخزن", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            return true;
        }
    }
}
