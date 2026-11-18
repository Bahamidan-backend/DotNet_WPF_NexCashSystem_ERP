const fs = require('fs');

const path = 'POSSystem.WPF/Views/Popups/WarehouseProductsControl.xaml.cs';
let content = fs.readFileSync(path, 'utf8');
content = content.replace(/\r\n/g, '\n');

const target = `        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)`;
const replace = `        private void RecordWasteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _warehouseFromProducts?.FirstOrDefault(p => p.IsSelected) 
                ?? ProductsDataGrid.SelectedItem as WarehouseFromProduct;

            var wasteControl = new RecordStockWasteControl(_warehouseId, selected?.Id);
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            if (window != null)
            {
                wasteControl.CloseRequested += (s, args) => window.HideOverlay();
                wasteControl.WasteRecorded += (s, args) =>
                {
                    LoadProducts();
                    ServiceLocator.NotifyInventoryChanged();
                };
                window.ShowOverlay(wasteControl);
            }
        }

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)`;

if (!content.includes(target)) {
    console.error('Target not found in WarehouseProductsControl.xaml.cs');
    process.exit(1);
}

content = content.replace(target, replace);
fs.writeFileSync(path, content.replace(/\n/g, '\r\n'), 'utf8');
console.log('Successfully updated WarehouseProductsControl.xaml.cs!');

