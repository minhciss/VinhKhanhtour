import docx
import sys

def read_docx(file_path):
    try:
        doc = docx.Document(file_path)
        full_text = []
        for para in doc.paragraphs:
            full_text.append(para.text)
        return '\n'.join(full_text)
    except Exception as e:
        return str(e)

if __name__ == '__main__':
    content = read_docx(r"c:\Users\minhn\Downloads\BaoCao_DoAn_TraCuuDiemThi_V3.docx")
    print(content[-5000:]) # Print last 5000 characters to see the context
