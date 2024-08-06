# Steps to Configure Azure Firewall
=====================================

## Step 1: Create a Virtual Network and Subnet for Azure Firewall

1. Go to Virtual Network or create a new one if it doesn't exist.
2. Create a subnet for the firewall with the purpose set to "Azure Firewall". ![Create subnet](Images/CreateSubnet.png)
3. Go to the firewall section and click on "Add firewall". ![Create firewall](Images/CreateFirewall.png)

## Step 2: Configure Firewall Settings

1. # Name : Choose a name for the firewall (e.g., "MyFirewall").
2. Create a new firewall policy. ![ Create policy](Images/CreatePolicy.png)
3. Select the existing Virtual Network.
4. # Public IP: Create a new public IP address (e.g., "MyFWPublicIP").
5. Review and create the firewall. 

## Step 3: Configure Firewall Policy

1. Open the firewall policy created in Step 2.
2. Navigate to  Settings.

#### Configure Application Rules (Ingress)

1. Add an application rule:
	* # Name: Give a descriptive name.
	* # Rule type: Application rule collection.
	* # Priority: Assign the lowest number.
	* # Rule name: Specify a name.
	* # Source addresses: Define the source (e.g., your VM subnet) or use `*` to allow all IP addresses.
	* # Destination FQDNs: Allow specific domains (e.g., `www.google.com`).
	* Save the rule.

#### Configure Network Rules (Egress)

1. Add a network rule:
	* # Name: Give a descriptive name.
	* # Rule type: Network rule collection.
	* # Priority: Assign the lowest number.
	* # Rule name: Specify a name.
	* # Source addresses: Define the source (e.g., your VM subnet) or use `*` to allow all IP addresses.
	* # Destination addresses: Specify external IPs or ranges (e.g., `0.0.0.0/0` for all).
	* # Protocols and ports: Specify allowed protocols and ports.
	* Save the rule.