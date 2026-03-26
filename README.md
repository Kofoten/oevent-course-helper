# OEvent Course Helper

OEvent Course Helper is an extensible command-line utility designed to assist with real-world logistics and data processing for orienteering events. Built on modern .NET 8 and `Spectre.Console`, it provides a robust foundation for various event management tasks.

Currently, its flagship feature solves a complex set cover problem: determining the most efficient order to test-run courses to ensure all control points are physically verified before an event.

## 🏗️ The 7 Design Principles

This codebase is governed by seven strict design "commandments" to ensure robustness, speed, and quality:

1. **Algorithmic Elegance over Brute Force:** No parallelization. Problems are solved intelligently through advanced heuristics and algorithms rather than relying on raw compute power.
2. **Strict Immutability:** State is read-only by default. Everything outside of an immediate working set is strictly immutable.
3. **Absolute Determinism:** The exact same input will always yield the exact same result, ensuring reliable and reproducible output.
4. **Minimal Memory Footprint:** The application utilizes highly optimized structures to run efficiently on virtually any hardware.
5. **Maintainability by Design:** The codebase is structured for clarity, modularity, and long-term ease of maintenance.
6. **High Testability:** Pure functions and a decoupled architecture ensure the application is easily and comprehensively testable.
7. **Maximized Performance:** We squeeze out every ounce of execution speed possible without ever compromising the preceding six rules.

## 🛠️ Available Commands

To run the application, use the base command followed by the specific tool you want to execute.

### `prioritize` (Test-Run Optimizer)

Before a major event, test runners must visit every control point to ensure they are correctly placed and described. This command takes an IOF XML 3.0 file containing all available courses and calculates the smallest set of courses needed to visit every single control at least once.

It uses a highly optimized Beam Search Algorithm guided by a "rarity score" heuristic, and it prunes "dominated" courses to ensure volunteers do the minimum amount of running necessary.

#### Syntax

```shell
OEventCourseHelper prioritize <IOFXmlFilePath> [options]
```

#### Arguments & Options

- `<IOFXmlFilePath>`: **(Required)** The file path to your IOF XML 3.0 course data file.
- `-w` or `--beam-width <int>`: Sets the width of the beam for the search algorithm. (Default: 3)
- `-f` or `--filter <string>`: One or more strings to filter courses by name. Only courses containing one of these strings will be included.
- `--strict`: If set, the command will fail/abort if any controls cannot be visited by the available/filtered courses. If omitted, it logs a warning instead.
- `--porcelain [version]`: Outputs the results in a strict, machine-readable format. Optionally specify the output version, available versions: v1. (Default: v1)
- `-h` or `--help`: Shows the help message and exits.

#### Example

```shell
OEventCourseHelper prioritize SampleData/Test.Courses.xml -w 5 -f "Long" --strict
```

## 🖥️ Output Modes

The tool uses structured logging with two distinct output formatters:

- **Human-Readable (Spectre)**: The default mode, providing clean console output.
- **Machine-Readable (Porcelain, v1)**: Activated via the `--porcelain` flag, outputting strict, tab-separated log entries (e.g., `INF:11003|PriorityResult\tpriority="1",courseName="Course 1",required="True"`) designed to be easily parsed by automation scripts.
  - **Sanitization**: To guarantee a single-line format, Carriage Returns (`\r`) are stripped and Newlines (`\n`) are replaced with a single space.
  - **Escaping**: Internal double quotes are escaped as `""` per RFC 4180.
  - **Consistency**: Values are always enclosed in double quotes to ensure reliable parsing.

## 📋 Exit Codes & Event IDs

To facilitate seamless integration into CI/CD pipelines or other automated systems, OEvent Course Helper returns predictable exit codes and structured Event IDs.

### Exit Codes

| Code | Name | Description |
| :--- | :--- | :--- |
| 0 | Success | The command completed successfully. |
| 1 | UnhandledException | An unexpected critical error occurred. |
| 2 | UnknownResult | An unknown state was reached. |
| 3 | UnexpectedErrorCode | An unexpected internal error code was generated. |
| 4 | FailedToParseArguments | Invalid arguments or options were provided to the CLI. |
| 5 | FailedToLoadFile | The specified IOF XML file could not be loaded or parsed. |
| 6 | NoSolutionFound | The prioritize command could not find a mathematical solution to cover all controls. |
| 7 | ValidationFailed | Strict validation failed (e.g., unreachable controls with `--strict` enabled). |

### Log Event IDs

When running with `--porcelain`, you can reliably parse these event IDs:

#### General Events (10000 - 10999)

| ID | Level | Name | Description |
| :--- | :--- | :--- | :--- |
| 10000 | Critical | UnhandledException | Logged when an unknown error occurs. |
| 10001 | Error | FailedToParseArguments | Logged when there invalid arguments are passed |
| 10002 | Error | FailedToLoadFile | Logged when the input file cannot be accessed or loaded. |
| 10003 | Error | IofSchemaViolation | Logged when the XML file violates the IOF 3.0 schema. |

#### Course Prioritizer Events (11000 - 11999)

| ID | Level | Name | Description |
| :--- | :--- | :--- | :--- |
| 11000 | Error | NoSolutionFound | Logged when no combination of courses can cover all controls. |
| 11001 | Warning | ControlSkippedWarning | A specific control cannot be visited by any available courses. |
| 11002 | Error | StrictModeValidationFailed | Strict mode aborted the run due to unvisitable controls. |
| 11003 | Info | PriorityResult | Logs the result for a single course, indicating its priority and if it is required. |
| 11004 | Info | PrioritizeSummary | Final count of courses, required courses, and total controls visited. |

## 📥 Installation

To run this tool, you must have the **.NET 8 Runtime** installed on your system. You do not need the full .NET SDK, but the executables are explicitly *not* published as self-contained binaries.

1. Navigate to the **Releases** page of this repository.
2. Download the `.zip` file matching your operating system (`win-x64`, `win-x86`, or `linux-x64`).
3. Extract the archive and run the `OEventCourseHelper` executable directly from your terminal.

## 🤖 CI/CD & Automation

This project is built with standard .NET 8 tooling and is designed to be highly testable (Commandment #6). The repository utilizes GitHub Actions (via self-hosted Actions Runner Controller Kubernetes pods) for continuous integration and deployment.

- **Testing:** Every push and pull request to the `main` branch automatically triggers the `xUnit` and `FluentAssertions` test suite, alongside strict license validation.
- **Releases:** Creating a new GitHub Release is fully automated. Pushing a semantic version tag (e.g., `v1.0.4`) to the `main` branch triggers the release workflow. This workflow compiles the standalone binaries, generates a `ThirdPartyNotices.txt` file using the custom internal generator, and publishes the artifacts directly to the GitHub Releases page.

## 📜 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
