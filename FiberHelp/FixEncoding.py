import os

directory = r"c:\Users\Genita\source\repos\FiberHelp\FiberHelp\Components\Pages"

for root, _, files in os.walk(directory):
    for file in files:
        if file.endswith(".razor"):
            path = os.path.join(root, file)
            # Try to read with utf-8 first, fallback to others
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    content = f.read()
                # Rewrite as pure utf-8
                with open(path, 'w', encoding='utf-8', newline='') as f:
                    f.write(content)
            except UnicodeDecodeError:
                try:
                    with open(path, 'r', encoding='utf-16') as f:
                        content = f.read()
                    with open(path, 'w', encoding='utf-8', newline='') as f:
                        f.write(content)
                except Exception as e:
                    print(f"Failed {path}: {e}")
