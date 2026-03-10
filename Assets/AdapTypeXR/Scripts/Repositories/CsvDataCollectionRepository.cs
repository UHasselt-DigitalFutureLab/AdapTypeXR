#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdapTypeXR.Core.Interfaces;
using AdapTypeXR.Core.Models;
using UnityEngine;

namespace AdapTypeXR.Repositories
{
    /// <summary>
    /// Persists reading session data to CSV files on disk.
    ///
    /// Design decisions:
    /// - Gaze samples are buffered in a <see cref="ConcurrentQueue{T}"/> and
    ///   flushed to disk on a background thread to avoid blocking the 200 Hz
    ///   sampling loop on the Unity main thread.
    /// - One CSV file per session for gaze data; a separate file for metrics.
    /// - Session metadata is written to a JSON sidecar file.
    ///
    /// File layout:
    ///   {dataRoot}/{sessionId}/gaze.csv
    ///   {dataRoot}/{sessionId}/metrics.csv
    ///   {dataRoot}/{sessionId}/session.json
    /// </summary>
    public sealed class CsvDataCollectionRepository : IDataCollectionRepository, IDisposable
    {
        // ── Configuration ──────────────────────────────────────────────────

        private readonly string _dataRoot;

        // ── State ──────────────────────────────────────────────────────────

        private ReadingSession? _activeSession;
        private string? _sessionDirectory;
        private StreamWriter? _gazeWriter;
        private StreamWriter? _metricsWriter;
        private readonly ConcurrentQueue<GazeDataPoint> _gazeQueue = new();
        private CancellationTokenSource? _flushCts;
        private Task? _flushTask;

        // ── Constructor ────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the repository.
        /// </summary>
        /// <param name="dataRoot">
        /// Root directory for session data. Defaults to
        /// <c>Application.persistentDataPath/Sessions</c> if null.
        /// </param>
        public CsvDataCollectionRepository(string? dataRoot = null)
        {
            _dataRoot = dataRoot ?? Path.Combine(Application.persistentDataPath, "Sessions");
            Directory.CreateDirectory(_dataRoot);
        }

        // ── IDataCollectionRepository ──────────────────────────────────────

        /// <inheritdoc />
        public Task BeginSessionAsync(ReadingSession session)
        {
            if (_activeSession != null)
                throw new InvalidOperationException(
                    "Cannot begin a new session while one is active. Call EndSessionAsync first.");

            _activeSession = session;
            _sessionDirectory = Path.Combine(_dataRoot, session.SessionId);
            Directory.CreateDirectory(_sessionDirectory);

            // Open writers.
            _gazeWriter = new StreamWriter(
                Path.Combine(_sessionDirectory, "gaze.csv"), append: false, Encoding.UTF8);
            _metricsWriter = new StreamWriter(
                Path.Combine(_sessionDirectory, "metrics.csv"), append: false, Encoding.UTF8);

            WriteGazeHeader();
            WriteMetricsHeader();
            WriteSessionMetadata(session);

            // Start background flush loop.
            _flushCts = new CancellationTokenSource();
            _flushTask = FlushLoopAsync(_flushCts.Token);

            Debug.Log($"[CsvDataCollectionRepository] Session started: {session.SessionId} → {_sessionDirectory}");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RecordGazePointAsync(GazeDataPoint point)
        {
            EnsureSessionActive();
            _gazeQueue.Enqueue(point);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task RecordReadingMetricsAsync(ReadingMetrics metrics)
        {
            EnsureSessionActive();
            await Task.Run(() => WriteMetricsRow(metrics));
        }

        /// <inheritdoc />
        public async Task EndSessionAsync()
        {
            if (_activeSession == null) return;

            // Signal flush loop to stop and wait for it to drain the queue.
            _flushCts?.Cancel();
            if (_flushTask != null)
                await _flushTask.ConfigureAwait(false);

            // Flush any remaining items.
            DrainGazeQueue();

            _gazeWriter?.Dispose();
            _metricsWriter?.Dispose();
            _gazeWriter = null;
            _metricsWriter = null;

            Debug.Log($"[CsvDataCollectionRepository] Session ended: {_activeSession.SessionId}");
            _activeSession = null;
            _sessionDirectory = null;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<ReadingSession>> GetAllSessionsAsync()
        {
            // Stub — full offline analysis tool is out of scope for Sprint 0.
            Debug.LogWarning("[CsvDataCollectionRepository] GetAllSessionsAsync is not yet implemented.");
            return Task.FromResult<IReadOnlyList<ReadingSession>>(Array.Empty<ReadingSession>());
        }

        // ── IDisposable ────────────────────────────────────────────────────

        public void Dispose()
        {
            _flushCts?.Cancel();
            _gazeWriter?.Dispose();
            _metricsWriter?.Dispose();
            _flushCts?.Dispose();
        }

        // ── Private Helpers ────────────────────────────────────────────────

        private void WriteGazeHeader()
        {
            _gazeWriter!.WriteLine(
                "Timestamp,SessionId,ConditionId," +
                "GazeOriginX,GazeOriginY,GazeOriginZ," +
                "GazeDirectionX,GazeDirectionY,GazeDirectionZ," +
                "HitPointX,HitPointY,HitPointZ,HitObjectId," +
                "LeftPupilMm,RightPupilMm,LeftOpenness,RightOpenness,IsBlink");
        }

        private void WriteGazeRow(GazeDataPoint p)
        {
            var hp = p.HitPoint;
            _gazeWriter!.WriteLine(
                $"{p.Timestamp:O},{p.SessionId},{p.ConditionId}," +
                $"{p.GazeOrigin.x:F4},{p.GazeOrigin.y:F4},{p.GazeOrigin.z:F4}," +
                $"{p.GazeDirection.x:F4},{p.GazeDirection.y:F4},{p.GazeDirection.z:F4}," +
                $"{(hp.HasValue ? hp.Value.x.ToString("F4") : "")}," +
                $"{(hp.HasValue ? hp.Value.y.ToString("F4") : "")}," +
                $"{(hp.HasValue ? hp.Value.z.ToString("F4") : "")}," +
                $"{p.HitObjectId ?? ""}," +
                $"{p.LeftPupilDiameterMm:F3},{p.RightPupilDiameterMm:F3}," +
                $"{p.LeftEyeOpenness:F2},{p.RightEyeOpenness:F2},{p.IsBlink}");
        }

        private void WriteMetricsHeader()
        {
            _metricsWriter!.WriteLine(
                "SessionId,PassageId,ConditionId,ReadingStartedAt,ReadingEndedAt," +
                "FixationCount,MeanFixationMs,RegressionCount,RegressionRate," +
                "MeanSaccadeAmpDeg,MeanPupilMm,PeakPupilMm,BlinkCount,BlinkRatePerMin," +
                "GrossWpm,NetWpm,ComprehensionScore,MaxComprehensionScore,ComprehensionRate," +
                "PassageWordCount,FleschKincaidGrade");
        }

        private void WriteMetricsRow(ReadingMetrics m)
        {
            _metricsWriter!.WriteLine(
                $"{m.SessionId},{m.PassageId},{m.ConditionId}," +
                $"{m.ReadingStartedAt:O},{m.ReadingEndedAt:O}," +
                $"{m.FixationCount},{m.MeanFixationDurationMs:F1}," +
                $"{m.RegressionCount},{m.RegressionRate:F3}," +
                $"{m.MeanSaccadeAmplitudeDeg:F2}," +
                $"{m.MeanPupilDiameterMm:F3},{m.PeakPupilDiameterMm:F3}," +
                $"{m.BlinkCount},{m.BlinkRatePerMinute:F1}," +
                $"{m.GrossReadingSpeedWpm:F1},{m.NetReadingSpeedWpm:F1}," +
                $"{m.ComprehensionScore:F1},{m.MaxComprehensionScore:F1},{m.ComprehensionRate:F3}," +
                $"{m.PassageWordCount},{m.FleschKincaidGradeLevel:F1}");
            _metricsWriter.Flush();
        }

        private void WriteSessionMetadata(ReadingSession session)
        {
            var json = $@"{{
  ""sessionId"": ""{session.SessionId}"",
  ""participantId"": ""{session.ParticipantId}"",
  ""startedAt"": ""{session.StartedAt:O}"",
  ""profile"": ""{session.Profile}"",
  ""conditionOrder"": [{string.Join(",", System.Linq.Enumerable.Select(session.ConditionOrder, c => $"\"{c}\""))}],
  ""ipd_mm"": {session.InterPupillaryDistanceMm:F1},
  ""gazeRecordingConsented"": {session.GazeRecordingConsented.ToString().ToLower()},
  ""physiologicalDataConsented"": {session.PhysiologicalDataConsented.ToString().ToLower()},
  ""appVersion"": ""{session.AppVersion}""
}}";
            File.WriteAllText(Path.Combine(_sessionDirectory!, "session.json"), json);
        }

        private async Task FlushLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                DrainGazeQueue();
                _gazeWriter?.Flush();
                try { await Task.Delay(100, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        private void DrainGazeQueue()
        {
            while (_gazeQueue.TryDequeue(out var point))
                WriteGazeRow(point);
        }

        private void EnsureSessionActive()
        {
            if (_activeSession == null)
                throw new InvalidOperationException(
                    "No active session. Call BeginSessionAsync before recording data.");
        }
    }
}
