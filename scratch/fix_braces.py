file_path = r"C:\Users\minhn\source\repos\VinhKhanhweb\VinhKhanhTour\Services\LocalizationResourceManager.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Replace all } } }; with } };
content = content.replace("} } };", "} };")

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
print("Fix extra braces completed.")
