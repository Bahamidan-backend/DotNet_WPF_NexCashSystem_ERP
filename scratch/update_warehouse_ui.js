const fs = require('fs');

const path = 'POSSystem.WPF/Views/Popups/WarehouseProductsControl.xaml';
let content = fs.readFileSync(path, 'utf8');
content = content.replace(/\r\n/g, '\n');

const target = `<Button Content="🔄 تحويل مخزون" Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,10,0" Padding="15,10" FontSize="14" Click="TransferProductButton_Click"/>`;
const replace = `<Button Content="🔄 تحويل مخزون" Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,10,0" Padding="15,10" FontSize="14" Click="TransferProductButton_Click"/>
                    <Button Content="⚠️ تسجيل هدر / تالف" Style="{StaticResource DangerButtonStyle}" Background="#C0392B" Margin="0,0,10,0" Padding="15,10" FontSize="14" Click="RecordWasteButton_Click"/>`;

if (!content.includes(target)) {
    console.error('Target not found in WarehouseProductsControl.xaml');
    process.exit(1);
}

content = content.replace(target, replace);
fs.writeFileSync(path, content.replace(/\n/g, '\r\n'), 'utf8');
console.log('Successfully updated WarehouseProductsControl.xaml!');

