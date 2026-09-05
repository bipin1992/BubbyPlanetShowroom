using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BubbyPlanetShowroom
{
    public sealed class MediaFileEntry
    {
        public string FullPath { get; init; } = "";
        public string FileName { get; init; } = "";
        public string Kind { get; init; } = "";
        public long SizeBytes { get; init; }
    }

    public static class MediaLibrary
    {
        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".wma", ".aac", ".m4a", ".ogg", ".flac", ".aiff"
        };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".wmv", ".mov", ".mkv", ".webm", ".mpg", ".mpeg", ".m4v"
        };

        public static bool IsAudio(string path) =>
            AudioExtensions.Contains(Path.GetExtension(path ?? ""));

        public static bool IsVideo(string path) =>
            VideoExtensions.Contains(Path.GetExtension(path ?? ""));

        public static bool IsMedia(string path) => IsAudio(path) || IsVideo(path);

        public static bool IsAnnouncement(string? path)
        {
            string name = Path.GetFileName(path ?? "").ToLowerInvariant();
            if (name.Length == 0)
                return false;

            return name.Contains("bubby-reward")
                || name.Contains("reward-system")
                || name.Contains("bubby-15")
                || name.Contains("bubby-sale")
                || name.Contains("15-percent")
                || name.Contains("40-percent")
                || name.Contains("percent-discount")
                || name.Contains("percent-hindi")
                || name.Contains("percent-kid")
                || name.Contains("percent-boy")
                || name.Contains("percent-child")
                || name.Contains("percent-clear")
                || name.Contains("ek-free")
                || name.Contains("one-free");
        }

        public static List<string> Announcements(IEnumerable<string> paths) =>
            paths.Where(IsAnnouncement).ToList();

        public static List<string> Songs(IEnumerable<string> paths) =>
            paths.Where(p => !IsAnnouncement(p)).ToList();

        public static string KindOf(string path)
        {
            if (IsVideo(path)) return "Video";
            if (IsAudio(path)) return "Audio";
            return "";
        }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            double kb = bytes / 1024d;
            if (kb < 1024) return $"{kb:0.#} KB";
            double mb = kb / 1024d;
            if (mb < 1024) return $"{mb:0.#} MB";
            return $"{mb / 1024d:0.##} GB";
        }

        public static List<MediaFileEntry> ScanFolder(string? folderPath)
        {
            var list = new List<MediaFileEntry>();
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return list;

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folderPath);
            }
            catch
            {
                return list;
            }

            foreach (string path in files.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
            {
                if (!IsMedia(path))
                    continue;

                long size = 0;
                try { size = new FileInfo(path).Length; }
                catch { /* skip size if locked */ }

                list.Add(new MediaFileEntry
                {
                    FullPath = path,
                    FileName = Path.GetFileName(path),
                    Kind = KindOf(path),
                    SizeBytes = size
                });
            }

            return list;
        }

        /// <summary>
        /// Next selected path after <paramref name="currentPath"/>.
        /// When current is null or not in the list, returns the first item.
        /// </summary>
        public static string? NextPath(IReadOnlyList<string> selected, string? currentPath, bool loop)
        {
            if (selected == null || selected.Count == 0)
                return null;

            if (string.IsNullOrWhiteSpace(currentPath))
                return selected[0];

            int index = -1;
            for (int i = 0; i < selected.Count; i++)
            {
                if (string.Equals(selected[i], currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return selected[0];

            if (index + 1 < selected.Count)
                return selected[index + 1];

            return loop ? selected[0] : null;
        }
    }
}
