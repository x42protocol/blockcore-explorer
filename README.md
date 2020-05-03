# Blockcore Explorer | Multi-Chain Block Explorer

[1]: https://github.com/block-core/blockcore-explorer/actions
[2]: https://github.com/block-core/blockcore-explorer/workflows/Build%20and%20Release%20Binaries/badge.svg
[3]: https://github.com/block-core/blockcore-explorer/workflows/Build%20and%20Release%20Docker%20Image/badge.svg

[![Build Status][2]][1] [![Release Status][3]][1]

## Build and Setup

When you fork this repo, you must ensure you add your own personal secret for DockerHub to your fork.

Do this by navigating to the "/settings/secrets" page on your GitHub project.

Create a new secret named "DockerHubSecret" and fill in your secret (password) from DockerHub.

## Developing

It is prefer to use either Visual Studio 2019 or Visual Studio Code to develop on this project.

The easiest way is to run debugging directly from either of these editors, but it is also possible to 
run using Docker.

## Docker

Manual build from the source directory:

```sh
docker build -t blockcoreexplorer .
```

```sh
docker run -p 9911:9911 --name myexplorer blockcoreexplorer:latest -e chain=CITY
```
