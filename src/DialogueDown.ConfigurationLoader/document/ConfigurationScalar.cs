namespace DialogueDown.ConfigurationLoader.Document;

/// <summary>A string, number, boolean, or date read from a configuration file.</summary>
/// <remarks>
/// <c>"best-effort"</c> is a <see cref="string"/>, <c>42</c> a <see cref="long"/>, <c>1.5</c> a
/// <see cref="double"/>, <c>true</c> a <see cref="bool"/>, and <c>1979-05-27</c> a
/// <see cref="DateTimeOffset"/>.
/// </remarks>
internal sealed record ConfigurationScalar(object Value, ConfigurationSourceLocation Location)
    : ConfigurationValue(Location);
