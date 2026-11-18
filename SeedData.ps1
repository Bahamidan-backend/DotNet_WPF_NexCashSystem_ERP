# =====================================================================
# SeedData.ps1 - تعبئة قاعدة البيانات بمنتجات ومطابخ وفئات
# =====================================================================
# تأكد من إغلاق التطبيق قبل تشغيل هذا السكريبت

$dbPath = "$PSScriptRoot\POSSystem.WPF\poss.db"

if (-not (Test-Path $dbPath)) {
    Write-Host "❌ ما وجدت قاعدة البيانات في: $dbPath" -ForegroundColor Red
    Write-Host "تأكد أنك شغلت التطبيق مرة واحدة على الأقل." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ وجدت قاعدة البيانات: $dbPath" -ForegroundColor Green

# Download SQLite CLI if not present
$sqlitePath = "$PSScriptRoot\sqlite3.exe"
if (-not (Test-Path $sqlitePath)) {
    Write-Host "📥 جاري تحميل SQLite CLI..." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri "https://www.sqlite.org/2024/sqlite-tools-win-x64-3460000.zip" -OutFile "$PSScriptRoot\sqlite-tools.zip" -UseBasicParsing
        Expand-Archive -Path "$PSScriptRoot\sqlite-tools.zip" -DestinationPath "$PSScriptRoot\sqlite-tools" -Force
        $sqliteExe = Get-ChildItem "$PSScriptRoot\sqlite-tools" -Recurse -Filter "sqlite3.exe" | Select-Object -First 1
        Copy-Item $sqliteExe.FullName $sqlitePath
        Remove-Item "$PSScriptRoot\sqlite-tools.zip" -Force
        Remove-Item "$PSScriptRoot\sqlite-tools" -Recurse -Force
        Write-Host "✅ تم تحميل SQLite CLI" -ForegroundColor Green
    } catch {
        Write-Host "❌ فشل تحميل SQLite. سنستخدم طريقة بديلة..." -ForegroundColor Yellow
    }
}

$now = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")

$sql = @"
-- =====================================================================
-- 1. مطابخ (Kitchens)
-- =====================================================================
INSERT OR IGNORE INTO Kitchens (Name, Description, IsActive, CreatedAt, UpdatedAt)
VALUES
  ('المطبخ الرئيسي',    'المطبخ الرئيسي للمطعم',        1, '$now', '$now'),
  ('مطبخ المشاوي',      'منطقة الشواء والمشويات',        1, '$now', '$now'),
  ('مطبخ الحلويات',     'منطقة الحلويات والمشروبات',     1, '$now', '$now'),
  ('مطبخ الوجبات السريعة', 'وجبات سريعة وسناكات',        1, '$now', '$now');

-- =====================================================================
-- 2. فئات (Categories) - مرتبطة بالمطابخ
-- =====================================================================
INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt)
SELECT 'المقبلات والسلطات', 'مقبلات وسلطات متنوعة', 1, 0, Id, '$now', '$now'
FROM Kitchens WHERE Name = 'المطبخ الرئيسي';

INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt)
SELECT 'الأطباق الرئيسية', 'أطباق رئيسية متنوعة', 1, 0, Id, '$now', '$now'
FROM Kitchens WHERE Name = 'المطبخ الرئيسي';

INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt)
SELECT 'المشاوي واللحوم', 'مشاوي ولحوم طازجة', 1, 0, Id, '$now', '$now'
FROM Kitchens WHERE Name = 'مطبخ المشاوي';

INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt)
SELECT 'الدجاج والدواجن', 'أطباق الدجاج بأنواعه', 1, 0, Id, '$now', '$now'
FROM Kitchens WHERE Name = 'مطبخ المشاوي';

INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt)
SELECT 'الأرز والمعكرونة', 'أطباق الأرز والمعكرونة', 1, 0, Id, '$now', '$now'
FROM Kitchens WHERE Name = 'المطبخ الرئيسي';

INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt)
SELECT 'المشروبات', 'مشروبات باردة وساخنة', 1, 0, Id, '$now', '$now'
FROM Kitchens WHERE Name = 'مطبخ الحلويات';

INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt)
SELECT 'الحلويات والتحلية', 'حلويات وتحلية متنوعة', 1, 0, Id, '$now', '$now'
FROM Kitchens WHERE Name = 'مطبخ الحلويات';

INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt)
SELECT 'البرغر والسناكس', 'برغر وسناكس لذيذة', 1, 0, Id, '$now', '$now'
FROM Kitchens WHERE Name = 'مطبخ الوجبات السريعة';

-- =====================================================================
-- 3. المنتجات (Products)
-- =====================================================================

-- === فئة: المقبلات والسلطات ===
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'حمص بالطحينة', 'حمص طازج بالطحينة وزيت الزيتون', 12, 12, 50, 50, 50, 1,
  (SELECT Id FROM Categories WHERE Name = 'المقبلات والسلطات'),
  'المقبلات والسلطات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'حمص بالطحينة');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'متبل الباذنجان', 'متبل باذنجان مشوي بالطحينة', 12, 12, 50, 50, 50, 1,
  (SELECT Id FROM Categories WHERE Name = 'المقبلات والسلطات'),
  'المقبلات والسلطات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'متبل الباذنجان');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'سلطة فتوش', 'سلطة فتوش طازجة بالخضروات', 10, 10, 50, 50, 50, 1,
  (SELECT Id FROM Categories WHERE Name = 'المقبلات والسلطات'),
  'المقبلات والسلطات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'سلطة فتوش');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'سلطة خضراء', 'سلطة خضراء بالخضروات الطازجة', 8, 8, 50, 50, 50, 1,
  (SELECT Id FROM Categories WHERE Name = 'المقبلات والسلطات'),
  'المقبلات والسلطات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'سلطة خضراء');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'فلافل', 'فلافل مقرمشة بالأعشاب', 10, 10, 50, 50, 50, 1,
  (SELECT Id FROM Categories WHERE Name = 'المقبلات والسلطات'),
  'المقبلات والسلطات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'فلافل');

-- === فئة: الأطباق الرئيسية ===
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'كبسة لحم', 'كبسة أرز بالعصافير واللحم البلدي', 45, 45, 30, 30, 30, 1,
  (SELECT Id FROM Categories WHERE Name = 'الأطباق الرئيسية'),
  'الأطباق الرئيسية', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'كبسة لحم');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'كبسة دجاج', 'كبسة أرز بالدجاج والبهارات', 35, 35, 30, 30, 30, 1,
  (SELECT Id FROM Categories WHERE Name = 'الأطباق الرئيسية'),
  'الأطباق الرئيسية', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'كبسة دجاج');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'مندي لحم', 'مندي لحم ضأن بالفرن الحجري', 55, 55, 20, 20, 20, 1,
  (SELECT Id FROM Categories WHERE Name = 'الأطباق الرئيسية'),
  'الأطباق الرئيسية', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'مندي لحم');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'مندي دجاج', 'مندي دجاج بالفرن الحجري', 38, 38, 25, 25, 25, 1,
  (SELECT Id FROM Categories WHERE Name = 'الأطباق الرئيسية'),
  'الأطباق الرئيسية', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'مندي دجاج');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'مجبوس سمك', 'مجبوس أرز بالسمك الطازج', 48, 48, 15, 15, 15, 1,
  (SELECT Id FROM Categories WHERE Name = 'الأطباق الرئيسية'),
  'الأطباق الرئيسية', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'مجبوس سمك');

-- === فئة: المشاوي واللحوم ===
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'مشاوي مشكلة', 'تشكيلة من المشاوي (لحم - دجاج - كفتة)', 65, 65, 20, 20, 20, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشاوي واللحوم'),
  'المشاوي واللحوم', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ المشاوي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'مشاوي مشكلة');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'كفتة مشوية', 'كفتة لحم مشوية على الفحم', 30, 30, 30, 30, 30, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشاوي واللحوم'),
  'المشاوي واللحوم', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ المشاوي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'كفتة مشوية');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'شيش طاووق', 'شيش طاووق دجاج متبل بالثوم', 28, 28, 30, 30, 30, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشاوي واللحوم'),
  'المشاوي واللحوم', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ المشاوي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'شيش طاووق');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'ريش مشوية', 'ريش غنم مشوية على الفحم', 50, 50, 15, 15, 15, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشاوي واللحوم'),
  'المشاوي واللحوم', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ المشاوي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ريش مشوية');

-- === فئة: الدجاج والدواجن ===
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'دجاج مشوي', 'دجاج كامل مشوي بالفحم', 32, 32, 25, 25, 25, 1,
  (SELECT Id FROM Categories WHERE Name = 'الدجاج والدواجن'),
  'الدجاج والدواجن', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ المشاوي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'دجاج مشوي');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'دجاج محمر', 'دجاج محمر مقلي مقرمش', 30, 30, 25, 25, 25, 1,
  (SELECT Id FROM Categories WHERE Name = 'الدجاج والدواجن'),
  'الدجاج والدواجن', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ المشاوي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'دجاج محمر');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'فيليه دجاج', 'فيليه دجاج متبل ومشوي', 26, 26, 30, 30, 30, 1,
  (SELECT Id FROM Categories WHERE Name = 'الدجاج والدواجن'),
  'الدجاج والدواجن', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ المشاوي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'فيليه دجاج');

-- === فئة: الأرز والمعكرونة ===
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'أرز بالزبدة', 'أرز أبيض بالزبدة والزعفران', 10, 10, 50, 50, 50, 1,
  (SELECT Id FROM Categories WHERE Name = 'الأرز والمعكرونة'),
  'الأرز والمعكرونة', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'أرز بالزبدة');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'معكرونة بولونيز', 'معكرونة بصلصة اللحم', 22, 22, 30, 30, 30, 1,
  (SELECT Id FROM Categories WHERE Name = 'الأرز والمعكرونة'),
  'الأرز والمعكرونة', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'معكرونة بولونيز');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'فتة لحم', 'فتة أرز وخبز باللحم واللبن', 40, 40, 20, 20, 20, 1,
  (SELECT Id FROM Categories WHERE Name = 'الأرز والمعكرونة'),
  'الأرز والمعكرونة', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'المطبخ الرئيسي'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'فتة لحم');

-- === فئة: المشروبات ===
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'عصير برتقال طازج', 'عصير برتقال طازج ومنعش', 10, 10, 100, 100, 100, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشروبات'),
  'المشروبات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الحلويات'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'عصير برتقال طازج');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'ليمون بالنعناع', 'عصير ليمون بالنعناع والثلج', 8, 8, 100, 100, 100, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشروبات'),
  'المشروبات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الحلويات'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ليمون بالنعناع');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'كولا', 'مشروب غازي كولا', 5, 5, 200, 200, 200, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشروبات'),
  'المشروبات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الحلويات'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'كولا');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'ماء معدني', 'ماء معدني 500مل', 2, 2, 200, 200, 200, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشروبات'),
  'المشروبات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الحلويات'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ماء معدني');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'شاي أحمر', 'شاي أحمر ساخن', 4, 4, 100, 100, 100, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشروبات'),
  'المشروبات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الحلويات'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'شاي أحمر');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'قهوة عربية', 'قهوة عربية بالهيل والزعفران', 6, 6, 100, 100, 100, 1,
  (SELECT Id FROM Categories WHERE Name = 'المشروبات'),
  'المشروبات', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الحلويات'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'قهوة عربية');

-- === فئة: الحلويات والتحلية ===
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'أم علي', 'حلوى أم علي بالكريمة والمكسرات', 18, 18, 30, 30, 30, 1,
  (SELECT Id FROM Categories WHERE Name = 'الحلويات والتحلية'),
  'الحلويات والتحلية', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الحلويات'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'أم علي');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'كنافة', 'كنافة بالجبن والقطر', 20, 20, 25, 25, 25, 1,
  (SELECT Id FROM Categories WHERE Name = 'الحلويات والتحلية'),
  'الحلويات والتحلية', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الحلويات'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'كنافة');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'بسبوسة', 'بسبوسة بالكريمة والمكسرات', 14, 14, 30, 30, 30, 1,
  (SELECT Id FROM Categories WHERE Name = 'الحلويات والتحلية'),
  'الحلويات والتحلية', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الحلويات'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'بسبوسة');

-- === فئة: البرغر والسناكس ===
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'برغر لحم', 'برغر لحم بقري مع الخضار والصلصة', 25, 25, 40, 40, 40, 1,
  (SELECT Id FROM Categories WHERE Name = 'البرغر والسناكس'),
  'البرغر والسناكس', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الوجبات السريعة'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'برغر لحم');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'برغر دجاج', 'برغر دجاج مقرمش مع الصلصة', 22, 22, 40, 40, 40, 1,
  (SELECT Id FROM Categories WHERE Name = 'البرغر والسناكس'),
  'البرغر والسناكس', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الوجبات السريعة'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'برغر دجاج');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'بطاطس مقلية', 'بطاطس مقلية مقرمشة', 10, 10, 100, 100, 100, 1,
  (SELECT Id FROM Categories WHERE Name = 'البرغر والسناكس'),
  'البرغر والسناكس', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الوجبات السريعة'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'بطاطس مقلية');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'ساندويش شاورما دجاج', 'شاورما دجاج بالخبز والثوم', 18, 18, 40, 40, 40, 1,
  (SELECT Id FROM Categories WHERE Name = 'البرغر والسناكس'),
  'البرغر والسناكس', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الوجبات السريعة'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ساندويش شاورما دجاج');

INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, StockQuantity, AvailableQtyInPOS, TotalValue, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, CreatedAt, UpdatedAt)
SELECT 'ساندويش شاورما لحم', 'شاورما لحم بالخبز والثوم والطحينة', 22, 22, 40, 40, 40, 1,
  (SELECT Id FROM Categories WHERE Name = 'البرغر والسناكس'),
  'البرغر والسناكس', 1,
  (SELECT Id FROM Kitchens WHERE Name = 'مطبخ الوجبات السريعة'),
  '$now', '$now'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ساندويش شاورما لحم');

-- =====================================================================
-- ملخص
-- =====================================================================
SELECT '--- الملخص ---' AS Info;
SELECT 'المطابخ: ' || COUNT(*) AS Info FROM Kitchens;
SELECT 'الفئات: ' || COUNT(*) AS Info FROM Categories;
SELECT 'المنتجات: ' || COUNT(*) AS Info FROM Products WHERE IsActive = 1;
"@

# Write SQL to temp file
$sqlFile = "$PSScriptRoot\seed_temp.sql"
$sql | Out-File -FilePath $sqlFile -Encoding UTF8

Write-Host ""
Write-Host "🚀 جاري إدخال البيانات..." -ForegroundColor Cyan

if (Test-Path $sqlitePath) {
    $result = & $sqlitePath $dbPath ".read `"$sqlFile`"" 2>&1
    Write-Host ""
    Write-Host "📊 النتائج:" -ForegroundColor Green
    $result | ForEach-Object { Write-Host "   $_" -ForegroundColor White }
} else {
    # Fallback: Use System.Data.SQLite via .NET reflection or direct file write
    Write-Host "⚠️  sqlite3.exe غير موجود، محاولة استخدام طريقة بديلة..." -ForegroundColor Yellow
    
    # Try to find SQLite in the app's bin directory
    $appSqlite = Get-ChildItem "$PSScriptRoot\POSSystem.WPF\bin" -Recurse -Filter "*.db" -ErrorAction SilentlyContinue | Select-Object -First 1
    
    Write-Host ""
    Write-Host "📝 تم إنشاء ملف SQL في: $sqlFile" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "لتشغيله يدوياً، قم بتحميل SQLite من:" -ForegroundColor Yellow
    Write-Host "https://www.sqlite.org/download.html" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "ثم شغّل الأمر:" -ForegroundColor Yellow
    Write-Host "sqlite3.exe `"$dbPath`" `".read '$sqlFile'`"" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "✅ انتهى! أعد تشغيل التطبيق لترى المنتجات الجديدة." -ForegroundColor Green
