# NOTAI - NEO Smart Contract

## Building the smart contract

In the root folder, run the following commands in terminal:

> dotnet restore


Downloads and installs the required packages and dependencies used in this project.

> dotnet build

Builds the smart contract.

> dotnet publish

Outputs the smart contract manifest and nef file in /bin/sc folder.

## Deploying the smart contract

Prerequisites:
- Neo-CLI R3 must be installed and block synchronization completed.
- Wallet used to login to Neo-CLI must have at least 500-1000 GAS

**Deploy with NEO-CLI**
https://docs.neo.org/docs/en-us/develop/deploy/deploy.html

In Neo-CLI run the following command to deploy your contract:
```
deploy <nefFilePath> [manifestFile]

<nefFilePath> : mandatory. The contract file
[manifestFile] : optional. The descriptive file of contract

Example:
deploy contract.nef contract.manifest.json

```
