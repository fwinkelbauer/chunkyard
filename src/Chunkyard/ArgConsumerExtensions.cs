namespace Chunkyard;

public static class ArgConsumerExtensions
{
    public static bool TryRepository(
        this ArgConsumer consumer,
        out string repository)
    {
        return consumer.TryString(
            "--repository",
            "The repository path",
            out repository);
    }

    public static bool TryParallel(
        this ArgConsumer consumer,
        out int parallel)
    {
        return consumer.TryInt(
            "--parallel",
            "The degree of parallelism",
            out parallel,
            Command.DefaultParallel);
    }

    public static bool TrySnapshot(
        this ArgConsumer consumer,
        out int snapshot)
    {
        return consumer.TryInt(
            "--snapshot",
            "The snapshot ID",
            out snapshot,
            SnapshotStore.LatestSnapshotId);
    }

    public static bool TryPrompt(
        this ArgConsumer consumer,
        out Prompt prompt)
    {
        var names = string.Join(", ", Enum.GetNames<Prompt>());

        return consumer.TryEnum(
            "--prompt",
            $"The password prompt method: {names}",
            out prompt,
            Command.DefaultPrompt);
    }

    public static bool TryIncludePatterns(
        this ArgConsumer consumer,
        out IReadOnlyCollection<string> includePatterns)
    {
        return consumer.TryList(
            "--include",
            "The fuzzy patterns for blobs to include",
            out includePatterns);
    }

    public static bool TryPreview(
        this ArgConsumer consumer,
        out bool preview)
    {
        return consumer.TryBool(
            "--preview",
            "Show only a preview",
            out preview);
    }

    public static bool TryChunksOnly(
        this ArgConsumer consumer,
        out bool chunksOnly)
    {
        return consumer.TryBool(
            "--chunks-only",
            "Show chunk IDs",
            out chunksOnly);
    }
}
