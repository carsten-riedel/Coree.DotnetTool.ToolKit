# `command-exists` Command Documentation

## Overview
The `command-exists` command checks for the availability of a specified shell command in the system's path. This is particularly useful for scripts and applications that rely on external command-line tools.

## Usage
Execute the command with the required command name and optional flags:

```
toolkit command-exists <CommandName> [options]
```

### Parameters
- **`<CommandName>`**: The shell command to verify, such as `cmd`, `curl`, or `bash`. This argument is required.

### Options
- **`-l|--loglevel <LogLevel>`**: Specifies the minimum log level for messages. Valid levels are `Verbose`, `Debug`, `Information`, `Warning`, `Error`, and `Fatal`. The default is `Information`.
- **`-t|--throwError`**: If enabled, the command exits with an error code if the specified command is not found. The default is `false`.

## Examples
Check if `curl` is available on the system, with the log level set to `Debug`:

```
toolkit command-exists curl --loglevel Verbose
toolkit command-exists curl
toolkit command-exists unknowncmd
toolkit command-exists unknowncmd -t
toolkit command-exists unknowncmd -t -l Fatal
```

## Return Codes
- `0` indicates the command was found, or not found without `throwError` enabled.
- `-99` indicates the command was not found with `throwError` enabled.
- Other values may indicate different errors encountered during execution.

## Validation
The command performs checks to ensure the `<CommandName>` argument is not empty, returning an error if it is. Proper error handling and log messages provide guidance and troubleshooting help in case of failures.

## Documentation and Help
Use the `-h` switch for command-line help:

```
toolkit command-exists -h
```

This documentation provides a quick overview and practical examples to help users get started with the `command-exists` command.
