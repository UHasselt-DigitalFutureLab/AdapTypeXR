#nullable enable
using System;
using System.Collections.Generic;
using AdapTypeXR.Core.Models;
using UnityEngine;

namespace AdapTypeXR.Core.Events
{
    /// <summary>
    /// A typed, synchronous event bus for domain events within AdapTypeXR.
    /// Follows the Observer pattern — producers publish events without
    /// knowledge of consumers. Consumers subscribe to specific event types.
    ///
    /// This is a lightweight alternative to Unity's built-in SendMessage
    /// that preserves type safety and SOLID principles.
    ///
    /// Usage:
    ///   ReadingEventBus.Instance.Subscribe&lt;SessionStartedEvent&gt;(OnSessionStarted);
    ///   ReadingEventBus.Instance.Publish(new SessionStartedEvent(session));
    /// </summary>
    public sealed class ReadingEventBus
    {
        private static ReadingEventBus? _instance;

        /// <summary>Singleton access. Thread-safe for read; initialise on main thread.</summary>
        public static ReadingEventBus Instance => _instance ??= new ReadingEventBus();

        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        private ReadingEventBus() { }

        /// <summary>
        /// Subscribes a handler to events of type <typeparamref name="T"/>.
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : IReadingEvent
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            list.Add(handler);
        }

        /// <summary>
        /// Unsubscribes a previously registered handler.
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : IReadingEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        /// <summary>
        /// Publishes an event to all registered handlers.
        /// Exceptions in individual handlers are caught and logged to avoid
        /// breaking other subscribers.
        /// </summary>
        public void Publish<T>(T evt) where T : IReadingEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;

            // Iterate over a snapshot to allow handlers to unsubscribe during dispatch.
            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                try
                {
                    ((Action<T>)handler)(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ReadingEventBus] Handler for {typeof(T).Name} threw: {ex}");
                }
            }
        }

        /// <summary>Removes all subscribers. Use during scene teardown.</summary>
        public void Reset() => _handlers.Clear();
    }

    // ── Event Marker Interface ─────────────────────────────────────────────────

    /// <summary>Marker interface for all domain events on the ReadingEventBus.</summary>
    public interface IReadingEvent { }

    // ── Domain Events ──────────────────────────────────────────────────────────

    /// <summary>Published when a new reading session is started.</summary>
    public readonly struct SessionStartedEvent : IReadingEvent
    {
        public ReadingSession Session { get; }
        public SessionStartedEvent(ReadingSession session) => Session = session;
    }

    /// <summary>Published when the active reading session ends.</summary>
    public readonly struct SessionEndedEvent : IReadingEvent
    {
        public ReadingSession Session { get; }
        public SessionEndedEvent(ReadingSession session) => Session = session;
    }

    /// <summary>Published when the active typography condition changes.</summary>
    public readonly struct ConditionChangedEvent : IReadingEvent
    {
        public TypographyConfig PreviousConfig { get; }
        public TypographyConfig NewConfig { get; }
        public ConditionChangedEvent(TypographyConfig previous, TypographyConfig next)
        {
            PreviousConfig = previous;
            NewConfig = next;
        }
    }

    /// <summary>Published when the participant turns to a new page.</summary>
    public readonly struct PageTurnedEvent : IReadingEvent
    {
        public int FromPage { get; }
        public int ToPage { get; }
        public PageTurnedEvent(int from, int to) { FromPage = from; ToPage = to; }
    }

    /// <summary>Published when a new gaze sample is available (200 Hz).</summary>
    public readonly struct GazeSampledEvent : IReadingEvent
    {
        public GazeDataPoint DataPoint { get; }
        public GazeSampledEvent(GazeDataPoint dataPoint) => DataPoint = dataPoint;
    }

    /// <summary>Published when a fixation is detected by the gaze processor.</summary>
    public readonly struct FixationDetectedEvent : IReadingEvent
    {
        public UnityEngine.Vector3 FixationPoint { get; }
        public float DurationMs { get; }
        public string? HitObjectId { get; }
        public FixationDetectedEvent(UnityEngine.Vector3 point, float durationMs, string? hitObjectId)
        {
            FixationPoint = point;
            DurationMs = durationMs;
            HitObjectId = hitObjectId;
        }
    }

    /// <summary>Published when a reading passage is completed.</summary>
    public readonly struct PassageCompletedEvent : IReadingEvent
    {
        public ReadingMetrics Metrics { get; }
        public PassageCompletedEvent(ReadingMetrics metrics) => Metrics = metrics;
    }
}
