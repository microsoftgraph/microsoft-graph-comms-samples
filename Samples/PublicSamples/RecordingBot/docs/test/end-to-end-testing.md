# End to end testing

The bot, being deployed to Kubernetes, and interacting with the Teams media services, can be complicated to test, especially when testing multiple callers on the same meeting. Integration testing will also be project specific and the technologies and approaches used can vary.

This document outlines some ideas using manual testing that could be used to test that the bot is working as expected. They can be used as a starting point to automate testing using the project specific testing technologies.

In this context testing means running calls and meetings that the bot is connected to.

## Testing Scenarios

#### Single call test to check if the bot is recording

* Run the bot either locally (using Ngrok) or deployed to AKS ensuring the compliance policy has been attached to a user to be used for testing.

* Create a meeting in Teams (simply add a meeting to the calendar using the test user selected)

* Join the meeting with the test user. Although it is only a single user 'meeting' it will still trigger the policy and bring the bot into the meeting. You should see the compliance recording banner appear at the top of the Teams Window. Anything spoken into the meeting will be recorded. In order to ensure the full audio stream is recorded, start and end the meeting with a known sequence (like a count up at the start and a countdown at the end). This helps to ensure the full stream has been captured.

* The call will be recorded and output to ...?

* A manual check of the recorded content can be conducted by unzipping the output file and using any audio playing application.

#### Multiple callers on a single bot

This scenario is similar to the single call test except it will require multiple test users although only a single one requires the compliance policy. Multiple users can be joined to a meeting on a development machine using the browser version of Teams.

To join multiple users to a call you will either need multiple different browsers using in private windows otherwise logged in sessions between users clash with each other. Microsoft Edge supports user profiles which can be used to create separate profiles for each attending user effectively keeping them separated.

Alternatively use multiple machines / browsers / people to attend the meeting. If unique content per user is needed then a group of willing test users will be needed (or use automated content injection - see below)

#### Automation (using a media playback bot to inject content)

...need some help with this one...
