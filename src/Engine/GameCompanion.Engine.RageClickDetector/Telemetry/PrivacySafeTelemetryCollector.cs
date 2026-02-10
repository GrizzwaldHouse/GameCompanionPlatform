namespace GameCompanion.Engine.RageClickDetector.Telemetry;

using System.Security.Cryptography;
using System.Text;
using GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Collects user interaction data using only privacy-safe fields.
/// All identifiers are anonymized or hashed before storage.
/// No user identity, save file contents, input text, IP addresses,
/// or device fingerprinting data is ever captured.
/// </summary>
public sealed class PrivacySafeTelemetryCollector
{
    private readonly List<InteractionRecord> _buffer = [];
    private readonly object _lock = new();
    private readonly int _maxBufferSize;
    private readonly string _anonymizedSessionId;

    public PrivacySafeTelemetryCollector(int maxBufferSize = 1000)
    {
        _maxBufferSize = maxBufferSize;
        _anonymizedSessionId = GenerateAnonymizedSessionId();
    }

    /// <summary>
    /// Records a user interaction with privacy-safe fields only.
    /// </summary>
    public InteractionRecord Record(
        string rawElementId,
        InteractionType interactionType,
        string screenName,
        bool causedStateChange = false,
        bool targetWasDisabled = false,
        bool resultedInValidationError = false,
        bool newGuidanceShown = false,
        NavigationDirection? direction = null)
    {
        var hashedElementId = HashElementId(rawElementId);

        if (!TelemetryPolicy.ValidateElementId(hashedElementId))
            throw new ArgumentException("Element ID exceeds maximum allowed length after hashing.");

        if (!TelemetryPolicy.ValidateScreenName(screenName))
            throw new ArgumentException("Screen name is invalid or exceeds maximum allowed length.");

        var record = new InteractionRecord
        {
            AnonymizedSessionId = _anonymizedSessionId,
            UiElementId = hashedElementId,
            InteractionType = interactionType,
            Timestamp = DateTimeOffset.UtcNow,
            ScreenName = screenName,
            CausedStateChange = causedStateChange,
            TargetWasDisabled = targetWasDisabled,
            ResultedInValidationError = resultedInValidationError,
            NewGuidanceShown = newGuidanceShown,
            Direction = direction
        };

        lock (_lock)
        {
            _buffer.Add(record);

            // Evict oldest entries when buffer is full
            while (_buffer.Count > _maxBufferSize)
            {
                _buffer.RemoveAt(0);
            }
        }

        return record;
    }

    /// <summary>
    /// Returns a snapshot of the current interaction buffer.
    /// </summary>
    public IReadOnlyList<InteractionRecord> GetInteractions()
    {
        lock (_lock)
        {
            return _buffer.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Clears the interaction buffer.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }
    }

    /// <summary>
    /// Gets the current buffer count.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _buffer.Count;
            }
        }
    }

    /// <summary>
    /// Generates an ephemeral, anonymized session ID using a random GUID.
    /// Not linked to any user identity.
    /// </summary>
    private static string GenerateAnonymizedSessionId()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Hashes a UI element identifier using SHA-256 to prevent capturing raw element names
    /// that might contain user-facing text.
    /// </summary>
    internal static string HashElementId(string rawId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawId));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16]; // First 16 hex chars
    }
}
