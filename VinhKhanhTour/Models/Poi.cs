using SQLite;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using VinhKhanhTour.Models;
namespace VinhKhanhTour.Models
{
    public partial class Poi : ObservableObject
    {

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameEs { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string NameDe { get; set; } = string.Empty;
        public string NameZh { get; set; } = string.Empty;
        public string NameJa { get; set; } = string.Empty;
        public string NameKo { get; set; } = string.Empty;
        public string NameRu { get; set; } = string.Empty;
        public string NameIt { get; set; } = string.Empty;
        public string NamePt { get; set; } = string.Empty;
        public string NameHi { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }
        public string AudioFile { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; } = 30;
        public int Priority { get; set; }

        // ================= TRANSLATIONS =================
        [Ignore]
        public List<PoiTranslation> Translations { get; set; } = new();

        public class PoiTranslation
        {
            public int Id { get; set; }
            public int PoiId { get; set; }
            public string LanguageCode { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string AudioUrl { get; set; } = string.Empty;
        }

        // ================= DISTANCE =================
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayDistanceText))]
        [NotifyPropertyChangedFor(nameof(ListDisplayDistanceText))]
        private double? _distanceToUser;
        [Ignore]
        public ImageSource DisplayImageSource
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImageUrl) || ImageUrl == "poi_placeholder.png")
                    return "vinh_khanh_food_street_banner.webp";

                // Nếu là link online
                if (ImageUrl.StartsWith("http"))
                    return ImageSource.FromUri(new Uri(ImageUrl));

                // Nếu là file local
                return ImageSource.FromFile(ImageUrl);
            }
        }

        [Ignore]
        public string DisplayDistanceText =>
            DistanceToUser.HasValue
                ? $"📍 {GetText("Cách bạn")}: {DistanceToUser.Value:F0}m"
                : $"📍 {GetText("Đang định vị...")}";

        [Ignore]
        public string ListDisplayDistanceText =>
            DistanceToUser.HasValue ? $"📍 {DistanceToUser.Value:F0}m" : "📍 ---";

        [Ignore]
        public string DisplayRadiusText =>
            $"📏 {GetText("Bán kính")}: {Radius}m";

        [Ignore]
        public string ListDisplayRadiusText =>
            $"📏 {Radius}m";

        // ================= DESCRIPTION =================
        public string Description { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public string DescriptionEs { get; set; } = string.Empty;
        public string DescriptionFr { get; set; } = string.Empty;
        public string DescriptionDe { get; set; } = string.Empty;
        public string DescriptionZh { get; set; } = string.Empty;
        public string DescriptionJa { get; set; } = string.Empty;
        public string DescriptionKo { get; set; } = string.Empty;
        public string DescriptionRu { get; set; } = string.Empty;
        public string DescriptionIt { get; set; } = string.Empty;
        public string DescriptionPt { get; set; } = string.Empty;
        public string DescriptionHi { get; set; } = string.Empty;

        // ================= TTS =================
        public string TtsScript { get; set; } = string.Empty;
        public string TtsScriptEn { get; set; } = string.Empty;
        public string TtsScriptEs { get; set; } = string.Empty;
        public string TtsScriptFr { get; set; } = string.Empty;
        public string TtsScriptDe { get; set; } = string.Empty;
        public string TtsScriptZh { get; set; } = string.Empty;
        public string TtsScriptJa { get; set; } = string.Empty;
        public string TtsScriptKo { get; set; } = string.Empty;
        public string TtsScriptRu { get; set; } = string.Empty;
        public string TtsScriptIt { get; set; } = string.Empty;
        public string TtsScriptPt { get; set; } = string.Empty;
        public string TtsScriptHi { get; set; } = string.Empty;

        // ================= DISPLAY =================
        [Ignore]
        public string DisplayImage =>
            string.IsNullOrWhiteSpace(ImageUrl) || ImageUrl == "poi_placeholder.png"
                ? "vinh_khanh_food_street_banner.webp"
                : ImageUrl;

        [Ignore]
        public string DisplayName
        {
            get
            {
                var lang = Services.LocalizationResourceManager.Instance?.CurrentLanguageCode ?? "vi";
                if (Translations != null && Translations.Any())
                {
                    var trans = Translations.FirstOrDefault(t => t.LanguageCode == lang) ?? 
                                Translations.FirstOrDefault(t => t.LanguageCode == "en") ?? 
                                Translations.FirstOrDefault();
                    if (trans != null && !string.IsNullOrWhiteSpace(trans.Title)) return trans.Title;
                }
                return GetLocalized(Name, NameEn, NameEs, NameFr, NameDe, NameZh, NameJa, NameKo, NameRu, NameIt, NamePt, NameHi);
            }
        }

        [Ignore]
        public string DisplayDescription
        {
            get
            {
                var lang = Services.LocalizationResourceManager.Instance?.CurrentLanguageCode ?? "vi";
                if (Translations != null && Translations.Any())
                {
                    var trans = Translations.FirstOrDefault(t => t.LanguageCode == lang) ?? 
                                Translations.FirstOrDefault(t => t.LanguageCode == "en") ?? 
                                Translations.FirstOrDefault();
                    if (trans != null && !string.IsNullOrWhiteSpace(trans.Description)) return trans.Description;
                }
                return GetLocalized(Description, DescriptionEn, DescriptionEs,
                                    DescriptionFr, DescriptionDe, DescriptionZh,
                                    DescriptionJa, DescriptionKo, DescriptionRu,
                                    DescriptionIt, DescriptionPt, DescriptionHi);
            }
        }

        [Ignore]
        public string DisplayTtsScript => GetLocalized(TtsScript, TtsScriptEn, TtsScriptEs,
                                                      TtsScriptFr, TtsScriptDe, TtsScriptZh,
                                                      TtsScriptJa, TtsScriptKo, TtsScriptRu,
                                                      TtsScriptIt, TtsScriptPt, TtsScriptHi);

        // ================= HELPER =================
        private string GetText(string key)
        {
            return Services.LocalizationResourceManager.Instance?[key] ?? key;
        }

        private string GetLocalized(string vi, string en, string es, string fr, string de,
                                    string zh, string ja, string ko, string ru,
                                    string it, string pt, string hi)
        {
            var lang = Services.LocalizationResourceManager.Instance?.CurrentLanguageCode ?? "vi";

            return lang switch
            {
                "en" => !string.IsNullOrWhiteSpace(en) ? en : vi,
                "es" => !string.IsNullOrWhiteSpace(es) ? es : vi,
                "fr" => !string.IsNullOrWhiteSpace(fr) ? fr : vi,
                "de" => !string.IsNullOrWhiteSpace(de) ? de : vi,
                "zh" => !string.IsNullOrWhiteSpace(zh) ? zh : vi,
                "ja" => !string.IsNullOrWhiteSpace(ja) ? ja : vi,
                "ko" => !string.IsNullOrWhiteSpace(ko) ? ko : vi,
                "ru" => !string.IsNullOrWhiteSpace(ru) ? ru : vi,
                "it" => !string.IsNullOrWhiteSpace(it) ? it : vi,
                "pt" => !string.IsNullOrWhiteSpace(pt) ? pt : vi,
                "hi" => !string.IsNullOrWhiteSpace(hi) ? hi : vi,
                _    => vi
            };
        }

        /// <summary>
        /// Thông báo cho UI cập nhật lại tất cả computed display properties khi ngôn ngữ thay đổi.
        /// Cần gọi từ ViewModel mỗi khi LocalizationResourceManager.SetCulture() được gọi.
        /// </summary>
        public void RefreshDisplayProperties()
        {
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(DisplayDescription));
            OnPropertyChanged(nameof(DisplayTtsScript));
            OnPropertyChanged(nameof(DisplayDistanceText));
            OnPropertyChanged(nameof(ListDisplayDistanceText));
            OnPropertyChanged(nameof(DisplayImage));
            OnPropertyChanged(nameof(DisplayImageSource));
            OnPropertyChanged(nameof(DisplayRadiusText));
            OnPropertyChanged(nameof(ListDisplayRadiusText));
        }
        public static List<Poi> GetSampleData()
        {
            return new List<Poi>
            {
                new Poi
                {
                    Name        = "Ốc Đào",
                    NameEn      = "Oc Dao Snail Restaurant",
                    Description = "Quán ốc nổi tiếng tại phố Vĩnh Khánh với nhiều món ốc tươi ngon, chế biến theo phong cách Nam Bộ đặc trưng.",
                    DescriptionEn = "Famous snail restaurant on Vinh Khanh street offering fresh snails prepared in authentic Southern Vietnamese style.",
                    TtsScript   = "Bạn đang ở gần Ốc Đào, một trong những quán ốc nổi tiếng nhất phố Vĩnh Khánh, Quận 4. Nơi đây nức tiếng với các món ốc tươi ngon, đặc biệt là ốc len xào dừa và ốc hương nướng mỡ hành.",
                    TtsScriptEn = "You are near Oc Dao, one of the most famous snail restaurants on Vinh Khanh food street in District 4. They are renowned for their fresh snails, especially coconut-stir-fried mud creepers and grilled scallops with spring onions.",
                    Latitude    = 10.7560,
                    Longitude   = 106.7015,
                    ImageUrl    = "poi_placeholder.png",
                    Radius      = 35,
                    Priority    = 1
                },
                new Poi
                {
                    Name        = "Quán Ốc Len Sào Dừa Bà Hai",
                    NameEn      = "Ba Hai Coconut Snail Restaurant",
                    Description = "Đặc sản ốc len xào dừa thơm lừng, thu hút thực khách từ khắp nơi đến thưởng thức tại phố ẩm thực Vĩnh Khánh.",
                    DescriptionEn = "Specialty coconut-stir-fried mud creepers, attracting food lovers from all over to this culinary street.",
                    TtsScript   = "Bạn đang đến gần quán Ốc Len Xào Dừa Bà Hai. Đây là địa chỉ nổi tiếng với món ốc len xào dừa chuẩn vị, nước cốt dừa sánh quyện cùng hương sả gừng thoang thoảng.",
                    TtsScriptEn = "You are approaching Ba Hai Coconut Snail Restaurant, famous for its aromatic coconut-stir-fried mud creepers with a creamy coconut sauce infused with lemongrass and ginger.",
                    Latitude    = 10.7548,
                    Longitude   = 106.7025,
                    ImageUrl    = "poi_placeholder.png",
                    Radius      = 30,
                    Priority    = 2
                },
                new Poi
                {
                    Name        = "Lẩu Hải Sản Vĩnh Khánh",
                    NameEn      = "Vinh Khanh Seafood Hotpot",
                    Description = "Lẩu hải sản tươi sống với nước dùng đậm đà, là điểm đến lý tưởng cho các bữa ăn nhóm tại phố Vĩnh Khánh.",
                    DescriptionEn = "Fresh seafood hotpot with rich broth, the ideal spot for group dining on Vinh Khanh street.",
                    TtsScript   = "Bạn đang ở gần quán Lẩu Hải Sản Vĩnh Khánh. Quán nổi tiếng với nồi lẩu hải sản tươi ngon, thực đơn đa dạng từ tôm, cua, mực cho đến các loại ốc biển.",
                    TtsScriptEn = "You are near Vinh Khanh Seafood Hotpot. The restaurant is known for its delicious fresh seafood hotpot with a diverse menu including shrimp, crab, squid, and various sea snails.",
                    Latitude    = 10.7538,
                    Longitude   = 106.7032,
                    ImageUrl    = "poi_placeholder.png",
                    Radius      = 35,
                    Priority    = 3
                },
                new Poi
                {
                    Name        = "Sushi Vĩnh Khánh",
                    NameEn      = "Vinh Khanh Sushi",
                    Description = "Nhà hàng sushi phong cách Nhật Bản kết hợp với hải sản tươi địa phương, mang lại trải nghiệm ẩm thực độc đáo.",
                    DescriptionEn = "Japanese-style sushi restaurant combining local fresh seafood for a unique culinary experience.",
                    TtsScript   = "Bạn đang đến gần Sushi Vĩnh Khánh, nhà hàng kết hợp phong cách Nhật Bản với nguyên liệu hải sản tươi địa phương. Nơi đây nổi bật với các cuộn sushi sáng tạo và sashimi hải sản tươi sống.",
                    TtsScriptEn = "You are approaching Vinh Khanh Sushi, a restaurant blending Japanese style with fresh local seafood ingredients. It stands out for its creative sushi rolls and fresh seafood sashimi.",
                    Latitude    = 10.7528,
                    Longitude   = 106.7040,
                    ImageUrl    = "poi_placeholder.png",
                    Radius      = 30,
                    Priority    = 4
                },
                new Poi
                {
                    Name        = "Bún Bò Huế Dì Ba",
                    NameEn      = "Di Ba Hue Beef Noodle Soup",
                    Description = "Quán bún bò Huế chuẩn vị miền Trung, nước dùng đỏ au, thơm sả và mắm ruốc đặc trưng.",
                    DescriptionEn = "Authentic Central Vietnamese beef noodle soup with rich reddish broth, fragrant lemongrass and shrimp paste.",
                    TtsScript   = "Bạn đang ở gần quán Bún Bò Huế Dì Ba. Đây là địa chỉ quen thuộc của người dân Quận 4, nổi tiếng với nồi bún bò đỏ au, thơm lừng mùi sả và mắm ruốc từ sáng sớm.",
                    TtsScriptEn = "You are near Di Ba Hue Beef Noodle Soup. This is a familiar address for District 4 residents, famous for its aromatic beef noodle soup fragrant with lemongrass and shrimp paste, served from early morning.",
                    Latitude    = 10.7572,
                    Longitude   = 106.7005,
                    ImageUrl    = "poi_placeholder.png",
                    Radius      = 30,
                    Priority    = 5
                },
            };
        }
    }
   



    }
