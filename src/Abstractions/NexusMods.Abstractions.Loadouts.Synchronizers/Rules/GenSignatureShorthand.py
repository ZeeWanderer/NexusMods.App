import re

def process_summary_line(summary_line):
    summary_values = [f"Signature.{value.strip()}" for value in summary_line.split(',') if value.strip()]
    return " | ".join(summary_values)

def main():
    input_file_path = 'SignatureShorthand.cs'
    output_file_path = 'SignatureShorthand.cs'
    
    with open(input_file_path, 'r') as file:
        lines = file.readlines()
    
    in_summary = False
    summary_value = ""
    output_lines = []

    for i, line in enumerate(lines):
        if '<summary>' in line:
            in_summary = True
            summary_value = ""
        
        elif '</summary>' in line:
            in_summary = False
        
        elif in_summary:
            # Accumulate the summary value line(s)
            summary_value += line.replace("///","").strip() + ", "

        elif re.search(r'^\s*[a-zA-Z_]+\s*=', line):
            if summary_value:
                processed_summary = process_summary_line(summary_value.strip())
                line = re.sub(r'=\s*[^,]*,', f'= {processed_summary},', line)
                summary_value = ""
        
        output_lines.append(line)
    
    with open(output_file_path, 'w') as file:
        file.writelines(output_lines)

if __name__ == "__main__":
    main()
