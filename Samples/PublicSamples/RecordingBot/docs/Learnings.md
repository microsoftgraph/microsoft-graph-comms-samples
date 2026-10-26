# Project Learnings

## Learning areas

- Recording options available are limited for custom or bespoke recording, Policy Based recording is the only practically available option without getting a legal exemption. Policy based recording has a number of challenges unique to its working.
  - Requiring a policy to be attached to a user who will be recorded. Every call that user joins in Teams is then recorded by default. In some usage scenarios this is not desirable.
  - Policies can only be applied to members of your AAD subscription. Guests cannot have a recording policy, it has to be applied in their home Active Directory.
  - If you need the capability to have a user sometimes recorded and sometimes not, that user either needs two identities or a capability to instruct the bot not to record. This is possible but adds complexity to the overall architecture of a solution and requires a centralised mechanism to query the status and request status changes in the bot.
- Scaling of the recording bot is non-trivial
  - Due to the bot being stateful and potentially participating in long running transactions (calls can be up to 24 Hours in duration), it is non-trivial to upgrade or scale down the number of bots deployed. A bot has to wait until all calls it is interacting with are complete. There are samples of configuration for Kubernetes in this repository that demonstrate how to do this.
- Dependencies on Media Libraries that are Windows only
  - A full installation of Windows is required for the bot to work. This creates large containers on build (includes the full Windows installation). It also requires consideration and management of the node types in Kubernetes as there will be at least one Windows pod instance in the cluster and therefore Windows nodes. There is nothing difficult about it, it just requires thinking about.
  - Development is done with the .NET Framework (it cannot be done using .NET Core).
- Micro-services architectures that depend on the bot require careful planning
  - Handing off processing to other services in latency critical applications, while in a call is not simple. This is not specific to the bot or its deployment environment but a consequence of working with audio or any near real-time source and expected output. When working with speech it does not make sense to break segments at arbitrary points. This will significantly impact the accuracy of things like speech recognition. The implications are you either have to re-assemble coherent segments of speech in a downstream service (and then determining what is a coherent segment becomes challenging and dealing with latency) or manage the service from the bot itself. This is easier to develop but results in the bot becoming monolithic. Services that are not latency dependent (e.g. speech recognition after the event) would not be impacted by this.
