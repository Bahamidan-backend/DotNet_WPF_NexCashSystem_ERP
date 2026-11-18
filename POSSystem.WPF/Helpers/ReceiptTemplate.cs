using POSSystem.WPF.Services.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.Helpers
{
    public static class ReceiptTemplate
    {
        public static FrameworkElement CreateVisual(
            Order order,
            SettingsProberty settings,
            string copyTitle = "فاتورة الزبون",
            string referenceBadge = "مرجع الزبون")
        {
            // Set Padding and Width to match physical paper (580px for 80mm)
            Border mainBorder = new Border { Width = 580, Background = Brushes.White, Padding = new Thickness(20, 0, 20, 50) };
            StackPanel mainStack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            mainBorder.Child = mainStack;

            // 1. LOGO (Centered)
            if (!string.IsNullOrEmpty(settings.LogoPath) && File.Exists(settings.LogoPath))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(settings.LogoPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    mainStack.Children.Add(new Image
                    {
                        Source = bitmap,
                        Width = 220,
                        Margin = new Thickness(0, 10, 0, 10),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                catch { }
            }

            // 2. HEADER
            string storeName = string.IsNullOrWhiteSpace(settings.ReceiptHeader) ? settings.CompanyName : settings.ReceiptHeader;
            mainStack.Children.Add(new TextBlock
            {
                Text = storeName,
                FontSize = 34,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            string safeOrdNum = order.OrderNumber ?? string.Empty;
            string invoiceNum = safeOrdNum.Length > 6 ? safeOrdNum.Substring(safeOrdNum.Length - 6) : safeOrdNum;
            string effectiveTitle = string.IsNullOrWhiteSpace(copyTitle) ? "فاتورة الزبون" : copyTitle;
            TextBlock invoiceHeader = new TextBlock
            {
                Text = $"{effectiveTitle}  {invoiceNum}",
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            invoiceHeader.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
            System.Windows.Media.NumberSubstitution.SetSubstitution(invoiceHeader, System.Windows.Media.NumberSubstitutionMethod.European);
            mainStack.Children.Add(invoiceHeader);

            if (!string.IsNullOrWhiteSpace(referenceBadge))
            {
                Border badgeBorder = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(18, 4, 18, 4),
                    Margin = new Thickness(0, 0, 0, 8),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                badgeBorder.Child = new TextBlock
                {
                    Text = $"[ {referenceBadge} ]",
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                mainStack.Children.Add(badgeBorder);
            }

            // 2.1 ORDER TYPE (Dynamic)
            if (!string.IsNullOrEmpty(order.OrderType))
            {
                mainStack.Children.Add(new TextBlock
                {
                    Text = order.OrderTypeText,
                    FontSize = 24,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5)
                });
            }

            // 2.2 CUSTOMER INFO (Dynamic for Delivery)
            if (order.OrderType == "delivery" && order.Delivery != null)
            {
                StackPanel deliveryPanel = new StackPanel { Margin = new Thickness(0, 5, 0, 10) };
                if (!string.IsNullOrEmpty(order.Delivery.DeliveryCustomerName))
                    deliveryPanel.Children.Add(new TextBlock { Text = $"العميل: {order.Delivery.DeliveryCustomerName}", FontSize = 20, FontWeight = FontWeights.Bold });
                if (!string.IsNullOrEmpty(order.Delivery.DeliveryPhone))
                    deliveryPanel.Children.Add(new TextBlock { Text = $"الجوال: {order.Delivery.DeliveryPhone}", FontSize = 18 });
                if (!string.IsNullOrEmpty(order.Delivery.DeliveryAddress))
                    deliveryPanel.Children.Add(new TextBlock { Text = $"العنوان: {order.Delivery.DeliveryAddress}", FontSize = 18, TextWrapping = TextWrapping.Wrap });
                
                mainStack.Children.Add(deliveryPanel);
            }

            mainStack.Children.Add(new TextBlock
            {
                Text = "----------------------------------------------------------------",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            });

            // 3. ITEMS TABLE HEADER (Centered with padding)
            Grid tableHeader = new Grid();
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   // Cushion Left
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }); // Name
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });   // Qty
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) }); // Price
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   // Cushion Right

            AddTableCell(tableHeader, 0, "", FontWeights.Bold, HorizontalAlignment.Center);
            AddTableCell(tableHeader, 1, "الصنف", FontWeights.Bold, HorizontalAlignment.Center);
            AddTableCell(tableHeader, 2, "الكمية", FontWeights.Bold, HorizontalAlignment.Center);
            AddTableCell(tableHeader, 3, "السعر", FontWeights.Bold, HorizontalAlignment.Center);
            AddTableCell(tableHeader, 4, "", FontWeights.Bold, HorizontalAlignment.Center);
            mainStack.Children.Add(tableHeader);

            // 4. ITEMS
            foreach (var item in order.OrderItems)
            {
                Grid row = new Grid { Margin = new Thickness(0, 5, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   // Cushion Left
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }); // Name
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });   // Qty
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) }); // Price
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   // Cushion Right

                string name = item.Name ?? "";
                string size = item.Size ?? "";
                string displayName = name;
                if (!string.IsNullOrEmpty(size) && name.EndsWith($" - {size}")) displayName = name.Substring(0, name.Length - (size.Length + 3));
                if (!string.IsNullOrEmpty(size) && !displayName.Contains(size)) displayName = $"{size} {displayName}";

                AddTableCell(row, 0, "", FontWeights.Normal, HorizontalAlignment.Center);
                AddTableCell(row, 1, displayName, FontWeights.Normal, HorizontalAlignment.Center);
                AddTableCell(row, 2, item.Quantity.ToString(), FontWeights.Normal, HorizontalAlignment.Center, forceEnglish: true);
                AddTableCell(row, 3, item.TotalPrice.ToString("N0"), FontWeights.Normal, HorizontalAlignment.Center, forceEnglish: true);
                AddTableCell(row, 4, "", FontWeights.Normal, HorizontalAlignment.Center);
                mainStack.Children.Add(row);

                if (!string.IsNullOrWhiteSpace(item.Notes))
                {
                    mainStack.Children.Add(new TextBlock 
                    { 
                        Text = $"ملاحظة: {item.Notes}", 
                        FontSize = 16, 
                        Margin = new Thickness(0, -3, 0, 5),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = Brushes.Gray
                    });
                }
            }

            mainStack.Children.Add(new TextBlock
            {
                Text = "----------------------------------------------------------------",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            });

            // 5. TOTALS SECTION (Dynamic)
            decimal subtotal = order.SubTotal > 0 ? order.SubTotal : (order.Total + order.Discount - order.ServiceCharge - order.Tax);
            
            mainStack.Children.Add(CreateTotalRow("المجموع الفرعي:", subtotal.ToString("N0")));

            if (order.Discount > 0)
            {
                mainStack.Children.Add(CreateTotalRow("الخصم:", order.Discount.ToString("N0")));
            }

            if (order.ServiceCharge > 0)
            {
                mainStack.Children.Add(CreateTotalRow("رسوم الخدمة:", order.ServiceCharge.ToString("N0")));
            }

            mainStack.Children.Add(new TextBlock
            {
                Text = "----------------------------------------------------------------",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            });
            mainStack.Children.Add(CreateTotalRow("الإجمالي النهائي:", order.Total.ToString("N0")));

            // 5.1 Payment Method & Deposit Details / Notes
            var depositInfo = OrderDepositHelper.ParseDepositInfo(order.Notes);
            if (depositInfo != null)
            {
                mainStack.Children.Add(new TextBlock 
                { 
                    Text = "طريقة الدفع: 💳 إيداع", 
                    FontSize = 20, 
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 2) 
                });
                mainStack.Children.Add(new TextBlock 
                { 
                    Text = $"عملة الإيداع: {depositInfo.Currency}", 
                    FontSize = 18, 
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 2, 0, 2) 
                });
                mainStack.Children.Add(new TextBlock 
                { 
                    Text = $"المبلغ المودع: {depositInfo.Amount:N0} {depositInfo.Currency}", 
                    FontSize = 18, 
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 2, 0, 2) 
                });
                if (!string.IsNullOrWhiteSpace(depositInfo.BankOrWallet))
                {
                    mainStack.Children.Add(new TextBlock 
                    { 
                        Text = $"جهة الإيداع: {depositInfo.BankOrWallet}", 
                        FontSize = 16, 
                        Margin = new Thickness(0, 2, 0, 2) 
                    });
                }
                if (!string.IsNullOrWhiteSpace(depositInfo.ReferenceNumber))
                {
                    mainStack.Children.Add(new TextBlock 
                    { 
                        Text = $"رقم الإشعار / الحوالة: {depositInfo.ReferenceNumber}", 
                        FontSize = 16, 
                        Margin = new Thickness(0, 2, 0, 2) 
                    });
                }

                string generalNotes = OrderDepositHelper.CleanNotesWithoutDepositTag(order.Notes);
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
            }
            else
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
                        Text = $"💵 نقدي: {cashVal:N0} ريال  |  💳 شبكة ({bankName}): {cardVal:N0} ريال", 
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
            }

            // 6. FOOTER (Machine/User on Right, Time on Left)
            Grid footerInfo = new Grid { Margin = new Thickness(0, 10, 0, 5) };
            footerInfo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerInfo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string machineName = Environment.MachineName;
            string userName = order.User != null && !string.IsNullOrWhiteSpace(order.User.Name) ? order.User.Name : "المشرف";
            string amPm = DateTime.Now.Hour >= 12 ? "م" : "ص";
            string timeExact = $"{DateTime.Now.ToString("hh:mm:ss", CultureInfo.InvariantCulture)} {amPm}";

            AddTableCell(footerInfo, 0, $"{machineName} - {userName}", FontWeights.SemiBold, HorizontalAlignment.Right);
            AddTableCell(footerInfo, 1, timeExact, FontWeights.SemiBold, HorizontalAlignment.Left, forceEnglish: true);
            mainStack.Children.Add(footerInfo);

            // Final Double Separator (Solid)
            mainStack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 2, 0, 1) });
            mainStack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 1, 0, 10) });

            // 7. DATE (Centered at the very bottom as per photo)
            string dayArabic = DateTime.Now.ToString("dddd", new CultureInfo("ar-SA"));
            string dateStr = DateTime.Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            mainStack.Children.Add(new TextBlock 
            { 
                Text = $"{dayArabic}  {dateStr}", 
                FontSize = 22, 
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 5, 0, 15) 
            });

            // 8. ZATCA Simplified Tax Invoice Section & E-Invoicing QR Code (Only if TaxRate > 0)
            if (settings.TaxRate > 0)
            {
                try
                {
                    string seller = !string.IsNullOrWhiteSpace(settings.CompanyName) ? settings.CompanyName : "متجر نقاط البيع";
                    string vatNum = !string.IsNullOrWhiteSpace(settings.TaxNumber) ? settings.TaxNumber : "300000000000003";
                    decimal taxRate = settings.TaxRate;
                    
                    decimal vatAmount = settings.TaxIncludedInPrice 
                        ? Math.Round(order.Total - (order.Total / (1 + (taxRate / 100))), 2)
                        : Math.Round(order.Total * (taxRate / 100), 2);

                    string zatcaBase64 = ZatcaQrCodeHelper.GenerateZatcaBase64(
                        seller,
                        vatNum,
                        order.CreatedAt,
                        order.Total,
                        vatAmount);

                    BitmapSource qrBitmap = ZatcaQrCodeHelper.GenerateQrBitmap(zatcaBase64, moduleSize: 4);

                    StackPanel zatcaPanel = new StackPanel 
                    { 
                        HorizontalAlignment = HorizontalAlignment.Center, 
                        Margin = new Thickness(0, 10, 0, 15) 
                    };

                    zatcaPanel.Children.Add(new TextBlock
                    {
                        Text = "فاتورة ضريبية مبسطة",
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 3)
                    });

                    if (!string.IsNullOrWhiteSpace(vatNum))
                    {
                        zatcaPanel.Children.Add(new TextBlock
                        {
                            Text = $"الرقم الضريبي: {vatNum}",
                            FontSize = 16,
                            FontWeight = FontWeights.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 5)
                        });
                    }

                    Image qrImage = new Image
                    {
                        Source = qrBitmap,
                        Width = 180,
                        Height = 180,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 5, 0, 5)
                    };
                    zatcaPanel.Children.Add(qrImage);

                    zatcaPanel.Children.Add(new TextBlock
                    {
                        Text = $"الضريبة ({taxRate:0}%): {vatAmount:N0} ريال",
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 3, 0, 0)
                    });

                    mainStack.Children.Add(zatcaPanel);
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"ZATCA QR Generation error: {ex.Message}", ex);
                }
            }

            return mainBorder;
        }

        public static FrameworkElement CreateKitchenVisual(Order order, List<OrderItem> items, string categoryName)
        {
            // Set Padding for minimal vertical waste
            Border mainBorder = new Border { Width = 580, Background = Brushes.White, Padding = new Thickness(20, 0, 20, 50) };
            StackPanel mainStack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            mainBorder.Child = mainStack;


            string dayArabic = DateTime.Now.ToString("dddd", new CultureInfo("ar-SA"));
            // Force Gregorian date using InvariantCulture
            string dateStr = DateTime.Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            // 1. HEADER (Order Number, Order Type, Day, Date then Kitchen Name)
            TextBlock orderInfo = new TextBlock 
            { 
                Text = $"#{(string.IsNullOrWhiteSpace(order.OrderNumber) ? "000" : order.OrderNumber.Split('-').Last().PadLeft(3, '0'))} - {order.OrderTypeText}", 
                FontSize = 38, 
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 5)
            };
            mainStack.Children.Add(orderInfo);

            TextBlock dateTimeHeader = new TextBlock 
            { 
                Text = $"{dayArabic}  {dateStr}", 
                FontSize = 24, 
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 2),
                Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US")
            };
            System.Windows.Media.NumberSubstitution.SetSubstitution(dateTimeHeader, System.Windows.Media.NumberSubstitutionMethod.European);
            mainStack.Children.Add(dateTimeHeader);

            mainStack.Children.Add(new TextBlock
            {
                Text = categoryName,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 1, 0, 4)
            });
            mainStack.Children.Add(new TextBlock
            {
                Text = "----------------------------------------------------------------",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            });

            // 3. ITEMS TABLE
            foreach (var item in items)
            {
                Grid row = new Grid { Margin = new Thickness(0, 1, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   // Cushion Left
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }); // Name
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });   // Qty
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) }); // Price
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   // Cushion Right


                string name = item.Name ?? "";
                string size = item.Size ?? "";
                string displayName = name;
                if (!string.IsNullOrEmpty(size) && name.EndsWith($" - {size}")) displayName = name.Substring(0, name.Length - (size.Length + 3));
                if (!string.IsNullOrEmpty(size) && !displayName.Contains(size)) displayName = $"{size} {displayName}";

                AddTableCell(row, 0, "", FontWeights.Bold, HorizontalAlignment.Center);
                AddTableCell(row, 1, displayName, FontWeights.Bold, HorizontalAlignment.Center);
                AddTableCell(row, 2, item.Quantity.ToString(), FontWeights.Bold, HorizontalAlignment.Center, forceEnglish: true);
                AddTableCell(row, 3, item.UnitPrice.ToString("N0"), FontWeights.Bold, HorizontalAlignment.Center, forceEnglish: true);
                AddTableCell(row, 4, "", FontWeights.Bold, HorizontalAlignment.Center);
                
                mainStack.Children.Add(row);

                // ملاحظة الصنف
                if (!string.IsNullOrWhiteSpace(item.Notes))
                {
                    mainStack.Children.Add(new TextBlock { 
                        Text = $"ملاحظة: {item.Notes}", 
                        FontSize = 18, 
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 4), 
                    });
                }
            }

            // Order level notes in Kitchen Receipt
            if (!string.IsNullOrWhiteSpace(order.Notes))
            {
                mainStack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 5, 0, 5), StrokeDashArray = new DoubleCollection(new double[] { 2, 2 })});
                mainStack.Children.Add(new TextBlock 
                { 
                    Text = $"ملاحظة الطلب: {order.Notes}", 
                    FontSize = 20, 
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 5, 0, 5) 
                });
            }

            mainStack.Children.Add(new TextBlock
            {
                Text = "----------------------------------------------------------------",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            });

            // 5. FOOTER (Time on Left, Machine & User on Right)
            Grid footer = new Grid { Margin = new Thickness(0, 2, 0, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Right (User/PC)
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Left (Time)

            string machineName = Environment.MachineName;
            string userName = order.User != null && !string.IsNullOrWhiteSpace(order.User.Name) ? order.User.Name : "المشرف";
            string userAndPc = $"{machineName} - {userName}";
            
            string amPm = DateTime.Now.Hour >= 12 ? "م" : "ص";
            string timeExact = $"{DateTime.Now.ToString("hh:mm:ss", CultureInfo.InvariantCulture)} {amPm}";
            
            TextBlock machineText = new TextBlock { Text = userAndPc, FontSize = 20, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 10, 0) };
            TextBlock footerTimeText = new TextBlock { Text = timeExact, FontSize = 20, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(10, 0, 0, 0) };

            Grid.SetColumn(machineText, 0);
            Grid.SetColumn(footerTimeText, 1);
            footer.Children.Add(machineText);
            footer.Children.Add(footerTimeText);
            mainStack.Children.Add(footer);

            return mainBorder;
        }

        public static FrameworkElement CreateReturnVisual(ReturnOrder returnOrder, Order originalOrder, SettingsProberty settings)
        {
            Border mainBorder = new Border { Width = 580, Background = Brushes.White, Padding = new Thickness(0, 0, 0, 15) };
            StackPanel mainStack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            mainBorder.Child = mainStack;

            string dayArabic = DateTime.Now.ToString("dddd", new CultureInfo("ar-SA"));
            string dateStr = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            string timeStr = DateTime.Now.ToString("hh:mm:ss tt", new CultureInfo("ar-SA"));

            mainStack.Children.Add(new TextBlock { Text = "فاتورة مرتجع", FontSize = 24, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 2) });
            mainStack.Children.Add(new TextBlock { Text = $"{dayArabic} {dateStr} - {timeStr}", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 5) });

            Grid infoGrid = new Grid();
            AddInfoRow(infoGrid, 0, "رقم المرتجع:", returnOrder.ReturnNumber);
            AddInfoRow(infoGrid, 1, "رقم الطلب الأصلي:", originalOrder.OrderNumber);
            mainStack.Children.Add(infoGrid);

            mainStack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 5, 0, 5), StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }) });

            Grid tableHeader = new Grid();
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AddTableCell(tableHeader, 0, "الصنف", FontWeights.Bold, HorizontalAlignment.Right);
            AddTableCell(tableHeader, 1, "الكمية", FontWeights.Bold, HorizontalAlignment.Center);
            AddTableCell(tableHeader, 2, "الإجمالي", FontWeights.Bold, HorizontalAlignment.Left);
            mainStack.Children.Add(tableHeader);

            foreach (var item in returnOrder.ReturnOrderItems)
            {
                Grid row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                string name = item.OriginalOrderItem?.Product?.Name ?? (item.OriginalOrderItem?.Name ?? "صنف");
                string size = item.OriginalOrderItem?.Size ?? "";
                string displayName = name;

                if (!string.IsNullOrEmpty(size) && name.EndsWith($" - {size}"))
                {
                    displayName = name.Substring(0, name.Length - (size.Length + 3));
                }

                if (!string.IsNullOrEmpty(size) && !displayName.Contains(size))
                {
                    displayName = $"{size} {displayName}";
                }
                    
                AddTableCell(row, 0, displayName, FontWeights.Normal, HorizontalAlignment.Right);
                AddTableCell(row, 1, item.Quantity.ToString(), FontWeights.Normal, HorizontalAlignment.Center, forceEnglish: true);
                AddTableCell(row, 2, item.TotalAmount.ToString("N0"), FontWeights.Normal, HorizontalAlignment.Left, forceEnglish: true);
                mainStack.Children.Add(row);
            }

            mainStack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 5, 0, 5), StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }) });
            mainStack.Children.Add(CreateTotalRow("إجمالي المرتجع:", returnOrder.TotalAmount.ToString("N0")));
            mainStack.Children.Add(CreateTotalRow("طريقة الاسترداد:", returnOrder.ReturnMethodText));
            mainStack.Children.Add(new TextBlock { Text = $"سبب المرتجع: {returnOrder.Reason}", FontSize = 18, Margin = new Thickness(0, 5, 0, 5) });
            mainStack.Children.Add(new TextBlock { Text = settings.ReceiptFooter ?? "شكراً لكم", FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 10) });

            return mainBorder;
        }

        public static FrameworkElement CreateDebtVisual(Order order, CustomerDebt debt, SettingsProberty settings)
        {
            Border mainBorder = new Border { Width = 580, Background = Brushes.White, Padding = new Thickness(15, 15, 15, 50) };
            StackPanel mainStack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            mainBorder.Child = mainStack;

            // 1. HEADER (Store Name)
            string storeName = settings.ReceiptHeader;
            if (string.IsNullOrWhiteSpace(storeName)) storeName = settings.CompanyName;
            mainStack.Children.Add(new TextBlock 
            { 
                Text = storeName, 
                FontSize = 36, 
                FontWeight = FontWeights.Bold, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 0, 0, 10) 
            });

            mainStack.Children.Add(new TextBlock 
            { 
                Text = "سند دين (آجل)", 
                FontSize = 28, 
                FontWeight = FontWeights.Bold, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 0, 0, 5) 
            });

            mainStack.Children.Add(new TextBlock 
            { 
                Text = $"رقم الطلب: {order.OrderNumber}", 
                FontSize = 24, 
                FontWeight = FontWeights.Bold, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 5, 0, 5) 
            });

            string dayArabic = DateTime.Now.ToString("dddd", new CultureInfo("ar-SA"));
            string dateStr = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            string timeStr = DateTime.Now.ToString("hh:mm:ss tt", new CultureInfo("ar-SA"));

            mainStack.Children.Add(new TextBlock 
            { 
                Text = $"{dayArabic} {dateStr} - {timeStr}", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 0, 0, 20) 
            });

            // 2. CUSTOMER INFO
            StackPanel customerInfo = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            customerInfo.Children.Add(new TextBlock 
            { 
                Text = $"العميل: {debt.CustomerName}", 
                FontSize = 28, 
                FontWeight = FontWeights.Bold, 
                HorizontalAlignment = HorizontalAlignment.Center 
            });

            if (!string.IsNullOrWhiteSpace(debt.PhoneNumber))
            {
                customerInfo.Children.Add(new TextBlock 
                { 
                    Text = $"الجوال: {debt.PhoneNumber}", 
                    FontSize = 24, 
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                });
            }
            mainStack.Children.Add(customerInfo);

            mainStack.Children.Add(new Rectangle { Height = 2, Fill = Brushes.Black, Margin = new Thickness(0, 5, 0, 10), StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }) });

            // 3. ITEMS TABLE
            Grid tableHeader = new Grid();
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Name
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Qty
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Price

            AddTableCell(tableHeader, 0, "الصنف", FontWeights.Bold, HorizontalAlignment.Right);
            AddTableCell(tableHeader, 1, "الكمية", FontWeights.Bold, HorizontalAlignment.Center);
            AddTableCell(tableHeader, 2, "السعر", FontWeights.Bold, HorizontalAlignment.Left);
            mainStack.Children.Add(tableHeader);

            mainStack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 5, 0, 10), StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }) });

            foreach (var item in order.OrderItems)
            {
                Grid row = new Grid { Margin = new Thickness(0, 5, 0, 5) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                string name = item.Name ?? "";
                string size = item.Size ?? "";
                string displayName = name;

                if (!string.IsNullOrEmpty(size) && name.EndsWith($" - {size}"))
                {
                    displayName = name.Substring(0, name.Length - (size.Length + 3));
                }

                if (!string.IsNullOrEmpty(size) && !displayName.Contains(size))
                {
                    displayName = $"{size} {displayName}";
                }

                AddTableCell(row, 0, displayName, FontWeights.Bold, HorizontalAlignment.Right);
                AddTableCell(row, 1, item.Quantity.ToString(), FontWeights.Bold, HorizontalAlignment.Center, forceEnglish: true);
                AddTableCell(row, 2, item.TotalPrice.ToString("N0"), FontWeights.Bold, HorizontalAlignment.Left, forceEnglish: true);
                mainStack.Children.Add(row);
            }

            mainStack.Children.Add(new Rectangle { Height = 2, Fill = Brushes.Black, Margin = new Thickness(0, 15, 0, 25), StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }) });
            
            // Total
            FrameworkElement totalRow = CreateTotalRow("إجمالي الدين:", order.Total.ToString("N0"));
            totalRow.Margin = new Thickness(0, 5, 0, 5);
            mainStack.Children.Add(totalRow);

            mainStack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 10, 0, 10) });

            mainStack.Children.Add(new TextBlock 
            { 
                Text = settings.ReceiptFooter ?? "شكراً لكم", 
                FontSize = 22, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 5, 0, 40)
            });

            return mainBorder;
        }

        public static FrameworkElement CreateDebtPaymentVisual(CustomerDebt debt, SettingsProberty settings)
        {
            Border mainBorder = new Border { Width = 580, Background = Brushes.White, Padding = new Thickness(20, 0, 20, 50) };
            StackPanel mainStack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            mainBorder.Child = mainStack;

            // 1. HEADER (Store Name)
            string storeName = settings.ReceiptHeader;
            if (string.IsNullOrWhiteSpace(storeName)) storeName = settings.CompanyName;
            mainStack.Children.Add(new TextBlock 
            { 
                Text = storeName, 
                FontSize = 36, 
                FontWeight = FontWeights.Bold, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 10, 0, 10) 
            });

            mainStack.Children.Add(new TextBlock 
            { 
                Text = "سند سداد دين", 
                FontSize = 32, 
                FontWeight = FontWeights.Bold, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 0, 0, 5) 
            });

            string dayArabic = DateTime.Now.ToString("dddd", new CultureInfo("ar-SA"));
            string dateStr = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            string timeStr = DateTime.Now.ToString("hh:mm:ss tt", new CultureInfo("ar-SA"));

            mainStack.Children.Add(new TextBlock 
            { 
                Text = $"{dayArabic} {dateStr} - {timeStr}", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 0, 0, 20) 
            });
            
            mainStack.Children.Add(new TextBlock
            {
                Text = "----------------------------------------------------------------",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 15)
            });

            // 2. CUSTOMER INFO
            StackPanel customerInfo = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            customerInfo.Children.Add(new TextBlock 
            { 
                Text = $"العميل: {debt.CustomerName}", 
                FontSize = 28, 
                FontWeight = FontWeights.Bold, 
                HorizontalAlignment = HorizontalAlignment.Center 
            });

            if (!string.IsNullOrWhiteSpace(debt.PhoneNumber))
            {
                customerInfo.Children.Add(new TextBlock 
                { 
                    Text = $"الجوال: {debt.PhoneNumber}", 
                    FontSize = 24, 
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                });
            }
            mainStack.Children.Add(customerInfo);

            mainStack.Children.Add(new TextBlock
            {
                Text = "----------------------------------------------------------------",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 15)
            });

            // 3. PAYMENT AMOUNT
            mainStack.Children.Add(CreateTotalRow("المبلغ المسدد:", debt.Amount.ToString("N0")));

            mainStack.Children.Add(new TextBlock
            {
                Text = "----------------------------------------------------------------",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 15)
            });
            mainStack.Children.Add(new TextBlock 
            { 
                Text = settings.ReceiptFooter ?? "شكراً لكم", 
                FontSize = 22, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 5, 0, 10) 
            });

            return mainBorder;
        }

        private static void AddInfoRow(Grid grid, int row, string label, string value)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            sp.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.Bold, FontSize = 20, Margin = new Thickness(0, 0, 10, 0) });
            sp.Children.Add(new TextBlock { Text = value, FontSize = 20 });
            RowDefinition rd = new RowDefinition { Height = GridLength.Auto };
            grid.RowDefinitions.Add(rd);
            Grid.SetRow(sp, grid.RowDefinitions.Count - 1);
            grid.Children.Add(sp);
        }

        private static void AddTableCell(Grid grid, int col, string text, FontWeight weight, HorizontalAlignment align, bool forceEnglish = false)
        {
            TextBlock tb = new TextBlock { Text = text, FontSize = 20, FontWeight = weight, HorizontalAlignment = align, TextWrapping = TextWrapping.Wrap };
            if (forceEnglish)
            {
                tb.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
                System.Windows.Media.NumberSubstitution.SetSubstitution(tb, System.Windows.Media.NumberSubstitutionMethod.European);
            }
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private static FrameworkElement CreateTotalRow(string label, string value, bool forceEnglish = true)
        {
            Grid g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            TextBlock lbl = new TextBlock { Text = label, FontSize = 22, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right };
            TextBlock val = new TextBlock { Text = value, FontSize = 22, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Left };
            if (forceEnglish)
            {
                val.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
                System.Windows.Media.NumberSubstitution.SetSubstitution(val, System.Windows.Media.NumberSubstitutionMethod.European);
            }
            Grid.SetColumn(lbl, 0); Grid.SetColumn(val, 1);
            g.Children.Add(lbl); g.Children.Add(val);
            return g;
        }
    }
}
