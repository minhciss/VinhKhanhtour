import re

file_path = r"C:\Users\minhn\source\repos\VinhKhanhweb\VinhKhanhTour\Services\LocalizationResourceManager.cs"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

translations = {
    "spanish": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / Idioma" }', '{ "Ngôn ngữ / Language", "Idioma" }'),
        ('{ "Tùy chỉnh hệ thống", "Ajustes del sistema" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "Configurar la experiencia de la aplicación Vinh Khanh Tour" }'),
        ('{ "CÀI ĐẶT CHUNG", "AJUSTES GENERALES" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "AUDIO Y NARRACIÓN" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "Velocidad de voz (TTS)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "Ajustar la velocidad del guía virtual" }')
    ],
    "french": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / Langue" }', '{ "Ngôn ngữ / Language", "Langue" }'),
        ('{ "Tùy chỉnh hệ thống", "Paramètres du système" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "Configurer l\'expérience de l\'application Vinh Khanh Tour" }'),
        ('{ "CÀI ĐẶT CHUNG", "PARAMÈTRES GÉNÉRAUX" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "AUDIO ET NARRATION" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "Vitesse vocale (TTS)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "Ajuster la vitesse du guide virtuel" }')
    ],
    "german": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / Sprache" }', '{ "Ngôn ngữ / Language", "Sprache" }'),
        ('{ "Tùy chỉnh hệ thống", "Systemeinstellungen" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "Konfigurieren Sie das Erlebnis der Vinh Khanh Tour App" }'),
        ('{ "CÀI ĐẶT CHUNG", "ALLGEMEINE EINSTELLUNGEN" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "AUDIO & ERZÄHLUNG" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "Sprechgeschwindigkeit (TTS)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "Passen Sie die Geschwindigkeit des virtuellen Führers an" }')
    ],
    "chinese": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / 语言" }', '{ "Ngôn ngữ / Language", "语言" }'),
        ('{ "Tùy chỉnh hệ thống", "系统设置" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "配置 Vinh Khanh Tour 应用程序体验" }'),
        ('{ "CÀI ĐẶT CHUNG", "常规设置" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "音频与解说" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "语速 (TTS)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "调整虚拟导游的语速" }')
    ],
    "japanese": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / 言語" }', '{ "Ngôn ngữ / Language", "言語" }'),
        ('{ "Tùy chỉnh hệ thống", "システム設定" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "Vinh Khanh Tourアプリのエクスペリエンスを設定する" }'),
        ('{ "CÀI ĐẶT CHUNG", "一般設定" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "オーディオとナレーション" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "話す速度 (TTS)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "バーチャルガイドの話す速度を調整する" }')
    ],
    "korean": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / 언어" }', '{ "Ngôn ngữ / Language", "언어" }'),
        ('{ "Tùy chỉnh hệ thống", "시스템 설정" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "Vinh Khanh Tour 앱 환경 설정" }'),
        ('{ "CÀI ĐẶT CHUNG", "일반 설정" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "오디오 및 내레이션" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "말하기 속도 (TTS)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "가상 가이드의 말하기 속도 조정" }')
    ],
    "russian": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / Язык" }', '{ "Ngôn ngữ / Language", "Язык" }'),
        ('{ "Tùy chỉnh hệ thống", "Системные настройки" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "Настройка интерфейса приложения Vinh Khanh Tour" }'),
        ('{ "CÀI ĐẶT CHUNG", "ОБЩИЕ НАСТРОЙКИ" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "АУДИО И ПОВЕСТВОВАНИЕ" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "Скорость речи (TTS)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "Настроить скорость виртуального гида" }')
    ],
    "italian": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / Lingua" }', '{ "Ngôn ngữ / Language", "Lingua" }'),
        ('{ "Tùy chỉnh hệ thống", "Impostazioni di sistema" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "Configura l\'esperienza dell\'app Vinh Khanh Tour" }'),
        ('{ "CÀI ĐẶT CHUNG", "IMPOSTAZIONI GENERALI" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "AUDIO E NARRAZIONE" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "Velocità della voce (TTS)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "Regola la velocità della guida virtuale" }')
    ],
    "portuguese": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / Idioma" }', '{ "Ngôn ngữ / Language", "Idioma" }'),
        ('{ "Tùy chỉnh hệ thống", "Configurações do sistema" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "Configurar a experiência do aplicativo Vinh Khanh Tour" }'),
        ('{ "CÀI ĐẶT CHUNG", "CONFIGURAÇÕES GERAIS" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "ÁUDIO E NARRAÇÃO" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "Velocidade da voz (TTS)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "Ajustar a velocidade do guia virtual" }')
    ],
    "hindi": [
        ('{ "Ngôn ngữ / Language", "Ngôn ngữ / भाषा" }', '{ "Ngôn ngữ / Language", "भाषा" }'),
        ('{ "Tùy chỉnh hệ thống", "सिस्टम सेटिंग्स" }'),
        ('{ "Thiết lập trải nghiệm ứng dụng Vinh Khanh Tour", "विन्ह खान टूर ऐप का अनुभव कॉन्फ़िगर करें" }'),
        ('{ "CÀI ĐẶT CHUNG", "सामान्य सेटिंग्स" }'),
        ('{ "NGHE VÀ THUYẾT MINH", "ऑडियो और कथन" }'),
        ('{ "Tốc độ giọng đọc (TTS)", "बोलने की गति (टीटीएस)" }'),
        ('{ "Tùy chỉnh tốc độ của hướng dẫn viên ảo", "वर्चुअल गाइड की बोलने की गति समायोजित करें" }')
    ]
}

def update_line(line, lang_name):
    # Fix the Ngôn ngữ / Language string
    old_lang, new_lang = translations[lang_name][0]
    line = line.replace(old_lang, new_lang)
    
    # Append the rest before the final } };
    append_str = ", ".join(translations[lang_name][1:])
    
    # Check if we already appended
    if "Tùy chỉnh hệ thống" in line:
        return line
        
    # Replace ending
    if line.endswith(" } };\n"):
        line = line[:-6] + ", " + append_str + " } };\n"
    elif line.endswith(" } };"):
        line = line[:-5] + ", " + append_str + " } };"
        
    return line

lines = content.splitlines(keepends=True)
for i, line in enumerate(lines):
    if "_spanishResources" in line:
        lines[i] = update_line(line, "spanish")
    elif "_frenchResources" in line:
        lines[i] = update_line(line, "french")
    elif "_germanResources" in line:
        lines[i] = update_line(line, "german")
    elif "_chineseResources" in line:
        lines[i] = update_line(line, "chinese")
    elif "_japaneseResources" in line:
        lines[i] = update_line(line, "japanese")
    elif "_koreanResources" in line:
        lines[i] = update_line(line, "korean")
    elif "_russianResources" in line:
        lines[i] = update_line(line, "russian")
    elif "_italianResources" in line:
        lines[i] = update_line(line, "italian")
    elif "_portugueseResources" in line:
        lines[i] = update_line(line, "portuguese")
    elif "_hindiResources" in line:
        lines[i] = update_line(line, "hindi")

with open(file_path, "w", encoding="utf-8") as f:
    f.writelines(lines)
print("Done!")
