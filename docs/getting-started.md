# Getting Started

Tye is a tool that makes developing, testing, and deploying microservices and distributed applications easier. Project Tye includes a local orchestrator to make developing microservices easier and the ability to deploy microservices to Kubernetes with minimal configuration.

## Installing Tye

1. Install [.NET 8.0](<https://dot.net>).

    > Tye currently requires .NET 8. Earlier releases (`0.10.0` and earlier) required .NET Core 3.1.

1. Install tye via the following command:

    ```text
    dotnet tool install -g tye2
    ```

    OR if you already have Tye2 installed and want to update:

    ```text
    dotnet tool update -g tye2
    ```

    > If using Mac and, if getting "command not found" errors when running `tye`, you may need to ensure that the `$HOME/.dotnet/tools` directory has been added to `PATH`.
    >
    > For example, add the following to the end of your `~/.zshrc` or `~/.zprofile`:
    >
    > ```
    > # Add .NET global tools (like Tye) to PATH.
    > export PATH=$HOME/.dotnet/tools:$PATH
    > ```

## Tye VSCode extension

Install the [Tye Visual Studio Code extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-tye).

## Next steps

1. Once tye is installed, continue to the [Basic Tutorial](/tutorials/00_run_locally).
1. Check out additional samples for more advanced concepts, such as using redis, rabbitmq, and service discovery.

## Building from source

See the [developer guide](developer-guide.md) for instructions on building and installing from source.


