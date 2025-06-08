> [!NOTE]  
> Public Samples are provided by developers from the Microsoft Graph community.  
> Public Samples are not official Microsoft Communication samples, and not supported by the Microsoft Communication engineering team. It is recommended that you contact the sample owner before using code from Public Samples in production systems.

---

**Title:**  
RecordingBot

**Description:**  
A compliance recording bot sample running on AKS (Azure Kubernetes Service).

**Authors:**  
Owning organization: LM IT Services AG ([@LM-Development](https://github.com/LM-Development) on GitHub)  
Lead maintainer: [@InDieTasten](https://github.com/InDieTasten)  

**Fork for issues and contributions:**  
[LM-Development/aks-sample](https://github.com/LM-Development/aks-sample)

# Introduction

This sample allows you to build, deploy and test a compliance recording bot running on Azure Kubernetes Service and is currently the only sample demonstrating a basis for zero-downtime deployments and horizontal scaling ability.

The unique purpose of this sample is to demonstrate how to run production grade bots. The bot implementation can easily be changed to fit other use cases other than compliance recording.

## Contents

| File/folder       | Description                                |
|-------------------|--------------------------------------------|
| `build`           | Contains `Dockerfile` to containerise app. |
| `deploy`          | Helm chart and other manifests to deploy.  |
| `docs`            | Markdown files with steps and guides.      |
| `scripts`         | Helpful scripts for running project.       |
| `src`             | Sample source code.                        |
| `.gitignore`      | Define what to ignore at commit time.      |
| `README.md`       | This README file.                          |
| `LICENSE`         | The license for the sample.                |

## Getting Started

The easiest way to grasp the basics surrounding compliance bots is to read up on the following documentation topics:

- [High Level Overview over the Infrastructure for the Recording Bot](./docs/explanations/recording-bot-overview.md)
- [Bot Service - Entra Id and Microsoft Graph API Calling Permissions](./docs/explanations/recording-bot-permission.md)
- [Compliance Recording Policies](./docs/explanations/recording-bot-policy.md)

### Deploy

[Deploy the recording bot sample to AKS with the tutorial](./docs/tutorials/deploy-tutorial.md), to get your own recording bot up and running.

### Test

1. [Assign a policy to a Teams user](./docs/guides/policy.md)
2. Sign into Teams with user under compliance recording policy
3. Start a meeting
4. Verify existence of recording banner in meeting

> [!NOTE]  
> Propagation of Compliance Recording Policy assignments can take up to 24 hours.

## Questions and Support

Please open an issue in the [issue tracker](https://github.com/LM-Development/aks-sample/issues) of the source repository.
