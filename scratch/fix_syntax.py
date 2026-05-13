file_path = r"C:\Users\minhn\source\repos\VinhKhanhweb\VinhKhanhTour\Services\LocalizationResourceManager.cs"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Fix the missing brace
content = content.replace(', { "Tùy chỉnh hệ thống"', '} , { "Tùy chỉnh hệ thống"')

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
print("Fixed!")
