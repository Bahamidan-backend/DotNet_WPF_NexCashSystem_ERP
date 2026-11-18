const fs = require('fs');

const path = 'POSSystem.WPF/ViewModels/Cashier/ShiftManagementViewModel.cs';
let content = fs.readFileSync(path, 'utf8');
content = content.replace(/\r\n/g, '\n');

const target = `                if (CurrentShift == null) return;
                if (_hasPendingOrders)
                {
                    MessageBox.Show("لا يمكن إنهاء المناوبة وهناك طلبات جارية في قائمة الطلبات. الرجاء إتمامها أو إلغائها أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }`;

const replace = `                if (CurrentShift == null) return;
                if (_hasPendingOrders)
                {
                    MessageBox.Show("لا يمكن إنهاء المناوبة وهناك طلبات جارية في قائمة الطلبات. الرجاء إتمامها أو إلغائها أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من عدم وجود طلبات معلقة (HeldOrders) محفوظة في النظام
                var heldOrders = await ServiceLocator.HeldOrderService.GetHeldOrdersAsync();
                if (heldOrders != null && heldOrders.Any())
                {
                    MessageBox.Show(
                        $"لا يمكن إنهاء المناوبة لوجود ({heldOrders.Count()}) طلبات معلقة محفوظة في النظام!\\n\\nيرجى استرجاع هذه الطلبات وإتمامها أو إلغاؤها أولاً قبل إغلاق المناوبة لضمان دقة الحسابات والمخزون.",
                        "تنبيه - توجد طلبات معلقة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }`;

if (!content.includes(target)) {
    console.error('target not found!');
    process.exit(1);
}

content = content.replace(target, replace);
fs.writeFileSync(path, content.replace(/\n/g, '\r\n'), 'utf8');
console.log('Successfully updated ShiftManagementViewModel.cs!');

