Create a command to init with a prompt or path to a prompt.

Based on the prompt, the cli will make some assumptions at the intent of solution and structure populate the AGENTS.md file

1. If the solution involve backend code and frontend code (it's web app) then

	- backend code goes in \backend folder 

		- src
		- tests

	- frontend code goes in \frontend folder

		- Angular multipe project workspace

			- components, api, domain library projects and a application project that consumes the others and launches web app. (maybe more than 1 application projects -- admin, client, etc..)

2. If the solution is just some code running on a machine, a cli tool then

3. Always add

	- Speed is not a goal

	- ATDD

		- if there is a web frontend than add the bit about acceptance tests are implemented using Playeight Page Object Model

		- if there is a backend component, assume .NET and assume intergration tests for acceptance

		- mention that there never shall be Architecture Tests

	- Imlementatiation shall always be radically simple


4. If web app then backend shall be 

	- Clean Architecture

	- Controllers

	- free version of MediatR

	- file per type

	- within C# projects there are proper folders and namespace properly

		- for example controllers go in a folder called Controllers and the namespace is <SOME NAME>.Api.Controllers

5. if there is a web project, specific the design-system is a first class deliverable, ensure captured correctly in the folder outline

	- see C:\projects\saturdaze\design-system, C:\projects\quinntyne-brown-consulting\design-system, C:\projects\reconciliation-through-relationships-hackathon\design-system

6. if there are cli's mention, then

	- Use .NET and `System.CommandLine`.
	- Package the application as an installable .NET tool.
	- Use Microsoft.Extensions libraries and patterns, including:
  		- Dependency injection (DI)
  		- Options
  		- Configuration
	- Apply SOLID principles throughout the codebase.
