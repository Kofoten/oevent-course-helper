using OEventCourseHelper.Core.CoursePrioritizer;
using OEventCourseHelper.Core.CoursePrioritizer.IO;
using OEventCourseHelper.Core.CoursePrioritizer.Solver;
using OEventCourseHelper.Core.Data;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;

byte[] seed = [123, 89, 244, 187, 31, 210, 174, 50];
var watch = new Stopwatch();
var beamWidth = 20;
var courseCount = 5_000;
var controlCount = 100_032;

if (args.Length > 0 && int.TryParse(args[0], out var beamWidthArgValue))
{
    beamWidth = beamWidthArgValue;
}

if (args.Length > 1 && int.TryParse(args[1], out var courseCountArgValue))
{
    courseCount = courseCountArgValue;
}

if (args.Length > 2 && int.TryParse(args[2], out var controlCountArgValue))
{
    controlCount = controlCountArgValue;
}

var controlBucketCount = BitMask.GetBucketCount(controlCount);
var remainder = controlCount % 64;
var lastBucketMask = ulong.MaxValue;
if (remainder > 0)
{
    lastBucketMask >>= 64 - remainder;
}

Console.WriteLine("Generating data set...");
watch.Start();

var controlCodes = Enumerable.Range(0, controlCount)
    .Select(i => (31 + i).ToString())
    .ToImmutableArray();

ImmutableArray<Course> courses;
using (var hmac = new HMACSHA256(seed))
{
    byte[] indexBytes = new byte[8];
    courses = [.. Enumerable.Range(0, courseCount)
        .Select(i =>
        {
            var courseControls = new ulong[controlBucketCount];
            for (var j = 0; j < controlBucketCount; j++)
            {
                var coordinate = ((ulong)i << 32) | (uint)j;
                BinaryPrimitives.WriteUInt64LittleEndian(indexBytes, coordinate);
                var bytes = hmac.ComputeHash(indexBytes);
                var bucket = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
                courseControls[j] = bucket;
            }

            courseControls[controlBucketCount - 1] &= lastBucketMask;

            var mask = new BitMask([.. courseControls]);
            return new Course(i, $"Course {i}", mask, mask.PopCount);
        })];
}

watch.Stop();
Console.WriteLine($"Generated data set with {controlCount} controls and {courseCount} courses in {watch.Elapsed}");

var dataSet = new EventDataSet(controlCodes, courses);
var solver = new BeamSearchSolver(beamWidth);

Console.WriteLine("Processing data set...");
watch.Restart();

solver.Solve(dataSet);

watch.Stop();
Console.WriteLine($"Processed the generated data set using a beam width of {beamWidth} in {watch.Elapsed}");
