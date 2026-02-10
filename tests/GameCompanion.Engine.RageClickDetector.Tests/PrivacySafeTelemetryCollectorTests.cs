namespace GameCompanion.Engine.RageClickDetector.Tests;

using FluentAssertions;
using GameCompanion.Engine.RageClickDetector.Models;
using GameCompanion.Engine.RageClickDetector.Telemetry;

public class PrivacySafeTelemetryCollectorTests
{
    [Fact]
    public void Record_StoresInteractionWithHashedElementId()
    {
        var collector = new PrivacySafeTelemetryCollector();

        var record = collector.Record("SaveButton", InteractionType.Click, "Settings");

        record.UiElementId.Should().NotBe("SaveButton");
        record.UiElementId.Should().HaveLength(16); // SHA-256 first 16 hex chars
        record.ScreenName.Should().Be("Settings");
        record.InteractionType.Should().Be(InteractionType.Click);
        record.AnonymizedSessionId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Record_ConsistentHashing()
    {
        var hash1 = PrivacySafeTelemetryCollector.HashElementId("SaveButton");
        var hash2 = PrivacySafeTelemetryCollector.HashElementId("SaveButton");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Record_DifferentElementsGetDifferentHashes()
    {
        var hash1 = PrivacySafeTelemetryCollector.HashElementId("SaveButton");
        var hash2 = PrivacySafeTelemetryCollector.HashElementId("CancelButton");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Record_BufferRespectsSizeLimit()
    {
        var collector = new PrivacySafeTelemetryCollector(maxBufferSize: 5);

        for (int i = 0; i < 10; i++)
        {
            collector.Record($"element_{i}", InteractionType.Click, "Screen");
        }

        collector.Count.Should().Be(5);
        var interactions = collector.GetInteractions();
        interactions.Should().HaveCount(5);
    }

    [Fact]
    public void Record_EvictsOldestEntriesWhenFull()
    {
        var collector = new PrivacySafeTelemetryCollector(maxBufferSize: 3);

        collector.Record("elem_0", InteractionType.Click, "Screen");
        collector.Record("elem_1", InteractionType.Click, "Screen");
        collector.Record("elem_2", InteractionType.Click, "Screen");
        collector.Record("elem_3", InteractionType.Click, "Screen");

        var interactions = collector.GetInteractions();
        interactions.Should().HaveCount(3);
        // Oldest (elem_0) should be evicted
        interactions.All(i => i.UiElementId != PrivacySafeTelemetryCollector.HashElementId("elem_0"))
            .Should().BeTrue();
    }

    [Fact]
    public void Clear_EmptiesBuffer()
    {
        var collector = new PrivacySafeTelemetryCollector();
        collector.Record("element", InteractionType.Click, "Screen");
        collector.Count.Should().Be(1);

        collector.Clear();

        collector.Count.Should().Be(0);
        collector.GetInteractions().Should().BeEmpty();
    }

    [Fact]
    public void Record_SameSessionIdForAllInteractions()
    {
        var collector = new PrivacySafeTelemetryCollector();

        var record1 = collector.Record("elem1", InteractionType.Click, "Screen");
        var record2 = collector.Record("elem2", InteractionType.Submit, "Screen");

        record1.AnonymizedSessionId.Should().Be(record2.AnonymizedSessionId);
    }

    [Fact]
    public void Record_InvalidScreenName_Throws()
    {
        var collector = new PrivacySafeTelemetryCollector();

        var act = () => collector.Record("element", InteractionType.Click, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetInteractions_ReturnsSnapshot()
    {
        var collector = new PrivacySafeTelemetryCollector();
        collector.Record("elem1", InteractionType.Click, "Screen");

        var snapshot = collector.GetInteractions();
        collector.Record("elem2", InteractionType.Click, "Screen");

        // Snapshot should not be affected by new additions
        snapshot.Should().HaveCount(1);
        collector.Count.Should().Be(2);
    }
}
