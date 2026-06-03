using OEventCourseHelper.Core.CoursePrioritizer;
using OEventCourseHelper.Core.CoursePrioritizer.IO;
using OEventCourseHelper.Core.CoursePrioritizer.Solver;
using OEventCourseHelper.Core.Data;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
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

var courseNamesBuilder = new string[courseCount];
var courseMaskBuilder = new BitMask.Builder(courseCount * controlBucketCount);

ImmutableArray<Course> courses;
using (var hmac = new HMACSHA256(seed))
{
    byte[] indexBytes = new byte[8];
    courses = [.. Enumerable.Range(0, courseCount)
        .Select(i =>
        {
            var courseControlCount = 0;
            var courseOffset = i * controlBucketCount;
            for (var j = 0; j < controlBucketCount; j++)
            {
                var coordinate = ((ulong)i << 32) | (uint)j;
                BinaryPrimitives.WriteUInt64LittleEndian(indexBytes, coordinate);
                var bytes = hmac.ComputeHash(indexBytes);
                var bucket = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
                courseControlCount += BitOperations.PopCount(bucket);
                courseMaskBuilder.OrBucket(new BitMask.BucketMask(courseOffset + j, bucket));
            }

            courseNamesBuilder[i] = $"Course {i}";
            return new Course(i, courseOffset, courseControlCount);
        })];
}

watch.Stop();
Console.WriteLine($"Generated data set with {controlCount} controls and {courseCount} courses in {watch.Elapsed}");

var dataSet = new EventDataSet(
    controlCodes,
    ImmutableCollectionsMarshal.AsImmutableArray(courseNamesBuilder),
    courses,
    courseMaskBuilder.ToBitMask(),
    controlBucketCount);

var solver = new BeamSearchSolver(beamWidth);

Console.WriteLine("Processing data set...");
watch.Restart();

solver.Solve(dataSet);

watch.Stop();
Console.WriteLine($"Processed the generated data set using a beam width of {beamWidth} in {watch.Elapsed}");
