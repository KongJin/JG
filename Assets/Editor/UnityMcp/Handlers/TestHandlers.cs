#if UNITY_EDITOR
#pragma warning disable CS0649
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class TestHandlers
    {
        static TestHandlers()
        {
            "POST".Register("/tests/editmode/run", "Run EditMode tests inside the open Unity Editor", async (req, res) => await HandleRunEditModeAsync(req, res));
        }

        private static async Task HandleRunEditModeAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var parsed = string.IsNullOrWhiteSpace(body)
                ? new EditModeTestRunRequest()
                : JsonUtility.FromJson<EditModeTestRunRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() => RunEditModeTests(parsed));
            await UnityMcpBridge.WriteJsonAsync(response, result.success ? 200 : 500, result);
        }

        private static EditModeTestRunResponse RunEditModeTests(EditModeTestRunRequest request)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return EditModeTestRunResponse.Blocked("play-mode-active", "EditMode test execution requires the editor to stay in Edit Mode.");
            }

            if (EditorApplication.isCompiling)
            {
                return EditModeTestRunResponse.Blocked("editor-compiling", "Unity is compiling. Wait for compile to settle before running tests.");
            }

            var timeoutMs = request != null && request.timeoutMs > 0 ? request.timeoutMs : 120000;
            var outputPath = NormalizeOutputPath(request != null ? request.outputPath : null);
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                groupNames = NormalizeFilterList(request != null ? request.groupNames : null),
                testNames = NormalizeFilterList(request != null ? request.testNames : null),
                assemblyNames = NormalizeFilterList(request != null ? request.assemblyNames : null),
                categoryNames = NormalizeFilterList(request != null ? request.categoryNames : null)
            };

            var settings = new ExecutionSettings(filter)
            {
                runSynchronously = request == null || request.runSynchronously
            };

            var callback = new EditModeTestRunCallbacks(outputPath);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var startedUtc = DateTime.UtcNow;
            var runId = string.Empty;

            try
            {
                api.RegisterCallbacks(callback, int.MaxValue);
                runId = api.Execute(settings);

                var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                while (!callback.IsFinished && DateTime.UtcNow < deadline)
                {
                    UnityMcpBridge.DrainMainThreadActionsForBatchmode();
                    Thread.Sleep(50);
                }

                if (!callback.IsFinished)
                {
                    if (!string.IsNullOrWhiteSpace(runId))
                    {
                        TestRunnerApi.CancelTestRun(runId);
                    }

                    return EditModeTestRunResponse.Blocked("test-run-timeout", "EditMode test run did not finish before timeout.")
                        .WithRun(runId, startedUtc, DateTime.UtcNow, outputPath, callback);
                }

                return EditModeTestRunResponse.FromCallback(runId, startedUtc, DateTime.UtcNow, outputPath, callback);
            }
            catch (Exception ex)
            {
                return EditModeTestRunResponse.Failed("test-run-exception", ex.Message)
                    .WithRun(runId, startedUtc, DateTime.UtcNow, outputPath, callback);
            }
            finally
            {
                api.UnregisterCallbacks(callback);
                UnityEngine.Object.DestroyImmediate(api);
            }
        }

        private static string NormalizeOutputPath(string outputPath)
        {
            var relativePath = string.IsNullOrWhiteSpace(outputPath)
                ? "artifacts/unity/editmode-direct-tests.xml"
                : outputPath.Replace('\\', '/');

            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            return Path.GetFullPath(Path.Combine(UnityMcpBridge.ProjectRootPath, relativePath));
        }

        private static string[] NormalizeFilterList(string[] values)
        {
            if (values == null)
            {
                return null;
            }

            var normalized = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToArray();
            return normalized.Length == 0 ? null : normalized;
        }

        private sealed class EditModeTestRunCallbacks : ICallbacks
        {
            private readonly string _outputPath;
            private readonly List<EditModeTestCaseResult> _failedTests = new List<EditModeTestCaseResult>();

            public EditModeTestRunCallbacks(string outputPath)
            {
                _outputPath = outputPath;
            }

            public bool IsFinished { get; private set; }
            public int TestCount { get; private set; }
            public int PassedCount { get; private set; }
            public int FailedCount { get; private set; }
            public int SkippedCount { get; private set; }
            public string ResultState { get; private set; }
            public string Message { get; private set; }
            public EditModeTestCaseResult[] FailedTests => _failedTests.ToArray();

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                ResultState = result.TestStatus.ToString();
                Message = result.Message ?? string.Empty;
                var directory = Path.GetDirectoryName(_outputPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                TestRunnerApi.SaveResultToFile(result, _outputPath);
                IsFinished = true;
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test == null || result.Test.IsSuite)
                {
                    return;
                }

                TestCount++;
                if (result.TestStatus == TestStatus.Passed)
                {
                    PassedCount++;
                    return;
                }

                if (result.TestStatus == TestStatus.Skipped)
                {
                    SkippedCount++;
                    return;
                }

                FailedCount++;
                _failedTests.Add(new EditModeTestCaseResult
                {
                    name = result.Test.FullName,
                    status = result.TestStatus.ToString(),
                    message = result.Message ?? string.Empty,
                    stackTrace = result.StackTrace ?? string.Empty
                });
            }
        }

        [Serializable]
        private sealed class EditModeTestRunRequest
        {
            public string[] groupNames;
            public string[] testNames;
            public string[] assemblyNames;
            public string[] categoryNames;
            public string outputPath;
            public int timeoutMs;
            public bool runSynchronously = true;
        }

        [Serializable]
        private sealed class EditModeTestCaseResult
        {
            public string name;
            public string status;
            public string message;
            public string stackTrace;
        }

        [Serializable]
        private sealed class EditModeTestRunResponse
        {
            public bool success;
            public string terminalVerdict;
            public string blockedReason;
            public string message;
            public string runId;
            public string startedUtc;
            public string finishedUtc;
            public int testCount;
            public int passedCount;
            public int failedCount;
            public int skippedCount;
            public string resultState;
            public string outputPath;
            public EditModeTestCaseResult[] failedTests;

            public static EditModeTestRunResponse Blocked(string reason, string message)
            {
                return new EditModeTestRunResponse
                {
                    success = false,
                    terminalVerdict = "blocked",
                    blockedReason = reason,
                    message = message,
                    failedTests = Array.Empty<EditModeTestCaseResult>()
                };
            }

            public static EditModeTestRunResponse Failed(string reason, string message)
            {
                return new EditModeTestRunResponse
                {
                    success = false,
                    terminalVerdict = "mismatch",
                    blockedReason = reason,
                    message = message,
                    failedTests = Array.Empty<EditModeTestCaseResult>()
                };
            }

            public static EditModeTestRunResponse FromCallback(
                string runId,
                DateTime startedUtc,
                DateTime finishedUtc,
                string outputPath,
                EditModeTestRunCallbacks callback)
            {
                var success = callback.FailedCount == 0;
                return new EditModeTestRunResponse
                {
                    success = success,
                    terminalVerdict = success ? "success" : "mismatch",
                    blockedReason = string.Empty,
                    message = callback.Message ?? string.Empty,
                    runId = runId,
                    startedUtc = startedUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    finishedUtc = finishedUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    testCount = callback.TestCount,
                    passedCount = callback.PassedCount,
                    failedCount = callback.FailedCount,
                    skippedCount = callback.SkippedCount,
                    resultState = callback.ResultState ?? string.Empty,
                    outputPath = outputPath,
                    failedTests = callback.FailedTests
                };
            }

            public EditModeTestRunResponse WithRun(
                string runId,
                DateTime startedUtc,
                DateTime finishedUtc,
                string outputPath,
                EditModeTestRunCallbacks callback)
            {
                this.runId = runId;
                this.startedUtc = startedUtc.ToString("yyyy-MM-dd HH:mm:ss.fff");
                this.finishedUtc = finishedUtc.ToString("yyyy-MM-dd HH:mm:ss.fff");
                this.outputPath = outputPath;
                testCount = callback.TestCount;
                passedCount = callback.PassedCount;
                failedCount = callback.FailedCount;
                skippedCount = callback.SkippedCount;
                resultState = callback.ResultState ?? string.Empty;
                failedTests = callback.FailedTests;
                return this;
            }
        }
    }
}
#pragma warning restore CS0649
#endif
