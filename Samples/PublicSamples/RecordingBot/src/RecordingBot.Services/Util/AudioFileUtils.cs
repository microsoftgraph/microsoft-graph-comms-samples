using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using RecordingBot.Model.Constants;
using RecordingBot.Services.ServiceSetup;
using System.IO;

namespace RecordingBot.Services.Util
{
    public static class AudioFileUtils
    {
        private const string STEREO = "stereo";

        public static string CreateFilePath(string rootFolder, string fileName)
        {
            var path = Path.Combine(rootFolder, fileName);
            var fInfo = new FileInfo(path);
            if (fInfo.Directory != null && !fInfo.Directory.Exists)
            {
                fInfo.Directory.Create();
            }

            return path;
        }

        public static string ConvertToStereo(string monoFilePath)
        {
            var outputFilePath = monoFilePath[..^4] + monoFilePath[^4..].Replace(".wav", $"-{STEREO}.wav");
            using (var inputReader = new AudioFileReader(monoFilePath))
            {
                // convert our mono ISampleProvider to stereo
                var stereo = new MonoToStereoSampleProvider(inputReader);

                // write the stereo audio out to a WAV file
                WaveFileWriter.CreateWaveFile16(outputFilePath, stereo);
            }

            return outputFilePath;
        }

        public static string ResampleAudio(string audioFilePath, WAVSettings resamplerSettings, bool convertToStereo)
        {
            var stereoFlag = (convertToStereo)? $"-{STEREO}" : "";
            var outFile = audioFilePath[..^4] + audioFilePath[^4..].Replace(".wav", $"-{resamplerSettings.SampleRate / 1000}kHz{stereoFlag}.wav");
            using (var reader = new WaveFileReader(audioFilePath))
            {
                var outFormat = new WaveFormat((int)resamplerSettings.SampleRate, (convertToStereo)? 2 : 1);

                using var resampler = new MediaFoundationResampler(reader, outFormat);
                resampler.ResamplerQuality = resamplerSettings.Quality * AudioConstants.HighestSamplingQualityLevel / 100
                                             ?? AudioConstants.HighestSamplingQualityLevel;
                WaveFileWriter.CreateWaveFile(outFile, resampler);
            }

            return outFile;
        }
    }
}
