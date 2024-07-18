# teams-recording-bot

Full setup guide can be found [here](../../docs/deploy/aks.md).

## Configuration

The following table lists the configurable parameters of the teams-recording-bot and their default calues.

Parameter | Description | Default
--- | --- | ---
`host` | Used by the bot and ingress. Specifies where Teams is sending the media and notifications to, as well as instructing `cert-manager` the domain to generate the certificate for. This value is required for the chart to deploy. | `null`
`override.name` | If not set, the default name of `teams-recording-bot` will be used to deploy the chart. | `""`
`override.namespace` | If not set, the default namespace the chart will be deployed into will be `teams-recording-bot`. | `""`
`scale.maxReplicaCount` | Used to deploy a number of services, adds port mappings to the `ConfigMap` and opens additional ports on the `LoadBalancer` in preparation for additional bots to be deployed. | `3`
`scale.replicaCount` | The number of bots to deploy. | `3`
`image.domain` | Where your recording bot container lives (for example `acr.azurecr.io`). This value is required for the chart to deploy. | `null`
`image.pullPolicy` | Sets the pull policy. By default the image will only be pulled if it is not present locally. | `IfNotPresent`
`image.tag` | Override the image tag you want to pull and deploy. If not set, by default, the `.Chart.AppVersion` will be used instead. | `""`
`ingress.tls.secretName`| The secret name `cert-manager` generates after creating the certificate. | `ingress-tls`
`autoscaling.enabled` | Flag for enabling and disabling `replicas` in the `StatefulSet`. | `false`
`internal.port` | HTTP port the bot listens to to receive HTTP based events (like joining calls and notifications) from Teams. | `9441`
`internal.media` | The internal TCP port the bot listens to and receives media from. | `8445`
`public.media` | The port is used to send media traffic from Teams to the bot. `public.media` is added to the `LoadBalancer` with each bot receiving their own public facing TCP port. | `28550`
`public.ip` | This value should be the static IP address you have reserved in your Azure subscription and is what your `host` is pointing to. This value is required for the chart to deploy. | `null`
`node.target` | Name of the node to bound the `StatefulSet` to. By default, the `StatefulSet` expects there to be a Windows node deployed in your node pool with the name `scale`. | `scale`
`terminationGracePeriod` | When scaling down, `terminationGracePeriod` allows pods with ongoing calls to remain active until either the call ends or the `terminationGracePeriod` expires. This number is in seconds and by default is set to `54000` seconds (15 hours). This should give the pod enough time to wait for the call to end before the pod is allowed to terminate and remove itself. | `54000`
`container.env.azureSettings.captureEvents` | Flag to indicate if events should be saved or not. If set to `false` the no events will be saved. | `false`
`container.env.azureSettings.eventsFolder` | Folder where events (like HTTP events) are saved. Folder will be created is it does not exist. Folder can be located under `%TEMP%\teams-recording-bot\`. Events are separated in their own folders using the `callId`. Events are saved as `BSON` files and is used to help generate test data which can be archived as `zip` files and reused in unit tests. | `events`
`container.env.azureSettings.mediaFolder` | Folder where audio archives will be saved. If the folder does not exist, it will be created. Folder can be located under `%TEMP%\teams-recording-bot\`. Media for each call will be saved in their own folder using the `callId`. | `archive`
`container.env.azureSettings.eventhubKey` | API Key of your Azure Event Hub. This is required if you want to send events using Azure Event Hub. If not set, no events will be sent. | `""`
`container.env.azureSettings.eventhubName` | The name of your Azure Event Hub. | `recordingbotevents`
`container.env.azureSettings.eventhubRegion` | Azure region your Azure Event Hub is deployed to. | `""`
`container.env.azureSettings.isStereo` | Flag to indicate the output audio file should be stereo or mono. If set to `false`, the output audio file saved to disk will be mono while if set to `true`, the output audio file saved to disk will be stereo. | `false`
`container.env.azureSettings.wavSampleRate` | When omitted, audio file will be saved a sample rate of 16000 Hz, but this env variable can be used to re-sample the audio stream into a different sample bit rate, i.e. 44.1 KHz for mp3 files. | `0`
`container.env.azureSettings.wavQuality` | From 0 to 100%, when omitted, by default is 100%. | `100`
`container.port` | Internal port the bot listens to for HTTP requests. | `9441`
`resources` | Used to set resource limits on the bot deployed. | `{}`
