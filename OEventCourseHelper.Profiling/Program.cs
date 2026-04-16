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
var courseCount = 5000;
var controlCount = 100_032;
var controlBucketCount = controlCount >> 6;

if (args.Length > 0 && int.TryParse(args[0], out var beamWidthArgValue))
{
    beamWidth = beamWidthArgValue;
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
