using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using POSSystem.WPF.Views.AdminPages;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class BankDepositDetailControl : UserControl
    {
        private readonly Action _close;
        private BankDeposit? _record;

        public BankDepositDetailControl(BankDeposit record, Action closeOverlay)
        {
            InitializeComponent();
            _close = closeOverlay;
            _record = record;
            DataContext = record;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_record == null) return;

            NotesBlock.Text = string.IsNullOrWhiteSpace(_record.Notes) ? "—" : _record.Notes;

            StatusBadgeText.Text = _record.StatusBadgeText;
            StatusBadge.Background = _record.Status switch
            {
                "Approved" => new SolidColorBrush(Color.FromRgb(0x28, 0xa7, 0x45)),
                "Rejected" => new SolidColorBrush(Color.FromRgb(0xdc, 0x35, 0x45)),
                _ => new SolidColorBrush(Color.FromRgb(0x86, 0x8e, 0x96))
            };

            var path = _record.AttachmentPath;
            var ext = string.IsNullOrEmpty(path) ? "" : Path.GetExtension(path).ToLowerInvariant();
            var imageExts = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path) && Array.IndexOf(imageExts, ext) >= 0)
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmp.EndInit();
                    bmp.Freeze();
                    AttachmentImage.Source = bmp;
                    AttachmentImage.Visibility = Visibility.Visible;
                    AttachmentPlaceholder.Visibility = Visibility.Collapsed;
                    AttachmentPlaceholder.Text = "";
                }
                catch
                {
                    ShowAttachmentFallback(path, ext);
                }
            }
            else
            {
                ShowAttachmentFallback(path, ext);
            }
        }

        private void ShowAttachmentFallback(string? path, string ext)
        {
            AttachmentImage.Visibility = Visibility.Collapsed;
            AttachmentPlaceholder.Visibility = Visibility.Visible;
            if (string.IsNullOrWhiteSpace(path))
            {
                AttachmentPlaceholder.Text = "لا يوجد مسار مرفق.";
                return;
            }

            if (!File.Exists(path))
            {
                AttachmentPlaceholder.Text = $"الملف غير موجود على الجهاز.\nالاسم: {_record?.AttachmentFileName ?? Path.GetFileName(path)}";
                return;
            }

            if (ext == ".pdf")
                AttachmentPlaceholder.Text = $"ملف PDF: {_record?.AttachmentFileName ?? Path.GetFileName(path)}\nلا تتوفر معاينة صورة داخل النظام.";
            else
                AttachmentPlaceholder.Text = $"المرفق: {_record?.AttachmentFileName ?? Path.GetFileName(path)}\nلا تتوفر معاينة لهذا النوع من الملفات.";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => _close();
    }
}
