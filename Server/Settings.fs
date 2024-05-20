namespace ImaginaryAlchemy

open System.IO
open Microsoft.Extensions.Configuration

/// Server-side settings.
type Settings =
    {
        /// OpenAI API key. Don't share this!
        ApiKey : string
    }

module Settings =

    /// Gets settings from the given directory.
    let get dir =
        let path = Path.Combine(dir, "appsettings.json")
        ConfigurationBuilder()
            .AddJsonFile(path)
            .Build()
            .Get<Settings>()
