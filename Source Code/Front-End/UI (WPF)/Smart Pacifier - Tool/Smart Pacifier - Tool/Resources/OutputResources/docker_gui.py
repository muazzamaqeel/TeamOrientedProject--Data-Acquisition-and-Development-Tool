import sys
import json

def main():
    try:
        # Read JSON data from STDIN
        input_data = sys.stdin.read()
        if not input_data:
            raise ValueError("No input data received.")

        campaign = json.loads(input_data)

        # Process the campaign data
        # Example processing: Echo back the campaign name and number of pacifiers
        campaign_name = campaign.get("CampaignName", "Unknown")
        pacifiers = campaign.get("Pacifiers", [])
        num_pacifiers = len(pacifiers)

        output = {
            "Status": "Success",
            "CampaignName": campaign_name,
            "NumberOfPacifiers": num_pacifiers,
            "Details": pacifiers  # You can add more detailed processing here
        }

        # Write the output as JSON to STDOUT
        print(json.dumps(output))
    except Exception as e:
        # Write error message to STDERR
        sys.stderr.write(f"Error: {str(e)}\n")
        sys.exit(1)

if __name__ == "__main__":
    main()
