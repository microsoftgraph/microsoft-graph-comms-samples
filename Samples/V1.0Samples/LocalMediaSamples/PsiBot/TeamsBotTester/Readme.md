# TeamsBot Tester

This project is a simple test application for running a Teams Bot from recorded data.

Running the [PsiBot sample](../PsiBot/PsiBot.Service/) in a live Teams meeting will produce a \psi store containing the participant audio and video streams (including that of the bot). This store may be then used with this `TeamsBotTester` application to further test and develop the bot's implementation offline.

Replace the line within `CreateTeamsBot(...)` to create your own `ITeamsBot` instance and replace the line within `OpenStore(...)` to open your recorded data.
