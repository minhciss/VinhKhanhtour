import re

file_path = r"C:\Users\minhn\source\repos\VinhKhanhweb\VinhKhanhTour\Services\LocalizationResourceManager.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# For English
content = content.replace('"No snail stall found" }} ,', '"No snail stall found" },')

# For Spanish
content = content.replace('"No se encontró ningún puesto"} ,', '"No se encontró ningún puesto" },')

# For French
content = content.replace('"Aucun stand trouvé"} ,', '"Aucun stand trouvé" },')

# For German
content = content.replace('"Kein Stand gefunden"} ,', '"Kein Stand gefunden" },')

# For Chinese
content = content.replace('"未找到摊位"} ,', '"未找到摊位" },')

# For Japanese
content = content.replace('"屋台が見つかりません"} ,', '"屋台が見つかりません" },')

# For Korean
content = content.replace('"노점을 찾을 수 없습니다"} ,', '"노점을 찾을 수 없습니다" },')

# For Russian
content = content.replace('"Киоск не найден"} ,', '"Киоск не найден" },')

# For Italian
content = content.replace('"Nessuna bancarella"} ,', '"Nessuna bancarella" },')

# For Portuguese
content = content.replace('"Nenhuma banca"} ,', '"Nenhuma banca" },')

# For Hindi
content = content.replace('"कोई स्टॉल नहीं मिला"} ,', '"कोई स्टॉल नहीं मिला" },')

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
print("Fix script completed.")
