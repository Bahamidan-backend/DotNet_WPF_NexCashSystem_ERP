const fs = require('fs');
let xaml = fs.readFileSync('POSSystem.WPF/Views/AdminPages/FinancialOverviewView.xaml', 'utf8');

const target = `<StackPanel>
                <TextBlock Text="التقارير المالية" FontSize="28" FontWeight="Bold" Foreground="#2c3e50" Margin="0,0,0,8"/>
                <TextBlock Text="متابعة الإيرادات مقابل الإيداعات البنكية لاكتشاف الفروقات، التأخير، أو الإيداعات الناقصة" FontSize="14" Foreground="#6c757d" TextWrapping="Wrap"/>
            </StackPanel>`;

const replacement = `<Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0">
                    <TextBlock Text="التقارير المالية" FontSize="28" FontWeight="Bold" Foreground="#2c3e50" Margin="0,0,0,8"/>
                    <TextBlock Text="متابعة الإيرادات مقابل الإيداعات البنكية لاكتشاف الفروقات، التأخير، أو الإيداعات الناقصة" FontSize="14" Foreground="#6c757d" TextWrapping="Wrap"/>
                </StackPanel>
                <Button Grid.Column="1" Command="{Binding RefreshCommand}" Style="{StaticResource PrimaryButtonStyle}" Height="42" Padding="18,0" VerticalAlignment="Center" Cursor="Hand" ToolTip="تحديث فوري لبيانات الإيرادات والورديات والإيداعات">
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="🔄" FontSize="16" Margin="0,0,8,0" VerticalAlignment="Center"/>
                        <TextBlock Text="تحديث البيانات" FontSize="14" FontWeight="Bold" VerticalAlignment="Center"/>
                    </StackPanel>
                </Button>
            </Grid>`;

// Normalize CRLF to LF for comparison
const isCRLF = xaml.includes('\r\n');
const normalizedXaml = xaml.replace(/\r\n/g, '\n');
const normalizedTarget = target.replace(/\r\n/g, '\n');
const normalizedReplacement = replacement.replace(/\r\n/g, '\n');

if (!normalizedXaml.includes(normalizedTarget)) {
    console.error("Target not found!");
    process.exit(1);
}

let result = normalizedXaml.replace(normalizedTarget, normalizedReplacement);
if (isCRLF) {
    result = result.replace(/\n/g, '\r\n');
}

fs.writeFileSync('POSSystem.WPF/Views/AdminPages/FinancialOverviewView.xaml', result, 'utf8');
console.log("Successfully updated FinancialOverviewView.xaml!");

