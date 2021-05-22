# TeamsBot Data Converter Tool

This command-line tool is meant to process Teams Bot data stores. Raw stores recorded by a Bot based on \psi contain the following streams:

* `BotAudio` - Bot output audio (`AudioBuffer`)
* `BotVideo` - Bot output video (`Shared<Image>`)
* `BotScreen` - Bot shared screen output (`Shared<Image>`)
* `ParticipantAudio` - Per-participant audio (`Dictionary<string, AudioBuffer>`)
* `ParticipantVideo` - Per-participant video (`Dictionary<string, Shared<Image>>`)
* `Diagnostics` - System diagnostics (`PipelineDiagnostics`)

Notice that image streams and images within dictionary streams are unencoded `Shared<Image>`. This allows for high throughput while the bot is running live, however the size of the stores produced are *very* large! Copying these from the WorkerRole instance in Azure is quite unwieldy.

## Encoding Image Streams

This tool allows for encoding image streams to compress the datastore.

## Verbs and Options

The following verbs are supported:

| Verb       | Description                                     |
| ---------- | ----------------------------------------------- |
| `split`    | Split dictionary streams.                       |
| `encode`   | Encode image streams.                           |
| `help`     | Display more information on a specific command. |
| `version`  | Display version information.                    |

The following options are available:

| Option | Abbr         | Description                                                                 |
| ------ | ------------ | --------------------------------------------------------------------------- |
| `p`    | `path`       | File path to Psi data store (default=working directory).                    |
| `d`    | `data`       | Name of Psi data store(s).                                                  |
| `o`    | `output`     | Optional name of concatenated output Psi data store (default=Concatenated). |
| `q`    | `quality`    | Optional quality of JPEG compression 0-100 (optional, default 90).          |

## Splitting

To split dictionary streams in a store:

```bash
> dotnet TeamsBotDataConverter.dll split -p E:\Data -d MyStore -o MyOutput
```

This will split any streams of `Dictionary<string, Shared<Image>)` into per-ID (`string`) streams of `Shared<Image>`, making them easier to visualize individually in PsiStudio.

## Encoding

To encode image streams in a store:

```bash
> dotnet TeamsBotDataConverter.dll encode -p E:\Data -d MyStore -o MyOutput -q 80
```

This will encode any streams of `Shared<Image>` or of `Dictionary<string, Shared<Image>)`, reducing the size of the store significantly.
