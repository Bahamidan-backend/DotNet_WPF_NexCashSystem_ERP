using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business;
using POSSystem.WPF.Services;

namespace POSSystem.WPF.Views.Popups
{
    public partial class EditEmployeePopup : Window
    {
        private readonly IEmployeeService _employeeService;
        private readonly User _employee;
        private readonly Dictionary<string, CheckBox> _permissionCheckBoxes = new(StringComparer.OrdinalIgnoreCase);
        private bool _suppressTemplateSync = false;
        private static readonly Brush NormalBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dee2e6"));
        private static readonly Brush ErrorBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc3545"));

        public EditEmployeePopup(User employee)
        {
            InitializeComponent();
            _employeeService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IEmployeeService>();
            _employee = employee;

            NameTextBox.Text = employee.Name;
            EmailTextBox.Text = employee.Email;
            PhoneTextBox.Text = employee.Phone;
            JobTitleComboBox.Text = employee.JobTitle;
            BaseSalaryTextBox.Text = employee.BaseSalary?.ToString("#,0.##", CultureInfo.InvariantCulture) ?? "";
            HireDatePicker.SelectedDate = employee.HireDate;
            IsActiveCheckBox.IsChecked = employee.IsActive;

            BuildPermissionsUI(employee);
        }

        private void BuildPermissionsUI(User employee)
        {
            RoleTemplateComboBox.Items.Clear();
            foreach (var templateName in SystemPermissions.RoleTemplates.Keys)
            {
                RoleTemplateComboBox.Items.Add(templateName);
            }
            RoleTemplateComboBox.Items.Add("مخصص");

            PermissionsContainerPanel.Children.Clear();
            _permissionCheckBoxes.Clear();

            var effectivePerms = PermissionGuard.GetEffectivePermissions(employee);
            bool isPrimaryAdmin = employee.Role == UserRole.Admin || employee.Id == 1;

            var grouped = SystemPermissions.AllDefinitions.GroupBy(d => d.Category);
            foreach (var group in grouped)
            {
                var catHeader = new TextBlock
                {
                    Text = $"▪ {group.Key}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 53, 140)),
                    Margin = new Thickness(0, 8, 0, 6)
                };
                PermissionsContainerPanel.Children.Add(catHeader);

                var wrap = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
                foreach (var perm in group)
                {
                    var cb = new CheckBox
                    {
                        Content = perm.DisplayName,
                        Tag = perm.Key,
                        IsChecked = effectivePerms.Contains(perm.Key),
                        IsEnabled = !isPrimaryAdmin,
                        Width = 285,
                        Margin = new Thickness(4, 3, 4, 3),
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
                    };
                    cb.Checked += PermissionCheckBox_Changed;
                    cb.Unchecked += PermissionCheckBox_Changed;
                    _permissionCheckBoxes[perm.Key] = cb;
                    wrap.Children.Add(cb);
                }
                PermissionsContainerPanel.Children.Add(wrap);
            }

            _suppressTemplateSync = true;
            if (isPrimaryAdmin)
            {
                RoleTemplateComboBox.SelectedItem = "مدير كامل الصلاحيات";
                RoleTemplateComboBox.IsEnabled = false;
                QuickSelectButtonsPanel.Visibility = Visibility.Collapsed;
                IsActiveCheckBox.IsEnabled = false;
                PermissionsSubtitleText.Text = "⚠️ حساب المدير الرئيسي محمي بكامل الصلاحيات ولا يمكن تقييده أو تعطيله.";
            }
            else if (!string.IsNullOrWhiteSpace(employee.RoleTemplateName) && RoleTemplateComboBox.Items.Contains(employee.RoleTemplateName))
            {
                RoleTemplateComboBox.SelectedItem = employee.RoleTemplateName;
            }
            else
            {
                RoleTemplateComboBox.SelectedItem = "مخصص";
            }
            _suppressTemplateSync = false;
        }

        private void RoleTemplateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressTemplateSync) return;
            if (RoleTemplateComboBox.SelectedItem is string templateName &&
                SystemPermissions.RoleTemplates.TryGetValue(templateName, out var templateKeys))
            {
                _suppressTemplateSync = true;
                var keySet = new HashSet<string>(templateKeys, StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in _permissionCheckBoxes)
                {
                    kvp.Value.IsChecked = keySet.Contains(kvp.Key);
                }
                _suppressTemplateSync = false;
            }
        }

        private void PermissionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressTemplateSync) return;
            _suppressTemplateSync = true;
            RoleTemplateComboBox.SelectedItem = "مخصص";
            _suppressTemplateSync = false;
        }

        private void SelectAllPermissions_Click(object sender, RoutedEventArgs e)
        {
            _suppressTemplateSync = true;
            foreach (var cb in _permissionCheckBoxes.Values)
                cb.IsChecked = true;
            RoleTemplateComboBox.SelectedItem = "مدير كامل الصلاحيات";
            _suppressTemplateSync = false;
        }

        private void ClearAllPermissions_Click(object sender, RoutedEventArgs e)
        {
            _suppressTemplateSync = true;
            foreach (var cb in _permissionCheckBoxes.Values)
                cb.IsChecked = false;
            RoleTemplateComboBox.SelectedItem = "مخصص";
            _suppressTemplateSync = false;
        }

        private void ResetFieldBorders()
        {
            NameTextBox.BorderBrush = NormalBorderBrush;
            EmailTextBox.BorderBrush = NormalBorderBrush;
            PhoneTextBox.BorderBrush = NormalBorderBrush;
            JobTitleComboBox.BorderBrush = NormalBorderBrush;
            BaseSalaryTextBox.BorderBrush = NormalBorderBrush;
            HireDatePicker.BorderBrush = NormalBorderBrush;
            PasswordBox.BorderBrush = NormalBorderBrush;
        }

        private void HighlightErrorField(Control control)
        {
            control.BorderBrush = ErrorBorderBrush;
            control.Focus();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResetFieldBorders();

                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    HighlightErrorField(NameTextBox);
                    MessageBox.Show("الرجاء إدخال اسم الموظف.", "حقل إجباري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string? email = null;
                if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    email = EmailTextBox.Text.Trim();
                    if (!email.Contains("@") || !email.Contains("."))
                    {
                        HighlightErrorField(EmailTextBox);
                        MessageBox.Show("صيغة البريد الإلكتروني غير صحيحة (مثال: employee@example.com).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var existingEmployees = await _employeeService.GetAllEmployeesAsync();
                    if (existingEmployees.Any(u => u.Id != _employee.Id && !string.IsNullOrWhiteSpace(u.Email) && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                    {
                        HighlightErrorField(EmailTextBox);
                        MessageBox.Show("البريد الإلكتروني المدخل مسجل مسبقاً لموظف آخر. يرجى استخدام بريد إلكتروني مختلف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                {
                    HighlightErrorField(PhoneTextBox);
                    MessageBox.Show("الرجاء إدخال رقم هاتف الموظف.", "حقل إجباري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(JobTitleComboBox.Text))
                {
                    HighlightErrorField(JobTitleComboBox);
                    MessageBox.Show("الرجاء اختيار أو كتابة المسمى الوظيفي.", "حقل إجباري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string salaryStr = BaseSalaryTextBox.Text.Replace(",", "").Trim();
                if (string.IsNullOrWhiteSpace(salaryStr) ||
                    (!decimal.TryParse(salaryStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal baseSalary) &&
                     !decimal.TryParse(salaryStr, out baseSalary)) ||
                    baseSalary <= 0)
                {
                    HighlightErrorField(BaseSalaryTextBox);
                    MessageBox.Show("الرجاء إدخال الراتب الأساسي (يجب أن يكون أكبر من صفر).", "حقل إجباري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!HireDatePicker.SelectedDate.HasValue)
                {
                    HighlightErrorField(HireDatePicker);
                    MessageBox.Show("الرجاء تحديد تاريخ التعيين.", "حقل إجباري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _employee.Name = NameTextBox.Text.Trim();
                _employee.Email = email;
                _employee.Phone = PhoneTextBox.Text.Trim();
                _employee.JobTitle = JobTitleComboBox.Text.Trim();
                _employee.BaseSalary = baseSalary;
                _employee.HireDate = HireDatePicker.SelectedDate.Value;

                if (_employee.Role != UserRole.Admin && _employee.Id != 1)
                {
                    _employee.IsActive = IsActiveCheckBox.IsChecked ?? true;
                    _employee.RoleTemplateName = RoleTemplateComboBox.SelectedItem as string ?? "مخصص";
                    var selectedPerms = _permissionCheckBoxes
                        .Where(kvp => kvp.Value.IsChecked == true)
                        .Select(kvp => kvp.Key)
                        .ToList();
                    _employee.PermissionsJson = PermissionGuard.SerializePermissions(selectedPerms);
                }

                if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    _employee.Password = PasswordBox.Password;
                }

                await _employeeService.UpdateEmployeeAsync(_employee);
                
                MessageBox.Show("تم تحديث بيانات الموظف وصلاحياته بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحديث بيانات الموظف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BaseSalaryTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.TextChanged -= BaseSalaryTextBox_TextChanged;
                string valueStr = textBox.Text.Replace(",", "");
                bool endsWithDecimal = valueStr.EndsWith(".") || valueStr.EndsWith(",");

                if (decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) ||
                    decimal.TryParse(valueStr, out value))
                {
                    textBox.Text = value.ToString("#,0.##", CultureInfo.InvariantCulture) + (endsWithDecimal ? "." : "");
                    textBox.SelectionStart = textBox.Text.Length;
                }

                textBox.TextChanged += BaseSalaryTextBox_TextChanged;
            }
        }
    }
}
