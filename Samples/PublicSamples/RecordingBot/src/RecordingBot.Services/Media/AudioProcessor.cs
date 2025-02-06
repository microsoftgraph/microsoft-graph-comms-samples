using NAudio.Wave;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Services.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace RecordingBot.Services.Media
{
    public class AudioProcessor : BufferBase<SerializableAudioMediaBuffer>
    {
        readonly Dictionary<string, WaveFileWriter> _writers = [];
        private readonly string _processorId = null;
        private readonly AzureSettings _settings;

        public AudioProcessor(IAzureSettings settings)
        {
            _processorId = Guid.NewGuid().ToString();
            _settings = (AzureSettings)settings;
        }

        protected override async Task Process(SerializableAudioMediaBuffer data)
        {
            if (data.Timestamp == 0)
            {
                return;
            }

            var path = Path.Combine(Path.GetTempPath(), BotConstants.DEFAULT_OUTPUT_FOLDER, _settings.MediaFolder, _processorId);

            // First, write all audio buffer, unless the data.IsSilence is checked for true, into the all speakers buffer
            var all = "all";
            var all_writer = _writers.TryGetValue(all, out WaveFileWriter allWaveWriter) ? allWaveWriter : InitialiseWavFileWriter(path, all);

            if (data.Buffer != null)
            {
                // Buffers are saved to disk even when there is silence.
                // If you do not want this to happen, check if data.IsSilence == true.
                await all_writer.WriteAsync(data.Buffer.AsMemory(0, data.Buffer.Length)).ConfigureAwait(false);
            }

            if (data.SerializableUnmixedAudioBuffers != null)
            {
                foreach (var s in data.SerializableUnmixedAudioBuffers)
                {
                    if (string.IsNullOrWhiteSpace(s.AdId) || string.IsNullOrWhiteSpace(s.DisplayName))
                    {
                        continue;
                    }

                    var id = s.AdId;

                    var writer = _writers.TryGetValue(id, out WaveFileWriter bufferWaveWriter) ? bufferWaveWriter : InitialiseWavFileWriter(path, id);

                    // Write audio buffer into the WAV file for individual speaker
                    await writer.WriteAsync(s.Buffer.AsMemory(0, s.Buffer.Length)).ConfigureAwait(false);

                    // Write audio buffer into the WAV file for all speakers
                    await all_writer.WriteAsync(s.Buffer.AsMemory(0, s.Buffer.Length)).ConfigureAwait(false);
                }
            }
        }

        private WaveFileWriter InitialiseWavFileWriter(string rootFolder, string id)
        {
            var path = AudioFileUtils.CreateFilePath(rootFolder, $"{id}.wav");

            // Initialize the Wave Format using the default PCM 16bit 16K supported by Teams audio settings
            var writer = new WaveFileWriter(path, new WaveFormat(
                rate: AudioConstants.DEFAULT_SAMPLE_RATE,
                bits: AudioConstants.DEFAULT_BITS,
                channels: AudioConstants.DEFAULT_CHANNELS));

            _writers.Add(id, writer);

            return writer;
        }

        public async Task<string> Finalize()
        {
            //drain the un-processed buffers on this object
            while (Buffer.Count > 0)
            {
                await Task.Delay(200);
            }

            var archiveFile = Path.Combine(Path.GetTempPath(), BotConstants.DEFAULT_OUTPUT_FOLDER, _settings.MediaFolder, _processorId, $"{Guid.NewGuid()}.zip");

            try
            {
                using var stream = File.OpenWrite(archiveFile);
                using ZipArchive archive = new(stream, ZipArchiveMode.Create);
                // drain all the writers
                foreach (var writer in _writers.Values)
                {
                    List<string> localFiles = [];
                    var localArchive = archive; //protect the closure below
                    var localFileName = writer.Filename;

                    localFiles.Add(writer.Filename);

                    await writer.FlushAsync();

                    writer.Dispose();

                    // Is Resampling and/or mono to stereo conversion required?
                    if (_settings.AudioSettings.WavSettings != null)
                    {
                        // The resampling is required
                        localFiles.Add(AudioFileUtils.ResampleAudio(localFileName, _settings.AudioSettings.WavSettings, _settings.IsStereo));
                    }
                    else if (_settings.IsStereo) // Is Stereo audio required?
                    {
                        // Convert mono WAV to stereo
                        localFiles.Add(AudioFileUtils.ConvertToStereo(localFileName));
                    }

                    // Remove temporary saved local WAV file from the disk
                    foreach (var localFile in localFiles)
                    {
                        await Task.Run(() =>
                        {
                            var fInfo = new FileInfo(localFile);
                            localArchive.CreateEntryFromFile(localFile, fInfo.Name, CompressionLevel.Optimal);
                            File.Delete(localFile);
                        }).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await End();
            }

            return archiveFile;
        }
    }
}
