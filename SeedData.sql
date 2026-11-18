-- SeedData.sql - تعبئة المطابخ والفئات والمنتجات والمخازن وأصناف المخازن والإضافات

-- =====================================================================
-- 1. Kitchens (المطابخ)
-- =====================================================================
INSERT OR IGNORE INTO Kitchens (Name, Description, IsActive, CreatedAt, UpdatedAt) VALUES ('المطبخ الرئيسي', 'المطبخ الرئيسي للمطعم', 1, datetime('now'), datetime('now'));
INSERT OR IGNORE INTO Kitchens (Name, Description, IsActive, CreatedAt, UpdatedAt) VALUES ('مطبخ المشاوي', 'منطقة الشواء والمشويات', 1, datetime('now'), datetime('now'));
INSERT OR IGNORE INTO Kitchens (Name, Description, IsActive, CreatedAt, UpdatedAt) VALUES ('مطبخ الحلويات', 'منطقة الحلويات والمشروبات', 1, datetime('now'), datetime('now'));
INSERT OR IGNORE INTO Kitchens (Name, Description, IsActive, CreatedAt, UpdatedAt) VALUES ('مطبخ الوجبات السريعة', 'وجبات سريعة وسناكات', 1, datetime('now'), datetime('now'));

-- =====================================================================
-- 2. Categories (الفئات)
-- =====================================================================
INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt) SELECT 'المقبلات والسلطات', 'مقبلات وسلطات متنوعة', 1, 0, Id, datetime('now'), datetime('now') FROM Kitchens WHERE Name = 'المطبخ الرئيسي' LIMIT 1;
INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt) SELECT 'الأطباق الرئيسية', 'أطباق رئيسية متنوعة', 1, 0, Id, datetime('now'), datetime('now') FROM Kitchens WHERE Name = 'المطبخ الرئيسي' LIMIT 1;
INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt) SELECT 'المشاوي واللحوم', 'مشاوي ولحوم طازجة', 1, 0, Id, datetime('now'), datetime('now') FROM Kitchens WHERE Name = 'مطبخ المشاوي' LIMIT 1;
INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt) SELECT 'الدجاج والدواجن', 'أطباق الدجاج بأنواعه', 1, 0, Id, datetime('now'), datetime('now') FROM Kitchens WHERE Name = 'مطبخ المشاوي' LIMIT 1;
INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt) SELECT 'الأرز والمعكرونة', 'أطباق الأرز والمعكرونة', 1, 0, Id, datetime('now'), datetime('now') FROM Kitchens WHERE Name = 'المطبخ الرئيسي' LIMIT 1;
INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt) SELECT 'المشروبات', 'مشروبات باردة وساخنة', 1, 0, Id, datetime('now'), datetime('now') FROM Kitchens WHERE Name = 'مطبخ الحلويات' LIMIT 1;
INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt) SELECT 'الحلويات والتحلية', 'حلويات وتحلية متنوعة', 1, 0, Id, datetime('now'), datetime('now') FROM Kitchens WHERE Name = 'مطبخ الحلويات' LIMIT 1;
INSERT OR IGNORE INTO Categories (Name, Description, IsActive, IsSubCategory, KitchenId, CreatedAt, UpdatedAt) SELECT 'البرغر والسناكس', 'برغر وسناكس لذيذة', 1, 0, Id, datetime('now'), datetime('now') FROM Kitchens WHERE Name = 'مطبخ الوجبات السريعة' LIMIT 1;

-- =====================================================================
-- 3. Products (المنتجات)
-- =====================================================================
-- المقبلات والسلطات
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'حمص بالطحينة', 'حمص طازج بالطحينة وزيت الزيتون', 12, 12, 50, 0, 1, Id, 'المقبلات والسلطات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'متبل الباذنجان', 'متبل باذنجان مشوي بالطحينة', 12, 12, 50, 0, 1, Id, 'المقبلات والسلطات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'سلطة فتوش', 'سلطة فتوش طازجة بالخضروات والخبز المحمص', 10, 10, 50, 0, 1, Id, 'المقبلات والسلطات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'سلطة خضراء', 'سلطة خضراء بالخضروات الطازجة', 8, 8, 50, 0, 1, Id, 'المقبلات والسلطات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'فلافل', 'فلافل مقرمشة بالأعشاب', 10, 10, 50, 0, 1, Id, 'المقبلات والسلطات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

-- الأطباق الرئيسية
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'كبسة لحم', 'كبسة أرز بالعصافير واللحم البلدي', 45, 45, 30, 0, 1, Id, 'الأطباق الرئيسية', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'كبسة دجاج', 'كبسة أرز بالدجاج والبهارات', 35, 35, 30, 0, 1, Id, 'الأطباق الرئيسية', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'مندي لحم', 'مندي لحم ضأن بالفرن الحجري', 55, 55, 20, 0, 1, Id, 'الأطباق الرئيسية', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'مندي دجاج', 'مندي دجاج بالفرن الحجري', 38, 38, 25, 0, 1, Id, 'الأطباق الرئيسية', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'مجبوس سمك', 'مجبوس أرز بالسمك الطازج', 48, 48, 15, 0, 1, Id, 'الأطباق الرئيسية', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;

-- المشاوي واللحوم
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'مشاوي مشكلة', 'تشكيلة مشاوي لحم ودجاج وكفتة', 65, 65, 20, 0, 1, Id, 'المشاوي واللحوم', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشاوي واللحوم' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'كفتة مشوية', 'كفتة لحم مشوية على الفحم', 30, 30, 30, 0, 1, Id, 'المشاوي واللحوم', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشاوي واللحوم' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'شيش طاووق', 'شيش طاووق دجاج متبل بالثوم', 28, 28, 30, 0, 1, Id, 'المشاوي واللحوم', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشاوي واللحوم' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'ريش مشوية', 'ريش غنم مشوية على الفحم', 50, 50, 15, 0, 1, Id, 'المشاوي واللحوم', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشاوي واللحوم' LIMIT 1;

-- الدجاج والدواجن
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'دجاج مشوي', 'دجاج كامل مشوي بالفحم', 32, 32, 25, 0, 1, Id, 'الدجاج والدواجن', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الدجاج والدواجن' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'دجاج محمر', 'دجاج محمر مقلي مقرمش', 30, 30, 25, 0, 1, Id, 'الدجاج والدواجن', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الدجاج والدواجن' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'فيليه دجاج', 'فيليه دجاج متبل ومشوي', 26, 26, 30, 0, 1, Id, 'الدجاج والدواجن', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الدجاج والدواجن' LIMIT 1;

-- الأرز والمعكرونة
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'أرز بالزبدة', 'أرز أبيض بالزبدة والزعفران', 10, 10, 50, 0, 1, Id, 'الأرز والمعكرونة', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأرز والمعكرونة' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'معكرونة بولونيز', 'معكرونة بصلصة اللحم', 22, 22, 30, 0, 1, Id, 'الأرز والمعكرونة', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأرز والمعكرونة' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'فتة لحم', 'فتة أرز وخبز باللحم واللبن', 40, 40, 20, 0, 1, Id, 'الأرز والمعكرونة', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأرز والمعكرونة' LIMIT 1;

-- المشروبات
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'عصير برتقال طازج', 'عصير برتقال طازج ومنعش', 10, 10, 100, 0, 1, Id, 'المشروبات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشروبات' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'ليمون بالنعناع', 'عصير ليمون بالنعناع والثلج', 8, 8, 100, 0, 1, Id, 'المشروبات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشروبات' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'كولا', 'مشروب غازي كولا', 5, 5, 200, 0, 1, Id, 'المشروبات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشروبات' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'ماء معدني', 'ماء معدني 500مل', 2, 2, 200, 0, 1, Id, 'المشروبات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشروبات' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'شاي أحمر', 'شاي أحمر ساخن', 4, 4, 100, 0, 1, Id, 'المشروبات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشروبات' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'قهوة عربية', 'قهوة عربية بالهيل والزعفران', 6, 6, 100, 0, 1, Id, 'المشروبات', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشروبات' LIMIT 1;

-- الحلويات والتحلية
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'أم علي', 'حلوى أم علي بالكريمة والمكسرات', 18, 18, 30, 0, 1, Id, 'الحلويات والتحلية', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الحلويات والتحلية' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'كنافة', 'كنافة بالجبن والقطر', 20, 20, 25, 0, 1, Id, 'الحلويات والتحلية', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الحلويات والتحلية' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'بسبوسة', 'بسبوسة بالكريمة والمكسرات', 14, 14, 30, 0, 1, Id, 'الحلويات والتحلية', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الحلويات والتحلية' LIMIT 1;

-- البرغر والسناكس
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'برغر لحم', 'برغر لحم بقري مع الخضار والصلصة', 25, 25, 40, 0, 1, Id, 'البرغر والسناكس', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'البرغر والسناكس' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'برغر دجاج', 'برغر دجاج مقرمش مع الصلصة', 22, 22, 40, 0, 1, Id, 'البرغر والسناكس', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'البرغر والسناكس' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'بطاطس مقلية', 'بطاطس مقلية مقرمشة', 10, 10, 100, 0, 1, Id, 'البرغر والسناكس', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'البرغر والسناكس' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'شاورما دجاج', 'شاورما دجاج بالخبز والثوم', 18, 18, 40, 0, 1, Id, 'البرغر والسناكس', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'البرغر والسناكس' LIMIT 1;
INSERT OR IGNORE INTO Products (Name, Description, Price, BasePrice, AvailableQtyInPOS, LastCookedQty, PersonNumberFood, CategoryId, CategoryName, IsActive, KitchenId, ImagePath, CreatedAt, UpdatedAt) SELECT 'شاورما لحم', 'شاورما لحم بالخبز والطحينة', 22, 22, 40, 0, 1, Id, 'البرغر والسناكس', 1, KitchenId, '', datetime('now'), datetime('now') FROM Categories WHERE Name = 'البرغر والسناكس' LIMIT 1;

-- =====================================================================
-- 4. Warehouses (المخازن والمستودعات)
-- =====================================================================
INSERT OR IGNORE INTO Warehouses (Name, Location, ManagerName, ContactInfo, Description, IsActive, CreatedAt, UpdatedAt)
VALUES ('مخزن مواد جافة', 'المستودع الرئيسي - القسم الجاف', 'مسؤول المواد الجافة', '0550000001', 'مخزن الأرز، السكر، الدقيق، البقوليات، الزيوت والمعلبات', 1, datetime('now'), datetime('now'));

INSERT OR IGNORE INTO Warehouses (Name, Location, ManagerName, ContactInfo, Description, IsActive, CreatedAt, UpdatedAt)
VALUES ('مخزن البهارات', 'مستودع التوابل والبهارات', 'مسؤول قسم البهارات', '0550000002', 'مخزن مخصص لجميع أنواع البهارات والتوابل والأعشاب والمطيبات', 1, datetime('now'), datetime('now'));

INSERT OR IGNORE INTO Warehouses (Name, Location, ManagerName, ContactInfo, Description, IsActive, CreatedAt, UpdatedAt)
VALUES ('مخزن المجمدات', 'غرفة التجميد المركزية (-18°)', 'مسؤول قسم المجمدات', '0550000003', 'مخزن مخصص للحوم والدواجن والأسماك والخضار المجمدة', 1, datetime('now'), datetime('now'));

INSERT OR IGNORE INTO Warehouses (Name, Location, ManagerName, ContactInfo, Description, IsActive, CreatedAt, UpdatedAt)
VALUES ('مخزن الثلاجة', 'غرفة التبريد وحفظ الطازج (+4°)', 'مسؤول قسم التبريد', '0550000004', 'مخزن مخصص لمنتجات الألبان والأجبان والبيض والخضروات الطازجة', 1, datetime('now'), datetime('now'));

-- =====================================================================
-- 5. Warehouse Products (أصناف المخازن - الكمية والسعر صفر لجميع الأصناف)
-- Units: 1=كيس, 2=شدة, 3=كرتون, 4=صندوق, 5=حبة, 6=كجم, 7=ذبيحة, 8=جالون, 9=ثمن
-- =====================================================================

-- أ. مخزن المواد الجافة
INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'سكر أبيض ناعم (كيس 50 كجم)', 'سكر أبيض نقي للحلويات والطبخ', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'سكر خشن (كيس 10 كجم)', 'سكر خشن للمشروبات والحلويات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'أرز بسمتي درجة أولى (كيس 40 كجم)', 'أرز بسمتي طويل الحبة فاخر', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'أرز مزة هندي (كيس 40 كجم)', 'أرز مزة عنبر للكبسات والمندي', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'أرز مصري فاخر (كيس 25 كجم)', 'أرز مصري للمحاشي والأطباق المتنوعة', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'دقيق فاخر رقم 1 (كيس 45 كجم)', 'دقيق أبيض للمخبوزات والمعجنات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'دقيق بر أسمر (كيس 45 كجم)', 'دقيق بر كامل للخبز الصحي', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 8, 'زيت طهي نباتي (جالون 18 لتر)', 'زيت نباتي نقي للقلي والطهي', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 8, 'زيت زيتون بكر ممتاز (جالون 5 لتر)', 'زيت زيتون بكر معصور على البارد', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'سمن نباتي فاخر (كرتون)', 'سمن ممتاز للطبخ والأرز', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'مكرونة مشكلة (كرتون)', 'مكرونة متنوعة للأطباق الإيطالية والشرقية', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'ملح طعام ناعم (كرتون)', 'ملح طعام مكرر ومعالج باليود', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'معجون طماطم صلصة (كرتون)', 'صلصة طماطم مركزة عالية الجودة', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'نشا ذرة ناعم (كرتون)', 'نشا ذرة للحلويات والصلصات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'حمص حب جاف (كيس 25 كجم)', 'حمص حب يابس للمقبلات والطبخ', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'فول مدمس حب جاف (كيس 25 كجم)', 'فول مدمس درجة أولى', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'عدس أحمر مجروش (كيس 25 كجم)', 'عدس أحمر للشوربات والفتات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'تونة معلبة فاخرة (كرتون 48 حبة)', 'لحم تونة أبيض فاخر بالزيت', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'شطة حارة معلبة عبوات (كرتون)', 'شطة حمراء حارة جاهزة', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'قشطة معلبة (كرتون 48 حبة)', 'قشطة صافية للحلويات والإضافات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'مكسرات مشكلة للتزيين (كجم)', 'كاجو ولوز وصنوبر محمص', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'زبيب فاخر (كجم)', 'زبيب أشقر وأسود فاخر', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'شاي سيلاني فاخر (كرتون)', 'شاي أسود سيلاني نخب أول', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'بن قهوة عربي محمص (كجم)', 'بن هرري / خولاني محمص ومطحون', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن مواد جافة' LIMIT 1;


-- ب. مخزن البهارات
INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'بهارات مشكلة مطحونة (كجم)', 'خلطة بهارات الشيف الخاصة', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'بهارات كبسة ملكية (كجم)', 'توابل الكبسة السعودية الأصلية', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'بهارات مندي ومظبي (كجم)', 'خلطة المندي الحجري والمظبي', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'بهارات شاورما ومشويات (كجم)', 'تتبيلة الشاورما والمشويات على الفحم', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'فلفل أسود ناعم (كجم)', 'فلفل أسود مطحون نقي 100%', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'فلفل أسود حب كامل (كجم)', 'حبوب فلفل أسود كاملة للشوربات والكبسات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'كمون مطحون ناعم (كجم)', 'كمون بلدي مطحون طازج', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'كمون حب صحيح (كجم)', 'حبوب كمون كاملة', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'كزبرة جافة مطحونة (كجم)', 'كزبرة ناشفة ناعمة عالية النقاء', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'كركم أصفر مطحون (كجم)', 'كركم هندي ناصع اللون', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'كاري مدراسي فاخر (كجم)', 'مسحوق الكاري الهندي المميز', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'بابريكا مدخنة وحلوة (كجم)', 'بابريكا إسبانية مدخنة', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'قرفة عيدان خشبية (كجم)', 'أعواد قرفة سيلانية أصلية', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'قرفة ناعمة مطحونة (كجم)', 'قرفة مطحونة للحلويات والأطباق', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'هيل أخضر هندي فاخر (كجم)', 'هيل هندي حبة كبيرة درجة أولى', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'هيل مطحون طازج (كجم)', 'هيل مطحون ناعم برائحة زكية', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'قرنفل / مسمار حب (كجم)', 'مسمار قرنفل فاخر كامل', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'ورق غار هندي (كجم)', 'أوراق غار عطرية لسلق اللحوم', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'لومي أسود يابس (كجم)', 'ليمون أسود مجفف للكبسات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'زعفران أصلي سوبر نقيل (كجم)', 'زعفران أحمر فاخر للأرز والقهوة', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'سماق بلدي حامض (كجم)', 'سماق مطحون للمشويات والسلطات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'زنجبيل مطحون ناعم (كجم)', 'زنجبيل ناعم نقي للتتبيلات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'بودرة ثوم وبصل مجفف (كجم)', 'مسحوق الثوم والبصل المجفف', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن البهارات' LIMIT 1;


-- ج. مخزن المجمدات
INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'لحم ضأن بالعظم مجمد (كجم)', 'قطع لحم ضأن طازجة مجمدة للطبخ', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'لحم بقري مفروم مجمد (كجم)', 'لحم بقري مفروم ناعم للكفتة والحشوات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'لحم عجل مكعبات هبرة (كجم)', 'مكعبات لحم عجل بدون عظم للإيدامات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'دجاج مجمد كامل 1000جم (كرتون 10 حبات)', 'دجاج مجمد عالي الجودة للكبسات والشواية', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'صدور دجاج فيليه طرية (كرتون 15 كجم)', 'صدور دجاج منزوعة الجلد والعظم', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'شيش طاووق متبل جاهز (كرتون)', 'قطع دجاج متبلة جاهزة للشوي', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'فيليه سمك هامور مجمد (كرتون 10 كجم)', 'فيليه سمك أبيض فاخر بدون شوك', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'روبيان / جمبري وسط مجمد (كرتون)', 'جمبري منظف ومجمد للأطباق البحرية', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'بطاطس أصابع نصف مقلية (كرتون 4 أكياس)', 'بطاطس مقرمشة مخصصة للقلي السريع', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'خضار مشكل مجمد بازلاء وجزر (كرتون)', 'تشكيلة خضروات مجمدة للأرز والإيدام', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'ملوخية مفرومة مجمدة (كرتون)', 'ملوخية خضراء مفرومة جاهزة للطبخ', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'عجينة كنافة وبف باستري مجمدة (كرتون)', 'عجائن حلويات ومعجنات مجمدة', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 7, 'ذبيحة غنم نعيمي كاملة (ذبيحة)', 'ذبيحة خروف نعيمي مجهزة للمندي والمفطحات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 7, 'ذبيحة تيس بلدي (ذبيحة)', 'ذبيحة تيس بلدي طازجة للمظبي والمندي', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن المجمدات' LIMIT 1;


-- د. مخزن الثلاجة
INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'حليب طازج كامل الدسم (كرتون)', 'حليب سائل مبستر طازج', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'لبن زبادي طازج (كرتون)', 'لبن رائب وزبادي للسلطات والتتبيل', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'كريمة طبخ وخفق سائلة (كرتون)', 'كريمة للشوربات والحلويات والباستا', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'زبدة طبيعية قوالب (كرتون)', 'زبدة نقية غير مملحة للأرز والحلويات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'جبنة موزاريلا مبشورة (كرتون 10 كجم)', 'جبن موزاريلا ممتاز للمخبوزات والبيتزا', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'جبنة شيدر شرائح وسائلة (كرتون)', 'جبنة شيدر للبرغر والسندويشات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 6, 'جبنة عكاوي / نابلسية (كجم)', 'جبنة بيضاء حلوة للكنافة والحلويات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 4, 'بيض مائدة طازج (صندوق 360 بيضة)', 'بيض طازج للمطبخ والمخبوزات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 4, 'طماطم طازجة درجة أولى (صندوق)', 'طماطم حمراء طازجة للسلطات والطبخ', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 4, 'خيار طازج محمي (صندوق)', 'خيار أخضر طازج للسلطات والمقبلات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 4, 'خس أمريكي وسلطة كابوتشا (صندوق)', 'خس مقرمش للسلطات والبرغر', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'بصل أحمر خيش (كيس 20 كجم)', 'بصل أحمر طازج للكشنات والطبخ', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 1, 'بطاطس طازجة (كيس 20 كجم)', 'بطاطس طازجة للإيدامات والطبخ', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 3, 'ثوم طازج ومقشر (كرتون)', 'ثوم طازج مقشر ومجهز للمطبخ', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 4, 'ليمون أصفر وأخضر طازج (صندوق)', 'ليمون طازج للعصائر والمأكولات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 2, 'كزبرة وبقدونس ونعناع طازج (شدة)', 'ورقيات خضراء طازجة يومية', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 8, 'مايونيز وصوصات تبريد (جالون)', 'مايونيز مبرد للسندويشات والمقبلات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;

INSERT OR IGNORE INTO warehouseFromProducts (WarehouseId, Price, StockQuantity, PortionUnit, Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT Id, '0.0', '0.0', 8, 'طحينة سمسم سائلة خام (جالون)', 'طحينة سمسم نقي للمتبل والحمص والصوصات', 1, datetime('now'), datetime('now') FROM Warehouses WHERE Name = 'مخزن الثلاجة' LIMIT 1;


-- =====================================================================
-- 6. Addons (قسم إدارة الإضافات - الكمية صفر لجميع الأصناف)
-- Price = سعر البيع, BuyingPrice = سعر التكلفة/الشراء, Quantity = 0.0
-- =====================================================================
UPDATE Addons SET Price = 2.0, BuyingPrice = 0.8, Quantity = '0.0', Description = 'شطة حارة سائلة / عبوة إضافية' WHERE Name = 'شطة';

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'شطة', 2.0, 0.8, '0.0', Id, '0.0', 1, 0, 1, 'شطة حارة سائلة / عبوة إضافية', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'قشطة', 4.0, 2.0, '0.0', Id, '0.0', 1, 0, 1, 'علبة قشطة طازجة إضافية', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الحلويات والتحلية' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'فول', 5.0, 2.5, '0.0', Id, '0.0', 1, 0, 1, 'صحن فول مدمس إضافي بالزيت', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'تونة', 7.0, 3.5, '0.0', Id, '0.0', 1, 0, 1, 'علبة تونة فاخرة إضافية', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'طحينة', 3.0, 1.2, '0.0', Id, '0.0', 1, 0, 1, 'صوص طحينة سمسم إضافي', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'دقوس حار (شطة حمراء)', 2.0, 0.7, '0.0', Id, '0.0', 1, 0, 1, 'صلصة دقوس حار طازجة للكبسة والمندي', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'شطة خضراء حارة', 2.0, 0.7, '0.0', Id, '0.0', 1, 0, 1, 'سحاوق يمني / شطة خضراء بالجبن', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'ثومية (صوص الثوم)', 2.5, 1.0, '0.0', Id, '0.0', 1, 0, 1, 'كريم ثومية كريمي للشاورما والمشويات', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشاوي واللحوم' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'جبنة شرائح', 3.0, 1.5, '0.0', Id, '0.0', 1, 0, 1, 'شريحة جبنة شيدر إضافية للبرغر', datetime('now'), datetime('now') FROM Categories WHERE Name = 'البرغر والسناكس' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'جبنة سائلة', 4.0, 2.0, '0.0', Id, '0.0', 1, 0, 1, 'صوص جبنة شيدر سائل وغني', datetime('now'), datetime('now') FROM Categories WHERE Name = 'البرغر والسناكس' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'جبنة موزاريلا إضافية', 5.0, 2.5, '0.0', Id, '0.0', 1, 0, 1, 'جبنة موزاريلا ذائبة إضافية', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأرز والمعكرونة' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'صوص باربكيو', 3.0, 1.2, '0.0', Id, '0.0', 1, 0, 1, 'صلصة باربكيو مدخنة', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المشاوي واللحوم' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'صوص رانش', 3.0, 1.2, '0.0', Id, '0.0', 1, 0, 1, 'صلصة رانش كريمية بالأعشاب', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'صوص مايونيز بالثوم', 2.5, 1.0, '0.0', Id, '0.0', 1, 0, 1, 'مايونيز بنكهة الثوم والأعشاب', datetime('now'), datetime('now') FROM Categories WHERE Name = 'البرغر والسناكس' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'صوص كتشب إضافي', 1.5, 0.5, '0.0', Id, '0.0', 1, 0, 1, 'عبوة كتشب طماطم إضافية', datetime('now'), datetime('now') FROM Categories WHERE Name = 'البرغر والسناكس' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'بيض مقلي / مسلوق إضافي', 3.0, 1.2, '0.0', Id, '0.0', 1, 0, 1, 'حبة بيض طازجة مقلية أو مسلوقة', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'مكسرات إضافية (صنوبر وكاجو وزبيب)', 6.0, 3.0, '0.0', Id, '0.0', 1, 0, 1, 'تزيين مكسرات فاخرة للأرز والأطباق', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'عسل طبيعي إضافي', 4.0, 2.0, '0.0', Id, '0.0', 1, 0, 1, 'عسل سدر / طبيعي للتحلية والعريكة', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الحلويات والتحلية' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'زيت زيتون بكر إضافي', 2.5, 1.0, '0.0', Id, '0.0', 1, 0, 1, 'زيت زيتون بكر ممتاز معصور على البارد', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'بصل مقلي مقرمش إضافي', 2.0, 0.8, '0.0', Id, '0.0', 1, 0, 1, 'بصل محمر مقرمش لتزيين الكبسات والمضغوط', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'مخلل مشكل إضافي', 2.0, 0.7, '0.0', Id, '0.0', 1, 0, 1, 'تشكيلة مخللات خيار ولفت وجزر مقرمشة', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'شرائح ليمون وفلفل أخضر', 1.5, 0.5, '0.0', Id, '0.0', 1, 0, 1, 'ليمون طازج وفلفل حار جانبي', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'خبز تنور / عربي إضافي', 1.0, 0.4, '0.0', Id, '0.0', 1, 0, 1, 'رغيف خبز تنور حار طازج', datetime('now'), datetime('now') FROM Categories WHERE Name = 'المقبلات والسلطات' LIMIT 1;

INSERT OR IGNORE INTO Addons (Name, Price, BuyingPrice, Quantity, CategoryId, BaseAddonQuantity, DeductOnSale, IsInHouse, IsActive, Description, CreatedAt, UpdatedAt)
SELECT 'مرق لحم / دجاج إضافي', 3.0, 1.0, '0.0', Id, '0.0', 1, 0, 1, 'كوب مرق غني بالبهارات والليمون', datetime('now'), datetime('now') FROM Categories WHERE Name = 'الأطباق الرئيسية' LIMIT 1;

-- =====================================================================
-- Summary Results
-- =====================================================================
SELECT 'Kitchens: ' || COUNT(*) FROM Kitchens;
SELECT 'Categories: ' || COUNT(*) FROM Categories;
SELECT 'Products: ' || COUNT(*) FROM Products WHERE IsActive = 1;
SELECT 'Warehouses: ' || COUNT(*) FROM Warehouses WHERE IsActive = 1;
SELECT 'Warehouse Products: ' || COUNT(*) FROM warehouseFromProducts WHERE IsActive = 1;
SELECT 'Addons: ' || COUNT(*) FROM Addons WHERE IsActive = 1;
