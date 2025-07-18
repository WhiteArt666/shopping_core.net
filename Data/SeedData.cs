using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Data
{
    public static class SeedData
    {
        public static void SeedColorsAndSizesSync(DataContext context)
        {
            // Seed Colors if not exists
            if (!context.Colors.Any())
            {
                var colors = new List<ColorModel>
                {
                    new ColorModel { Name = "Đỏ", ColorCode = "#FF0000", Description = "Màu đỏ cổ điển" },
                    new ColorModel { Name = "Xanh dương", ColorCode = "#0000FF", Description = "Màu xanh dương" },
                    new ColorModel { Name = "Xanh lá", ColorCode = "#008000", Description = "Màu xanh lá cây" },
                    new ColorModel { Name = "Vàng", ColorCode = "#FFFF00", Description = "Màu vàng tươi" },
                    new ColorModel { Name = "Đen", ColorCode = "#000000", Description = "Màu đen cơ bản" },
                    new ColorModel { Name = "Trắng", ColorCode = "#FFFFFF", Description = "Màu trắng tinh khôi" },
                    new ColorModel { Name = "Hồng", ColorCode = "#FFC0CB", Description = "Màu hồng nhạt" },
                    new ColorModel { Name = "Tím", ColorCode = "#800080", Description = "Màu tím đậm" },
                    new ColorModel { Name = "Nâu", ColorCode = "#A52A2A", Description = "Màu nâu đất" },
                    new ColorModel { Name = "Xám", ColorCode = "#808080", Description = "Màu xám trung tính" }
                };

                context.Colors.AddRange(colors);
                context.SaveChanges();
            }

            // Seed Sizes if not exists
            if (!context.Sizes.Any())
            {
                var sizes = new List<SizeModel>
                {
                    // Clothing sizes
                    new SizeModel { Name = "XS", Description = "Extra Small - Rất nhỏ" },
                    new SizeModel { Name = "S", Description = "Small - Nhỏ" },
                    new SizeModel { Name = "M", Description = "Medium - Vừa" },
                    new SizeModel { Name = "L", Description = "Large - Lớn" },
                    new SizeModel { Name = "XL", Description = "Extra Large - Rất lớn" },
                    new SizeModel { Name = "XXL", Description = "Double Extra Large - Cực lớn" },
                    
                    // Shoe sizes
                    new SizeModel { Name = "35", Description = "Size giày 35" },
                    new SizeModel { Name = "36", Description = "Size giày 36" },
                    new SizeModel { Name = "37", Description = "Size giày 37" },
                    new SizeModel { Name = "38", Description = "Size giày 38" },
                    new SizeModel { Name = "39", Description = "Size giày 39" },
                    new SizeModel { Name = "40", Description = "Size giày 40" },
                    new SizeModel { Name = "41", Description = "Size giày 41" },
                    new SizeModel { Name = "42", Description = "Size giày 42" },
                    new SizeModel { Name = "43", Description = "Size giày 43" },
                    new SizeModel { Name = "44", Description = "Size giày 44" },
                    
                    // Other sizes
                    new SizeModel { Name = "Free Size", Description = "Size tự do" },
                    new SizeModel { Name = "One Size", Description = "Một size duy nhất" }
                };

                context.Sizes.AddRange(sizes);
                context.SaveChanges();
            }
        }
    }
}
