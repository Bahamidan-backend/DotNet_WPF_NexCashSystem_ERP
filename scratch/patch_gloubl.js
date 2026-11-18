const fs = require('fs');
let code = fs.readFileSync('POSSystem.WPF/StaticFunction/GloublStatic.cs', 'utf8');

if (!code.includes('using POSSystem.WPF.Services;')) {
    code = 'using POSSystem.WPF.Services;\r\n' + code;
}

const target = 'receipt.AppendLine($"خزينة إيداع/شبكة | {report.CardSales:N2}");';
const replacement = `string defaultBank = SettingsService.LoadSettings().DefaultBankOrWallet;
                string bankSuffix = !string.IsNullOrWhiteSpace(defaultBank) ? $" ({defaultBank})" : "";
                receipt.AppendLine($"خزينة إيداع/شبكة{bankSuffix} | {report.CardSales:N2}");`;

if (code.includes(target)) {
    code = code.replace(target, replacement);
    fs.writeFileSync('POSSystem.WPF/StaticFunction/GloublStatic.cs', code, 'utf8');
    console.log('GLOUBLSTATIC PATCHED SUCCESSFULLY!');
} else {
    console.log('TARGET NOT FOUND IN GLOUBLSTATIC!');
}

