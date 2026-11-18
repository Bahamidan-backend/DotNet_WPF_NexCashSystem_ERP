const fs = require('fs');
const content = fs.readFileSync('POSSystem.WPF/Styles/CommonStyles.xaml', 'utf8');
const keys = ['SuccessButtonStyle', 'PrimaryButtonStyle', 'DangerButtonStyle', 'ActionButtonStyle'];
for (const k of keys) {
    const start = content.indexOf(`x:Key="${k}"`);
    if (start !== -1) {
        const end = content.indexOf('</Style>', start);
        console.log(`=== ${k} ===`);
        console.log(content.substring(start - 10, end + 8));
    }
}
