/***************************************************************

•   File: PathCombiner.cs

•   Description

    The PathCombiner class provides static methods for combining,
    splitting, and recombining path-like strings using specified
    separators. It supports converting paths between different
    separator styles and ensures segments are cleanly combined
    without redundant separator characters.

•   Copyright

    © Pavel Bashkardin, 2026

***************************************************************/

using System;
using System.Collections.Generic;

/// <summary>
/// Static utility class for combining and recombining path-like strings using specified separators.
/// </summary>
public static class PathCombiner
{
    /// <summary>
    /// Combines multiple path segments into a single path string using the specified separator.
    /// </summary>
    /// <param name="separator">The character used to separate path segments.</param>
    /// <param name="segments">Path segments to combine.</param>
    /// <returns>Combined path string.</returns>
    public static string Combine(char separator, params string[] segments)
    {
        if (segments == null || segments.Length == 0)
            return string.Empty;

        List<string> cleanedSegments = new List<string>();
        foreach (string segment in segments)
        {
            if (!string.IsNullOrEmpty(segment))
            {
                // Remove any spaces from the start/end of the segment
                string cleaned = segment.Trim();
                if (!string.IsNullOrEmpty(cleaned))
                    cleanedSegments.Add(cleaned);
            }
        }

        return string.Join(separator.ToString(), cleanedSegments);
    }

    /// <summary>
    /// Splits a combined path string into individual segments using the specified separator.
    /// </summary>
    /// <param name="path">The combined path string.</param>
    /// <param name="separator">The separator to use for splitting.</param>
    /// <returns>Array of path segments.</returns>
    public static string[] Split(string path, char separator)
    {
        if (string.IsNullOrEmpty(path))
            return new string[0];

        return path.Split(new char[] { separator }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Recombines a path string from one separator to another.
    /// </summary>
    /// <param name="path">The original path string.</param>
    /// <param name="oldSeparator">The separator used in the original path.</param>
    /// <param name="newSeparator">The separator to use in the recombined path.</param>
    /// <returns>Recombined path string.</returns>
    public static string Recombine(string path, char oldSeparator, char newSeparator)
    {
        string[] segments = Split(path, oldSeparator);
        return string.Join(newSeparator.ToString(), segments);
    }
}
