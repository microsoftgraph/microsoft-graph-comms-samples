Step 1: Create a Virtual Network and Subnet for Azure Firewall**
 
### Create a Virtual Network and Subnet
 
* Go to Virtual Network or create a new one if it doesn't exist.
* Create a subnet for the firewall with the purpose set to "Azure Firewall".
* Go to the firewall section and click on "Add firewall".
 
Step 2: Configure Firewall Settings*

### Configure Firewall Settings
 
* **Name**: Choose a name for the firewall (e.g., "MyFirewall").
* Create a new firewall policy.
* Select the existing Virtual Network.
* **Public IP**: Create a new public IP address (e.g., "MyFWPublicIP").
* Review and create the firewall.
 
Step 3: Configure Firewall Policy

### Configure Firewall Policy
 
* Open the firewall policy created in Step 2.
* Navigate to **Settings**.
 
#### Configure Application Rules (Ingress)
 
* Add an application rule:
	 **Name**: Give a descriptive name.
	 **Rule type**: Application rule collection.
	 **Priority**: Assign the lowest number.
	 **Rule name**: Specify a name.
	 **Source addresses**: Define the source (e.g., your VM subnet) or use `*` to allow all IP addresses.
	 **Destination FQDNs**: Allow specific domains (e.g., `www.google.com`).
	 **Save the rule**.
 
#### Configure Network Rules (Egress)
 
* Add a network rule:
	 **Name**: Give a descriptive name.
	 **Rule type**: Network rule collection.
	 **Priority**: Assign the lowest number.
	 **Rule name**: Specify a name.
	 **Source addresses**: Define the source (e.g., your VM subnet) or use `*` to allow all IP addresses.
	 **Destination addresses**: Specify external IPs or ranges (e.g., `0.0.0.0/0` for all).
	 **Protocols and ports**: Specify allowed protocols and ports.
	 **Save the rule**.