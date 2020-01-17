Solution
- Solution Items
- IncidentBot
	- wwwroot
		- audio
			<Files for audio prompt>
	- Bot
		- Bot.cs
			<Calls management>
		- CallHandler.cs
			<Base call handler for each call>
		- MeetingCallHandler.cs
			<Call handler for bot meeting calls>
		- ResponderCallHandler.cs
			<Call handler for responder calls>
		- AuthenticationProvider.cs
			<Provide the auth token from bot, and check the Incoming Request token from platform>
		- CallAffinityHelper.cs
			<Helper class to keep IncomingRequest keep in the same web instance>
	- Controller
		- IncidentsController.cs
			<Raise incidents, and get incident logs>
		- PlatformCallController.cs
			<Receive Incoming Request from platform, and hand over the message to Bot to process
		- <Other controllers>
			<Test purpose for each call actions>
	- Data
		<Data structures used by controller>
	- IncidentManagement
		<Data structures used by Bot to store the incident status>
	- Logging
		<Logging helper classes, might be updated soon>
	- appsettings.json
		<The configuration file>
	- Program.cs
		<The entry class of the executable file>
	- Startup.cs
		<The class to initialize and configure the web site>
