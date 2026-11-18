const fs = require('fs');
let code = fs.readFileSync('POSSystem.WPF/Views/AdminPages/SettingsView.xaml', 'utf8');

const anchor = '<!-- Print Settings Section -->';

const section = `<!-- Default Bank & POS Settlement Section -->
                <Border Background="White" CornerRadius="12,12,12,12" Padding="25" Margin="0,0,0,20">
                    <Border.Effect>
                        <DropShadowEffect Color="#000000" Direction="270" ShadowDepth="1" Opacity="0.03" BlurRadius="5"/>
                    </Border.Effect>
                    <StackPanel>
                        <TextBlock Text="💳 الحساب البنكي الافتراضي للشبكة والإيداع (مدى / كارد)" FontSize="18" FontWeight="Bold" Foreground="#2c3e50" Margin="0,0,0,8"/>
                        <TextBlock Text="البنك أو المحفظة المعتمدة التي تُودع فيها مبيعات الشبكة والبطاقات وعمليات الدفع المجزأ تلقائياً." FontSize="13" Foreground="#6c757d" Margin="0,0,0,16"/>
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="20"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <StackPanel Grid.Column="0">
                                <TextBlock Text="اسم البنك أو المحفظة الافتراضية *" FontSize="14" FontWeight="Medium" Foreground="#495057" Margin="0,0,0,5"/>
                                <TextBox Text="{Binding settingsProberty.DefaultBankOrWallet, UpdateSourceTrigger=PropertyChanged}" Style="{StaticResource ModernTextBoxStyle}"/>
                            </StackPanel>
                            <StackPanel Grid.Column="2">
                                <TextBlock Text="توجيه العمليات التلقائي" FontSize="14" FontWeight="Medium" Foreground="#495057" Margin="0,0,0,5"/>
                                <Border Background="#f0fdf4" BorderBrush="#bbf7d0" BorderThickness="1" CornerRadius="8" Padding="12,10">
                                    <TextBlock Text="✓ تُسجل مبيعات الشبكة والدفع المجزأ باسم هذا البنك آلياً لتسريع الكاشير ومطابقتها في نهاية الوردية." FontSize="13" Foreground="#15803d" FontWeight="SemiBold" TextWrapping="Wrap"/>
                                </Border>
                            </StackPanel>
                        </Grid>
                    </StackPanel>
                </Border>
                ` + anchor;

const normCode = code.replace(/\r\n/g, '\n');
const normAnchor = anchor;
const normSection = section.replace(/\r\n/g, '\n');

if (normCode.includes(normAnchor)) {
    const updated = normCode.replace(normAnchor, normSection);
    fs.writeFileSync('POSSystem.WPF/Views/AdminPages/SettingsView.xaml', updated.replace(/\n/g, '\r\n'), 'utf8');
    console.log('SETTINGSVIEW PATCHED SUCCESSFULLY!');
} else {
    console.log('ANCHOR NOT FOUND!');
}

