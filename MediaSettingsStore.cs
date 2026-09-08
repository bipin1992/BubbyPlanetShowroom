using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BubbyPlanetShowroom
{
    public sealed class MediaPlayerSettings
    {
        public string FolderPath { get; set; } = "";
        public int IntervalSeconds { get; set; } = 11;
        public bool Loop { get; set; } = true;
        public List<string> SelectedFiles { get; set; } = new();
    }

    public static class MediaSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// C:\Users\{this PC user}\Documents\MediaBubbyplanet
        /// </summary>
        public static string ShowroomMediaFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents",
                "MediaBubbyplanet");

        public static string DefaultFolderPath
        {
            get
            {
                try
                {
                    Directory.CreateDirectory(ShowroomMediaFolder);
                }
                catch
                {
                    // Folder may already exist or will be created later.
                }

                return ShowroomMediaFolder;
            }
        }

        public static string FilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BubbyPlanetShowroom");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "media_settings.json");
            }
        }

        public static MediaPlayerSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    MediaPlayerSettings? loaded = JsonSerializer.Deserialize<MediaPlayerSettings>(json);
                    if (loaded != null)
                    {
                        if (loaded.IntervalSeconds < 0)
                            loaded.IntervalSeconds = 0;
                        if (loaded.IntervalSeconds > 3600)
                            loaded.IntervalSeconds = 3600;
                        loaded.SelectedFiles ??= new List<string>();
                        loaded.FolderPath = DefaultFolderPath;
                        return loaded;
                    }
                }
            }
            catch
            {
                // Fall through to defaults.
            }

            return new MediaPlayerSettings
            {
                FolderPath = DefaultFolderPath,
                IntervalSeconds = 11,
                Loop = true
            };
        }

        public static void Save(MediaPlayerSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
    }
}
