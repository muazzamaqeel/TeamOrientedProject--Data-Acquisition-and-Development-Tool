import sys

def parse_line_protocol(lines):
    campaign_data = []
    print("Debug: Starting to parse line protocol data.")

    for line in lines:
        print(f"Processing Line: {line.strip()}")
        if ',' not in line or '=' not in line:
            print("Skipping non-protocol line.")
            continue

        parts = line.split(',')
        measurement = parts[0]
        tags = {}
        fields = {}

        # Extract tags and fields
        for tag in parts[1:]:
            if '=' in tag:
                key, value = tag.split('=', 1)
                # Determine if it's a tag or a field
                if key in ["campaign_name", "pacifier_name", "sensor_type", "status"]:
                    tags[key] = value.strip()
                else:
                    fields[key] = value.strip()

        entry = {
            "Measurement": measurement,
            "Tags": tags,
            "Fields": fields
        }
        campaign_data.append(entry)

        # Print structured data for verification
        print(f"Measurement: {measurement}")
        print("Tags:")
        for tag_key, tag_value in tags.items():
            print(f"  {tag_key}: {tag_value}")
        print("Fields:")
        for field_key, field_value in fields.items():
            print(f"  {field_key}: {field_value}")
        print("\n" + "-" * 30 + "\n")

    return campaign_data

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("No file provided.")
        sys.exit(1)

    file_path = sys.argv[1]
    try:
        with open(file_path, 'r') as f:
            lines = f.readlines()
        print(f"Reading from file: {file_path}")
        print("Loaded campaign data successfully.")
        campaign_data = parse_line_protocol(lines)

        # Save parsed data to output file
        output_file_path = r'C:\Users\muazz\AppData\Local\Temp\script_output.txt'
        with open(output_file_path, 'w') as f:
            for entry in campaign_data:
                f.write(str(entry) + '\n')
        print(f"Data saved to {output_file_path}")
        
    except Exception as e:
        print(f"Failed to load file: {e}")
