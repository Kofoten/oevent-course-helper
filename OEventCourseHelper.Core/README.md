# OEventCourseHelper.Core

A high-performance, zero-allocation domain engine for parsing IOF 3.0 XML and solving orienteering course logistics.

This package provides the core algorithmic engine used by the `OEventCourseHelper` tool. It solves a complex set cover problem to determine the most efficient order to test-run courses, ensuring all physical control points are visited with the minimum amount of running.

## Features

* **IOF XML 3.0 Parser:** A highly optimized, streaming `XmlReader` designed to parse IOF 3.0 course data with a minimal memory footprint.
* **Beam Search Solver:** A deterministic, heuristically-guided solver (via a "rarity score") that calculates the optimal set of test-run courses.
* **Strict Immutability & Zero Exceptions:** Designed with strict memory boundaries. Malformed data is caught and returned via a robust Result pattern (`CoursePrioritizerResult`) rather than throwing expensive runtime exceptions.

## Quick Start

```csharp
using OEventCourseHelper.Core.CoursePrioritizer;

// 1. Initialize the engine (e.g., Beam Width: 3, Strict Mode: true)
var engine = new CoursePrioritizerEngine(BeamWidth: 3, Strict: true, Filters: []);

// 2. Pass your IOF XML stream directly into the engine
using var fileStream = File.OpenRead("courses.xml");
var result = engine.Run(fileStream);

// 3. Match on the Result pattern
switch (result)
{
    case CoursePrioritizerResult.Success success:
        foreach (var course in success.PriorityOrder.Where(c => c.IsRequired))
        {
            Console.WriteLine($"Required: {course.CourseName}");
        }
        break;

    case CoursePrioritizerResult.ValidationFailure failure:
        Console.WriteLine($"Missing controls: {string.Join(", ", failure.ValidationInfo.SkippedControls)}");
        break;

    case CoursePrioritizerResult.ParseStreamFailure parseError:
        Console.WriteLine("Invalid IOF 3.0 XML.");
        break;
}
```
