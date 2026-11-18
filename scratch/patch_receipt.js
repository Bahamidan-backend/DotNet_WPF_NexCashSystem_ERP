const fs = require('fs');
let code = fs.readFileSync('POSSystem.WPF/Helpers/ReceiptTemplate.cs', 'utf8');

const target = `            else
            {
                mainStack.Children.Add(new TextBlock 
                { 
                    Text = $"طريقة الدفع: {order.PaymentMethodText}", 
                    FontSize = 20, 
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 10, 0, 2) 
                });

                if (!string.IsNullOrWhiteSpace(order.Notes))
                {
                    mainStack.Children.Add(new TextBlock 
                    { 
                        Text = $"ملاحظات: {order.Notes}", 
                        FontSize = 18, 
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 5, 0, 5),
                        FontStyle = FontStyles.Italic
                    });
                }
            }`;

const replacement = `            else
            {
                if (order.PaymentMethod == PaymentMethod.Split)
                {
                    var split = OrderSplitHelper.ParseSplit(order.Notes);
                    string bankName = !string.IsNullOrWhiteSpace(split?.BankOrWallet) ? split.BankOrWallet : "مدى / شبكة";
                    decimal cashVal = split?.CashAmount ?? 0;
                    decimal cardVal = split?.CardAmount ?? 0;

                    mainStack.Children.Add(new TextBlock 
                    { 
                        Text = "طريقة الدفع: ⚖️ دفع مجزأ (نقدي + شبكة)", 
                        FontSize = 20, 
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 10, 0, 4) 
                    });
                    mainStack.Children.Add(new TextBlock 
                    { 
                        Text = $"💵 نقدي: {cashVal:N2} ريال  |  💳 شبكة ({bankName}): {cardVal:N2} ريال", 
                        FontSize = 17, 
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 2, 0, 4) 
                    });
                }
                else
                {
                    mainStack.Children.Add(new TextBlock 
                    { 
                        Text = $"طريقة الدفع: {order.PaymentMethodText}", 
                        FontSize = 20, 
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 10, 0, 2) 
                    });
                }

                string generalNotes = OrderSplitHelper.CleanNotesWithoutSplitTag(OrderDepositHelper.CleanNotesWithoutDepositTag(order.Notes));
                if (!string.IsNullOrWhiteSpace(generalNotes))
                {
                    mainStack.Children.Add(new TextBlock 
                    { 
                        Text = $"ملاحظات: {generalNotes}", 
                        FontSize = 18, 
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 5, 0, 5),
                        FontStyle = FontStyles.Italic
                    });
                }
            }`;

// normalize \r\n to find
const normalizedCode = code.replace(/\r\n/g, '\n');
const normalizedTarget = target.replace(/\r\n/g, '\n');
const normalizedReplacement = replacement.replace(/\r\n/g, '\n');

if (normalizedCode.includes(normalizedTarget)) {
    const updated = normalizedCode.replace(normalizedTarget, normalizedReplacement);
    fs.writeFileSync('POSSystem.WPF/Helpers/ReceiptTemplate.cs', updated.replace(/\n/g, '\r\n'), 'utf8');
    console.log('REPLACED SUCCESSFULLY!');
} else {
    console.log('TARGET NOT FOUND!');
}

