using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class MediaLibraryTests
    {
        [Theory]
        [InlineData("song.mp3", true)]
        [InlineData("clip.MP4", true)]
        [InlineData("note.wav", true)]
        [InlineData("promo.wmv", true)]
        [InlineData("readme.txt", false)]
        [InlineData("photo.jpg", false)]
        [InlineData("sheet.xlsx", false)]
        public void IsMedia_FiltersAudioAndVideoOnly(string fileName, bool expected)
        {
            Assert.Equal(expected, MediaLibrary.IsMedia(fileName));
        }

        [Fact]
        public void KindOf_ClassifiesAudioAndVideo()
        {
            Assert.Equal("Audio", MediaLibrary.KindOf(@"D:\ads\jingle.mp3"));
            Assert.Equal("Video", MediaLibrary.KindOf(@"D:\ads\promo.mp4"));
            Assert.Equal("", MediaLibrary.KindOf("notes.pdf"));
        }

        [Fact]
        public void ScanFolder_ReturnsOnlyMediaFilesSortedByName()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bubby-media-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, "zebra.mp3"), "a");
                File.WriteAllText(Path.Combine(dir, "alpha.mp4"), "v");
                File.WriteAllText(Path.Combine(dir, "ignore.txt"), "x");
                File.WriteAllText(Path.Combine(dir, "photo.png"), "p");

                List<MediaFileEntry> files = MediaLibrary.ScanFolder(dir);

                Assert.Equal(2, files.Count);
                Assert.Equal("alpha.mp4", files[0].FileName);
                Assert.Equal("Video", files[0].Kind);
                Assert.Equal("zebra.mp3", files[1].FileName);
                Assert.Equal("Audio", files[1].Kind);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void ScanFolder_MissingOrEmpty_ReturnsEmpty()
        {
            Assert.Empty(MediaLibrary.ScanFolder(null));
            Assert.Empty(MediaLibrary.ScanFolder(@"C:\this-folder-should-not-exist-bubby-media"));
        }

        [Fact]
        public void NextPath_WalksSelectionThenLoops()
        {
            string[] selected = { @"D:\a.mp3", @"D:\b.mp4", @"D:\c.wav" };

            Assert.Equal(@"D:\a.mp3", MediaLibrary.NextPath(selected, null, loop: true));
            Assert.Equal(@"D:\b.mp4", MediaLibrary.NextPath(selected, @"D:\a.mp3", loop: true));
            Assert.Equal(@"D:\c.wav", MediaLibrary.NextPath(selected, @"D:\b.mp4", loop: true));
            Assert.Equal(@"D:\a.mp3", MediaLibrary.NextPath(selected, @"D:\c.wav", loop: true));
            Assert.Null(MediaLibrary.NextPath(selected, @"D:\c.wav", loop: false));
        }

        [Fact]
        public void NextPath_UnknownCurrent_StartsAtFirst()
        {
            string[] selected = { @"D:\a.mp3", @"D:\b.mp4" };
            Assert.Equal(@"D:\a.mp3", MediaLibrary.NextPath(selected, @"D:\gone.mp3", loop: true));
        }

        [Fact]
        public void NextPath_AfterLast_GoesToFirstSelected()
        {
            string[] selected = { @"D:\one.mp3", @"D:\two.mp4", @"D:\three.wav" };
            Assert.Equal(@"D:\one.mp3", MediaLibrary.NextPath(selected, @"D:\three.wav", loop: true));
        }

        [Fact]
        public void NextPath_UsesLiveSelection_WhenCurrentWasUnchecked()
        {
            string[] remaining = { @"D:\two.mp4", @"D:\four.mp3" };
            Assert.Equal(@"D:\two.mp4", MediaLibrary.NextPath(remaining, @"D:\one.mp3", loop: true));
        }

        [Theory]
        [InlineData(@"C:\Media\bubby-reward-system.mp3", true)]
        [InlineData(@"C:\Media\bubby-15-percent-hindi.mp3", true)]
        [InlineData(@"C:\Media\bubby-15-percent-kid.mp3", true)]
        [InlineData(@"C:\Media\bubby-sale-40-percent.mp3", true)]
        [InlineData(@"C:\Media\shape-of-you.mp3", false)]
        [InlineData(@"C:\Media\kids-party-song.mp4", false)]
        public void IsAnnouncement_DetectsPromoFiles(string path, bool expected)
        {
            Assert.Equal(expected, MediaLibrary.IsAnnouncement(path));
        }

        [Fact]
        public void SongsAndAnnouncements_SplitSelection()
        {
            string[] selected =
            {
                @"C:\Media\song.mp3",
                @"C:\Media\bubby-reward-system.mp3",
                @"C:\Media\bubby-15-percent-hindi.mp3"
            };

            Assert.Equal(new[] { @"C:\Media\song.mp3" }, MediaLibrary.Songs(selected));
            Assert.Equal(2, MediaLibrary.Announcements(selected).Count);
        }

        [Theory]
        [InlineData(500, "500 B")]
        [InlineData(2048, "2 KB")]
        [InlineData(1_572_864, "1.5 MB")]
        public void FormatSize_UsesReadableUnits(long bytes, string expected)
        {
            Assert.Equal(expected, MediaLibrary.FormatSize(bytes));
        }
    }
}
