import os
import re

views_dir = r"c:\Users\LOQ LENOVO\Documents\UPDS\Desarrollo de Sistemas II\sistema-gestion-llaves\src\SistemaGestionLlaves\Views"

def process_file(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception as e:
        print(f"Error reading {filepath}: {e}")
        return

    original = content

    # 1. card shadow-sm
    # Find any <div class="card..."> or <div class="card shadow..."> and replace with standard
    # Only replace 'card shadow' or 'card mb-4' or 'card shadow mb-4'
    content = re.sub(r'class="card\s+shadow"', r'class="card shadow-sm"', content)
    content = re.sub(r'class="card\s+shadow\s+mb-4"', r'class="card shadow-sm mb-4"', content)
    content = re.sub(r'class="card\s+mb-4"', r'class="card shadow-sm mb-4"', content)
    # If standalone card without shadow
    content = re.sub(r'class="card"(?!\s)', r'class="card shadow-sm"', content)
    # If using shadow but it's not sm
    content = re.sub(r'class="card\s+shadow"', r'class="card shadow-sm"', content)

    # 2. Table classes
    # match <table ... class="table..." ...>
    def replace_table_class(match):
        # We only want to replace the classes related to table styling.
        # We replace the entire class string with our target if it starts with 'table'.
        return match.group(1) + 'class="table table-striped table-hover table-bordered align-middle"' + match.group(2)
        
    content = re.sub(r'(<table[^>]*?)class="table[^"]*"([^>]*>)', replace_table_class, content)

    # 3. Buttons (safely replacing the color variants)
    color_variants = r'\b(?:btn-outline-primary|btn-outline-secondary|btn-outline-success|btn-outline-danger|btn-outline-warning|btn-outline-info|btn-outline-dark|btn-outline-light|btn-primary|btn-secondary|btn-success|btn-danger|btn-warning|btn-info|btn-dark|btn-light)\b'
    
    # helper to replace color variant
    def change_color(target_color, text):
        return re.sub(color_variants, target_color, text)

    # For a given tag string, if condition matches, change its color variant
    def transform_button(match):
        tag = match.group(0)
        # Check actions
        if 'asp-action="Create"' in tag:
            tag = change_color('btn-success', tag)
        elif 'asp-action="Edit"' in tag:
            tag = change_color('btn-warning', tag)
        elif 'asp-action="Delete"' in tag:
            tag = change_color('btn-danger', tag)
        elif 'asp-action="Index"' in tag:
            tag = change_color('btn-secondary', tag)
        elif 'type="submit"' in tag:
            tag = change_color('btn-primary', tag)
        return tag

    # Find all <a> and <button> tags
    content = re.sub(r'<a\s+[^>]*class="[^"]*\bbtn\b[^"]*"[^>]*>', transform_button, content)
    content = re.sub(r'<button\s+[^>]*class="[^"]*\bbtn\b[^"]*"[^>]*>', transform_button, content)

    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Updated: {filepath}")

for root, _, files in os.walk(views_dir):
    for filename in files:
        if filename.endswith(".cshtml"):
            # Skip _Layout and CambiarPassword
            if filename in ["_Layout.cshtml", "CambiarPassword.cshtml"]:
                continue
            filepath = os.path.join(root, filename)
            process_file(filepath)
